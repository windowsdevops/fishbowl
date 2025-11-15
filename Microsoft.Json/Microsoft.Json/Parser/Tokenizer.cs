//
// Copyright Microsoft Corporation.  All rights reserved.
// 

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Json.Parser
{
    using Internal;

    /// <summary>
    /// Tokenizer for JSON code.
    /// </summary>
    internal sealed class Tokenizer
    {
        #region Private fields

        /// <summary>
        /// JSON code text.
        /// </summary>
        private string _input;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new tokenizer for JSON code.
        /// </summary>
        /// <param name="input">JSON code text.</param>
        public Tokenizer(string input)
        {
            _input = input;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Tokenizes the input.
        /// </summary>
        /// <returns>Sequence of tokens.</returns>
        public IEnumerable<Token> Tokenize()
        {
            //
            // Note: \G enforces a match at the start of the input text.
            //
            var WHITE = new Regex(@"\G[ \t\r\n]+");
            var LEFTCURLY = new Regex(@"\G\{");
            var RIGHTCURLY = new Regex(@"\G\}");
            var LEFTBRACKET = new Regex(@"\G\[");
            var RIGHTBRACKET = new Regex(@"\G\]");
            var COMMA = new Regex(@"\G,");
            var COLON = new Regex(@"\G:");
            var FALSE = new Regex(@"\Gfalse");
            var TRUE = new Regex(@"\Gtrue");
            var NULL = new Regex(@"\Gnull");
            var NUM = new Regex(@"\G-?(0|[1-9][0-9]*)(\.[0-9]+)?(e[\+-]?[0-9]+)?");
            var STR = new Regex(@"\G""([\u0020-\u0021]|[\u0023-\u005b]|[\u005d-\uffff]|\\([""\\/bfnrt]|u[0-9a-fA-F]{4}))*""");
            var EOF = new Regex(@"\G\0");

            int pos = 0;
            string input = _input;

            while (input.Length > 0)
            {
                //
                // Simple mapping table for regular expressions onto token productions.
                //
                var m = input.MatchWith(WHITE, _ => Token.White(pos))
                             .OrWith(LEFTCURLY, _ => Token.LeftCurly(pos))
                             .OrWith(RIGHTCURLY, _ => Token.RightCurly(pos))
                             .OrWith(LEFTBRACKET, _ => Token.LeftBracket(pos))
                             .OrWith(RIGHTBRACKET, _ => Token.RightBracket(pos))
                             .OrWith(COMMA, _ => Token.Comma(pos))
                             .OrWith(COLON, _ => Token.Colon(pos))
                             .OrWith(FALSE, _ => Token.False(pos))
                             .OrWith(TRUE, _ => Token.True(pos))
                             .OrWith(NULL, _ => Token.Null(pos))
                             .OrWith(NUM, num => Token.Number(pos, num))
                             .OrWith(STR, str => Token.String(pos, str))
                             .OrWith(EOF, _ => Token.Eof(pos));

                //
                // If none of the regular expressions match, we have an unknown token.
                //
                if (!m.Success)
                    throw new ParseException("Unrecognized token.", pos, ParseError.InvalidToken);

                //
                // Yield to consumer of iterator.
                //
                yield return m.Value;

                //
                // Eat matched input and proceed.
                //
                input = input.Substring(m.Length);
                pos += m.Length;
            }
        }

        #endregion
    }
}
