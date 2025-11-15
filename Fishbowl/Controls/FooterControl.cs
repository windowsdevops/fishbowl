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

    public class FooterControl : Control
    {
        public static readonly DependencyProperty NotificationControlProperty = DependencyProperty.Register(
            "NotificationControl",
            typeof(NotificationCountControl),
            typeof(FooterControl));

        public NotificationCountControl NotificationControl
        {
            get { return (NotificationCountControl)GetValue(NotificationControlProperty); }
            set { SetValue(NotificationControlProperty, value); }
        }


        public static readonly DependencyProperty AreNotificationsToggledProperty = DependencyProperty.Register(
            "AreNotificationsToggled",
            typeof(bool),
            typeof(FooterControl),
            new PropertyMetadata(
                false,
                (d, e) => ((FooterControl)d)._OnAreNotificationsToggledChanged()));

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
                IsBuddyListToggled = false;
            }
        }

        public static readonly DependencyProperty IsBuddyListToggledProperty = DependencyProperty.Register(
            "IsBuddyListToggled",
            typeof(bool),
            typeof(FooterControl),
            new PropertyMetadata(
                false,
                (d, e) => ((FooterControl)d)._OnIsBuddyListToggledChanged()));

        public bool IsBuddyListToggled
        {
            get { return (bool)GetValue(IsBuddyListToggledProperty); }
            set { SetValue(IsBuddyListToggledProperty, value); }
        }

        private void _OnIsBuddyListToggledChanged()
        {
            // Can't have both of these on at the same time.
            if (IsBuddyListToggled)
            {
                IsInboxToggled = false;
                AreNotificationsToggled = false;
            }
        }


        public static readonly DependencyProperty IsInboxToggledProperty = DependencyProperty.Register(
            "IsInboxToggled",
            typeof(bool),
            typeof(FooterControl),
            new PropertyMetadata(
                false,
                (d, e) => ((FooterControl)d)._OnIsInboxToggledChanged()));

        public bool IsInboxToggled
        {
            get { return (bool)GetValue(IsInboxToggledProperty); }
            set { SetValue(IsInboxToggledProperty, value); }
        }

        private void _OnIsInboxToggledChanged()
        {
#if FACEBOOK_HAS_GRANTED_INBOX_PERMISSIONS
            // Can't have both of these on at the same time.
            if (IsInboxToggled)
            {
                AreNotificationsToggled = false;
                IsBuddyListToggled = false;
            }
#else
            if (IsInboxToggled)
            {
                // Facebook isn't letting us display the inbox...
                IsInboxToggled = false;
                ((MainWindow)Application.Current.MainWindow).ApplicationCommands.ShowInboxCommand.Execute(Application.Current.MainWindow);
            }
#endif

        }

        public static RoutedCommand ShowSettingsCommand = new RoutedCommand("ShowSettings", typeof(FooterControl));
        public static RoutedCommand SignOutCommand = new RoutedCommand("SignOut", typeof(FooterControl));

        public FooterControl()
        {
            CommandBindings.Add(new CommandBinding(ShowSettingsCommand, OnShowSettingsCommand));
            CommandBindings.Add(new CommandBinding(SignOutCommand, OnSignOutCommand));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            NotificationControl = Template.FindName("NotificationControl", this) as NotificationCountControl;
        }

        private void OnShowSettingsCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ServiceProvider.ViewManager.ShowDialog(new SettingsDialog());
        }

        private void OnSignOutCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).SignOut();
        }
    }
}
