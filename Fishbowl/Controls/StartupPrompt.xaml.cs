namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using ClientManager;
    using FacebookClient.Properties;

    /// <summary>
    /// Interaction logic for StartupPrompt.xaml
    /// </summary>
    public partial class StartupPrompt : UserControl
    {
        public StartupPrompt()
        {
            InitializeComponent();
        }

        private void YesClick(object sender, RoutedEventArgs e)
        {
            Settings.Default.AreUpdatesEnabled = true;
            ServiceProvider.ViewManager.EndDialog(this);
        }

        private void NoClick(object sender, RoutedEventArgs e)
        {
            Settings.Default.AreUpdatesEnabled = false;
            ServiceProvider.ViewManager.EndDialog(this);
        }
    }
}
