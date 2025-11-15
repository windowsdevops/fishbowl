//
// Copyright Microsoft Corporation.  All rights reserved.
// 

namespace System.Collections.Generic
{
    using System;
    
    /// <summary>
    /// Read-only generic dictionary type.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public sealed class ReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        #region Private fields

        /// <summary>
        /// Underlying dictionary.
        /// </summary>
        private readonly IDictionary<TKey, TValue> _data;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new read-only dictionary based on the given keys and values.
        /// </summary>
        /// <param name="data">Dictionary with the keys and values to be stored inside the read-only dictionary.</param>
        internal ReadOnlyDictionary(IDictionary<TKey, TValue> data)
        {
            _data = data;
        }

        #endregion

        #region Indexers

        /// <summary>
        /// Gets the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <returns>The element with the specified key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                return _data[key];
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a collection with the keys of the dictionary.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get
            {
                return _data.Keys;
            }
        }

        /// <summary>
        /// Gets a collection with the values stored in the dictionary.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                return _data.Values;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the dictionary contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <returns>true if the dictionary contains an element with the key; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _data.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns an enumerator that enumerates over key/value pairs in the dictionary.
        /// </summary>
        /// <returns>Enumerator that enumerates over key/value pairs in the dictionary.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that enumerates over key/value pairs in the dictionary.
        /// </summary>
        /// <returns>Enumerator that enumerates over key/value pairs in the dictionary.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
