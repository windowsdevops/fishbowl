namespace Contigo
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Windows.Threading;
    using Standard;

    /// <summary>
    /// Support interface for EnumerateAndAddNotify.
    /// </summary>
    internal interface IFacebookCollection
    {
        IEnumerable EnumerateAndAddNotify(NotifyCollectionChangedEventHandler collectionChanged);
    }

    /// <summary>
    /// A read-only collection of Facebook data.
    /// </summary>
    /// <remarks>This class must only be accessed on the thread on which it was created.</remarks>
    public class FacebookCollection<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged, IFacebookObject, IFacebookCollection where T : class
    {
        private MergeableCollection<T> _sourceCollection;
        private readonly Dispatcher _dispatcher;
        private event PropertyChangedEventHandler _propertyChanged;
        private event NotifyCollectionChangedEventHandler _sourceCollectionChanged;

        // Derived classes should not directly expose a constructor that calls through to this.
        // To ensure that this is really what the caller wants a static factory method is preferred.
        internal FacebookCollection(IEnumerable<T> items)
        {
            Assert.IsFalse(items is MergeableCollection<T>);
            Verify.IsNotNull(items, "items");
            _dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            _sourceCollection = new MergeableCollection<T>(items);
        }

        internal FacebookCollection(MergeableCollection<T> rawCollection, FacebookService service)
        {
            Verify.IsNotNull(rawCollection, "rawCollection");
            Verify.IsNotNull(service, "service");

            Assert.IsTrue(service.Dispatcher.CheckAccess());
            _dispatcher = service.Dispatcher;

            _sourceCollection = rawCollection;
            SourceService = service;
        }

        /// <summary>
        /// Be careful of adding new uses of this.
        /// This is only intended to be called within the constructor of derived classes.
        /// </summary>
        internal void ReplaceSourceCollection(MergeableCollection<T> newSource)
        {
            Assert.IsTrue(_sourceCollectionChanged == null);
            Assert.IsNotNull(newSource);
            _sourceCollection = newSource; 
        }

        internal bool IsFrozen { get { return SourceService == null; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void VerifyAccess()
        {
            if (!IsFrozen)
            {
                SourceService.Dispatcher.VerifyAccess();
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                if (IsFrozen)
                {
                    return;
                }

                if (_sourceCollectionChanged == null)
                {
                    _sourceCollection.CollectionChanged += _OnSourceCollectionChanged;
                }
                _sourceCollectionChanged += value;
            }
            remove
            {
                if (IsFrozen)
                {
                    return;
                }

                _sourceCollectionChanged -= value;
                if (_sourceCollectionChanged == null)
                {
                    _sourceCollection.CollectionChanged -= _OnSourceCollectionChanged;
                }
            }
        }

        private void _OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var handler = _sourceCollectionChanged;
            if (handler != null)
            {
                _dispatcher.BeginInvoke((Action)delegate
                {
                    handler(this, e);
                    _NotifyPropertyChanged("Count");
                });
            }            
        }

        public static IEnumerable EnumerateAndAddNotify(IEnumerable collection, NotifyCollectionChangedEventHandler collectionChanged)
        {
            IFacebookCollection ifb = collection as IFacebookCollection;
            if (ifb != null)
            {
                return ifb.EnumerateAndAddNotify(collectionChanged);
            }
            else
            {
                ((INotifyCollectionChanged)collection).CollectionChanged += collectionChanged;
                return collection;
            }
        }

        #region IFacebookCollection Members

        IEnumerable IFacebookCollection.EnumerateAndAddNotify(NotifyCollectionChangedEventHandler collectionChanged)
        {
            lock (_sourceCollection.SyncRoot)
            {
                List<T> items = new List<T>(_sourceCollection);
                CollectionChanged += collectionChanged;
                return items;
            }
        }

        #endregion

        #region IList<T> Members

        public int IndexOf(T item)
        {
            lock (_sourceCollection.SyncRoot)
            {
                return _sourceCollection.IndexOf(item);
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_sourceCollection.SyncRoot)
                {
                    Assert.Implies(SourceService != null, () => SourceService.Dispatcher.CheckAccess());
                    return _sourceCollection[index];
                }
            }
            set { throw new NotSupportedException(); }
        }

        #region Unsupported IList<T> Modifying Members

        void IList<T>.Insert(int index, T item) { throw new NotSupportedException(); }
        void IList<T>.RemoveAt(int index) { throw new NotSupportedException(); }

        #endregion

        #endregion

        #region ICollection<T> Members

        public bool Contains(T item)
        {
            lock (_sourceCollection.SyncRoot)
            {
                return _sourceCollection.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_sourceCollection.SyncRoot)
            {
                _sourceCollection.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (_sourceCollection.SyncRoot)
                {
                    return _sourceCollection.Count;
                }
            }
        }
        public bool IsReadOnly { get { return true; } }

        #region Unsupported IList<T> Modifying Members

        void ICollection<T>.Add(T item) { throw new NotSupportedException(); }
        void ICollection<T>.Clear() { throw new NotSupportedException(); }
        bool ICollection<T>.Remove(T item) { throw new NotSupportedException(); }

        #endregion

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            Assert.Implies(SourceService != null, () => SourceService.Dispatcher.CheckAccess());

            T[] copy;
            lock (_sourceCollection.SyncRoot)
            {
                copy = new T[_sourceCollection.Count];
                _sourceCollection.CopyTo(copy, 0);
            }

            return ((IEnumerable<T>)copy).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IFacebookObject Members

        FacebookService IFacebookObject.SourceService { get; set; }

        private FacebookService SourceService
        {
            get { return ((IFacebookObject)this).SourceService; }
            set { ((IFacebookObject)this).SourceService = value; }
        }

        #endregion

        private void _NotifyPropertyChanged(string propertyName)
        {
            Assert.IsNeitherNullNorEmpty(propertyName);
            var handler = _propertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void _IgnorableCollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {}

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (_propertyChanged == null)
                {
                    CollectionChanged += _IgnorableCollectionChangedHandler;
                }
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
                if (_propertyChanged == null)
                {
                    CollectionChanged -= _IgnorableCollectionChangedHandler;
                }
            }
        }

        #endregion
    }
}
