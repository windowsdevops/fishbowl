namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using ClientManager;
    using ClientManager.View;
    using System.Diagnostics;

    public class MainHeaderControl : Control
    {
        private RadioButton _homeNavigationButton;
        private RadioButton _friendsNavigationButton;
        private RadioButton _profileNavigationButton;
        private RadioButton _photoAlbumsNavigationButton;
        private TextBox _searchTextBox;

        public static readonly DependencyProperty NotificationControlProperty = DependencyProperty.Register(
            "NotificationControl",
            typeof(NotificationCountControl),
            typeof(MainHeaderControl));

        public NotificationCountControl NotificationControl
        {
            get { return (NotificationCountControl)GetValue(NotificationControlProperty); }
            set { SetValue(NotificationControlProperty, value); }
        }

        public static readonly DependencyProperty AreNotificationsToggledProperty = DependencyProperty.Register(
            "AreNotificationsToggled",
            typeof(bool),
            typeof(MainHeaderControl),
            new PropertyMetadata(
                false,
                (d,e) => ((MainHeaderControl)d)._OnAreNotificationsToggledChanged()));

        public bool AreNotificationsToggled
        {
            get { return (bool)GetValue(AreNotificationsToggledProperty); }
            set { SetValue(AreNotificationsToggledProperty, value); }
        }

        private void _OnAreNotificationsToggledChanged()
        {
            // Can't have both of these on at the same time.
            if (AreNotificationsToggled)
            {
                IsInboxToggled = false;
            }
        }
        
        /// <summary>
        /// InboxCountControl Dependency Property
        /// </summary>
        public static readonly DependencyProperty InboxCountControlProperty = DependencyProperty.Register(
            "InboxCountControl",
            typeof(NotificationCountControl),
            typeof(MainHeaderControl));

        public NotificationCountControl InboxCountControl
        {
            get { return (NotificationCountControl)GetValue(InboxCountControlProperty); }
            set { SetValue(InboxCountControlProperty, value); }
        }

        /// <summary>
        /// IsInboxToggled Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsInboxToggledProperty = DependencyProperty.Register(
            "IsInboxToggled",
            typeof(bool),
            typeof(MainHeaderControl),
            new PropertyMetadata(
                false,
                (d, e) => ((MainHeaderControl)d)._OnIsInboxToggledChanged()));

        public bool IsInboxToggled
        {
            get { return (bool)GetValue(IsInboxToggledProperty); }
            set { SetValue(IsInboxToggledProperty, value); }
        }

        private void _OnIsInboxToggledChanged()
        {
            // Can't have both of these on at the same time.
            if (IsInboxToggled)
            {
                AreNotificationsToggled = false;
            }
        }

        public static RoutedCommand ShowChatWindowCommand = new RoutedCommand("ShowChatWindow", typeof(MainHeaderControl));
        public static RoutedCommand ShowMiniModeCommand = new RoutedCommand("ShowMiniMode", typeof(MainHeaderControl));
        public static RoutedCommand GoToFacebookCommand = new RoutedCommand("GoToFacebook", typeof(MainHeaderControl));
        public static RoutedCommand ShowSettingsCommand = new RoutedCommand("ShowSettings", typeof(MainHeaderControl));
        public static RoutedCommand SignOutCommand = new RoutedCommand("SignOut", typeof(MainHeaderControl));
        public static RoutedCommand RefreshCommand = new RoutedCommand("Refresh", typeof(MainHeaderControl));

        public MainHeaderControl()
        {
            CommandBindings.Add(new CommandBinding(ShowChatWindowCommand,   OnShowChatWindowCommand));
            CommandBindings.Add(new CommandBinding(ShowMiniModeCommand,     OnShowMiniModeCommand));
            CommandBindings.Add(new CommandBinding(GoToFacebookCommand,     OnGoToFacebookCommand));
            CommandBindings.Add(new CommandBinding(ShowSettingsCommand,     OnShowSettingsCommand));
            CommandBindings.Add(new CommandBinding(SignOutCommand,          OnSignOutCommand));
            CommandBindings.Add(new CommandBinding(RefreshCommand,          OnRefreshCommand));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _homeNavigationButton = Template.FindName("HomeNavigationButton", this) as RadioButton;
            _friendsNavigationButton = Template.FindName("FriendsNavigationButton", this) as RadioButton;
            _profileNavigationButton = Template.FindName("ProfileNavigationButton", this) as RadioButton;
            _photoAlbumsNavigationButton = Template.FindName("PhotoAlbumsNavigationButton", this) as RadioButton;
            _searchTextBox = Template.FindName("SearchTextBox", this) as TextBox;
            _searchTextBox.KeyDown += new KeyEventHandler(OnSearchTextBoxKeyDown);

            NotificationControl = Template.FindName("NotificationControl", this) as NotificationCountControl;
            InboxCountControl = Template.FindName("InboxCountControl", this) as NotificationCountControl;
        }

        public void OnRootNavigatorChanged(Navigator rootNavigator)
        {
            if (rootNavigator == ServiceProvider.ViewManager.MasterNavigator.HomeNavigator)
            {
                _homeNavigationButton.IsChecked = true;
            }
            else if (rootNavigator == ServiceProvider.ViewManager.MasterNavigator.FriendsNavigator)
            {
                _friendsNavigationButton.IsChecked = true;
            }
            else if (rootNavigator == ServiceProvider.ViewManager.MasterNavigator.ProfileNavigator)
            {
                _profileNavigationButton.IsChecked = true;
            }
            else if (rootNavigator == ServiceProvider.ViewManager.MasterNavigator.PhotoAlbumsNavigator)
            {
                _photoAlbumsNavigationButton.IsChecked = true;
            }
        }

        private void OnSearchTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Enter)
                {
                    // On commit, search and clear text
                    e.Handled = true;
                    _searchTextBox.Text = "";
                    Focus();
                }
            }

            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        private void OnShowMiniModeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ApplicationCommands.SwitchToMiniModeCommand.Execute(Application.Current.MainWindow);
        }

        private void OnShowChatWindowCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ApplicationCommands.ShowChatWindowCommand.Execute(Application.Current.MainWindow);
        }

        private void OnGoToFacebookCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.Execute("[CurrentNavigator]");
        }

        private void OnShowSettingsCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ServiceProvider.ViewManager.ShowDialog(new SettingsDialog());
        }

        private void OnSignOutCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).SignOut();
        }

        private void OnRefreshCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ServiceProvider.ViewManager.ActionCommands.StartSyncCommand.Execute(null);
        }

    }
}
