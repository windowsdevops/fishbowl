using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Json.Expressions;
using Microsoft.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Json.Parser;

namespace Tests
{
    [TestClass]
    public class ParserTests
    {
        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Tokenize

        [TestMethod]
        public void TokenizeWhite()
        {
            ExpectOneToken(" ", TokenType.White);
            ExpectOneToken("\t", TokenType.White);
            ExpectOneToken("\r", TokenType.White);
            ExpectOneToken("\n", TokenType.White);
            ExpectOneToken(" \t\r\n", TokenType.White);
        }

        [TestMethod]
        public void TokenizeCurlies()
        {
            ExpectOneToken("{", TokenType.LeftCurly);
            ExpectOneToken("}", TokenType.RightCurly);
        }

        [TestMethod]
        public void TokenizeBrackets()
        {
            ExpectOneToken("[", TokenType.LeftBracket);
            ExpectOneToken("]", TokenType.RightBracket);
        }

        [TestMethod]
        public void TokenizeComma()
        {
            ExpectOneToken(",", TokenType.Comma);
        }

        [TestMethod]
        public void TokenizeColon()
        {
            ExpectOneToken(":", TokenType.Colon);
        }

        [TestMethod]
        public void TokenizeEof()
        {
            ExpectOneToken("\0", TokenType.Eof);
        }

        [TestMethod]
        public void TokenizeLiterals()
        {
            ExpectOneToken("false", TokenType.False);
            ExpectOneToken("true", TokenType.True);
            ExpectOneToken("null", TokenType.Null);
        }

        [TestMethod]
        public void TokenizeString()
        {
            string s = "\"Bart says \\\"Hi\\\"!\"";
            ExpectOneToken(s, TokenType.String, s);
        }

        [TestMethod]
        public void TokenizeStringEscapes()
        {
            string escape = "\"\\{0}\"";
            foreach (char c in new [] { '\\', '/', 'r', 't', 'n', 'b', 'f', '"' })
                ExpectOneToken(string.Format(escape, c), TokenType.String, string.Format(escape, c));
        }

        [TestMethod]
        public void TokenizeStringUnicodeEscapes()
        {
            string escape = "\"\\u{0}\"";
            foreach (string s in new[] { "1234", "5678", "0000", "9999", "abcd", "cdef", "ffff" })
                ExpectOneToken(string.Format(escape, s), TokenType.String, string.Format(escape, s));
        }

        [TestMethod]
        public void TokenizeVarious()
        {
            var res = Tokenize(@"-123.45e67   [ ] { } : , 
                                ""Hi"" true false         null  " + "\0").ToArray();
            var tokenTypes = res.Select(t => t.Type).ToArray();
            var expected = new[] { TokenType.Number, TokenType.White, TokenType.LeftBracket, TokenType.White, TokenType.RightBracket, TokenType.White, TokenType.LeftCurly, TokenType.White, TokenType.RightCurly, TokenType.White, TokenType.Colon, TokenType.White, TokenType.Comma, TokenType.White, TokenType.String, TokenType.White, TokenType.True, TokenType.White, TokenType.False, TokenType.White, TokenType.Null, TokenType.White, TokenType.Eof };
            Assert.IsTrue(res.Select(t => t.Type).SequenceEqual(expected));
        }

        [TestMethod]
        public void TokenizeVariousToString()
        {
            var res = Tokenize(@"-123.45e67   [ ] { } : , 
                                ""Hi"" true false         null  " + "\0").ToArray();
            var tokenTypes = res.Select(t => t.ToString()).Aggregate("", (l, c) => l + " " + c).Trim();
            var expected = "NUM(-123.45e67) WHITE LEFTBRACKET WHITE RIGHTBRACKET WHITE LEFTCURLY WHITE RIGHTCURLY WHITE COLON WHITE COMMA WHITE STRING(\"Hi\") WHITE TRUE WHITE FALSE WHITE NULL WHITE EOF";
            Assert.AreEqual(tokenTypes, expected);
        }

        [TestMethod]
        public void TokenizeInvalidToken()
        {
            EnsureInvalidTokenIsDetected(
                "    x",
                "    ^");

            EnsureInvalidTokenIsDetected(
                "    [1, 2, 3, !]",
                "              ^");
        }

        private void EnsureInvalidTokenIsDetected(string input, string error)
        {
            try
            {
                Tokenize(input).ToArray();
                Assert.Fail();
            }
            catch (ParseException ex)
            {
                Assert.AreEqual(ex.Error, ParseError.InvalidToken);
                Assert.AreEqual(ex.Position, error.IndexOf('^'));
            }
        }

        private void ExpectOneToken(string input, TokenType type)
        {
            ExpectOneToken(input, type, null);
        }

        private void ExpectOneToken(string input, TokenType type, string data)
        {
            var onlyToken = Tokenize(input).First(); // Throws if more than one token exists.
            Assert.AreEqual(onlyToken.Type, type);
            Assert.AreEqual(onlyToken.Data, data);
        }

        private static IEnumerable<Token> Tokenize(string input)
        {
            var t = new Tokenizer(input);
            return t.Tokenize();
        }

        #endregion

        #region Parse

        [TestMethod]
        public void ParseEmpty()
        {
            ParseExpectError("    ", ParseError.EmptyInput);
            ParseExpectError("    \0", ParseError.EmptyInput);
        }

        [TestMethod]
        public void ParseInvalidTopLevel()
        {
            ParseExpectError("42", ParseError.NoArrayOrObjectTopLevelExpression);
        }

        [TestMethod]
        public void ParseRedundantInput()
        {
            string err = "42";
            string s = "[1,2,3]" + err;
            ParseExpectError(s, ParseError.ImproperTermination, s.IndexOf(err));
        }

        [TestMethod]
        public void ParseUnexpectedToken()
        {
            ParseExpectError(",", ParseError.UnexpectedToken, 0);
            ParseExpectError(":", ParseError.UnexpectedToken, 0);
            ParseExpectError("}", ParseError.UnexpectedToken, 0);
            ParseExpectError("]", ParseError.UnexpectedToken, 0);
        }

        [TestMethod]
        public void ParseInvalidObjectEmpty()
        {
            ParseExpectError("{", ParseError.PrematureEndOfInput, -1);
            ParseExpectError("{\0", ParseError.PrematureEndOfInput, -1);
        }

        [TestMethod]
        public void ParseInvalidObjectMemberName()
        {
            ParseExpectError("{42", ParseError.ObjectNoStringMemberName, 1);
        }

        [TestMethod]
        public void ParseInvalidObjectNoColonInput()
        {
            ParseExpectError("{\"Test\"", ParseError.PrematureEndOfInput, -1);
        }

        [TestMethod]
        public void ParseInvalidObjectNoColon()
        {
            string err = "42";
            string s = "{\"Test\"" + err;
            ParseExpectError(s, ParseError.ObjectNoColonMemberNameValueSeparator, s.IndexOf(err));
        }

        [TestMethod]
        public void ParseInvalidObjectNoMemberValueInput()
        {
            ParseExpectError("{\"Test\":", ParseError.PrematureEndOfInput, -1);
        }

        [TestMethod]
        public void ParseInvalidObjectNoStuffAfterMember()
        {
            ParseExpectError("{\"Test\":42", ParseError.PrematureEndOfInput, -1);
        }

        [TestMethod]
        public void ParseInvalidObjectNoCommaOrCurlyAfterMember()
        {
            string err = "123";
            string s = "{\"Test\":42 " + err;
            ParseExpectError(s, ParseError.ObjectInvalidMemberSeparator, s.IndexOf(err));
        }

        [TestMethod]
        public void ParseInvalidObjectEmptyMember()
        {
            string s = "{\"Test\":42,}";
            ParseExpectError(s, ParseError.ObjectEmptyMember, s.Length - 1);
        }

        [TestMethod]
        public void ParseEmptyObject()
        {
            Parser.Parse("{}");
        }

        [TestMethod]
        public void ParseInvalidArrayEmpty()
        {
            ParseExpectError("[", ParseError.PrematureEndOfInput, -1);
            ParseExpectError("[\0", ParseError.PrematureEndOfInput, -1);
        }

        [TestMethod]
        public void ParseInvalidArrayNoNextElementOrEnd()
        {
            ParseExpectError("[42", ParseError.PrematureEndOfInput, -1);
        }

        [TestMethod]
        public void ParseInvalidArrayNoElementSeparator()
        {
            string err = "123";
            string s = "[42 " + err;
            ParseExpectError(s, ParseError.ArrayInvalidElementSeparator, s.IndexOf(err));
        }

        [TestMethod]
        public void ParseInvalidArrayEmptyElement()
        {
            string s = "[42,]";
            ParseExpectError(s, ParseError.ArrayEmptyElement, s.Length - 1);
        }

        private void ParseExpectError(string input, ParseError error)
        {
            ParseExpectError(input, error, 0);
        }

        private void ParseExpectError(string input, ParseError error, int position)
        {
            try
            {
                Parser.Parse(input);
                Assert.Fail();
            }
            catch (ParseException ex)
            {
                Assert.AreEqual(ex.Error, error);
                Assert.AreEqual(ex.Position, position);
            }
        }

        #endregion

        #region Deserialize

        #region Null

        [TestMethod]
        public void PrintNull()
        {
            Assert.AreEqual(((ArrayExpression)Parse("[null]")).Elements[0].ToString(), "null");
        }

        [TestMethod]
        public void DeserializeNullToObject()
        {
            Assert.IsNull(Deserialize<object[]>(@"[null]")[0]);
        }

        [TestMethod]
        public void DeserializeNullToString()
        {
            Assert.IsNull(Deserialize<string[]>(@"[null]")[0]);
        }

        [TestMethod]
        public void DeserializeNullToNullableInt()
        {
            Assert.IsNull(Deserialize<int?[]>(@"[null]")[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeNullToInt()
        {
            Deserialize<int[]>(@"[null]");
        }

        #endregion

        #region Number

        [TestMethod]
        public void PrintNumberInteger()
        {
            Assert.AreEqual(((ArrayExpression)Parse("[42]")).Elements[0].ToString(), "42");
        }

        [TestMethod]
        public void PrintNumberDecimal()
        {
            Assert.AreEqual(((ArrayExpression)Parse("[123.45]")).Elements[0].ToString(), "123.45");
        }

        [TestMethod]
        public void DeserializeNumberToObject()
        {
            Assert.AreEqual(Deserialize<object[]>(@"[42]")[0], "42");
        }

        [TestMethod]
        public void DeserializeNumberToString()
        {
            Assert.AreEqual(Deserialize<string[]>(@"[42]")[0], "42");
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeNumberToDateTime()
        {
            Deserialize<DateTime[]>(@"[42]");
        }

        [TestMethod]
        public void DeserializeNumberToEnum()
        {
            Assert.AreEqual(Deserialize<MyEnum[]>(@"[1]")[0], MyEnum.First);
        }

        [TestMethod]
        public void DeserializeNumberToFlagsEnum()
        {
            Assert.AreEqual(Deserialize<MyFlagsEnum[]>(@"[3]")[0], MyFlagsEnum.First | MyFlagsEnum.Second);
        }

        [TestMethod]
        public void DeserializeNumberToInt()
        {
            Assert.AreEqual(Deserialize<int[]>(@"[42]")[0], 42);
        }

        [TestMethod]
        public void DeserializeNumberToNullableInt()
        {
            Assert.AreEqual(Deserialize<int?[]>(@"[42]")[0], 42);
        }

        [TestMethod]
        public void DeserializeNumberToUint()
        {
            Assert.AreEqual(Deserialize<uint[]>(@"[42]")[0], 42u);
        }

        [TestMethod]
        public void DeserializeNumberToNullableUint()
        {
            Assert.AreEqual(Deserialize<uint?[]>(@"[42]")[0], 42u);
        }

        [TestMethod]
        public void DeserializeNumberToByte()
        {
            Assert.AreEqual(Deserialize<byte[]>(@"[42]")[0], (byte)42);
        }

        [TestMethod]
        public void DeserializeNumberToNullableByte()
        {
            Assert.AreEqual(Deserialize<byte?[]>(@"[42]")[0], (byte?)42);
        }

        [TestMethod]
        public void DeserializeNumberToSbyte()
        {
            Assert.AreEqual(Deserialize<sbyte[]>(@"[42]")[0], (sbyte)42);
        }

        [TestMethod]
        public void DeserializeNumberToNullableSbyte()
        {
            Assert.AreEqual(Deserialize<sbyte?[]>(@"[42]")[0], (sbyte?)42);
        }

        [TestMethod]
        public void DeserializeNumberToLong()
        {
            Assert.AreEqual(Deserialize<long[]>(@"[42]")[0], 42L);
        }

        [TestMethod]
        public void DeserializeNumberToNullableLong()
        {
            Assert.AreEqual(Deserialize<long?[]>(@"[42]")[0], 42L);
        }

        [TestMethod]
        public void DeserializeNumberToUlong()
        {
            Assert.AreEqual(Deserialize<ulong[]>(@"[42]")[0], 42UL);
        }

        [TestMethod]
        public void DeserializeNumberToNullableUlong()
        {
            Assert.AreEqual(Deserialize<ulong?[]>(@"[42]")[0], 42UL);
        }

        [TestMethod]
        public void DeserializeNumberToShort()
        {
            Assert.AreEqual(Deserialize<short[]>(@"[42]")[0], (short)42);
        }

        [TestMethod]
        public void DeserializeNumberToNullableShort()
        {
            Assert.AreEqual(Deserialize<short?[]>(@"[42]")[0], (short?)42);
        }

        [TestMethod]
        public void DeserializeNumberToUshort()
        {
            Assert.AreEqual(Deserialize<ushort[]>(@"[42]")[0], (ushort)42);
        }

        [TestMethod]
        public void DeserializeNumberToNullableUshort()
        {
            Assert.AreEqual(Deserialize<ushort?[]>(@"[42]")[0], (ushort?)42);
        }

        [TestMethod]
        public void DeserializeNumberToFloat()
        {
            Assert.AreEqual(Deserialize<float[]>(@"[4.2]")[0], 4.2f);
        }

        [TestMethod]
        public void DeserializeNumberToNullableFloat()
        {
            Assert.AreEqual(Deserialize<float?[]>(@"[4.2]")[0], 4.2f);
        }

        [TestMethod]
        public void DeserializeNumberToDouble()
        {
            Assert.AreEqual(Deserialize<double[]>(@"[4.2]")[0], 4.2d);
        }

        [TestMethod]
        public void DeserializeNumberToNullableDouble()
        {
            Assert.AreEqual(Deserialize<double?[]>(@"[4.2]")[0], 4.2d);
        }

        [TestMethod]
        public void DeserializeNumberToDecimal()
        {
            Assert.AreEqual(Deserialize<decimal[]>(@"[4.2]")[0], 4.2m);
        }

        [TestMethod]
        public void DeserializeNumberToNullableDecimal()
        {
            Assert.AreEqual(Deserialize<decimal?[]>(@"[4.2]")[0], 4.2m);
        }

        #endregion

        #region Boolean

        [TestMethod]
        public void PrintBooleanTrue()
        {
            Assert.AreEqual(((ArrayExpression)Parse("[true]")).Elements[0].ToString(), "true");
        }

        [TestMethod]
        public void PrintBooleanFalse()
        {
            Assert.AreEqual(((ArrayExpression)Parse("[false]")).Elements[0].ToString(), "false");
        }

        [TestMethod]
        public void DeserializeBooleanFalse()
        {
            Assert.AreEqual(Deserialize<bool[]>(@"[false]")[0], false);
        }

        [TestMethod]
        public void DeserializeBooleanTrue()
        {
            Assert.AreEqual(Deserialize<bool[]>(@"[true]")[0], true);
        }

        [TestMethod]
        public void DeserializeBooleanFalseNullable()
        {
            Assert.AreEqual(Deserialize<bool?[]>(@"[false]")[0], false);
        }

        [TestMethod]
        public void DeserializeBooleanTrueNullable()
        {
            Assert.AreEqual(Deserialize<bool?[]>(@"[true]")[0], true);
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeBooleanToInt()
        {
            Deserialize<int[]>(@"[false]");
        }

        #endregion

        #region Array

        [TestMethod]
        public void PrintArray()
        {
            Assert.AreEqual(Parse("[1,2]").ToString(), "[1, 2]");
        }

        [TestMethod]
        public void DeserializeArrayEmpty()
        {
            Assert.IsTrue(Deserialize<int[]>(@"[]").Length == 0);
        }

        [TestMethod]
        public void DeserializeArraySingleton()
        {
            var arr = Deserialize<int[]>(@"[42]");
            Assert.IsTrue(arr.Length == 1);
            Assert.AreEqual(arr[0], 42);
        }

        [TestMethod]
        public void DeserializeArrayNested()
        {
            var arr = Deserialize<int[][]>(@"[[42]]");
            Assert.IsTrue(arr.Length == 1);
            Assert.IsTrue(arr[0].Length == 1);
            Assert.AreEqual(arr[0][0], 42);
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeArrayMultiDimensional()
        {
            Deserialize<int[,]>(@"[]");
        }

        [TestMethod]
        public void DeserializeArrayToArrayList()
        {
            var arr = Deserialize<ArrayList>(@"[42]");
            Assert.IsTrue(arr.Count == 1);
            Assert.AreEqual(arr[0], "42"); // This is right!
        }

        [TestMethod]
        public void DeserializeArrayToGenericList()
        {
            var arr = Deserialize<List<int>>(@"[42]");
            Assert.IsTrue(arr.Count == 1);
            Assert.AreEqual(arr[0], 42);
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeArrayToString()
        {
            Deserialize<string>(@"[42]");
        }

        [TestMethod]
        public void DeserializeArrayToObject()
        {
            var arr = (IList)Deserialize<object>(@"[42]");
            Assert.IsTrue(arr.Count == 1);
            Assert.AreEqual(arr[0], "42"); // This is right!
        }

        #endregion

        #region String

        [TestMethod]
        public void PrintString()
        {
            TestPrintString(@"""Bart""");
        }

        [TestMethod]
        public void PrintStringEscapes()
        {
            TestPrintString(@"""Bart \/\\ says \""Hi\""\t\r\n\f""");
        }

        private void TestPrintString(string s)
        {
            Assert.AreEqual(((ArrayExpression)Parse("[" + s + "]")).Elements[0].ToString(), s);
        }

        [TestMethod]
        public void DeserializeString()
        {
            Assert.AreEqual(Deserialize<string[]>(@"[""Bart""]")[0], "Bart");
        }

        [TestMethod]
        public void DeserializeStringToObject()
        {
            Assert.AreEqual(Deserialize<object[]>(@"[""Bart""]")[0], "Bart");
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeStringToInt()
        {
            Deserialize<int[]>(@"[""Bart""]");
        }

        [TestMethod]
        public void DeserializeStringToEnum()
        {
            Assert.AreEqual(Deserialize<MyEnum[]>(@"[""First""]")[0], MyEnum.First);
        }

        #endregion

        #region Object

        [TestMethod]
        public void PrintObject()
        {
            Assert.AreEqual(Parse(@"{""x"":1,""y"":2}").ToString(), @"{""x"": 1, ""y"": 2}");
        }

        [TestMethod]
        public void DeserializeObjectProperties()
        {
            Assert.AreEqual(Deserialize<Person>(@"{""Name"": ""Bart"", ""Age"": 26}"), new Person { Name = "Bart", Age = 26 });
        }

        [TestMethod]
        public void DeserializeObjectFields()
        {
            Assert.AreEqual(Deserialize<PersonFields>(@"{""Name"": ""Bart"", ""Age"": 26}"), new PersonFields { Name = "Bart", Age = 26 });
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeObjectReadOnly()
        {
            Deserialize<PersonReadOnly>(@"{""Name"": ""Bart"", ""Age"": 26}");
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeObjectMissingField()
        {
            Deserialize<Person>(@"{""Name"": ""Bart"", ""Age"": 26, ""ShoeSize"": 42}");
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeObjectThrowingPropertySetter()
        {
            Deserialize<PersonBad>(@"{""Name"": ""Bart"", ""Age"": 26}");
        }

        [TestMethod]
        public void DeserializeObjectToDictionary()
        {
            var d = Deserialize<Dictionary<string, object>>(@"{""Name"": ""Bart"", ""Age"": 26}");
            Assert.IsTrue(d.Keys.Count == 2);
            Assert.AreEqual(d["Name"], "Bart");
            Assert.AreEqual(d["Age"], "26");
        }

        [TestMethod]
        public void DeserializeObjectToObject()
        {
            var d = (IDictionary<string, object>)Deserialize<object>(@"{""Name"": ""Bart"", ""Age"": 26}");
            Assert.IsTrue(d.Keys.Count == 2);
            Assert.AreEqual(d["Name"], "Bart");
            Assert.AreEqual(d["Age"], "26");
        }

        [TestMethod]
        public void DeserializeObjectToHashtable()
        {
            var d = Deserialize<Hashtable>(@"{""Name"": ""Bart"", ""Age"": 26}");
            Assert.IsTrue(d.Keys.Count == 2);
            Assert.AreEqual(d["Name"], "Bart");
            Assert.AreEqual(d["Age"], "26");
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void DeserializeObjectToDictionaryNoStringKey()
        {
            Deserialize<Dictionary<int, object>>(@"{""Name"": ""Bart"", ""Age"": 26}");
        }

        #endregion

        #endregion

        #region Serialize

        #region Null

        [TestMethod]
        public void SerializeNull()
        {
            Assert.AreEqual(Serialize(null), "null");
        }

        [TestMethod]
        public void SerializeNullable()
        {
            Assert.AreEqual(Serialize((bool?)null), "null");
        }

        #endregion

        #region String

        [TestMethod]
        public void SerializeString()
        {
            Assert.AreEqual(Serialize("Bart"), "\"Bart\"");
        }

        [TestMethod]
        public void SerializeStringEscapes()
        {
            Assert.AreEqual(Serialize("Bart says \"Hi\tJoe\""), "\"Bart says \\\"Hi\\tJoe\\\"\"");
        }

        #endregion

        #region Boolean

        [TestMethod]
        public void SerializeBooleanFalse()
        {
            Assert.AreEqual(Serialize(false), "false");
        }

        [TestMethod]
        public void SerializeBooleanTrue()
        {
            Assert.AreEqual(Serialize(true), "true");
        }

        #endregion

        #region Number

        [TestMethod]
        public void SerializeInt()
        {
            Assert.AreEqual(Serialize(42), "42");
        }

        [TestMethod]
        public void SerializeNullableInt()
        {
            Assert.AreEqual(Serialize((int?)42), "42");
        }

        [TestMethod]
        public void SerializeUint()
        {
            Assert.AreEqual(Serialize(42u), "42");
        }

        [TestMethod]
        public void SerializeNullableUint()
        {
            Assert.AreEqual(Serialize((uint?)42u), "42");
        }

        [TestMethod]
        public void SerializeByte()
        {
            Assert.AreEqual(Serialize((byte)42), "42");
        }

        [TestMethod]
        public void SerializeNullableByte()
        {
            Assert.AreEqual(Serialize((byte?)42), "42");
        }

        [TestMethod]
        public void SerializeSbyte()
        {
            Assert.AreEqual(Serialize((sbyte)42), "42");
        }

        [TestMethod]
        public void SerializeNullableSbyte()
        {
            Assert.AreEqual(Serialize((sbyte?)42), "42");
        }

        [TestMethod]
        public void SerializeLong()
        {
            Assert.AreEqual(Serialize(42L), "42");
        }

        [TestMethod]
        public void SerializeNullableLong()
        {
            Assert.AreEqual(Serialize((long?)42L), "42");
        }

        [TestMethod]
        public void SerializeUlong()
        {
            Assert.AreEqual(Serialize(42UL), "42");
        }

        [TestMethod]
        public void SerializeNullableUlong()
        {
            Assert.AreEqual(Serialize((ulong?)42UL), "42");
        }

        [TestMethod]
        public void SerializeShort()
        {
            Assert.AreEqual(Serialize((short)42), "42");
        }

        [TestMethod]
        public void SerializeNullableShort()
        {
            Assert.AreEqual(Serialize((short?)42), "42");
        }

        [TestMethod]
        public void SerializeUshort()
        {
            Assert.AreEqual(Serialize((ushort)42), "42");
        }

        [TestMethod]
        public void SerializeNullableUshort()
        {
            Assert.AreEqual(Serialize((ushort?)42), "42");
        }

        [TestMethod]
        public void SerializeFloat()
        {
            Assert.AreEqual(Serialize(4.2f), "4.2");
        }

        [TestMethod]
        public void SerializeNullableFloat()
        {
            Assert.AreEqual(Serialize((float?)4.2f), "4.2");
        }

        [TestMethod]
        public void SerializeDouble()
        {
            Assert.AreEqual(Serialize(4.2d), "4.2");
        }

        [TestMethod]
        public void SerializeNullableDouble()
        {
            Assert.AreEqual(Serialize((double?)4.2d), "4.2");
        }

        [TestMethod]
        public void SerializeDecimal()
        {
            Assert.AreEqual(Serialize(4.2m), "4.2");
        }

        [TestMethod]
        public void SerializeNullableDecimal()
        {
            Assert.AreEqual(Serialize((decimal?)4.2m), "4.2");
        }

        #endregion

        #region Enum

        [TestMethod]
        public void SerializeEnum()
        {
            Assert.AreEqual(Serialize(MyEnum.First), "1");
            Assert.AreEqual(Serialize(MyFlagsEnum.First | MyFlagsEnum.Second), "3");
        }

        #endregion

        #region Arrays

        [TestMethod]
        public void SerializeArray()
        {
            Assert.AreEqual(Serialize(new object[] { "Bart", 26 }), "[\"Bart\", 26]");
        }

        [TestMethod]
        public void SerializeArrayList()
        {
            Assert.AreEqual(Serialize(new ArrayList { "Bart", 26 }), "[\"Bart\", 26]");
        }

        [TestMethod]
        public void SerializeList()
        {
            Assert.AreEqual(Serialize(new List<object> { "Bart", 26 }), "[\"Bart\", 26]");
        }

        #endregion

        #region Objects

        [TestMethod]
        public void SerializeObject()
        {
            Assert.AreEqual(Serialize(new Person { Name = "Bart", Age = 26 }), "{\"Name\": \"Bart\", \"Age\": 26}");
        }

        [TestMethod]
        public void SerializeHashtable()
        {
            Assert.AreEqual(Serialize(new Hashtable { { "Name", "Bart" }, { "Age", 26 } }), "{\"Name\": \"Bart\", \"Age\": 26}");
        }

        [TestMethod]
        public void SerializeDictionary()
        {
            Assert.AreEqual(Serialize(new Dictionary<string, object> { { "Name", "Bart" }, { "Age", 26 } }), "{\"Name\": \"Bart\", \"Age\": 26}");
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void SerializeHashtableNonStringKey()
        {
            Serialize(new Hashtable { { "Name", "Bart" }, { 123, 26 } });
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void SerializeDictionaryNonStringKey()
        {
            Serialize(new Dictionary<int, object>());
        }

        #endregion

        #region Cycle

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void SerializeCyclicArray()
        {
            object[] arr = new object[1];
            arr[0] = arr;
            Serialize(arr);
        }

        [TestMethod]
        [ExpectedException(typeof(SerializationException))]
        public void SerializeCyclicObject()
        {
            var a = new Cyclic();
            var b = new Cyclic();
            b.Parent = a;
            a.Parent = b;
            Serialize(b);
        }

        #endregion

        #endregion

        #region ReadOnlyDictionary

        [TestMethod]
        public void ReadOnlyDictionary()
        {
            var arr = (ArrayExpression)Expression.Parse(@"[{""Name"": ""Bart"", ""Age"": 26}]");
            var obj = (ObjectExpression)arr.Elements[0];
            var dict = obj.Members;

            Assert.IsTrue(dict.ContainsKey("Name"));
            Assert.IsTrue(dict.Keys.Contains("Name"));
            Assert.IsFalse(dict.ContainsKey("ShoeSize"));
            Assert.IsTrue(dict["Age"] is ConstantExpression);
            Assert.AreEqual(dict.Values.Count, 2);
            Expression age;
            Assert.IsTrue(dict.TryGetValue("Age", out age));
            Assert.IsTrue(age is ConstantExpression);
            Assert.IsNotNull(((IEnumerable)dict).GetEnumerator());
        }

        #endregion

        private T Deserialize<T>(string json)
        {
            return (T)new JsonSerializer(typeof(T)).Deserialize(json);
        }

        private string Serialize(object o)
        {
            return new JsonSerializer(typeof(object)).Serialize(o);
        }

        private Expression Parse(string input)
        {
            return Expression.Parse(input);
        }
    }

    enum MyEnum
    {
        First = 1,
        Second = 2
    }

    [Flags]
    enum MyFlagsEnum
    {
        First = 1,
        Second = 2
    }

    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Person p = (Person)obj;
            return (Name == p.Name) && (Age == p.Age);
        }

        public override int GetHashCode()
        {
            return Age.GetHashCode() ^ (Name ?? "").GetHashCode();
        }
    }

    class PersonReadOnly
    {
        public string Name { get; private set; }
        public int Age { get; private set; }
    }

    class PersonFields
    {
        public string Name;
        public int Age;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            PersonFields p = (PersonFields)obj;
            return (Name == p.Name) && (Age == p.Age);
        }

        public override int GetHashCode()
        {
            return Age.GetHashCode() ^ (Name ?? "").GetHashCode();
        }
    }

    class PersonBad
    {
        public string Name { get; set; }
        public int Age { get { return 0; } set { throw new Exception(); } }
    }

    class Cyclic
    {
        public Cyclic Parent { get; set; }
    }
}
