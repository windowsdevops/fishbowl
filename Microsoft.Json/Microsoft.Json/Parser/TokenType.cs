//
// Copyright Microsoft Corporation.  All rights reserved.
// 

namespace Microsoft.Json.Parser
{
    /// <summary>
    /// JSON token types.
    /// </summary>
    enum TokenType
    {
        /// <summary>
        /// End of file.
        /// </summary>
        Eof,

        /// <summary>
        /// Whitespace.
        /// </summary>
        White,

        /// <summary>
        /// Left curly brace.
        /// </summary>
        LeftCurly,

        /// <summary>
        /// Right curly brace.
        /// </summary>
        RightCurly,

        /// <summary>
        /// Left square bracket.
        /// </summary>
        LeftBracket,

        /// <summary>
        /// Right square bracket.
        /// </summary>
        RightBracket,

        /// <summary>
        /// Comma.
        /// </summary>
        Comma,

        /// <summary>
        /// Colon.
        /// </summary>
        Colon,
        
        /// <summary>
        /// "false" literal.
        /// </summary>
        False,

        /// <summary>
        /// "true" literal.
        /// </summary>
        True,

        /// <summary>
        /// "null" literal.
        /// </summary>
        Null,

        /// <summary>
        /// String literal.
        /// </summary>
        String,

        /// <summary>
        /// Number literal.
        /// </summary>
        Number
    }
}
