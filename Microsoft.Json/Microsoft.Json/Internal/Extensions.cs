//
// Copyright Microsoft Corporation.  All rights reserved.
// 

namespace Microsoft.Json.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Extension methods for internal use.
    /// </summary>
    static class Extensions
    {
        /// <summary>
        /// Checks whether a type is a nullable value type.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <returns>true if the type is a nullable value type (Nullable&lt;T&gt;); false otherwise.</returns>
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Entry-point for multi-way matching of a string against a set of regular expressions.
        /// </summary>
        /// <typeparam name="T">Result type for the production upon a match.</typeparam>
        /// <param name="input">Input to be matched.</param>
        /// <param name="regex">First regular expression to match.</param>
        /// <param name="ctor">Constructor to turn the match into a production object.</param>
        /// <returns>Matcher object for subsequent regular expression matches, evaluated when the first match didn't produce a result.</returns>
        public static Matcher<T> MatchWith<T>(this string input, Regex regex, Func<string, T> ctor)
        {
            return new Matcher<T>(input, regex, ctor);
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection, producing the element as an output parameter if the enumerator was advanced.
        /// </summary>
        /// <typeparam name="T">The type of objects to enumerate.</typeparam>
        /// <param name="enumerator">Enumerator to consume input from.</param>
        /// <param name="value">Value of the enumerator's Current property in case the MoveNext operation succeeded.</param>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public static bool TryMoveNext<T>(this IEnumerator<T> enumerator, out T value)
        {
            value = default(T);

            var res = enumerator.MoveNext();
            if (res)
                value = enumerator.Current;

            return res;
        }
    }
}
