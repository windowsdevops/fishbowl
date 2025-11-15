//
// Copyright Microsoft Corporation.  All rights reserved.
// 

namespace Microsoft.Json.Expressions
{
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Expression tree node representing a JSON constant value.
    /// </summary>
    public sealed class ConstantExpression : Expression
    {
        #region Static fields

        /// <summary>
        /// Singleton expression node object for a null value.
        /// </summary>
        internal static ConstantExpression NULL = new ConstantExpression(ExpressionType.Null, null);

        #endregion

        #region Instance fields

        /// <summary>
        /// Type of the constant represented by this expression tree node.
        /// Supported valued are Boolean, Number, String or Null.
        /// </summary>
        private ExpressionType _type;

        /// <summary>
        /// Value of the constant represented by this expression tree node.
        /// </summary>
        private object _value;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new constant expression tree node with the given constant type and value.
        /// </summary>
        /// <param name="type">Type of the constant. Supported valued are Boolean, Number, String and Null.</param>
        /// <param name="value">Value of the constant.</param>
        internal ConstantExpression(ExpressionType type, object value)
        {
            Debug.Assert(type == ExpressionType.Boolean || type == ExpressionType.Number || type == ExpressionType.String || type == ExpressionType.Null, "Unexpected expression tree type for constant.");
            _type = type;
            _value = value;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value of constant represented by this expression tree node.
        /// </summary>
        public object Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Gets the type of the JSON expression tree node.
        /// </summary>
        public override ExpressionType NodeType
        {
            get { return _type; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the JSON fragment corresponding to the expression tree node.
        /// </summary>
        /// <returns>JSON fragment corresponding to the expression tree node.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        public override string ToString()
        {
            switch (_type)
            {
                case ExpressionType.Null:
                    return "null";
                case ExpressionType.Boolean:
                    return _value.ToString().ToLowerInvariant();
                case ExpressionType.Number:
                    return _value.ToString();
                case ExpressionType.String:
                    {
                        var sb = new StringBuilder();
                        sb.Append(_value)
                          .Replace("\\", "\\\\")
                          .Replace("/", "\\/")
                          .Replace("\"", "\\\"")
                          .Replace("\b", "\\b")
                          .Replace("\f", "\\f")
                          .Replace("\t", "\\t")
                          .Replace("\r", "\\r")
                          .Replace("\n", "\\n");

                        return "\"" + sb.ToString() + "\"";
                    }
                default:
                    Debug.Assert(false, "Incomplete switch on ExpressionType.");
                    return _type.ToString();
            }
        }

        #endregion
    }
}
