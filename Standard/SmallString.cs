
namespace Standard
{
    using System;

    [System.Diagnostics.DebuggerDisplay("SmallString: { GetString() }")]
    internal struct SmallString : IEquatable<SmallString>, IComparable<SmallString>
    {
        private static readonly System.Text.UTF8Encoding s_Encoder = new System.Text.UTF8Encoding(false /* do not emit BOM */, true /* throw on error */);
        private readonly byte[] _utf8String;

        public SmallString(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _utf8String = s_Encoder.GetBytes(value);
                Assert.IsNotNull(_utf8String);
            }
            else
            {
                _utf8String = null;
            }
        }

        #region Object Overrides

        public override string ToString()
        {
            Assert.Fail();
            throw new NotSupportedException("This exception exists to prevent accidental performance penalties.  Call GetString() instead.");
        }

        public override int GetHashCode()
        {
            // Intentionally hashes similarly to the expanded strings.
            return GetString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            try
            {
                return Equals((SmallString)obj);
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        #endregion

        #region IEquatable<SmallString> Members

        public bool Equals(SmallString other)
        {
            if (_utf8String == null)
            {
                return other._utf8String == null;
            }

            if (other._utf8String == null)
            {
                return false;
            }

            if (_utf8String.Length != other._utf8String.Length)
            {
                return false;
            }

            // Note that this is doing a literal binary comparison of the two strings.
            // It's possible for two real strings to compare equally even though they
            // can be encoded in different ways with UTF8.
            return Utility.MemCmp(_utf8String, other._utf8String, _utf8String.Length);
        }

        #endregion

        public string GetString()
        {
            if (_utf8String == null)
            {
                return "";
            }
            return s_Encoder.GetString(_utf8String);
        }

        public static bool operator==(SmallString left, SmallString right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(SmallString left, SmallString right)
        {
            return !left.Equals(right);
        }

        #region IComparable<SmallString> Members

        public int CompareTo(SmallString other)
        {
            // If either of the strings contains multibyte characters 
            // then we can't do a strictly bitwise comparison.
            // We can look for a signaled high-bit in the byte to detect this.
            // Opportunistically, we're going to assume that the strings are
            // ASCII compatible until we find out they aren't.

            if (_utf8String == null)
            {
                if (other._utf8String == null)
                {
                    return 0;
                }
                Assert.AreNotEqual(0, other._utf8String.Length);
                return -1;
            }
            else if (other._utf8String == null)
            {
                Assert.AreNotEqual(0, _utf8String.Length);
                return 1;
            }

            bool? isThisStringShorter = null;
            int cb = _utf8String.Length;
            int cbDiffernce = other._utf8String.Length - cb;

            if (cbDiffernce < 0)
            {
                isThisStringShorter = false;
                cb = other._utf8String.Length; 
            }
            else if (cbDiffernce > 0)
            {
                isThisStringShorter = true;
            }

            for (int i = 0; i < cb; ++i)
            {
                bool isEitherHighBitSet = ((_utf8String[i] | other._utf8String[i]) & 0x80) != 0;
                // If the byte array contains multibyte characters
                // we need to do a real string comparison.
                if (isEitherHighBitSet)
                {
                    string left = this.GetString();
                    string right = other.GetString();

                    return left.CompareTo(right);
                }

                if (_utf8String[i] != other._utf8String[i])
                {
                    return _utf8String[i] - other._utf8String[i];
                }
            }

            if (isThisStringShorter == null)
            {
                return 0;
            }

            if (isThisStringShorter == false)
            {
                return -1;
            }

            return 1;
        }

        #endregion
    }
}