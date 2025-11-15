//
// Copyright Microsoft Corporation.  All rights reserved.
// 

namespace Microsoft.Json.Expressions
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Expression tree node representing a JSON array.
    /// </summary>
    public sealed class ArrayExpression : Expression
    {
        #region Private fields

        /// <summary>
        /// Elements of the array represented by this expression tree node.
        /// </summary>
        private ReadOnlyCollection<Expression> _elements;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new array expression tree node object with the given elements.
        /// </summary>
        /// <param name="elements">Expression tree objects for the array elements.</param>
        internal ArrayExpression(Expression[] elements)
        {
            _elements = new ReadOnlyCollection<Expression>(elements);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the expression tree objects for the array elements.
        /// </summary>
        public ReadOnlyCollection<Expression> Elements
        {
            get { return _elements; }
        }

        /// <summary>
        /// Gets the type of the JSON expression tree node.
        /// </summary>
        public override ExpressionType NodeType
        {
            get { return ExpressionType.Array; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the JSON fragment corresponding to the expression tree node.
        /// </summary>
        /// <returns>JSON fragment corresponding to the expression tree node.</returns>
        public override string ToString()
        {
            return "[" + string.Join(", ", _elements.Select(element => element.ToString()).ToArray()) + "]";
        }

        #endregion
    }
}
