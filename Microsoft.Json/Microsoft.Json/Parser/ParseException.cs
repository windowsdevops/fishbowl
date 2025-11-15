//
// Copyright Microsoft Corporation.  All rights reserved.
// 

using System;

namespace Microsoft.Json.Parser
{
    /// <summary>
    /// Parser exception signaling a parsing error in the input stream.
    /// </summary>
    [Serializable]
    public class ParseException : Exception
    {
        /// <summary>
        /// Creates a new parser exception signaling an error in the input stream at the given position.
        /// </summary>
        /// <param name="message">Message describing the parsing error.</param>
        /// <param name="position">Position in the input stream where the parsing error occurred.</param>
        /// <param name="error">Parser error.</param>
        public ParseException(string message, int position, ParseError error)
            : base(message)
        {
            Position = position;
            Error = error;
        }

        /// <summary>
        /// Gets the position in the input stream where the parsing error occurred.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// Gets the parser error.
        /// </summary>
        public ParseError Error { get; private set; }
    }
}
