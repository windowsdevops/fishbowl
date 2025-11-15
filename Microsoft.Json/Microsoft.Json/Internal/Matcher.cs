//
// Copyright Microsoft Corporation.  All rights reserved.
// 

using System;
using System.Text.RegularExpressions;

namespace Microsoft.Json.Internal
{
    /// <summary>
    /// Multi-way regular expression branch helper type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class Matcher<T>
    {
        #region Private fields

        /// <summary>
        /// Input to be matched.
        /// </summary>
        private string _input;

        /// <summary>
        /// Constructor to turn the match into a production object.
        /// </summary>
        private Func<string, T> _ctor;

        /// <summary>
        /// Regular expression match for the input.
        /// </summary>
        private Match _match;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new match clause.
        /// </summary>
        /// <param name="input">Input to be matched.</param>
        /// <param name="regex">Regular expression to match.</param>
        /// <param name="ctor">Constructor to turn the match into a production object.</param>
        internal Matcher(string input, Regex regex, Func<string, T> ctor)
        {
            _input = input;
            _ctor = ctor;
            _match = regex.Match(input);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the current matcher successfully matches the input.
        /// </summary>
        public bool Success
        {
            get { return _match.Success; }
        }

        /// <summary>
        /// Gets the production value for the match; only to be called when Success is true.
        /// </summary>
        public T Value
        {
            get { return _ctor(_match.Value); }
        }

        /// <summary>
        /// Gets the length of the match; only to be called when Success is true.
        /// </summary>
        public int Length
        {
            get { return _match.Length; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a matcher that evaluates the input against the given regular expression in case the current matcher didn't produce a result.
        /// </summary>
        /// <param name="regex">Regular expression to match.</param>
        /// <param name="ctor">Constructor to turn the match into a production object.</param>
        /// <returns>Matcher object for subsequent regular expression matches, evaluated when the current match didn't produce a result.</returns>
        public Matcher<T> OrWith(Regex regex, Func<string, T> ctor)
        {
            if (!Success)
            {
                _ctor = ctor;
                _match = regex.Match(_input);
            }

            return this;
        }

        #endregion
    }
}
