namespace FacebookClient
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Navigation;
    using ClientManager;
    using Standard;

    public partial class EmbeddedBrowserControl : UserControl
    {
        private DateTime _lastRefresh = DateTime.Now;
        private bool _hasHookedIWebBrowser = false;
        private WebBrowser _browser;
        private WebBrowserEvents _browserEvents;
        public event EventHandler BrowserShown;
        public event EventHandler BrowserHidden;

        private void _InitializeWebBrowser()
        {
            VerifyAccess();

            Assert.IsNull(_browser);
            Assert.IsNull(_browserEvents);
            Assert.IsNull(BrowserHost.Child);

            _hasHookedIWebBrowser = false;
            _browser = new WebBrowser();
            _browser.Navigated += _OnNavigated;
            _browser.LoadCompleted += _OnLoadCompleted;
            if (Source != null)
            {
                _browser.Source = Source;
            }

            BrowserHost.Child = _browser;
        }

        private void _DisposeWebBrowser()
        {
            VerifyAccess();

            Assert.IsNotNull(_browser);
            Assert.AreEqual(BrowserHost.Child, _browser);

            BrowserHost.Child = null;
            Utility.SafeDispose(ref _browserEvents);
            Utility.SafeDispose(ref _browser);
        }

        public void Show()
        {
            if (ServiceProvider.ViewManager.Dialog == null)
            {
                _InitializeWebBrowser();
                ServiceProvider.ViewManager.ShowDialog(this);

                if (BrowserShown != null)
                {
                    BrowserShown(this, EventArgs.Empty);
                }
            }
        }

        public void Hide()
        {
            ServiceProvider.ViewManager.EndDialog(this);
            _DisposeWebBrowser();

            TitleTextBlock.Text = "";
            UriTextBlock.Text = "";

            if (BrowserHidden != null)
            {
                BrowserHidden(this, EventArgs.Empty);
            }
        }

        public EmbeddedBrowserControl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source",
            typeof(Uri),
            typeof(EmbeddedBrowserControl),
            new FrameworkPropertyMetadata(
                null,
                (d, e) => ((EmbeddedBrowserControl)d)._OnSourceChanged()));

        private void _OnSourceChanged()
        {
            if (Source != null)
            {
                if (_browser != null)
                {
                    _browser.Source = Source;
                }
                Show();
            }
            else
            {
                Hide();
            }
        }

        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        private void _UpdateCaptionText(string uri)
        {
            string title = Utility.GetWebPageTitle(_browser);
            if (string.IsNullOrEmpty(title))
            {
                title = "External Content";
            }
            TitleTextBlock.Text = title;

            UriTextBlock.Text = uri;
        }

        private void _OnLoadCompleted(object sender, NavigationEventArgs e)
        {
            string uriText = "";
            if (e.Uri != null)
            {
                uriText = e.Uri.OriginalString;
            }
            _UpdateCaptionText(e.Uri.OriginalString);

            // There's an issue with the web-browser control and showing videos on Facebook.
            // Not sure of a viable fix, so try to detect it and notify the user.
            GoldBar.Visibility = e.Uri.Host.ToLower().Contains("facebook") && e.Uri.AbsoluteUri.ToLower().Contains("video.php")
                ? Visibility.Visible
                : Visibility.Collapsed;

            _HookNativeBrowserWindow();
        }

        private void _HookNativeBrowserWindow()
        {
            if (!_hasHookedIWebBrowser)
            {
                Assert.IsNotNull(_browser.Document);
                Utility.SuppressJavaScriptErrors(_browser);
                _browserEvents = new WebBrowserEvents(_browser);
                _browserEvents.WindowClosing += _OnBrowserWindowClosing;
                _hasHookedIWebBrowser = true;
            }
        }

        private void _OnNavigated(object sender, NavigationEventArgs e)
        {
            string uriText = "";
            if (e.Uri != null)
            {
                uriText = e.Uri.OriginalString;
            }
            _UpdateCaptionText(uriText);

            _HookNativeBrowserWindow();
        }

        private void _OnBreakout(object sender, RoutedEventArgs e)
        {
            // If the browser got stuck and didn't navigate, then use the Source DP as the launching point.
            string newSource = "www.facebook.com";
            if (_browser.Source != null)
            {
                newSource = _browser.Source.OriginalString;
            }
            else if (Source != null)
            {
                newSource = Source.OriginalString;
            }
            Process.Start(newSource);
            Source = null;
        }

        private void _OnBrowserWindowClosing(object sender, EventArgs e)
        {
            _OnClose(this, null);
        }

        private void _OnRefresh(object sender, RoutedEventArgs e)
        {
            // Despite the catch below, just prevent the user from doing this incessantly.
            // We don't want to bubble a bunch of COM Exceptions.
            if (_lastRefresh > DateTime.Now.Subtract(TimeSpan.FromSeconds(2)))
            {
                return;
            }
            _lastRefresh = DateTime.Now;
            try
            {
                _browser.Refresh();
            }
            // WebBrowser tends to return E_FAIL when clicking refresh multiple times.
            catch (Exception) {}
        }

        private void _OnClose(object sender, RoutedEventArgs e)
        {
            Source = null;
        }

        private void _OnBrowseBackClicked(object sender, RoutedEventArgs e)
        {
            if (_browser.CanGoBack)
            {
                _browser.GoBack();
            }
            else
            {
                Hide();
            }
        }
    }
}
