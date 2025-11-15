//
// Copyright Microsoft Corporation.  All rights reserved.
// 

namespace Microsoft.Json.Expressions
{
    using System;

    /// <summary>
    /// Expression tree types.
    /// </summary>
    public enum ExpressionType
    {
        /// <summary>
        /// JSON object.
        /// </summary>
        Object,

        /// <summary>
        /// JSON array.
        /// </summary>
        Array,

        /// <summary>
        /// JSON Boolean constant value.
        /// </summary>
        Boolean,

        /// <summary>
        /// JSON Boolean numeric value.
        /// </summary>
        Number,

        /// <summary>
        /// JSON Boolean string value.
        /// </summary>
        String,

        /// <summary>
        /// JSON null value.
        /// </summary>
        Null
    }
}
