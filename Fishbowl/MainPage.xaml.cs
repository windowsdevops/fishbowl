//#define USE_STANDARD_DRAGDROP

namespace FacebookClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using ClientManager;
    using ClientManager.Controls;
    using ClientManager.View;
    using Contigo;
    using Standard;

    /// <summary>
    /// The ScePhoto view mode; regular or full-screen with options.
    /// </summary>
    public enum ViewingMode
    {
        /// <summary>
        /// Full-screen viewing with the navigation UI.
        /// </summary>
        FullScreenNavigationUI,

        /// <summary>
        /// Full-screen viewing without the navigation UI.
        /// </summary>
        FullScreenNoNavigationUI,

        /// <summary>
        /// Normal viewing without the navigation UI.
        /// </summary>
        NormalScreenNoNavigationUI,

        /// <summary>
        /// Normal viewing with the navigation UI.
        /// </summary>
        NormalScreenNavigationUI
    }

    public partial class MainPage
    {
        public static readonly DependencyProperty NavigationUIVisibilityProperty = DependencyProperty.Register(
            "NavigationUIVisibility",
            typeof(Visibility),
            typeof(MainPage),
            new UIPropertyMetadata(Visibility.Visible));

        /// <summary>Gets the visibility of the Navigation UI.</summary>
        public Visibility NavigationUIVisibility
        {
            get { return (Visibility)GetValue(NavigationUIVisibilityProperty); }
            protected set { SetValue(NavigationUIVisibilityProperty, value); }
        }

        public static readonly DependencyProperty FullScreenModeProperty = DependencyProperty.Register(
            "FullScreenMode",
            typeof(bool),
            typeof(MainPage),
            new UIPropertyMetadata(false));

        /// <summary>Gets a value indicating whether the app is full screen.</summary>
        public bool FullScreenMode
        {
            get { return (bool)GetValue(FullScreenModeProperty); }
            protected set { SetValue(FullScreenModeProperty, value); }
        }

        public static readonly DependencyProperty IsOnlineProperty = DependencyProperty.Register(
            "IsOnline",
            typeof(bool),
            typeof(MainPage),
            new PropertyMetadata(false));

        public bool IsOnline
        {
            get { return (bool)GetValue(IsOnlineProperty); }
            private set { SetValue(IsOnlineProperty, value); }
        }

        /// <summary>
        /// Viewing mode for the next view.
        /// </summary>
        private ViewingMode _viewingMode = ViewingMode.NormalScreenNavigationUI;

        private readonly MainWindowCommands _LocalApplicationCommands;

        private readonly RoutedCommand _SwitchFullScreenModeCommand;

        public MainPage()
        {
            ServiceProvider.Initialize("aff9f004793a1d32d26fe2361d5fc723", "33dbf033a01d5a3be4a631b4d47950c6", Dispatcher);
            ServiceProvider.GoneOnline += (sender, e) => IsOnline = true;
            ServiceProvider.GoneOffline += (sender, e) =>
            {
                IsOnline = false;
                ServiceProvider.ViewManager.NavigationCommands.NavigateLoginCommand.Execute(null);
            };

            ServiceProvider.ViewManager.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_OnViewManagerPropertyChanged);

            Loaded += (sender, e) =>
            {
                this.StartSwooshes();

                ServiceProvider.ViewManager.NavigationCommands.NavigateLoginCommand.Execute(null);
            };


            DeploymentManager.StartMonitor();
            DeploymentManager.ApplicationUpdated += _OnApplicationUpdated;

            _LocalApplicationCommands = new MainWindowCommands(ServiceProvider.ViewManager);
            _SwitchFullScreenModeCommand = new RoutedCommand("SwitchFullScreenMode", typeof(MainPage));

            InitializeComponent();

            CommandBindings.Add(new CommandBinding(System.Windows.Input.NavigationCommands.BrowseBack, (sender, e) => _SafeBrowseBack(), (sender, e) => e.CanExecute = CanGoBack));
            CommandBindings.Add(new CommandBinding(System.Windows.Input.NavigationCommands.Refresh, (sender, e) => ServiceProvider.ViewManager.NavigationCommands.StartSyncCommand.Execute(null)));
            CommandBindings.Add(new CommandBinding(MediaCommands.TogglePlayPause, OnPlayCommandExecuted));
            CommandBindings.Add(new CommandBinding(MediaCommands.Play, OnPlayCommandExecuted));
            CommandBindings.Add(new CommandBinding(_SwitchFullScreenModeCommand, OnSwitchFullScreenCommand));

            RoutedCommand backNavigationKeyOverrideCommand = new RoutedCommand();
            CommandBindings.Add(
                new CommandBinding(
                    backNavigationKeyOverrideCommand,
                    (sender, e) => ((MainPage)sender)._SafeBrowseBack()));

            InputBindings.Add(new InputBinding(backNavigationKeyOverrideCommand, new KeyGesture(Key.Back)));
            InputBindings.Add(new KeyBinding(_LocalApplicationCommands.SwitchThemeCommand, new KeyGesture(Key.T, ModifierKeys.Control)));
        }

        private void _OnViewManagerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentNavigator":
                    _OnCurrentNavigatorChanged();
                    break;
                case "CurrentRootNavigator":
                    _OnRootNavigatorChanged();
                    break;
            }
        }

        private void _OnCurrentNavigatorChanged()
        {
            if (ServiceProvider.ViewManager.CurrentNavigator.Content.GetType() == typeof(HomePage))
            {
                AnimationHelper.FadeOut(this.BrowseBackButton, 0, TimeSpan.FromMilliseconds(200));
            }
            else
            {
                AnimationHelper.FadeIn(this.BrowseBackButton);
            }

            if (ServiceProvider.ViewManager.CurrentNavigator.Content.GetType() == typeof(LoadingPage))
            {
                (this.AnimatedSwooshesContainer.Children[0] as AnimatedSwooshes).Fade(1);
            }
            else
            {
                if ((this.AnimatedSwooshesContainer.Children[0] as AnimatedSwooshes).Opacity == 1)
                {
                    (this.AnimatedSwooshesContainer.Children[0] as AnimatedSwooshes).Fade(.65);
                }
            }
        }

        private void _OnRootNavigatorChanged()
        {
            Navigator rootNavigator = ServiceProvider.ViewManager.CurrentRootNavigator;

            if (rootNavigator == ServiceProvider.ViewManager.MasterNavigator.HomeNavigator)
            {
                this.HomeNavigationButton.IsChecked = true;
            }
            else if (rootNavigator == ServiceProvider.ViewManager.MasterNavigator.FriendsNavigator)
            {
                this.FriendsNavigationButton.IsChecked = true;
            }
            else if (rootNavigator == ServiceProvider.ViewManager.MasterNavigator.ProfileNavigator)
            {
                this.ProfileNavigationButton.IsChecked = true;
            }
            else if (rootNavigator == ServiceProvider.ViewManager.MasterNavigator.PhotoAlbumsNavigator)
            {
                this.PhotoAlbumsNavigationButton.IsChecked = true;
            }
        }

        private void _OnApplicationUpdated(object sender, EventArgs e)
        {
            RestartPrompt.Visibility = Visibility.Visible;
        }

        /*
        protected override void OnClosed(EventArgs args)
        {
            this.StopSwooshes();
            base.OnClosed(args);
            ServiceProvider.Shutdown();
        }
        */

        //private void _OnCaptionButton(object sender, RoutedEventArgs e)
        //{
        //    if (sender == CloseButton)
        //    {
        //        Close();
        //    }
        //    else if (sender == MinimizeButton)
        //    {
        //        WindowState = WindowState.Minimized;
        //    }
        //    else
        //    {
        //        WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        //    }
        //}

        public void SetSlideshowViewingMode(bool active)
        {
            if (active)
            {
                SwitchNavigationUIVisibility(false);
                _SwitchFullScreenMode(true);
            }
            else
            {
                RestoreViewingMode();
            }
        }

        public void ToggleUploadWizard()
        {
            PhotoUploadWizard.Toggle();
        }

        /// <summary>
        /// Switches the viewing mode between full screen and windowed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the routed event.</param>
        private void OnSwitchFullScreenCommand(object sender, ExecutedRoutedEventArgs e)
        {
            UpdateViewingModeFullScreen();
            ContentPane.Focus();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // Custom logic for MainPage's MouseWheel should be put before calling global handler

            // Next, call global handler
            if (!e.Handled)
            {
                ApplicationInputHandler.OnMouseWheel(e);
            }

            // Finally, call base if not handled
            if (!e.Handled)
            {
                base.OnMouseWheel(e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Custom key handling for main page
            if (!e.Handled)
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                {
                    switch (e.Key)
                    {
                        case Key.Oem2:
                            OnOem2KeyPress(e);
                            break;
                        case Key.F9:
                            OnF9KeyPress(e);
                            break;
                        case Key.F11:
                            OnF11KeyPress(e);
                            break;
                        case Key.F12:
                            OnF12KeyPress(e);
                            break;
                        case Key.Escape:
                            OnEscapeKeyPress(e);
                            break;
                        default:
                            break;
                    }
                }
            }

            // Next, call application-wide input handler.
            if (!e.Handled)
            {
                ApplicationInputHandler.OnKeyDown(e);
            }

            // Finally, call base if not handled.
            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        #region Private Methods

        /// <summary>
        /// Can execute handler for play command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPlayCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
#if ENABLE_SLIDESHOW
            if (!e.Handled)
            {
                if (!(ServiceProvider.ViewManager.CurrentNavigator is PhotoSlideShowNavigator))
                {
                    e.CanExecute = true;
                }

                e.Handled = true;
            }
#endif
        }

        /// <summary>
        /// Executed event handler for play command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPlayCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!e.Handled)
            {
#if ENABLE_SLIDESHOW
                if (!(ServiceProvider.ViewManager.CurrentNavigator is PhotoSlideShowNavigator))
                {
                    if (ServiceProvider.ViewManager.NavigationCommands.NavigateToSlideShowCommand.CanExecute(null))
                    {
                        ServiceProvider.ViewManager.NavigationCommands.NavigateToSlideShowCommand.Execute(null);
                    }
                }

                e.Handled = true;
#endif
            }
        }


        /// <summary>
        /// Turn off full screen, make navigation UI visible.
        /// </summary>
        private void RestoreViewingMode()
        {
            SwitchNavigationUIVisibility(true);
            _SwitchFullScreenMode(false);
        }

        /// <summary>
        /// On F12 key press, switch navigation UI visibility.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        private void OnF12KeyPress(KeyEventArgs e)
        {
            // Update viewing mode based on navigation UI visibility
            UpdateViewingModeNavUI();
            e.Handled = true;
        }

        /// <summary>
        /// On Oem2 press, move focus to Search text box. If focus move was successful, set handled to true to prevent further handling.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        private void OnOem2KeyPress(KeyEventArgs e)
        {
            if (MoveFocusToSearch())
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Get the next viewing mode for the given value.
        /// </summary>
        /// <param name="viewingMode">Current viewing mode.</param>
        /// <returns>Next viewing mode.</returns>
        private static ViewingMode _GetNextViewingMode(ViewingMode viewingMode)
        {
            switch (viewingMode)
            {
                case ViewingMode.FullScreenNavigationUI: return ViewingMode.FullScreenNoNavigationUI;
                case ViewingMode.FullScreenNoNavigationUI: return ViewingMode.NormalScreenNoNavigationUI;
                case ViewingMode.NormalScreenNoNavigationUI: return ViewingMode.NormalScreenNavigationUI;
                case ViewingMode.NormalScreenNavigationUI: return ViewingMode.FullScreenNavigationUI;
                default:
                    Assert.Fail();
                    return ViewingMode.FullScreenNavigationUI;
            }
        }

        /// <summary>
        /// Switches full screen mode on or off.
        /// </summary>
        /// <param name="fullScreen">If true, full screen mode is on, otherwise, it's turned off.</param>
        private void _SwitchFullScreenMode(bool fullScreen)
        {
            // If viewing mode is already in a full screen state, not changes are necessary
            if (fullScreen && !FullScreenMode)
            {
                // Window.ResizeMode must be set before other window properties or else the window
                // will be positioned slightly off screen when maximized. 
                _resizeMode = Application.Current.MainPage.ResizeMode;
                Application.Current.MainPage.ResizeMode = ResizeMode.NoResize;

                _windowStyle = Application.Current.MainPage.WindowStyle;
                Application.Current.MainPage.WindowStyle = WindowStyle.None;
                Application.Current.MainPage.Topmost = true;
                Application.Current.MainPage.WindowState = WindowState.Maximized;
                FullScreenMode = true;
            }
            else if (!fullScreen && FullScreenMode)
            {
                Application.Current.MainPage.Topmost = false;
                Application.Current.MainPage.WindowState = WindowState.Normal;
                Application.Current.MainPage.WindowStyle = _windowStyle;
                Application.Current.MainPage.ResizeMode = _resizeMode;
                FullScreenMode = false;
            }
        }

        /// <summary>
        /// Switches viewing mode based on navigation UI visiblity, and toggles the visibilty of navigation UI.
        /// </summary>
        private void UpdateViewingModeNavUI()
        {
            switch (_viewingMode)
            {
                case ViewingMode.FullScreenNavigationUI:
                    SwitchNavigationUIVisibility(false);
                    _viewingMode = ViewingMode.FullScreenNoNavigationUI;
                    break;
                case ViewingMode.NormalScreenNavigationUI:
                    SwitchNavigationUIVisibility(false);
                    _viewingMode = ViewingMode.NormalScreenNoNavigationUI;
                    break;
                case ViewingMode.FullScreenNoNavigationUI:
                    SwitchNavigationUIVisibility(true);
                    _viewingMode = ViewingMode.FullScreenNavigationUI;
                    break;
                case ViewingMode.NormalScreenNoNavigationUI:
                    SwitchNavigationUIVisibility(true);
                    _viewingMode = ViewingMode.NormalScreenNavigationUI;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// On escape, go back to normal screen and navigation ui.
        /// </summary>
        /// <param name="e">Event args describing the event.</param>
        private void OnEscapeKeyPress(KeyEventArgs e)
        {
            // Restore viewing mode
            RestoreViewingMode();
            ContentPane.Focus();
            e.Handled = true;
        }

        /// <summary>
        /// Cycle viewing mode on F9 key press.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        private void OnF9KeyPress(KeyEventArgs e)
        {
            CycleViewingMode();
            e.Handled = true;
        }

        /// <summary>
        /// On F11 key press, switch full screen mode.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        private void OnF11KeyPress(KeyEventArgs e)
        {
            // Update viewing mode based on full screen setting
            UpdateViewingModeFullScreen();
            e.Handled = true;
        }

        /// <summary>
        /// Switches viewing mode based on full screen setting, and toggles the visibilty of navigation UI.
        /// </summary>
        private void UpdateViewingModeFullScreen()
        {
            switch (_viewingMode)
            {
                case ViewingMode.FullScreenNavigationUI:
                    _SwitchFullScreenMode(false);
                    _viewingMode = ViewingMode.NormalScreenNavigationUI;
                    break;
                case ViewingMode.NormalScreenNavigationUI:
                    _SwitchFullScreenMode(true);
                    _viewingMode = ViewingMode.FullScreenNavigationUI;
                    break;
                case ViewingMode.FullScreenNoNavigationUI:
                    _SwitchFullScreenMode(false);
                    _viewingMode = ViewingMode.NormalScreenNoNavigationUI;
                    break;
                case ViewingMode.NormalScreenNoNavigationUI:
                    _SwitchFullScreenMode(true);
                    _viewingMode = ViewingMode.FullScreenNoNavigationUI;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Cycles the viewing mode to the next value.
        /// </summary>
        private void CycleViewingMode()
        {
            ViewingMode nextViewingMode = _GetNextViewingMode(_viewingMode);
            switch (nextViewingMode)
            {
                case ViewingMode.FullScreenNavigationUI:
                    _SwitchFullScreenMode(true);
                    SwitchNavigationUIVisibility(true);
                    break;
                case ViewingMode.FullScreenNoNavigationUI:
                    _SwitchFullScreenMode(true);
                    SwitchNavigationUIVisibility(false);
                    break;
                case ViewingMode.NormalScreenNoNavigationUI:
                    _SwitchFullScreenMode(false);
                    SwitchNavigationUIVisibility(false);
                    break;
                case ViewingMode.NormalScreenNavigationUI:
                    _SwitchFullScreenMode(false);
                    SwitchNavigationUIVisibility(true);
                    break;
                default:
                    break;
            }

            _viewingMode = nextViewingMode;
        }

        /// <summary>
        /// Moves the current focus to the SearchControl for page.
        /// </summary>
        /// <returns>True if focus move succeeded.</returns>
        private bool MoveFocusToSearch()
        {
#if ENABLE_SEARCH
            // Check that we're in a mode that allows search visibility, and that the search control does not already have focus in the search area
            if (SearchControl != null && NavigationUIVisibility == Visibility.Visible && !SearchControl.IsSearchAreaFocused)
            {
                return SearchControl.MoveFocusToSearch();
            }
#endif
            return false;
        }

        /// <summary>
        /// Switches navigation UI visibility on or off.
        /// </summary>
        /// <param name="visible">If true, navigation UI is visible, if false, visibility is collapsed.</param>
        private void SwitchNavigationUIVisibility(bool visible)
        {
            NavigationUIVisibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void _SafeBrowseBack()
        {
            if (CanGoBack)
            {
                GoBack();
            }
        }

        #endregion

        private void FBWebButton_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.facebook.com");
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;

#if !USE_STANDARD_DRAGDROP
            DropTargetHelper.DragEnter(this, e.Data, e.GetPosition(this), e.Effects);
#endif
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;

#if !USE_STANDARD_DRAGDROP
            DropTargetHelper.DragLeave(e.Data);
#endif
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;

#if !USE_STANDARD_DRAGDROP
            DropTargetHelper.DragOver(e.GetPosition(this), e.Effects);
#endif
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (!DragContainer.IsInDrag)
            {
                string[] fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
                List<string> imageFiles = PhotoUploadWizard.FindImageFiles(fileNames);
                if (imageFiles.Count != 0)
                {
                    PhotoUploadWizard.Show(imageFiles);
                }

#if !USE_STANDARD_DRAGDROP
                DropTargetHelper.Drop(e.Data, e.GetPosition(this), DragDropEffects.Copy);
#endif
            }
        }

        private void GoToUploadsButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleUploadWizard();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textbox = (CommandTextBox)sender;
            if (!e.Handled)
            {
                if (e.Key == Key.Enter)
                {
                    // On commit, search and clear text
                    e.Handled = true;
                    textbox.Text = "";
                    Focus();
                }
            }

            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        private void _OnSignOutClicked(object sender, RoutedEventArgs e)
        {
            _LocalApplicationCommands.InitiateRestartCommand.Execute(this);
        }

        private void RestartNow(object sender, RoutedEventArgs e)
        {
            RestartPrompt.Visibility = Visibility.Collapsed;
            _LocalApplicationCommands.InitiateRestartCommand.Execute(this);
        }

        private void RestartLater(object sender, RoutedEventArgs e)
        {
            RestartPrompt.Visibility = Visibility.Collapsed;
        }

        private void _OnNotificationsClick(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            button.ContextMenu.Items.Clear();
            foreach (var notification in ServiceProvider.ViewManager.Notifications)
            {
                var mi = new MenuItem
                {
                    Header = notification,
                    Width = 300,
                    Command = ServiceProvider.ViewManager.NavigationCommands.MarkAsReadCommand,
                    CommandParameter = notification,
                };
                button.ContextMenu.Items.Add(mi);
            }
            if (button.ContextMenu.Items.Count == 0)
            {
                button.ContextMenu.Items.Add(new MenuItem { Header = "No new notifications...", Width = 300 });
            }
            button.ContextMenu.MaxHeight = Math.Max(this.Height - 40, 400);
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }


        private void StopSwooshes()
        {
            this.AnimatedSwooshesContainer.Children.Clear();
        }

        private void StartSwooshes()
        {
            var swooshes = new AnimatedSwooshes();
            swooshes.Margin = new Thickness(0, 0, 0, 300);
            swooshes.VerticalAlignment = VerticalAlignment.Bottom;
            swooshes.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.AnimatedSwooshesContainer.Children.Add(swooshes);
        }
    }
}

