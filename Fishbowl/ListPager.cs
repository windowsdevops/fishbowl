namespace Microsoft.Samples.KMoore.WPFSamples.ListPager
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Windows;

    public class ListPager : DependencyObject
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(IList),
            typeof(ListPager),
            new PropertyMetadata(
                null,
                (d, e) => ((ListPager)d).ItemsSourcePropertyChanged()),
            value => !(value is INotifyCollectionChanged));

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public event EventHandler ItemsSourceChanged;

        protected virtual void OnItemsSourceChanged(EventArgs e)
        {
            EventHandler handler = ItemsSourceChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void ItemsSourcePropertyChanged()
        {
            resetPage(CurrentPageIndex);
            CoerceValue(CurrentPageIndexProperty);
            OnItemsSourceChanged(EventArgs.Empty);
        }

        private static readonly DependencyPropertyKey CurrentPagePropertyKey = DependencyProperty.RegisterReadOnly(
            "CurrentPage",
            typeof(IList),
            typeof(ListPager),
            new PropertyMetadata(new object[0]));

        public static readonly DependencyProperty CurrentPageProperty = CurrentPagePropertyKey.DependencyProperty;

        public IList CurrentPage
        {
            get { return (IList)GetValue(CurrentPageProperty); }
        }


        public static readonly DependencyProperty CurrentPageIndexProperty = DependencyProperty.Register(
            "CurrentPageIndex",
            typeof(int),
            typeof(ListPager),
            new PropertyMetadata(0, CurrentPageIndexPropertyChanged, CoerceCurrentPageIndex),
            new ValidateValueCallback(ValidateCurrentPageIndex));

        public int CurrentPageIndex
        {
            get { return (int)GetValue(CurrentPageIndexProperty); }
            set { SetValue(CurrentPageIndexProperty, value); }
        }

        protected virtual void OnCurrentPageIndexChanged(EventArgs e)
        {
            EventHandler handler = CurrentPageIndexChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler CurrentPageIndexChanged;

        private static bool ValidateCurrentPageIndex(object value)
        {
            int val = (int)value;
            if (val < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static void CurrentPageIndexPropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            ((ListPager)element).CurrentPageIndexPropertyChanged((int)args.NewValue);
        }

        private void CurrentPageIndexPropertyChanged(int newCurrentPageIndex)
        {
            resetPage(newCurrentPageIndex);
            OnCurrentPageIndexChanged(EventArgs.Empty);
        }

        private static object CoerceCurrentPageIndex(DependencyObject element, object value)
        {
            return ((ListPager)element).CoerceCurrentPageIndex((int)value);
        }

        private int CoerceCurrentPageIndex(int value)
        {
            return Math.Min(value, MaxPageIndex);
        }

        public int MaxPageIndex
        {
            get
            {
                return (int )GetValue(MaxPageIndexProperty) ;
            }
        }

        private static readonly DependencyPropertyKey MaxPageIndexPropertyKey = DependencyProperty.RegisterReadOnly("MaxPageIndex",
            typeof(int), typeof(ListPager), new PropertyMetadata(0));

        public static readonly DependencyProperty MaxPageIndexProperty = MaxPageIndexPropertyKey.DependencyProperty;

        public int PageCount
        {
            get
            {
                return (int )GetValue(PageCountProperty) ;
            }
        }

        private static readonly DependencyPropertyKey PageCountPropertyKey = DependencyProperty.RegisterReadOnly(
            "PageCount", typeof(int), typeof(ListPager), new PropertyMetadata(0));

        public static readonly DependencyProperty PageCountProperty = PageCountPropertyKey.DependencyProperty;

        public int PageSize
        {
            get { return (int)GetValue(PageSizeProperty); }
            set { SetValue (PageSizeProperty, value) ; }
        }

        public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register(
            "PageSize",
            typeof(int),
            typeof(ListPager),
            new PropertyMetadata(
                10,
                (d, e) => ((ListPager)d).PageSizePropertyChanged()),
            value => (int)value >= 1);

        public event EventHandler PageSizeChanged;

        protected virtual void OnPageSizeChanged(EventArgs e)
        {
            EventHandler handler = PageSizeChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void PageSizePropertyChanged()
        {
            resetPage(CurrentPageIndex);
            CoerceValue(CurrentPageIndexProperty);
            OnPageSizeChanged(EventArgs.Empty);
        }

        public event EventHandler CurrentPageChanged;

        private void resetPage(int currentPageIndex)
        {
            IList items = this.ItemsSource;
            if (items == null || items.Count == 0)
            {
                SetValue(CurrentPagePropertyKey, CurrentPageProperty.DefaultMetadata.DefaultValue);
                SetValue(PageCountPropertyKey, 1);
                SetValue(MaxPageIndexPropertyKey, 0);
            }
            else
            {
                PagedList pagedList = new PagedList(items, PageSize, currentPageIndex);
                SetValue(CurrentPagePropertyKey, pagedList);
                SetValue(PageCountPropertyKey, 1 + (items.Count - 1) / PageSize);
                SetValue(MaxPageIndexPropertyKey, PageCount - 1);
            }

            if (CurrentPageChanged != null)
            {
                CurrentPageChanged(this, new EventArgs());
            }
        }

        private class PagedList : IList<object>, IList
        {
            public PagedList(IList source, int pageSize, int pageNumber)
            {
                Debug.Assert(source != null);
                Debug.Assert(pageSize > 0);
                Debug.Assert(pageNumber >= 0);

                _source = source;
                _pageSize = pageSize;
                _pageNumber = pageNumber;
                _initialCount = source.Count;
            }

            private object GetItem(int index)
            {
                Debug.Assert(_source.Count == _initialCount);
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                return _source[_pageSize * _pageNumber + index];
            }

            public int Count
            {
                get
                {
                    Debug.Assert(_source.Count == _initialCount);
                    int minIndex = _pageSize * _pageNumber;
                    if (_source.Count > minIndex)
                    {
                        if (_pageSize * (_pageNumber + 1) <= _source.Count)
                        {
                            return _pageSize;
                        }
                        else
                        {
                            return _source.Count - minIndex;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            private readonly IList _source;
            private readonly int _pageSize;
            private readonly int _pageNumber;
            private int _initialCount;

            #region IList<object> Members

            public int IndexOf(object item)
            {
                for (int i = 0; i < Count; ++i)
                {
                    if (item == GetItem(i))
                    {
                        return i;
                    }
                }
                return -1;
            }

            public void Insert(int index, object item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            public object this[int index]
            {
                get { return GetItem(index); }
                set { throw new NotImplementedException(); }
            }

            #endregion

            #region ICollection<object> Members

            public void Add(object item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(object item)
            {
                return IndexOf(item) != -1;
            }

            public void CopyTo(object[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public bool Remove(object item)
            {
                throw new NotImplementedException();
            }

            #endregion

            #region IEnumerable<object> Members

            public IEnumerator<object> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region IList Members

            int IList.Add(object value)
            {
                throw new NotImplementedException();
            }

            public bool IsFixedSize
            {
                get { return true; }
            }

            void IList.Remove(object value)
            {
                throw new NotImplementedException();
            }

            #endregion

            #region ICollection Members

            public void CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }

            public bool IsSynchronized
            {
                get { return false; }
            }

            public object SyncRoot
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }

    }

}
