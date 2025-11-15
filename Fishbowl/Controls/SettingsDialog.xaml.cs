namespace FacebookClient
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using ClientManager;
    using Standard;

    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : UserControl
    {
        public SettingsDialog()
        {
            InitializeComponent();

            string versionText = null;
#if !DEBUG
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                versionText = "version: " + System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            else
            {
                versionText = "version: ret." + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.ProductVersion;
            }
#else
            versionText = "version: chk." + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.ProductVersion;
#endif
            Assert.IsNeitherNullNorEmpty(versionText);
            VersionInfoTextBlock.Text = versionText;
        }

        private void _OnClose(object sender, RoutedEventArgs e)
        {
            ServiceProvider.ViewManager.EndDialog(this);
        }

        private void _OnLoaded(object sender, RoutedEventArgs e)
        {
            DisableUpdatesCheckBox.IsChecked = !FacebookClientApplication.AreUpdatesEnabled;
            OpenPagesInBrowserCheckBox.IsChecked = FacebookClientApplication.OpenWebContentInExternalBrowser;
            KeepMiniModeOnTopCheckBox.IsChecked = FacebookClientApplication.KeepMiniModeWindowOnTop;
            ClearCacheButton.IsEnabled = !FacebookClientApplication.DeleteCacheOnShutdown;
            ClearCacheInfoTextBlock.Visibility = FacebookClientApplication.DeleteCacheOnShutdown
                ? Visibility.Visible
                : Visibility.Collapsed;

            foreach (string theme in FacebookClientApplication.AvailableThemes)
            {
                VisualStyleBox.Items.Add(theme);
            }
            VisualStyleBox.SelectedItem = FacebookClientApplication.ThemeName;
        }

        private void _OnUnloaded(object sender, RoutedEventArgs e)
        {
            FacebookClientApplication.AreUpdatesEnabled = (bool)!DisableUpdatesCheckBox.IsChecked;
            FacebookClientApplication.OpenWebContentInExternalBrowser = (bool)OpenPagesInBrowserCheckBox.IsChecked;
            FacebookClientApplication.KeepMiniModeWindowOnTop = (bool)KeepMiniModeOnTopCheckBox.IsChecked;
            FacebookClientApplication.DeleteCacheOnShutdown = !ClearCacheButton.IsEnabled;
            FacebookClientApplication.ThemeName = VisualStyleBox.SelectedItem.ToString();
        }

        private void _OnSupportWebsiteClicked(object sender, RoutedEventArgs e)
        {
            // Don't open these within the app.  Always open external.
            Process.Start(new ProcessStartInfo(FacebookClientApplication.SupportWebsite.OriginalString));
            e.Handled = true;
        }

        private void _OnPrivacyWebsiteClicked(object sender, RoutedEventArgs e)
        {
            // Don't open these within the app.  Always open external.
            Process.Start(new ProcessStartInfo(FacebookClientApplication.PrivacyWebsite.OriginalString));
            e.Handled = true;
        }

        private void _OnClearCacheButtonClicked(object sender, RoutedEventArgs e)
        {
            ClearCacheButton.IsEnabled = false;
            ClearCacheInfoTextBlock.Visibility = Visibility.Visible;
        }
    }
}
