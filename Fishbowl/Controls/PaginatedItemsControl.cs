namespace FacebookClient
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Samples.KMoore.WPFSamples.ListPager;
    using System.Collections.Generic;

    public class PaginatedItemsControl : ItemsControl
    {
        /// <summary>
        /// The items we actually display in the ItemsControl by feeding into ItemsControl.ItemsSource.
        /// </summary>
        private ObservableCollection<object> pageCollection = new ObservableCollection<object>();

        public static readonly DependencyProperty PaginatedItemsSourceProperty = DependencyProperty.Register(
            "PaginatedItemsSource",
            typeof(IEnumerable),
            typeof(PaginatedItemsControl),
            new FrameworkPropertyMetadata(
                (d, e) => ((PaginatedItemsControl)d).OnPaginatedItemsSourceChanged(e)));

        public IEnumerable PaginatedItemsSource
        {
            get { return (IEnumerable)GetValue(PaginatedItemsSourceProperty); }
            set { SetValue(PaginatedItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty CurrentItemProperty = DependencyProperty.Register(
            "CurrentItem",
            typeof(object),
            typeof(PaginatedItemsControl));

        public object CurrentItem
        {
            get { return GetValue(CurrentItemProperty); }
            set { SetValue(CurrentItemProperty, value); }
        }

        public UIListPager ListPager { get; private set; }

        public PaginatedItemsControl()
        {
            this.SizeChanged += OnSizeChanged;
            this.ItemsSource = pageCollection;
            this.ListPager = new UIListPager();

	        this.ListPager.CurrentPageChanged += OnListPagerCurrentPageChanged;

            this.Unloaded += OnUnloaded;
            this.Loaded += OnLoaded;
        }

        public void OnUnloaded(object sender, EventArgs e)
        {
            var ncc = PaginatedItemsSource as INotifyCollectionChanged;
            if (ncc != null)
            {
                ncc.CollectionChanged -= OnPaginatedItemsCollectionChanged;
            }
        }

        public void OnLoaded(object sender, EventArgs e)
        {
            var ncc = PaginatedItemsSource as INotifyCollectionChanged;
            if (ncc != null)
            {
                ncc.CollectionChanged += OnPaginatedItemsCollectionChanged;
            }

            this.UpdatePager();

            if (this.PaginatedItemsSource != null)
            {
                this.CurrentItem = PaginatedItemsSource.OfType<object>().FirstOrDefault();
            }
        }

        /// <summary>
        /// When ListPager.CurrentPage changes, we incrementally update our private page collection
        /// to avoid completely refreshing all of the items. (ListPager isn't smart enough to do this.)
        /// </summary>
        private void OnListPagerCurrentPageChanged(object sender, EventArgs e)
        {
            // This is all O(n^2) and bad. We have to work around ListPager's limitations.
            // Refactor this class so it doesn't rely on ListPager if there's time.

            for (int i = 0; i < this.pageCollection.Count; i++)
            {
                if (!this.ListPager.CurrentPage.Contains(this.pageCollection[i]))
                {
                    this.pageCollection.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < this.ListPager.CurrentPage.Count; i++)
            {
                if (!this.pageCollection.Contains(this.ListPager.CurrentPage[i]))
                {
                    this.pageCollection.Insert(i, this.ListPager.CurrentPage[i]);
                }
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                if (this.ListPager.NextCommand.CanExecute(null))
                {
                    this.ListPager.NextCommand.Execute(null);
                }
            }
            else
            {
                if (this.ListPager.PreviousCommand.CanExecute(null))
                {
                    this.ListPager.PreviousCommand.Execute(null);
                }
            }

            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            foreach (object item in this.Items)
            {
                UIElement container = base.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
                if (container != null && container.IsMouseOver)
                {
                    this.CurrentItem = item;
                    break;
                }
            }
        }

        private void OnPaginatedItemsSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded)
            {
                INotifyCollectionChanged oldValue = e.OldValue as INotifyCollectionChanged;
                if (oldValue != null)
                {
                    oldValue.CollectionChanged -= OnPaginatedItemsCollectionChanged;
                }

                INotifyCollectionChanged newValue = e.NewValue as INotifyCollectionChanged;
                if (newValue != null)
                {
                    newValue.CollectionChanged += OnPaginatedItemsCollectionChanged;
                }

                UpdatePager();

                if (PaginatedItemsSource != null)
                {
                    CurrentItem = PaginatedItemsSource.OfType<object>().FirstOrDefault();
                }
            }
        }

        private void OnPaginatedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdatePager();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _CalculatePageSize();
        }

        private void _CalculatePageSize()
        {
            FrameworkElement firstItem = base.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;

            if (firstItem != null)
            {
                firstItem.UpdateLayout();
                //firstItem.Measure(new Size(this.ActualWidth, this.ActualHeight));
                //firstItem.Arrange(new Rect(new Size(this.ActualWidth, this.ActualHeight)));

                int numRows = Math.Max(1, (int)(this.ActualHeight / firstItem.RenderSize.Height));
                int numCols = Math.Max(1, (int)(this.ActualWidth / firstItem.RenderSize.Width));

                this.ListPager.PageSize = numRows * numCols;
            }
        }

        private void UpdatePager()
        {
            if (this.PaginatedItemsSource != null)
            {
                this.ListPager.ItemsSource = new List<object>(PaginatedItemsSource.OfType<object>());
            }
            else
            {
                this.ListPager.ItemsSource = null;
            }
            _CalculatePageSize();
        }
    }
}
