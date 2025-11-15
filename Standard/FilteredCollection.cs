using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Standard
{
    internal class FilteredCollection<T> : IList<T>, INotifyCollectionChanged where T : class
    {
        private readonly IList<T> _source;
        private readonly IList<bool> _filterList;
        private readonly bool _areItemsNotifiable;

        public FilteredCollection(IList<T> sourceCollection)
        {
            Verify.IsNotNull(sourceCollection, "sourceCollection");
            var sourceChanged = sourceCollection as INotifyCollectionChanged;
            if (sourceChanged == null)
            {
                throw new ArgumentException("The source of the filtered collection must support INotifyCollectionChanged.", "sourceCollection");
            }

            _areItemsNotifiable = typeof(T).GetInterface(typeof(INotifyPropertyChanged).Name) != null;
            _filterList = new List<bool>(sourceCollection.Count);

            _source = sourceCollection;
            sourceChanged.CollectionChanged += _OnSourceCollectionChanged;
        }

        private event Predicate<T> _Filters;

        public event Predicate<T> Filters {
            add
            {
                _Filters += value;
            }
            remove
            {
                _Filters -= value;
            }
        }

        private void _RefreshFilterList()
        {
            if (_Filters == null)
            {
                _filterList.Clear();
                return;
            }

            _Filters.GetInvocationList();
        }

        private void _OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        { }

        #region IList<T> Members

        public int IndexOf(T item)
        {
            if (_filterList.Count == 0)
            {
                return _source.IndexOf(item);
            }

            Assert.AreEqual(_source.Count, _filterList.Count);
            int filteredIndex = 0;
            for (int i = 0; i < _source.Count; ++i)
            {
                Assert.IsNotNull(_source[i]);
                if (_filterList[i])
                {
                    if (_source[i].Equals(item))
                    {
                        return filteredIndex;
                    }
                    ++filteredIndex;
                }
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get { return _filteredList[index]; }
            set { throw new NotSupportedException(); }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
