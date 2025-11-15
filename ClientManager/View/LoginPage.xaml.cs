
namespace ClientManager.View
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Navigation;
    using System.Windows.Threading;
    using Contigo;
    using Standard;

    /// <summary>
    /// The Facebook login page as a ContentControl.
    /// </summary>
    public partial class LoginPage : ContentControl
    {
        private class _Navigator : Navigator
        {
            public _Navigator(LoginPage page, Dispatcher dispatcher)
                : base(page, "[Login Page]", null)
            { }

            public override bool IncludeInJournal { get { return false; } }
        }

#if FACEBOOK_HAS_GRANTED_INBOX_PERMISSIONS
#error Add back Permissions.ReadMailbox.  Don't need this for now since I can't use it :P
#endif
        private const Permissions _RequiredPermissions = Permissions.ReadStream | Permissions.PublishStream | Permissions.OfflineAccess;

        private readonly string _appId;
        private readonly bool _useCachedCredentials;

        private FacebookLoginService _service;
        private Navigator _nextPage;
        private bool _isLoggedIn;

        public Navigator Navigator { get; private set; }

        // Dummy URLs where we'll direct the browser in response to requesting extended permissions.
        // Facebook's APIs require that these be either in the Facebook domain or a subdomain of the connect URL.
        // If trying to use FBConnect, the connect URL must be specified through the application settings
        // or Facebook will redirect the user to an error page.
        private const string _GrantedPermissionUri = "http://www.facebook.com/connect/login_success.html";
        private const string _DeniedPermissionUri = "http://www.facebook.com/connect/login_failure.html";

        public LoginPage(string appId, Navigator next, bool useCachedCredentials)
        {
            InitializeComponent();

            Loaded += (sender, e) => _OnLoaded();
            Unloaded += (sender, e) => _OnUnloaded();

            _nextPage = next;
            _useCachedCredentials = useCachedCredentials;
            _appId = appId;
            Navigator = new _Navigator(this, this.Dispatcher);
        }

        private void _LoginBrowserNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            _SwitchToErrorPage(e.Exception, true);
        }

        private void _SwitchToInformationPage(string text)
        {
            LoginBorder.Visibility = Visibility.Collapsed;
            ErrorBorder.Visibility = Visibility.Collapsed;
            InformationBorder.Visibility = Visibility.Visible;

            InformationText.Text = text;
        }

        private void _SwitchToErrorPage(Exception e, bool canTryAgain)
        {
            _SwitchToErrorPage(e.Message, true, canTryAgain);
        }

        private void _SwitchToErrorPage(string text, bool fromException, bool canTryAgain)
        {
            LoginBorder.Visibility = Visibility.Collapsed;
            InformationBorder.Visibility = Visibility.Collapsed;

            ErrorBorder.Visibility = Visibility.Visible;

            ErrorText.Inlines.Clear();

            if (fromException)
            {
                ErrorText.Inlines.AddRange(
                    new Inline[] 
                    {
                        new Run { Text = "(" + text + ")" },
                        new LineBreak(),
                        new Run { Text = "Fishbowl was unable to connect to Facebook.  Please check your internet connection and ensure that your firewall is configured to allow internet access to Fishbowl." },
                    });
            }
            else
            {
                ErrorText.Inlines.Add(new Run(text));
            }

            if (canTryAgain)
            {
                TryAgainButton.Visibility = Visibility.Visible;
                TryAgainButton.IsEnabled = true;
            }
            else
            {
                TryAgainButton.Visibility = Visibility.Collapsed;
                TryAgainButton.IsEnabled = false;
            }
        }

        private void _OnLoaded()
        {
            Utility.SafeDispose(ref _service);
            FacebookLoginService service = null;
            try
            {
                service = new FacebookLoginService(_appId);
                if (!_useCachedCredentials)
                {
                    service.ClearCachedCredentials();
                }

                _service = service;
                service = null;

                if (_service.HasCachedSessionInfo)
                {
                    try
                    {
                        _OnUserLoggedIn();
                        return;
                    }
                    catch (Exception)
                    {
                        _service.ClearCachedCredentials();
                    }
                }

                _loginBrowser.Navigate(_service.GetLoginUri(_GrantedPermissionUri, _DeniedPermissionUri, _RequiredPermissions));
            }
            catch (Exception ex)
            {
                _SwitchToErrorPage(ex, _service != null);
            }
            finally
            {
                Utility.SafeDispose(ref service);
            }
        }

        private void _OnUnloaded()
        {
            Utility.SafeDispose(ref _service);
        }

        private void _OnUserLoggedIn()
        {
            _SwitchToInformationPage("Verifying permissions from Facebook.");
            _service.GetMissingPermissionsAsync(_RequiredPermissions, _OnGetMissingPermissionsCallback);
        }

        private void _OnGetMissingPermissionsCallback(object sender, AsyncCompletedEventArgs e)
        {
            Action<Permissions> callback = null;
            Permissions missingPermissions = Permissions.None;

            if (e.Error != null)
            {
                callback = (p) => _SwitchToErrorPage(e.Error, true);
            }
            else
            {
                callback = _OnMissingPermissionsVerified;
                missingPermissions = (Permissions)e.UserState;
            }

            this.Dispatcher.Invoke(DispatcherPriority.Normal, callback, missingPermissions);
        }

        private void _OnMissingPermissionsVerified(Permissions missingPermissions)
        {
            Assert.IsTrue(Dispatcher.CheckAccess());
            if (missingPermissions != Permissions.None)
            {
                _SwitchToErrorPage("Fishbowl requires additional permissions to work properly.", false, true);
            }
            else
            {
                _isLoggedIn = true;
                _GoOnline();
            }
        }

        private void _GoOnline()
        {
            ServiceProvider.GoOnline(_service.SessionKey, _service.SessionSecret, _service.UserId);

            ServiceProvider.ViewManager.NavigateByCommand(_nextPage);

            // After we've navigated away, dispose the fields.
            Utility.SafeDispose(ref _service);
            Utility.SafeDispose(ref _loginBrowser);
            _nextPage = null;
        }

        private void _OnBrowserNavigated(object sender, NavigationEventArgs e)
        {
            Utility.SuppressJavaScriptErrors(_loginBrowser);
            if (!_isLoggedIn)
            {
                // This will be contained in the page once the user has accepted the app.
                if (e.Uri.ToString().StartsWith(_GrantedPermissionUri))
                {
                    _service.InitiateNewSession(e.Uri);
                    try
                    {
                        _OnUserLoggedIn();
                    }
                    catch (Exception ex)
                    {
                        _SwitchToErrorPage(ex, true);
                    }
                    return;
                }
                else if (e.Uri.ToString().StartsWith(_DeniedPermissionUri))
                {
                    _SwitchToErrorPage("You didn't authorize the application.", true, true);
                    return;
                }
            }

            if (e.Uri.PathAndQuery.Contains(_service.AppId)
                || e.Uri.PathAndQuery.Contains("tos.php")
                || e.Uri.PathAndQuery.Contains("login_attempt"))
            {
                // Keep in this browser as long as it appears that we're in the context of this app.
                return;
            }
            else
            {
                // User did something other than log into the application.
                // Spawn a new webpage with the nevigated URI and close this browser session.
                Process.Start(e.Uri.ToString());
                _service.ClearCachedCredentials();

                // User canceled.  For now that means exit the application.  
                // I can't reliably get the browser back to home in this case.
                Environment.Exit(0);
            }
        }

        private void _OnTryAgain(object sender, RoutedEventArgs e)
        {
            if (_service != null)
            {
                LoginBorder.Visibility = Visibility.Visible;
                ErrorBorder.Visibility = Visibility.Collapsed;
                _service.ClearCachedCredentials();
                _loginBrowser.Navigate(_service.GetLoginUri(_GrantedPermissionUri, _DeniedPermissionUri, _RequiredPermissions));
            }
        }
    }
}
