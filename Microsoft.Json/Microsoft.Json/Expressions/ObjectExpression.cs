//
// Copyright Microsoft Corporation.  All rights reserved.
// 

namespace Microsoft.Json.Expressions
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Expression tree node representing a JSON object.
    /// </summary>
    public sealed class ObjectExpression : Expression
    {
        #region Private fields

        /// <summary>
        /// Members on the object.
        /// </summary>
        private ReadOnlyDictionary<string, Expression> _members;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new object expression tree node object with the given members.
        /// </summary>
        /// <param name="members">Object members.</param>
        internal ObjectExpression(Dictionary<string, Expression> members)
        {
            _members = new ReadOnlyDictionary<string, Expression>(members);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the members defined on the object.
        /// </summary>
        public ReadOnlyDictionary<string, Expression> Members
        {
            get { return _members; }
        }

        /// <summary>
        /// Gets the type of the JSON expression tree node.
        /// </summary>
        public override ExpressionType NodeType
        {
            get { return ExpressionType.Object; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the JSON fragment corresponding to the expression tree node.
        /// </summary>
        /// <returns>JSON fragment corresponding to the expression tree node.</returns>
        public override string ToString()
        {
            return "{" + string.Join(", ", _members.Select(value => "\"" + value.Key + "\": " + value.Value.ToString()).ToArray()) + "}";
        }

        #endregion
    }
}
