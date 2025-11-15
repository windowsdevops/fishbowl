using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClientManager;
using Contigo;

namespace FacebookClient
{
    public enum SortOrderMode
    {
        None,
        Contacts,
        Albums,
    }

    public class SortControlItem
    {
        public string Name { get; set; }
        public ImageSource Icon { get; set; }
        public PhotoAlbumSortOrder? PhotoAlbumSortOrder { get; set; }
        public ContactSortOrder? ContactSortOrder { get; set; }
    }

    public partial class SortControl : UserControl
    {
        private readonly List<SortControlItem> _AlbumSortOrders = new List<SortControlItem>
        {
            new SortControlItem{ Name = "Last Updated", PhotoAlbumSortOrder = PhotoAlbumSortOrder.DescendingByUpdate },
            new SortControlItem { Name = "Title", PhotoAlbumSortOrder = PhotoAlbumSortOrder.AscendingByTitle },
            new SortControlItem { Name = "Owner (by Name)", PhotoAlbumSortOrder = PhotoAlbumSortOrder.AscendingByFriend },
            new SortControlItem { Name = "Owner (by Interest Level)", PhotoAlbumSortOrder = PhotoAlbumSortOrder.DescendingByInterestLevel },
        };

        private readonly List<SortControlItem> _FriendSortOrders = new List<SortControlItem>
        {
            new SortControlItem{ Name = "Name (by Family)", ContactSortOrder = ContactSortOrder.AscendingByLastName },
            new SortControlItem{ Name = "Name (by Display)", ContactSortOrder = ContactSortOrder.AscendingByDisplayName },
            new SortControlItem{ Name = "Last Status Update", ContactSortOrder = ContactSortOrder.DescendingByRecentActivity },
            new SortControlItem{ Name = "Upcoming Birthdays", ContactSortOrder = ContactSortOrder.AscendingByBirthday },
            new SortControlItem{ Name = "Interest Level", ContactSortOrder = ContactSortOrder.DescendingByInterestLevel },
        };

        public static readonly DependencyProperty TitleTextProperty = DependencyProperty.Register(
            "TitleText",
            typeof(string),
            typeof(SortControl),
            new PropertyMetadata(
                "",
                (d, e) => {},
                (d, e) => ((SortControl)d)._CoerceTitleTextValue(e)));

        public string TitleText
        {
            get { return (string)GetValue(TitleTextProperty); }
            set { SetValue(TitleTextProperty, value); }
        }

        private object _CoerceTitleTextValue(object value)
        {
            var s = value as string;
            if (s == null)
            {
                return "";
            }
            return s;
        }

        public static readonly DependencyProperty SortOrderModeProperty = DependencyProperty.Register(
            "SortOrderMode",
            typeof(SortOrderMode),
            typeof(SortControl),
            new FrameworkPropertyMetadata(
                SortOrderMode.None,
                (d, e) => ((SortControl)d)._OnSortOrderModeChanged((SortOrderMode)e.NewValue)));

        private void _OnSortOrderModeChanged(SortOrderMode sortOrderMode)
        {
            if (SortOrderMode.Contacts == sortOrderMode)
            {
                SortTabs.ItemsSource = _FriendSortOrders;
            }
            else
            {
                SortTabs.ItemsSource = _AlbumSortOrders;
            }
            _SelectActiveSortOrder();
        }

        public SortOrderMode SortOrderMode
        {
            get { return (SortOrderMode)GetValue(SortOrderModeProperty); }
            set { SetValue(SortOrderModeProperty, value); }
        }

        public SortControl()
        {
            InitializeComponent();
        }

        private void _SelectActiveSortOrder()
        {
            if (SortOrderMode == SortOrderMode.Contacts)
            {
                ContactSortOrder sortOrder = ServiceProvider.ViewManager.ActiveContactSortOrder;
                foreach (SortControlItem item in SortTabs.ItemsSource)
                {
                    if (item.ContactSortOrder == sortOrder)
                    {
                        SortTabs.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                PhotoAlbumSortOrder sortOrder = ServiceProvider.ViewManager.ActivePhotoAlbumSortOrder;
                foreach (SortControlItem item in SortTabs.ItemsSource)
                {
                    if (item.PhotoAlbumSortOrder == sortOrder)
                    {
                        SortTabs.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tabItem = button.TemplatedParent as TabItem;
            tabItem.IsSelected = true;

            SortControlItem item = (SortControlItem)button.Tag;
            if (item.PhotoAlbumSortOrder != null)
            {
                ServiceProvider.ViewManager.ActionCommands.SetSortOrderCommand.Execute(item.PhotoAlbumSortOrder.Value);
            }
            else if (item.ContactSortOrder != null)
            {
                ServiceProvider.ViewManager.ActionCommands.SetSortOrderCommand.Execute(item.ContactSortOrder.Value);
            }
        }

        private void _OnUploadWizardButtonClicked(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ShowUploadWizard();
        }

    }
}
