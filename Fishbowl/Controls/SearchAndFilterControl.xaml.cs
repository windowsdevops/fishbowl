namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Collections.Generic;
    using System.Windows.Input;
    using ClientManager;
    using Contigo;
    using System.Collections.Specialized;

    /// <summary>
    /// Interaction logic for SearchAndFilterControl.xaml
    /// </summary>
    public partial class SearchAndFilterControl : UserControl
    {
        private static readonly DependencyPropertyKey ShowMorePropertyKey = DependencyProperty.RegisterReadOnly(
            "ShowMore",
            typeof(bool),
            typeof(SearchAndFilterControl),
            new FrameworkPropertyMetadata(
                false,
                (d, e) => FacebookClientApplication.ShowMoreNewsfeedFilters = (bool)e.NewValue));

        public static readonly DependencyProperty ShowMoreProperty = ShowMorePropertyKey.DependencyProperty;

        public bool ShowMore
        {
            get { return (bool)GetValue(ShowMoreProperty); }
            private set { SetValue(ShowMorePropertyKey, value); }
        }

        public SearchAndFilterControl()
        {
            InitializeComponent();

            Loaded += (sender, e) =>
            {
                SelectActiveNewsFeedFilter();
                ServiceProvider.ViewManager.NewsFeedFilters.CollectionChanged += OnNewsFeedFiltersCollectionChanged;
            };

            ShowMore = FacebookClientApplication.ShowMoreNewsfeedFilters;

            Unloaded += (sender, e) => ServiceProvider.ViewManager.NewsFeedFilters.CollectionChanged -= OnNewsFeedFiltersCollectionChanged;
        }

        private void OnNewsFeedFiltersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SelectActiveNewsFeedFilter();
        }

        private void SelectActiveNewsFeedFilter()
        {
            ActivityFilter activeFilter = ServiceProvider.ViewManager.ActiveNewsFeedFilter;
            if (activeFilter != null)
            {
                this.FilterTabs.SelectedItem = activeFilter;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tabItem = button.TemplatedParent as TabItem;
            tabItem.IsSelected = true;
        }

        private void MoreButtonClick(object sender, RoutedEventArgs e)
        {
            ShowMore = !ShowMore;
        }
    }
}
