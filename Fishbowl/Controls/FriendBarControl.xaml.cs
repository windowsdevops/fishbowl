namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Collections.Specialized;
    using ClientManager;
    using Contigo;

    /// <summary>
    /// Interaction logic for FriendBarControl.xaml
    /// </summary>
    public partial class FriendBarControl : UserControl
    {
        public FriendBarControl()
        {
            InitializeComponent();
            FacebookContactCollection friends = ServiceProvider.ViewManager.Friends;

            if (friends.Count > 0)
            {
                this.FilmStrip.SelectedItem = friends[0];
            }
            else
            {
                ServiceProvider.ViewManager.Friends.CollectionChanged += new NotifyCollectionChangedEventHandler(Friends_CollectionChanged);
            }
        }

        public void SetActiveItem(object item)
        {
            this.FilmStrip.SelectedItem = item;
        }

        private void Friends_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FacebookContactCollection friends = ServiceProvider.ViewManager.Friends;

            // The FilmStrip won't display anything if SelectedItem doesn't get set.
            if (friends.Count > 0)
            {
                if (this.FilmStrip.SelectedItem == null)
                {
                    this.FilmStrip.SelectedItem = friends[0];
                }

                friends.CollectionChanged -= new NotifyCollectionChangedEventHandler(Friends_CollectionChanged);
            }
        }
    }
}
