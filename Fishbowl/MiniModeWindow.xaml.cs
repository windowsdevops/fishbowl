
namespace FacebookClient
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using ClientManager;
    using ClientManager.Controls;
    using ClientManager.View;
    using Contigo;
    using FacebookClient.Properties;
    using Standard;

    public partial class MiniModeWindow : Window
    {
        public static readonly DependencyProperty NonGlassBackgroundProperty = DependencyProperty.RegisterAttached(
            "NonGlassBackground",
            typeof(Brush),
            typeof(MiniModeWindow),
            new FrameworkPropertyMetadata(SystemColors.WindowBrush));

        public static Brush GetNonGlassBackground(DependencyObject d)
        {
            return (Brush)d.GetValue(NonGlassBackgroundProperty);
        }

        public static void SetNonGlassBackground(DependencyObject d, Brush value)
        {
            d.SetValue(NonGlassBackgroundProperty, value);
        }

        /// <summary>Add and remove a native WindowStyle from the HWND.</summary>
        /// <param name="removeStyle">The styles to be removed.  These can be bitwise combined.</param>
        /// <param name="addStyle">The styles to be added.  These can be bitwise combined.</param>
        /// <returns>Whether the styles of the HWND were modified as a result of this call.</returns>
        private static bool _ModifyStyle(IntPtr hwnd, WS removeStyle, WS addStyle)
        {
            var dwStyle = (WS)NativeMethods.GetWindowLongPtr(hwnd, GWL.STYLE).ToInt32();
            var dwNewStyle = (dwStyle & ~removeStyle) | addStyle;
            if (dwStyle == dwNewStyle)
            {
                return false;
            }

            NativeMethods.SetWindowLongPtr(hwnd, GWL.STYLE, new IntPtr((int)dwNewStyle));
            return true;
        }

        static MiniModeWindow()
        {
            TopmostProperty.OverrideMetadata(typeof(MiniModeWindow), new FrameworkPropertyMetadata((d, e) => ((MiniModeWindow)d)._OnTopmostChanged()));
        }

        public MiniModeWindow()
        {
            InitializeComponent();

            this.Topmost = Settings.Default.KeepMiniModeWindowOnTop;

            // When the window loses focus take it as an opportunity to trim our workingset.
            Deactivated += (sender, e) => FacebookClientApplication.PerformAggressiveCleanup();

            // Tricks with WS_THICKFRAME make the window's template not cover the full client area.
            // Stretch the root panel with negative margins to correct for this.
            RootPanel.Margin = new Thickness(0, 0, SystemParameters.ResizeFrameVerticalBorderWidth * -2, SystemParameters.ResizeFrameHorizontalBorderHeight * -2);

            SourceInitialized += (sender, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;

                // Add back WS_THICKFRAME to get the glass border.
                _ModifyStyle(hwnd, 0, WS.THICKFRAME);

                _TryExtendGlass(hwnd);
                _SetDwmAttributes(hwnd);
                
                IntPtr hmenu = NativeMethods.GetSystemMenu(hwnd, false);
                Assert.IsNotDefault(hmenu);

                NativeMethods.RemoveMenu(hmenu, SC.MAXIMIZE, MF.BYCOMMAND);
                NativeMethods.RemoveMenu(hmenu, SC.MINIMIZE, MF.BYCOMMAND);
                NativeMethods.RemoveMenu(hmenu, SC.SIZE, MF.BYCOMMAND);

                NativeMethods.DrawMenuBar(hwnd);

                HwndSource.FromHwnd(hwnd).AddHook(_WndProc);
            };
        }

        private void _OnTopmostChanged()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            // Don't do this if the window hasn't yet been show.
            if (hwnd != IntPtr.Zero)
            {
                // Update the DWM attributes based on Topmost.
                _SetDwmAttributes(hwnd);
            }
        }

        private void _SetDwmAttributes(IntPtr hwnd)
        {
            if (Utility.IsOSVistaOrNewer)
            {
                DWMFLIP3D flipPolicy = this.Topmost ? DWMFLIP3D.EXCLUDEABOVE : DWMFLIP3D.EXCLUDEBELOW;
                NativeMethods.DwmSetWindowAttributeFlip3DPolicy(hwnd, flipPolicy);

                if (Utility.IsOSWindows7OrNewer)
                {
                    NativeMethods.DwmSetWindowAttributeDisallowPeek(hwnd, true);
                }
            }
        }

        private void _TryExtendGlass(IntPtr hwnd)
        {
            Assert.IsNotDefault(hwnd);
            Assert.IsTrue(CheckAccess());

            // Expect that this might be called on OSes other than Vista.
            if (!Utility.IsOSVistaOrNewer)
            {
                // Not an error.  Just not on Vista so we're not going to get glass.
                return;
            }

            HwndSource hwndSource = HwndSource.FromHwnd(hwnd);

            bool isGlassEnabled = NativeMethods.DwmIsCompositionEnabled();

            if (!isGlassEnabled)
            {
                Background = GetNonGlassBackground(this);
                hwndSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
            }
            else
            {
                // Apply the transparent background to both the Window and the HWND
                Background = Brushes.Transparent;
                hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;

                var dwmMargin = new MARGINS
                {
                    cxLeftWidth = -1,
                    cxRightWidth = -1,
                    cyTopHeight = -1,
                    cyBottomHeight = -1,
                };

                NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref dwmMargin);
            }
        }

        private IntPtr _WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WM)msg)
            {
                case WM.SIZE:
                    const int SIZE_MAXIMIZED = 2;
                    // No, you didn't.  Even if you think you did, you didn't.
                    if (wParam.ToInt32() == SIZE_MAXIMIZED)
                    {
                        NativeMethods.PostMessage(hwnd, WM.SYSCOMMAND, new IntPtr((int)SC.RESTORE), IntPtr.Zero);
                    }
                    break;
                case WM.NCACTIVATE:
                    handled = true;
                    // Need to do this to prevent the chrome from flickering on deactivate.
                    return NativeMethods.DefWindowProc(hwnd, WM.NCACTIVATE, wParam, new IntPtr(-1));
                case WM.GETMINMAXINFO:
                    // We have WS_THICKFRAME to enable the glass frame but really this should be a fixed size.
                    var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                    Point devSize = DpiHelper.LogicalPixelsToDevice(new Point(ActualWidth, ActualHeight));
                    var pt = new POINT { x = (int)devSize.X, y = (int)devSize.Y };
                    mmi.ptMinTrackSize = pt;
                    mmi.ptMaxTrackSize = pt;
                    mmi.ptMaxSize = pt;
                    Marshal.StructureToPtr(mmi, lParam, false);

                    handled = true;
                    return IntPtr.Zero;
                case WM.NCCALCSIZE:
                    // WM_NCCALCSIZE gives the window dimensions in the lParam and expects the client dimensions in the same field on return.
                    // By not modifying it and signaling the message as handled, we can use the entire window dimensions as our client area.
                    handled = true;
                    return IntPtr.Zero;

                case WM.DWMCOMPOSITIONCHANGED:
                    _TryExtendGlass(hwnd);
                    // Empirically it looks like these (or at least the no-peek flag) need to be reapplied when glass composition changes.
                    // Haven't found this documented, may be a bug in Windows.
                    _SetDwmAttributes(hwnd);

                    handled = false;
                    return IntPtr.Zero;
            }
            handled = false;
            return IntPtr.Zero;
        }


        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop))
                ? DragDropEffects.Copy
                : DragDropEffects.None;

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

            e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop))
                ? DragDropEffects.Copy
                : DragDropEffects.None;

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
                ((MainWindow)Application.Current.MainWindow).DoDrop(fileNames);
                Close();
                //((MainWindow)Application.Current.MainWindow).HideMiniMode();
            }

#if !USE_STANDARD_DRAGDROP
            DropTargetHelper.Drop(e.Data, e.GetPosition(this), DragDropEffects.Copy);
#endif
        }

        private void _OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.PART_PopupNotification.IsOpen = false;
            this.DragMove();
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                {
                    switch (e.Key)
                    {
                        case Key.Right:
                        case Key.Space:
                        case Key.PageDown:
                            if (this.PART_ZapScroller2.NextCommand.CanExecute(null))
                            {
                                this.PART_ZapScroller2.NextCommand.Execute(null);
                            }
                            e.Handled = true;
                            break;
                        case Key.Left:
                        case Key.PageUp:
                            if (this.PART_ZapScroller2.PreviousCommand.CanExecute(null))
                            {
                                this.PART_ZapScroller2.PreviousCommand.Execute(null);
                            }
                            e.Handled = true;
                            break;
                        case Key.End:
                            if (this.PART_ZapScroller2.LastCommand.CanExecute(null))
                            {
                                this.PART_ZapScroller2.LastCommand.Execute(null);
                            }
                            e.Handled = true;
                            break;
                        case Key.Home:
                            if (this.PART_ZapScroller2.FirstCommand.CanExecute(null))
                            {
                                this.PART_ZapScroller2.FirstCommand.Execute(null);
                            }
                            e.Handled = true;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void OnWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                if (DoubleUtilities.LessThan(e.Delta, 0))
                {
                    if (this.PART_ZapScroller2.NextCommand.CanExecute(null))
                    {
                        this.PART_ZapScroller2.NextCommand.Execute(null);
                    }
                }
                else if (DoubleUtilities.GreaterThan(e.Delta, 0))
                {
                    if (this.PART_ZapScroller2.PreviousCommand.CanExecute(null))
                    {
                        this.PART_ZapScroller2.PreviousCommand.Execute(null);
                    }
                }

                e.Handled = true;
            }
        }

        private void _OnNavigateToContentButtonClicked(object sender, RoutedEventArgs e)
        {
            object content = ViewManager.GetNavigationContent((DependencyObject)sender);
            Assert.IsNotNull(content);
            _DoNavigate(content);
            e.Handled = true;
        }

        private void _OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = (Hyperlink)sender;
            _DoNavigate(hyperlink.NavigateUri);
            e.Handled = true;
        }

        private void _DoNavigate(object content)
        {
            Uri externalUri;
            bool internalNavigation = ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.CanFindInternalNavigator(content, out externalUri);
            if (internalNavigation || !FacebookClientApplication.OpenWebContentInExternalBrowser)
            {
                ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.Execute(content);
                this.Close();
            }
            else if (externalUri != null)
            {
                Process.Start(new ProcessStartInfo(externalUri.OriginalString));
            }
        }

        private void _OnRestoreClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void _OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void _OnFacebookButtonClick(object sender, RoutedEventArgs e)
        {
            var currentPost = PART_ZapScroller2.Items.CurrentItem as ActivityPost;
            if (currentPost != null)
            {
                if (currentPost.Actor != null)
                {
                    Uri profileUri = currentPost.Actor.ProfileUri;
                    if (profileUri != null)
                    {
                        Process.Start(new ProcessStartInfo(profileUri.OriginalString));
                        return;
                    }
                }
            }

            Process.Start(new ProcessStartInfo("http://facebook.com"));
        }

        internal bool ProcessCommandLineArgs(IList<string> commandLineArgs)
        {
            if (commandLineArgs != null && commandLineArgs.Count > 0)
            {
                int argIndex = 0;
                while (argIndex < commandLineArgs.Count)
                {
                    string commandSwitch = commandLineArgs[argIndex].ToLowerInvariant();
                    if (commandSwitch.StartsWith("-uri:") || commandSwitch.StartsWith("/uri:"))
                    {
                        Uri uri;
                        if (Uri.TryCreate(commandSwitch.Substring("-uri:".Length), UriKind.Absolute, out uri))
                        {
                            _DoNavigate(uri);
                            return true;
                        }
                    }
                    else
                    {
                        switch (commandSwitch)
                        {
                            case "-exitminimode":
                            case "/exitminimode":
                                this.Close();
                                return true;
                            case "-exit":
                            case "/exit":
                                Application.Current.MainWindow.Close();
                                return true;
                        }
                    }
                    ++argIndex;
                }
            }
            return false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            PART_PopupNotification.IsOpen = false;

            ((FacebookClientApplication)Application.Current).SwitchToMainMode();
            e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
