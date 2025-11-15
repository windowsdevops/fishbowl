namespace FacebookClient
{
    using System;
    using System.Windows;
    using ClientManager.Controls;

    public class FriendSummaryControl : SizeTemplateControl
    {
        public static readonly DependencyProperty ShowSortBarProperty = DependencyProperty.Register(
            "ShowSortBar",
            typeof(bool),
            typeof(FriendSummaryControl),
            new FrameworkPropertyMetadata(true));

        public bool ShowSortBar
        {
            get { return (bool)GetValue(ShowSortBarProperty); }
            set { SetValue(ShowSortBarProperty, value); }
        }

        public FriendSummaryControl()
        {
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            this.Focus();
        }
    }
}
