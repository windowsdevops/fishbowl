namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using Contigo;
    using System.Windows.Data;

    public class FriendButton : Control
    {
        public static readonly DependencyProperty FriendProperty = DependencyProperty.Register("Friend", typeof(FacebookContact), typeof(FriendButton));
        public static readonly DependencyProperty IsClickableProperty = DependencyProperty.Register("IsClickable", typeof(bool), typeof(FriendButton),
            new FrameworkPropertyMetadata(true));

        public FacebookContact Friend
        {
            get { return (FacebookContact)GetValue(FriendProperty); }
            set { SetValue(FriendProperty, value); }
        }

        public bool IsClickable
        {
            get { return (bool)GetValue(IsClickableProperty); }
            set { SetValue(IsClickableProperty, value); }
        }
    }
}
