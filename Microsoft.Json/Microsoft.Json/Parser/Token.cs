//
// Copyright Microsoft Corporation.  All rights reserved.
// 

using System;

namespace Microsoft.Json.Parser
{
    /// <summary>
    /// JSON token.
    /// </summary>
    internal sealed class Token
    {
        #region Constructors

        /// <summary>
        /// Creates a new token of the given type, with the given data.
        /// </summary>
        /// <param name="type">Type of the token.</param>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <param name="data">Data for the token.</param>
        private Token(TokenType type, int pos, string data)
        {
            Type = type;
            Position = pos;
            Data = data;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type of the token.
        /// </summary>
        public TokenType Type { get; private set; }

        /// <summary>
        /// Gets the position of the first character of the token in the input stream.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// Gets the data for the token.
        /// </summary>
        /// <remarks>Will be null for anything other than a Number of String token type.</remarks>
        public string Data { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Provides a string representation of the token, based on the token type and its data (if any).
        /// </summary>
        /// <returns>String representation of the token.</returns>
        public override string ToString()
        {
            switch (Type)
            {
                case TokenType.Eof:
                case TokenType.White:
                case TokenType.LeftCurly:
                case TokenType.RightCurly:
                case TokenType.LeftBracket:
                case TokenType.RightBracket:
                case TokenType.Comma:
                case TokenType.Colon:
                case TokenType.False:
                case TokenType.True:
                case TokenType.Null:
                    return Type.ToString().ToUpperInvariant();
                case TokenType.String:
                    return "STRING(" + Data + ")";
                case TokenType.Number:
                    return "NUM(" + Data + ")";
                default:
                    return base.ToString();
            }
        }

        #endregion

        #region Factory methods

        /// <summary>
        /// Creates an Eof token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>Eof token.</returns>
        public static Token Eof(int pos)
        {
            return new Token(TokenType.Eof, pos, null);
        }

        /// <summary>
        /// Creates a White token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>White token.</returns>
        public static Token White(int pos)
        {
            return new Token(TokenType.White, pos, null);
        }

        /// <summary>
        /// Creates a LeftCurly token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>LeftCurly token.</returns>
        public static Token LeftCurly(int pos)
        {
            return new Token(TokenType.LeftCurly, pos, null);
        }

        /// <summary>
        /// Creates a RightCurly token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>RightCurly token.</returns>
        public static Token RightCurly(int pos)
        {
            return new Token(TokenType.RightCurly, pos, null);
        }

        /// <summary>
        /// Creates a LeftBracket token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>LeftBracket token.</returns>
        public static Token LeftBracket(int pos)
        {
            return new Token(TokenType.LeftBracket, pos, null);
        }

        /// <summary>
        /// Creates a RightBracket token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>RightBracket token.</returns>
        public static Token RightBracket(int pos)
        {
            return new Token(TokenType.RightBracket, pos, null);
        }

        /// <summary>
        /// Creates a True token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>True token.</returns>
        public static Token True(int pos)
        {
            return new Token(TokenType.True, pos, null);
        }

        /// <summary>
        /// Creates a False token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>False token.</returns>
        public static Token False(int pos)
        {
            return new Token(TokenType.False, pos, null);
        }

        /// <summary>
        /// Creates a Null token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>Null token.</returns>
        public static Token Null(int pos)
        {
            return new Token(TokenType.Null, pos, null);
        }

        /// <summary>
        /// Creates a Comma token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>Comma token.</returns>
        public static Token Comma(int pos)
        {
            return new Token(TokenType.Comma, pos, null);
        }

        /// <summary>
        /// Creates a Colon token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <returns>Colon token.</returns>
        public static Token Colon(int pos)
        {
            return new Token(TokenType.Colon, pos, null);
        }

        /// <summary>
        /// Creates a Number token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <param name="num">Text representing the number.</param>
        /// <returns>Number token for the given number text.</returns>
        public static Token Number(int pos, string num)
        {
            return new Token(TokenType.Number, pos, num);
        }

        /// <summary>
        /// Creates a String token.
        /// </summary>
        /// <param name="pos">Position of the first character of the token in the input stream.</param>
        /// <param name="str">Text representing the string (including surrounding quotes).</param>
        /// <returns>String token for the given string text.</returns>
        public static Token String(int pos, string str)
        {
            return new Token(TokenType.String, pos, str);
        }

        #endregion
    }
}
