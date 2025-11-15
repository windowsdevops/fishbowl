//-----------------------------------------------------------------------
// <copyright file="FacebookClientApplication.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code behind file for the FacebookClient Application XAML.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using FacebookClient.Properties;
    using Microsoft.Windows.Shell;
    using Standard;

    /// <summary>
    /// Code behind file for the FacebookClient Application XAML.
    /// </summary>
    public partial class FacebookClientApplication : Application
    {
        internal const string ApiKey = "f6310ebf42d462b20050f62bea75d7d2";

        private static readonly Uri _SupportUri = new Uri("http://www.fishbowlclient.com");
        private static readonly Uri _PrivacyUri = new Uri("http://go.microsoft.com/fwlink/?LinkId=167928");

        private static readonly Dictionary<string, Uri> _ThemeLookup = new Dictionary<string,Uri>
        {
            { "Blue",     new Uri(@"Resources\Themes\Blue\Blue.xaml", UriKind.Relative) },
            { "Dark",     new Uri(@"Resources\Themes\Dark\Dark.xaml", UriKind.Relative) },
            { "Facebook", new Uri(@"Resources\Themes\FBBlue\FBBlue.xaml", UriKind.Relative) },
#if DEBUG
            // Not a production quality theme.
            // This can be used to find resources that aren't properly styled.
            { "Red",      new Uri(@"Resources\Themes\Red\Red.xaml", UriKind.Relative) },
#endif
        };
        private static readonly List<string> _ThemeNames = new List<string>(_ThemeLookup.Keys);
        private const string _DefaultThemeName = "Dark";

        private MainWindow _mainWindow;
        private MiniModeWindow _minimodeWindow;
        private ChatWindow _chatWindow;
        private bool _isInMiniMode = false;

        public static IEnumerable<string> AvailableThemes { get { return _ThemeNames.AsReadOnly(); } }

        public static Uri SupportWebsite { get { return _SupportUri; } }

        public static Uri PrivacyWebsite { get { return _PrivacyUri; } }

        public static bool IsFirstRun
        {
            get { return Settings.Default.IsFirstRun; }
            set { Settings.Default.IsFirstRun = value; }
        }

        public static string ThemeName
        {
            get { return Settings.Default.ThemeName; }
            set
            {
                if (value != Settings.Default.ThemeName)
                {
                    Settings.Default.ThemeName = value;
                    ((FacebookClientApplication)Application.Current).SwitchTheme(value);
                }
            }
        }

        public static bool AreUpdatesEnabled
        {
            get { return Settings.Default.AreUpdatesEnabled; }
            set { Settings.Default.AreUpdatesEnabled = value; }
        }

        public static bool DeleteCacheOnShutdown { get; set; }

        public static bool OpenWebContentInExternalBrowser
        {
            get { return Settings.Default.OpenWebContentExternally; }
            set { Settings.Default.OpenWebContentExternally = value; }
        }

        public static bool ShowMoreNewsfeedFilters
        {
            get { return Settings.Default.ShowMoreNewsfeedFilters; }
            set { Settings.Default.ShowMoreNewsfeedFilters = value; }
        }

        public static bool KeepMiniModeWindowOnTop
        {
            get { return Settings.Default.KeepMiniModeWindowOnTop; }
            set
            {
                Settings.Default.KeepMiniModeWindowOnTop = value;
                Window window = (((FacebookClientApplication)Application.Current)._minimodeWindow);
                if (null != window)
                {
                    window.Topmost = value;
                }
            }
        }

        /// <summary>Whether the client is currently Tier 2 capable which is required for hardware-accelerated effects. </summary>
        public static bool IsShaderEffectSupported
        {
            get { return RenderCapability.Tier == 0x00020000 && RenderCapability.IsPixelShaderVersionSupported(2, 0); }
        }

        private string _GetNextTheme()
        {
            int index = _ThemeNames.IndexOf(ThemeName);
            if (index == -1)
            {
                return _DefaultThemeName;
            }

            return _ThemeNames[(index+1) % _ThemeNames.Count];
        }

        public void SwitchTheme(string themeName)
        {
            if (themeName == null)
            {
                themeName = _GetNextTheme();
            }

            Uri resourceUri = null;
            if (!_ThemeLookup.TryGetValue(themeName, out resourceUri))
            {
                themeName = _DefaultThemeName;
                resourceUri = _ThemeLookup[themeName];
            }

            ThemeName = themeName;

            if (_currentThemeDictionary != null)
            {
                this.Resources.MergedDictionaries.Remove(_currentThemeDictionary);
            }
            
            _currentThemeDictionary = LoadComponent(resourceUri) as ResourceDictionary;
            this.Resources.MergedDictionaries.Insert(0, _currentThemeDictionary);
        }

        internal static void PerformAggressiveCleanup()
        {
            GC.Collect(2);
            try
            {
                NativeMethods.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, 40000, 80000);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // This is a way to get the app back to zero.
            //Settings.Default.Reset();

            SwitchTheme(Settings.Default.ThemeName);

            _mainWindow = new MainWindow();
            _minimodeWindow = new MiniModeWindow();

            Point minimodeStartupLocation = FacebookClient.Properties.Settings.Default.MiniModeWindowBounds.TopLeft;
            if (minimodeStartupLocation == default(Point) || !DoubleUtilities.IsFinite(minimodeStartupLocation.X) || !DoubleUtilities.IsFinite(minimodeStartupLocation.Y))
            {
                _minimodeWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                _minimodeWindow.Left = minimodeStartupLocation.X;
                _minimodeWindow.Top = minimodeStartupLocation.Y;
            }

            this.MainWindow = _mainWindow;

            var jumpListResource = (JumpList)Resources["MainModeJumpList"];
            Assert.IsNotNull(jumpListResource);

            var jumpList = new JumpList(jumpListResource.JumpItems, false, false);

            JumpList.SetJumpList(this, jumpList);

            _mainWindow.Show();

            SingleInstance.SingleInstanceActivated += SignalExternalCommandLineArgs;


            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Persist the minimode window's location here, since it never lets itself close on its own.
            FacebookClient.Properties.Settings.Default.MiniModeWindowBounds = new Rect(_minimodeWindow.Left, _minimodeWindow.Top, _minimodeWindow.Width, _minimodeWindow.Height);

            FacebookClient.Properties.Settings.Default.Save();
            
            // By explicitly setting this list we'll remove the notifications items.
            var jumpList = (JumpList)Resources["SignedOutJumpList"];
            JumpList.SetJumpList(this, jumpList);

            base.OnExit(e);
        }

        private ResourceDictionary _currentThemeDictionary;

        private void SignalExternalCommandLineArgs(object sender, SingleInstanceEventArgs e)
        {
            bool handledByWindow = false;
            if (_isInMiniMode)
            {
                _minimodeWindow.Activate();

                handledByWindow = _minimodeWindow.ProcessCommandLineArgs(e.Args);
            }
            else
            {
                _mainWindow.Activate();
                handledByWindow = _mainWindow.ProcessCommandLineArgs(e.Args);
            }

            if (!handledByWindow)
            {
                ClientManager.ServiceProvider.ViewManager.ProcessCommandLineArgs(e.Args);
            }
        }

        internal void SwitchToMiniMode()
        {
            Dispatcher.VerifyAccess();
            if (_isInMiniMode)
            {
                return;
            }
            _isInMiniMode = true;

            _mainWindow.Hide();
            _minimodeWindow.Show();

            if (_minimodeWindow.WindowState == WindowState.Minimized)
            {
                _minimodeWindow.WindowState = WindowState.Normal;
            }
            _minimodeWindow.Activate();

            var miniJumpList = (JumpList)Resources["MiniModeJumpList"];
            JumpList currentJumpList = JumpList.GetJumpList(this);

            // Remove and replace all tasks.
            currentJumpList.JumpItems.RemoveAll(item => item.CustomCategory == null);
            currentJumpList.JumpItems.AddRange(miniJumpList.JumpItems);

            currentJumpList.Apply();
        }

        internal void SwitchToMainMode()
        {
            Dispatcher.VerifyAccess();
            if (!_isInMiniMode)
            {
                return;
            }
            _isInMiniMode = false;

            _minimodeWindow.Hide();
            _mainWindow.Show();

            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            _mainWindow.Activate();

            var mainJumpList = (JumpList)Resources["MainModeJumpList"];
            JumpList currentJumpList = JumpList.GetJumpList(this);

            // Remove and replace all tasks.
            currentJumpList.JumpItems.RemoveAll(item => item.CustomCategory == null);
            currentJumpList.JumpItems.AddRange(mainJumpList.JumpItems);

            currentJumpList.Apply();
        }

        internal void ShowChatWindow()
        {
            if (_chatWindow != null)
            {
                _chatWindow.Activate();
                return;
            }

            _chatWindow = new ChatWindow();
            _chatWindow.Closed += (sender, e) => _chatWindow = null;
            _chatWindow.Show();
        }
    }
}
