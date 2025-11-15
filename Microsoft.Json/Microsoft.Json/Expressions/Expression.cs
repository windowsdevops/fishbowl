//
// Copyright Microsoft Corporation.  All rights reserved.
// 

namespace Microsoft.Json.Expressions
{
    using System.Collections.Generic;
    using System.Linq;
    using Parser;

    /// <summary>
    /// Base type for JSON expression tree objects.
    /// </summary>
    public abstract class Expression
    {
        #region Properties

        /// <summary>
        /// Gets the type of the JSON expression tree node.
        /// </summary>
        public abstract ExpressionType NodeType { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the JSON fragment corresponding to the expression tree node.
        /// </summary>
        /// <returns>JSON fragment corresponding to the expression tree node.</returns>
        public abstract override string ToString();

        #endregion

        #region Factory methods

        /// <summary>
        /// Parses the given JSON text and returns an expression tree representing the JSON expression.
        /// </summary>
        /// <param name="input">JSON text to be parsed.</param>
        /// <returns>Expression tree representing the JSON expression.</returns>
        /// <remarks>See RFC 4627 for more information. This parses the production specified in section 2: <code>JSON-text = object / array</code>.</remarks>
        public static Expression Parse(string input)
        {
            return Parser.Parse(input);
        }

        /// <summary>
        /// Creates an expression tree node representing an object.
        /// </summary>
        /// <param name="members">Members of the object, represented as key-value pairs.</param>
        /// <returns>Object expression tree node for the given key-value member pairs.</returns>
        /// <remarks>The order of the members is not defined.</remarks>
        public static ObjectExpression Object(Dictionary<string, Expression> members)
        {
            return new ObjectExpression(members);
        }

        /// <summary>
        /// Creates an expression tree node representing an array.
        /// </summary>
        /// <param name="values">Expressions for the array elements.</param>
        /// <returns>Array expression tree node for the given elements.</returns>
        public static ArrayExpression Array(IEnumerable<Expression> values)
        {
            return Array(values.ToArray());
        }

        /// <summary>
        /// Creates an expression tree node representing an array.
        /// </summary>
        /// <param name="values">Expressions for the array elements.</param>
        /// <returns>Array expression tree node for the given elements.</returns>
        public static ArrayExpression Array(params Expression[] values)
        {
            return new ArrayExpression(values);
        }

        /// <summary>
        /// Creates an expression tree node representing a constant Boolean value.
        /// </summary>
        /// <param name="value">Boolean value.</param>
        /// <returns>Constant expression tree node for the given Boolean value.</returns>
        public static ConstantExpression Boolean(bool value)
        {
            return new ConstantExpression(ExpressionType.Boolean, value);
        }

        /// <summary>
        /// Creates an expression tree node representing a constant numeric value.
        /// </summary>
        /// <param name="value">Numeric value textual representation.</param>
        /// <returns>Constant expression tree node for the given number.</returns>
        /// <remarks>JSON numbers don't have a precision limit; we use a string to cover all accepted numeric tokens.</remarks>
        public static ConstantExpression Number(string value)
        {
            return new ConstantExpression(ExpressionType.Number, value);
        }

        /// <summary>
        /// Creates an expression tree node representing a constant string value.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <returns>Constant expression tree node for the given string.</returns>
        public static ConstantExpression String(string value)
        {
            return new ConstantExpression(ExpressionType.String, value);
        }

        /// <summary>
        /// Creates an expression tree node representing the null value.
        /// </summary>
        /// <returns>Null value expression tree node.</returns>
        public static ConstantExpression Null()
        {
            return ConstantExpression.NULL;
        }

        #endregion
    }
}
