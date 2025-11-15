namespace FacebookClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Navigation;
    using ClientManager;
    using Contigo;
    using Microsoft.Windows.Shell;
    using Standard;

    /// <summary>
    /// Interaction logic for NotificationsControl.xaml
    /// </summary>
    public partial class NotificationsControl : UserControl
    {
        private readonly NotifyingList<Notification> _currentNotifications;

        public static readonly DependencyProperty IsDisplayedProperty = DependencyProperty.Register("IsDisplayed", typeof(bool), typeof(NotificationsControl));

        public bool IsDisplayed
        {
            get { return (bool)GetValue(IsDisplayedProperty); }
            set { SetValue(IsDisplayedProperty, value); }
        }

        public static readonly RoutedCommand CloseCommand = new RoutedCommand("Close", typeof(NotificationsControl));

        public NotificationsControl()
        {
            InitializeComponent();

            _currentNotifications = new NotifyingList<Notification>();
            _currentNotifications.ItemPropertyChanged += (sender, e) => _OnNotificationsChanged(this, null);

            CommandBindings.Add(new CommandBinding(CloseCommand, new ExecutedRoutedEventHandler((sender, e) => IsDisplayed = false)));

            Loaded += (sender, e) => ServiceProvider.ViewManager.Notifications.CollectionChanged += _OnNotificationsChanged;
            Unloaded += (sender, e) =>
            {
                ServiceProvider.ViewManager.Notifications.CollectionChanged -= _OnNotificationsChanged;
                _currentNotifications.Clear();
            };
        }

        private static string _GetFirstLine(string source)
        {
            if (source == null)
            {
                return null;
            }

            string ret = source.Trim().Split(new [] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (ret != null)
            {
                ret = ret.Trim();

                if (ret.Length > Win32Value.MAX_PATH)
                {
                    ret = ret.Substring(0, (int)Win32Value.MAX_PATH - 4) + "…";
                }
            }

            return ret;
        }

        private void _OnNotificationsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ServiceProvider.ViewManager == null || ServiceProvider.ViewManager.Notifications == null)
            {
                return;
            }

            _currentNotifications.Clear();
            var notificationTaskList = new List<JumpItem>();
            foreach (Notification notification in from n in ServiceProvider.ViewManager.Notifications orderby n.Created ascending select n)
            {
                _currentNotifications.Add(notification);

                string argument = null;

                // Get the default URL from the notification.
                var htc = new HyperlinkTextContent { Text = notification.Title };
                Uri notifyUri = htc.DefaultUri;
                if (notifyUri != null)
                {
                    argument = "-uri:" + notifyUri.OriginalString;
                }

                string title = _GetFirstLine(notification.TitleText);

                if (title != null)
                {
                    notificationTaskList.Add(new JumpTask
                    {
                        Arguments = argument,
                        CustomCategory = notification is FriendRequestNotification ? "Friend Requests" : "Notifications",
                        // Shell silently fails to accept multi-line JumpTasks so we need to trim the strings ourselves.
                        Description = _GetFirstLine(notification.DescriptionText),
                        Title = title,
                    });
                }
            }

            JumpList jumpList = JumpList.GetJumpList(Application.Current);

            Assert.IsNotNull(jumpList);
            jumpList.JumpItems.RemoveAll(item => item.CustomCategory == "Notifications");
            jumpList.JumpItems.RemoveAll(item => item.CustomCategory == "Friend Requests");
            jumpList.JumpItems.AddRange(notificationTaskList);

            try
            {
                jumpList.JumpItemsRemovedByUser += _OnJumpItemsRemovedByUser;
                jumpList.Apply();
            }
            finally
            {
                jumpList.JumpItemsRemovedByUser -= _OnJumpItemsRemovedByUser;
            }
        }

        // Something isn't working right here... If the user dismissed the item in the jumplist we should respect that and mark it as read.
        // In my testing I'm not sure why this event isn't getting raised.  I'm opening a bug to look into this later.
        private void _OnJumpItemsRemovedByUser(object sender, JumpItemsRemovedEventArgs e)
        {
            foreach (var item in from removedItem in e.RemovedItems where removedItem.CustomCategory == "Notifications" select removedItem)
            {
            }
        }

        private void _OnNotificationRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var handler = RequestNavigate;
            if (handler != null)
            {
                handler(sender, e); 
            }
        }

        public event RequestNavigateEventHandler RequestNavigate;
    }
}
