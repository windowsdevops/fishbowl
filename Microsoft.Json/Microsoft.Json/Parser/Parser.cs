//
// Copyright Microsoft Corporation.  All rights reserved.
// 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Json.Parser
{
    using Expressions;
    using Internal;

    /// <summary>
    /// Parser for JSON code.
    /// </summary>
    internal static class Parser
    {
        #region Static fields

        /// <summary>
        /// Regular expression for \uHEX#4, representing a Unicode character represented as four hexadecimal digits.
        /// </summary>
        private static Regex s_unicode = new Regex(@"\\u([0-9a-fA-F]{4})", RegexOptions.Compiled);

        #endregion

        #region Public methods

        /// <summary>
        /// Parses the given JSON text and returns an expression tree representing the JSON expression.
        /// </summary>
        /// <param name="input">JSON text to be parsed.</param>
        /// <returns>Expression tree representing the JSON expression.</returns>
        /// <remarks>See RFC 4627 for more information. This parses the production specified in section 2: <code>JSON-text = object / array</code>.</remarks>
        public static Expression Parse(string input)
        {
            //
            // Tokenize the input, ignoring whitespace between tokens.
            //
            var tokenizer = new Tokenizer(input);
            var tokens = from token in tokenizer.Tokenize()
                         where token.Type != TokenType.White
                         select token;

            //
            // Initialize token enumeration, ensuring we're not faced with empty input.
            //
            var tokenStream = tokens.GetEnumerator();
            Token firstToken;
            if (!tokenStream.TryMoveNext(out firstToken) || firstToken.Type == TokenType.Eof)
                throw new ParseException("Empty input.", 0, ParseError.EmptyInput);

            //
            // Recursive parsing of the token stream, making sure the returned value is a
            // top-level array or object expression.
            //
            var res = Parse(tokenStream);
            if (res.NodeType != ExpressionType.Array && res.NodeType != ExpressionType.Object)
                throw new ParseException("Unexpected start token.", 0, ParseError.NoArrayOrObjectTopLevelExpression);

            //
            // Ensure proper termination, either by seeing an EOF (ignoring further input
            // that may follow it) or by not seeing any further input.
            //
            Token lastToken;
            if (tokenStream.TryMoveNext(out lastToken) && lastToken.Type != TokenType.Eof)
                throw new ParseException("Not properly terminated.", lastToken.Position, ParseError.ImproperTermination);

            return res;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Parses the given JSON token stream and returns an expression tree representing the JSON expression.
        /// </summary>
        /// <param name="tokens">JSON token stream to be parsed.</param>
        /// <returns>Expression tree representing the JSON expression.</returns>
        private static Expression Parse(IEnumerator<Token> tokens)
        {
            //
            // JSON is LL(1); we can simply look-ahead one symbol to decide how to proceed
            // with parsing, i.e. { indicates an object, [ indicates an array, etc.
            //
            var token = tokens.Current;
            switch (token.Type)
            {
                case TokenType.LeftCurly:
                    return ParseObject(tokens);
                case TokenType.LeftBracket:
                    return ParseArray(tokens);
                case TokenType.False:
                    return Expression.Boolean(false);
                case TokenType.True:
                    return Expression.Boolean(true);
                case TokenType.Null:
                    return Expression.Null();
                case TokenType.String:
                    return Expression.String(JsonStringToBclString(token.Data));
                case TokenType.Number:
                    return Expression.Number(token.Data);
                case TokenType.Eof:
                case TokenType.White: // indicates improper filtering earlier on
                case TokenType.RightCurly:
                case TokenType.RightBracket:
                case TokenType.Comma:
                case TokenType.Colon:
                default:
                    throw new ParseException("Unexpected token: " + token.Type, token.Position, ParseError.UnexpectedToken);
            }
        }

        /// <summary>
        /// Parses a JSON object consuming tokens from the given token stream.
        /// </summary>
        /// <param name="tokens">JSON token stream to consume tokens from.</param>
        /// <returns>Object expression for the JSON object expression.</returns>
        private static ObjectExpression ParseObject(IEnumerator<Token> tokens)
        {
            var members = new Dictionary<string, Expression>();

            bool? expectNext = null;

            Token token = null;
            while (tokens.TryMoveNext(out token) && token.Type != TokenType.RightCurly && token.Type != TokenType.Eof)
            {
                if (token.Type != TokenType.String)
                    throw new ParseException("Invalid member declaration on object. Expected string for member name.", token.Position, ParseError.ObjectNoStringMemberName);

                string name = JsonStringToBclString(token.Data);

                if (!tokens.TryMoveNext(out token))
                    throw new ParseException("Premature end of input during object expression parsing. Expected colon separator between member name and value.", -1 /* end */, ParseError.PrematureEndOfInput);
                
                if (token.Type != TokenType.Colon)
                    throw new ParseException("Invalid member declaration on object. Expected colon separator between member name and value.", token.Position, ParseError.ObjectNoColonMemberNameValueSeparator);

                if (!tokens.MoveNext())
                    throw new ParseException("Premature end of input during object expression parsing. Expected member value.", -1 /* end */, ParseError.PrematureEndOfInput);

                members[name] = Parse(tokens);

                if (!tokens.TryMoveNext(out token))
                    throw new ParseException("Premature end of input during object expression parsing. Expected either comma or closing curly brace.", -1 /* end */, ParseError.PrematureEndOfInput);

                if (token.Type != TokenType.Comma && token.Type != TokenType.RightCurly)
                    throw new ParseException("Invalid member declaration on object. Expected proper separator between members.", token.Position, ParseError.ObjectInvalidMemberSeparator);

                expectNext = token.Type == TokenType.Comma;

                if (token.Type == TokenType.RightCurly)
                    break;
            }

            if (token == null || token.Type != TokenType.RightCurly)
                throw new ParseException("Premature end of input during object expression parsing. Expected to reach a closing curly brace.", -1 /* end */, ParseError.PrematureEndOfInput);

            if (expectNext.HasValue && expectNext.Value)
                throw new ParseException("Empty member declaration on object.", token.Position, ParseError.ObjectEmptyMember);

            return Expression.Object(members);
        }

        /// <summary>
        /// Parses a JSON array consuming tokens from the given token stream.
        /// </summary>
        /// <param name="tokens">JSON token stream to consume tokens from.</param>
        /// <returns>Array expression for the JSON array expression.</returns>
        private static ArrayExpression ParseArray(IEnumerator<Token> tokens)
        {
            var values = new List<Expression>();

            bool? expectNext = null;

            Token token = null;
            while (tokens.TryMoveNext(out token) && token.Type != TokenType.RightBracket && token.Type != TokenType.Eof)
            {
                values.Add(Parse(tokens));

                if (!tokens.TryMoveNext(out token))
                    throw new ParseException("Premature end of input during array expression parsing. Expected colon separator between elements or closing square bracket.", -1 /* end */, ParseError.PrematureEndOfInput);

                if (token.Type != TokenType.Comma && token.Type != TokenType.RightBracket)
                    throw new ParseException("Invalid element declaration in array. Expected proper separator between elements.", token.Position, ParseError.ArrayInvalidElementSeparator);

                expectNext = token.Type == TokenType.Comma;

                if (token.Type == TokenType.RightBracket)
                    break;
            }

            if (token == null || token.Type != TokenType.RightBracket)
                throw new ParseException("Premature end of input during array expression parsing. Expected to reach a closing square bracket.", -1 /* end */, ParseError.PrematureEndOfInput);

            if (expectNext.HasValue && expectNext.Value)
                throw new ParseException("Empty element declaration on array.", token.Position, ParseError.ArrayEmptyElement);

            return Expression.Array(values.ToArray());
        }

        /// <summary>
        /// Converts a JSON string into a BCL string, unescaping all escape sequences into their proper characters.
        /// </summary>
        /// <param name="value">JSON string to be converted.</param>
        /// <returns>BCL string corresponding to the given JSON string.</returns>
        /// <remarks>See RFC 4627 for more information. This covers the escape sequences documented in section 2.5.</remarks>
        private static string JsonStringToBclString(string value)
        {
            var sb = new StringBuilder();

            string res = value.Substring(1, value.Length - 2 /* " quotes */);
            res = s_unicode.Replace(res, m => char.ConvertFromUtf32(int.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)));

            res = sb.Append(res)
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\f", "\f")
                    .Replace("\\b", "\b")
                    .Replace("\\\"", "\"")
                    .Replace("\\/", "/")
                    .Replace("\\\\", "\\")
                    .ToString();

            return res;
        }

        #endregion
    }
}