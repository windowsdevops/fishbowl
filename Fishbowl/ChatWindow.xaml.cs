
namespace FacebookClient
{
    using System;
    using System.Windows;
    using Standard;

    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private WebBrowserEvents _eventHook;

        public ChatWindow()
        {
            InitializeComponent();

            Rect settingsBounds = Properties.Settings.Default.ChatWindowBounds;
            if (!settingsBounds.IsEmpty)
            {
                if (settingsBounds.Left == 0 && settingsBounds.Top == 0)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                else
                {
                    this.Left = settingsBounds.Left;
                    this.Top = settingsBounds.Top;
                }
                this.Width = settingsBounds.Width;
                this.Height = settingsBounds.Height;
            }
            else
            {
                this.Left = Application.Current.MainWindow.Left;
                this.Top = Application.Current.MainWindow.Top;
            }

            this.Closed += (sender, e) =>
            {
                Utility.SafeDispose(ref _eventHook);

                if (this.WindowState == WindowState.Normal)
                {
                    Properties.Settings.Default.ChatWindowBounds = new Rect(Left, Top, Width, Height);
                }
                else
                {
                    Properties.Settings.Default.ChatWindowBounds = new Rect(0, 0, Width, Height);
                }
            };
        }

        private void _OnFirstNavigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            Browser.Navigated -= _OnFirstNavigated;

            Assert.IsNotNull(Browser.Document);

            Utility.SuppressJavaScriptErrors(Browser);
            _eventHook = new WebBrowserEvents(Browser);
            _eventHook.WindowClosing += (sender2, e2) => Close();
        }
    }
}
