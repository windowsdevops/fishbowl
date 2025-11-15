//-----------------------------------------------------------------------
// <copyright file="PhotoDisplayControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control used to display and animate a photo.
// </summary>
//-----------------------------------------------------------------------

using System.Windows.Interop;

namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using EffectLibrary;
    using FacebookClient.Controls;
    using Contigo;
    using System.Collections.Generic;
    using ClientManager.Controls;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Diagnostics;

    /// <summary>
    /// Control used to display and animate a photo.
    /// </summary>
    [TemplatePart(Name = "PART_PhotoViewbox", Type = typeof(Viewbox)),
     TemplatePart(Name = "PART_PhotoImage", Type = typeof(FacebookImageControl))]
    public class PhotoDisplayControl : Control
    {
        #region Fields
        public static readonly DependencyProperty FacebookPhotoProperty =
            DependencyProperty.Register("FacebookPhoto", typeof(FacebookPhoto), typeof(PhotoDisplayControl));

        /// <summary>
        /// Dependency Property backing store for PhotoZoomFactor.
        /// </summary>
        public static readonly DependencyProperty PhotoZoomFactorProperty =
            DependencyProperty.Register("PhotoZoomFactor", typeof(double), typeof(PhotoDisplayControl), new UIPropertyMetadata(1.0));

        /// <summary>
        /// DependencyProperty backing store for FittingPhotoToWindow.
        /// </summary>
        public static readonly DependencyProperty FittingPhotoToWindowProperty =
            DependencyProperty.Register("FittingPhotoToWindow", typeof(bool), typeof(PhotoDisplayControl), new UIPropertyMetadata(true, new PropertyChangedCallback(OnFittingPhotoToWindowChanged)));

        public static readonly DependencyProperty CommentsVisibleProperty =
            DependencyProperty.Register("CommentsVisible", typeof(bool), typeof(PhotoDisplayControl), new UIPropertyMetadata(false));

        /// <summary>
        /// RoutedCommand to zoom the displayed photo in.
        /// </summary>
        public static readonly RoutedCommand ZoomPhotoInCommand = new RoutedCommand("ZoomPhotoIn", typeof(PhotoDisplayControl));

        /// <summary>
        /// RoutedCommand to zoom the displayed photo out.
        /// </summary>
        public static readonly RoutedCommand ZoomPhotoOutCommand = new RoutedCommand("ZooomPhotoOut", typeof(PhotoDisplayControl));

        /// <summary>
        /// RoutedCommand to fit the displayed photo to the window size.
        /// </summary>
        public static readonly RoutedCommand FitPhotoToWindowCommand = new RoutedCommand("FitPhotoToWindow", typeof(PhotoDisplayControl));

        /// <summary>
        /// Whether the current animation results in a fit-to-window upon completion.
        /// </summary>
        private bool switchFittingMode;

        /// <summary>
        /// The Viewbox displaying the photo.
        /// </summary>
        private Viewbox photoViewbox;

        /// <summary>
        /// The ScrollViewer that hosts this control.
        /// </summary>
        private ScrollViewer scrollHost;

        /// <summary>
        /// Last MouseDown position.
        /// </summary>
        private Point lastPosition;

        public static readonly DependencyProperty FacebookImageControlProperty = DependencyProperty.Register(
            "FacebookImageControl", 
            typeof(FacebookImageControl), 
            typeof(PhotoDisplayControl),
                new FrameworkPropertyMetadata(null,
                    (d, e) => ((PhotoDisplayControl)d)._OnFacebookImageChanged(e)));

        private void _OnImageSourceChanged(object sender, EventArgs e)
        {
            this.DoInitialFit();
            if (this.PhotoStateChanged != null)
            {
                this.PhotoStateChanged(this, EventArgs.Empty);
            }
        }

        private void _OnFacebookImageChanged(DependencyPropertyChangedEventArgs e)
        {
            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(FacebookImageControl.ImageSourceProperty, typeof(FacebookImageControl));

            var oldControl = (FacebookImageControl)e.OldValue;
            var newControl = (FacebookImageControl)e.NewValue;
            if (oldControl != null)
            {
                dpd.RemoveValueChanged(oldControl, _OnImageSourceChanged);
            }

            if (!this.IsLoaded)
            {
                return;
            }

            if (newControl != null)
            {
                dpd.AddValueChanged(newControl, _OnImageSourceChanged);
            }
        }

        public FacebookImageControl FacebookImageControl
        {
            get { return (FacebookImageControl)GetValue(FacebookImageControlProperty); }
            set { SetValue(FacebookImageControlProperty, value); }
        }


        /// <summary>
        /// The FacebookImageControl containing the photo.
        /// </summary>
        private FacebookImageControl photoImage;

        /// <summary>
        /// Animation on the ZoomBlurEffect used when a photo's zoomPhotoAnimation is active.
        /// </summary>
        private DoubleAnimation zoomPhotoBlurEffectAnimation;

        /// <summary>
        /// The Effect applied to the photo when it is being zoomed.
        /// </summary>
        private ZoomBlurEffect zoomPhotoBlurEffect;

        /// <summary>
        /// Transform used to zoom the displayed photo.
        /// </summary>
        private ScaleTransform photoZoomTransform;

        /// <summary>
        /// The total size of all borders around the photo (to be excluded when calculating the fit-to-window zoom factor).
        /// </summary>
        private double photoBorderWidth = 65;

        /// <summary>
        /// The factor to scale by when zooming.
        /// </summary>
        private double baseZoomFactor = 0.20;

        /// <summary>
        /// The zoom factor for the current that causes a change in size by baseZoomFactor percent.
        /// </summary>
        private double scaledZoomFactor;

        /// <summary>
        /// Relative viewport position, coordinates should be between 0.0 and 1.0.
        /// </summary>
        private Point zoomCenter = new Point(0.5, 0.5); 

        /// <summary>
        /// The event that is raised when photo state has changed.s
        /// </summary>
        public event PhotoStateChangedEventHandler PhotoStateChanged;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the PhotoDisplayControl class.
        /// </summary>
        public PhotoDisplayControl()
        {
            this.CommandBindings.Add(new CommandBinding(ZoomPhotoInCommand, new ExecutedRoutedEventHandler(OnZoomPhotoInCommand)));
            this.CommandBindings.Add(new CommandBinding(ZoomPhotoOutCommand, new ExecutedRoutedEventHandler(OnZoomPhotoOutCommand), new CanExecuteRoutedEventHandler(OnZoomPhotoOutCanExecute)));
            this.CommandBindings.Add(new CommandBinding(FitPhotoToWindowCommand, new ExecutedRoutedEventHandler(OnFitPhotoToWindow)));
            this.Loaded += (sender, e) =>
            {
                InitializeMultiTouch();
                this.DoInitialFit();

                if (FacebookImageControl != null)
                {
                    DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(FacebookImageControl.ImageSourceProperty, typeof(FacebookImageControl));
                    dpd.AddValueChanged(FacebookImageControl, _OnImageSourceChanged);
                }
            };

            this.Unloaded += (sender, e) =>
            {
                if (FacebookImageControl != null)
                {
                    DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(FacebookImageControl.ImageSourceProperty, typeof(FacebookImageControl));
                    dpd.RemoveValueChanged(FacebookImageControl, _OnImageSourceChanged);
                }
            };
        }
        #endregion

        #region Enums
        /// <summary>
        /// How we're fitting the photo to the window size.
        /// </summary>
        protected enum FitToWindowType
        {
            /// <summary>
            /// Zoom the photo in (initial display).
            /// </summary>
            InitialFit,

            /// <summary>
            /// Animate the change in size.
            /// </summary>
            AnimatedFit,

            /// <summary>
            /// Snap the change in size.
            /// </summary>
            ImmediateFit
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the FacebookImageControl containing the photo.
        /// </summary>
        public FacebookImageControl PhotoImage
        {
            get { return this.photoImage; }
        }

        public FacebookPhoto FacebookPhoto
        {
            get { return (FacebookPhoto)GetValue(FacebookPhotoProperty); }
            set { SetValue(FacebookPhotoProperty, value); }
        }

        /// <summary>
        /// Gets a value by which to scale the displayed photo.
        /// </summary>
        public double PhotoZoomFactor
        {
            get { return (double)GetValue(PhotoZoomFactorProperty); }
            protected set { SetValue(PhotoZoomFactorProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether we are fitting the photo to the window size or resizing it independently.
        /// </summary>
        public bool FittingPhotoToWindow
        {
            get { return (bool)GetValue(FittingPhotoToWindowProperty); }
            set { SetValue(FittingPhotoToWindowProperty, value); }
        }

        public bool CommentsVisible
        {
            get { return (bool)GetValue(CommentsVisibleProperty); }
            set { SetValue(CommentsVisibleProperty, value); }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Sets up the rotation animations once the control template has been applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            zoomPhotoBlurEffectAnimation = new DoubleAnimation
            { 
                AccelerationRatio = .4,
                DecelerationRatio = .2
            };

            photoZoomTransform = this.Template.FindName("PART_PhotoZoomFactorTransform", this) as ScaleTransform;
            photoViewbox = this.Template.FindName("PART_PhotoViewbox", this) as Viewbox;
            photoImage = this.Template.FindName("PART_PhotoImage", this) as FacebookImageControl;

            DependencyObject element = this;
            while (element != null)
            {
                if (element is ScrollViewer)
                {
                    scrollHost = (ScrollViewer)element;
                    break;
                }

                element = VisualTreeHelper.GetParent(element);
            }

            if (element == null)
            {
                throw new InvalidOperationException("This control must be hosted in a ScrollViewer.");
            }

            this.scrollHost.ScrollChanged += new ScrollChangedEventHandler((sender, e) =>
            {
                if (!this.FittingPhotoToWindow && (e.ExtentWidthChange != 0.0 || e.ExtentHeightChange != 0.0))
                {
                    double previousExtentWidth = e.ExtentWidth - e.ExtentWidthChange;
                    double previousExtentHeight = e.ExtentHeight - e.ExtentHeightChange;

                    this.scrollHost.ScrollToHorizontalOffset(e.ExtentWidth / previousExtentWidth * (e.HorizontalOffset - e.HorizontalChange + e.ViewportWidth * zoomCenter.X) -
                        e.ViewportWidth * zoomCenter.X);
                    this.scrollHost.ScrollToVerticalOffset(e.ExtentHeight / previousExtentHeight * (e.VerticalOffset - e.VerticalChange + e.ViewportHeight * zoomCenter.Y) -
                        e.ViewportHeight * zoomCenter.Y);
                }
            });

            this.FacebookImageControl = photoImage;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);


        public static Point BetterGetCursorPosition(Visual visual)
        {
            Win32Point _win32mouse = new Win32Point();
            GetCursorPos(ref _win32mouse);
            return visual.PointFromScreen(new Point(_win32mouse.X, _win32mouse.Y));
        }

        /// <summary>
        /// Does initial animated fit after image source has been set.
        /// </summary>
        public void DoInitialFit()
        {
            this.FitPhotoToWindow(FitToWindowType.InitialFit);
            this.scaledZoomFactor = this.PhotoZoomFactor * this.baseZoomFactor;
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Fits the currently displayed photo to the window size.
        /// </summary>
        /// <param name="initialFit">Whether this is the initial fit-to-window pass, or as a response to a user click.</param>
        protected void FitPhotoToWindow(FitToWindowType initialFit)
        {
            if (this.PhotoImage != null && this.PhotoImage.ImageSource != null)
            {
                double oldPhotoZoomFactor = this.PhotoZoomFactor;

                this.CalculateStandardZoom();

                if (initialFit == FitToWindowType.InitialFit)
                {
                    // Reset the scaling value
                    if (this.photoZoomTransform != null)
                    {
                        this.photoZoomTransform.ScaleX = this.PhotoZoomFactor;
                        this.photoZoomTransform.ScaleY = this.PhotoZoomFactor;
                    }

                    this.FittingPhotoToWindow = true;
                }
                else if (initialFit == FitToWindowType.AnimatedFit)
                {
                    if (this.photoZoomTransform != null)
                    {
                        this.photoZoomTransform.ScaleX = this.PhotoZoomFactor;
                        this.photoZoomTransform.ScaleY = this.PhotoZoomFactor;

                        if (this.switchFittingMode && this.photoViewbox != null)
                        {
                            this.photoViewbox.Stretch = Stretch.Uniform;
                            this.switchFittingMode = false;
                        }

                        this.BeginZoomPhotoBlurEffectAnimationOnPhotoImage();
                    }
                }
                else
                {
                    if (this.photoZoomTransform != null)
                    {
                        this.photoZoomTransform.ScaleX = this.PhotoZoomFactor;
                        this.photoZoomTransform.ScaleY = this.PhotoZoomFactor;

                        if (this.switchFittingMode && this.photoViewbox != null)
                        {
                            this.photoViewbox.Stretch = Stretch.Uniform;
                            this.switchFittingMode = false;
                        }

                        this.BeginZoomPhotoBlurEffectAnimationOnPhotoImage();
                    }
                }
            }
        }

        /// <summary>
        /// Zooms the currently displayed photo in. 
        /// </summary>
        protected void ZoomPhotoIn()
        {
            this.zoomCenter.X = 0.5;
            this.zoomCenter.Y = 0.5;

            if (this.FittingPhotoToWindow)
            {
                this.FittingPhotoToWindow = false;
                this.CalculateStandardZoom();
            }

            this.PhotoZoomFactor += this.scaledZoomFactor;

            if (this.photoZoomTransform != null)
            {
                this.photoZoomTransform.ScaleX = this.PhotoZoomFactor;
                this.photoZoomTransform.ScaleY = this.PhotoZoomFactor;

                if (this.switchFittingMode && this.photoViewbox != null)
                {
                    this.photoViewbox.Stretch = Stretch.Uniform;
                    this.switchFittingMode = false;
                }
            }
        }

        /// <summary>
        /// Zooms the currently displayed photo out.
        /// </summary>
        protected void ZoomPhotoOut()
        {
            this.zoomCenter.X = 0.5;
            this.zoomCenter.Y = 0.5;

            if (this.CanZoomPhotoOut())
            {
                if (this.FittingPhotoToWindow)
                {
                    this.FittingPhotoToWindow = false;
                    this.CalculateStandardZoom();
                }

                this.PhotoZoomFactor -= this.scaledZoomFactor;

                if (this.photoZoomTransform != null)
                {
                    this.photoZoomTransform.ScaleX = PhotoZoomFactor;
                    this.photoZoomTransform.ScaleY = PhotoZoomFactor;

                    if (this.switchFittingMode && this.photoViewbox != null)
                    {
                        this.photoViewbox.Stretch = Stretch.Uniform;
                        this.switchFittingMode = false;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the currently displayed photo can be zoomed out any further.
        /// </summary>
        /// <returns>A value indicating whether the currently displayed photo can be zoomed out any further.</returns>
        protected bool CanZoomPhotoOut()
        {
            if ((this.PhotoZoomFactor - this.scaledZoomFactor) > 0.2)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets and beings and element's ZoomPhotoBlurEffectAnimation if the client
        /// has the right hardware capabilities to run the Effect.
        /// </summary>
        protected void BeginZoomPhotoBlurEffectAnimationOnPhotoImage()
        {
            if (FacebookClientApplication.IsShaderEffectSupported)
            {
                if (!(this.photoImage.Effect is ZoomBlurEffect))
                {
                    this.zoomPhotoBlurEffect = new ZoomBlurEffect();
                    this.zoomPhotoBlurEffect.CenterX = 0.5;
                    this.zoomPhotoBlurEffect.CenterY = 0.5;
                    this.photoImage.Effect = this.zoomPhotoBlurEffect;
                }

                // The amount of blur is relative to the zoomed factor of the photo
                double blurAmount = .2 * this.PhotoZoomFactor;

                this.zoomPhotoBlurEffectAnimation.Duration = TimeSpan.FromMilliseconds(100);
                this.zoomPhotoBlurEffectAnimation.From = blurAmount;
                this.zoomPhotoBlurEffectAnimation.To = 0;

                this.photoImage.Effect.BeginAnimation(ZoomBlurEffect.BlurAmountProperty, this.zoomPhotoBlurEffectAnimation, HandoffBehavior.SnapshotAndReplace);
            }
        }

        /// <summary>
        /// Handler to change the control layout when FittingPhotoToWindow changes so that
        /// fit to window does indeed cause the photo to fit to window.
        /// </summary>
        /// <param name="newValue">The new FittingPhotoToWindow value.</param>
        protected void OnFittingPhotoToWindowChanged(bool newValue)
        {
            if (this.photoViewbox != null)
            {
                // Stop tagging
                this.RestoreInitialState();

                if (newValue)
                {
                    this.FitPhotoToWindow(FitToWindowType.AnimatedFit);
                    this.switchFittingMode = true;

                    // Viewbox setting is changed at the end of the zoom animation.
                }
                else
                {
                    this.photoViewbox.Stretch = Stretch.None;
                }
            }
        }
        
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            lastPosition = e.GetPosition(this.scrollHost);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point newPosition = e.GetPosition(this.scrollHost);
                double dx = newPosition.X - lastPosition.X;
                double dy = newPosition.Y - lastPosition.Y;

                double horizontalOffset = Math.Min(Math.Max(this.scrollHost.HorizontalOffset - dx, 0.0), this.scrollHost.ScrollableWidth);
                double verticalOffset = Math.Min(Math.Max(this.scrollHost.VerticalOffset - dy, 0.0), this.scrollHost.ScrollableHeight);

                this.scrollHost.ScrollToHorizontalOffset(horizontalOffset);
                this.scrollHost.ScrollToVerticalOffset(verticalOffset);

                lastPosition = newPosition;
            }
        }
        

        #endregion

        #region Private Methods
        /// <summary>
        /// Command to zoom the displayed photo in.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnZoomPhotoInCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoDisplayControl photoDisplay = sender as PhotoDisplayControl;
            if (photoDisplay != null)
            {
                // Stop tagging
                photoDisplay.RestoreInitialState();

                photoDisplay.ZoomPhotoIn();
            }
        }

        /// <summary>
        /// Command to zoom the displayed photo out.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnZoomPhotoOutCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoDisplayControl photoDisplay = sender as PhotoDisplayControl;
            if (photoDisplay != null)
            {
                // Stop tagging
                photoDisplay.RestoreInitialState();

                photoDisplay.ZoomPhotoOut();
            }
        }

        /// <summary>
        /// Determines whether the photo can be zoomed out any further.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnZoomPhotoOutCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            PhotoDisplayControl photoDisplay = sender as PhotoDisplayControl;
            if (photoDisplay != null)
            {
                e.CanExecute = photoDisplay.CanZoomPhotoOut();
            }
        }

        /// <summary>
        /// Command to fit the displayed photo to the window size.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnFitPhotoToWindow(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoDisplayControl photoDisplay = sender as PhotoDisplayControl;

            if (photoDisplay != null)
            {
                photoDisplay.FittingPhotoToWindow = !photoDisplay.FittingPhotoToWindow;
            }
        }

        /// <summary>
        /// Handler for FittingPhotoToWindow changes.
        /// </summary>
        /// <param name="element">The element that changed.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnFittingPhotoToWindowChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            ((PhotoDisplayControl)element).OnFittingPhotoToWindowChanged((bool)e.NewValue);
        }

        /// <summary>
        /// Close the tagging and comments controls and bring album to initial state.
        /// </summary>
        private void RestoreInitialState()
        {
            // Close commenting function
            if (this.CommentsVisible)
            {
                this.CommentsVisible = false;
            }

            // Raise photo state changed event
            if (this.PhotoStateChanged != null)
            {
                this.PhotoStateChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Calculates the zoom factor for a fit-to-window zoom.
        /// </summary>
        private void CalculateStandardZoom()
        {
            double widthZoom;
            double heightZoom;

            if (this.PhotoImage.ImageSource != null)
            {
                widthZoom = (this.scrollHost.ViewportWidth - this.photoBorderWidth) / this.PhotoImage.ImageSource.Width;
                heightZoom = (this.scrollHost.ViewportHeight - this.photoBorderWidth) / this.PhotoImage.ImageSource.Height;
                this.PhotoZoomFactor = Math.Abs(Math.Min(widthZoom, heightZoom));
            }
        }
        #endregion

        #region multi-touch support
        [DllImport("user32.dll")]
        public static extern bool SetProp(IntPtr hWnd, string lpString, IntPtr hData);

        int _numTouches = 0;
        int[] _touchId = new int[2];
        Point[] _touchInitialPts = new Point[2];
        Point[] _touchLastPts = new Point[2];
        double _initialZoomFactor;
        bool _inZooming = false;

        private IInputElement GetContainer()
        {
           
            return (IInputElement)this.scrollHost;
        }

        private void InitializeMultiTouch()
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            // Enable multi-touch input for stylus
            if (hwndSource != null)
                SetProp(hwndSource.Handle, "MicrosoftTabletPenServiceProperty", new IntPtr(0x01000000));

            SetValue(Stylus.IsFlicksEnabledProperty, false);

        }

        protected override void OnPreviewStylusDown(StylusDownEventArgs e)
        {
            //Debug.WriteLine("Down");
            if (_numTouches == 0)
            {
                this.CaptureStylus();
                _inZooming = false;
            }
            if (_numTouches == 1 )
                _initialZoomFactor = this.PhotoZoomFactor;
            if (_numTouches < 2)
            {
                _touchId[_numTouches] = e.StylusDevice.Id;
                _touchInitialPts[_numTouches] = e.GetPosition(GetContainer());
                _touchLastPts[_numTouches] = _touchInitialPts[_numTouches];

                ++_numTouches;
            }
            e.Handled = true;
        }

        protected override void OnPreviewStylusMove(StylusEventArgs e)
        {
            base.OnPreviewStylusMove(e);

            if (_numTouches == 1 && _touchId[0] == e.StylusDevice.Id && !_inZooming)
            {
                Point p0 = e.GetPosition(GetContainer());
                double dx = p0.X - _touchLastPts[0].X;
                double dy = p0.Y - _touchLastPts[0].Y;

                double horizontalOffset = Math.Min(Math.Max(this.scrollHost.HorizontalOffset - dx, 0.0), this.scrollHost.ScrollableWidth);
                double verticalOffset = Math.Min(Math.Max(this.scrollHost.VerticalOffset - dy, 0.0), this.scrollHost.ScrollableHeight);

                this.scrollHost.ScrollToHorizontalOffset(horizontalOffset);
                this.scrollHost.ScrollToVerticalOffset(verticalOffset);

                _touchLastPts[0] = p0;
            }
            else if (_numTouches == 2 && (_touchId[0] == e.StylusDevice.Id || _touchId[1] == e.StylusDevice.Id))
            {
                int index = _touchId[0] == e.StylusDevice.Id ? 0 : 1;
                Point p0 = e.GetPosition(GetContainer());
                Point p1 = _touchLastPts[1 - index];
                double distance = (p0 - p1).Length;
                double scale = distance / (_touchInitialPts[0] - _touchInitialPts[1]).Length;
                if (scale > 0.1 && scale < 20)
                {

                    this.FittingPhotoToWindow = false;
                    this.PhotoZoomFactor = _initialZoomFactor * scale;

                    if (this.photoZoomTransform != null)
                    {
                        this.photoZoomTransform.ScaleX = this.PhotoZoomFactor;
                        this.photoZoomTransform.ScaleY = this.PhotoZoomFactor;
                    }
                }
                /*
                double dx = ((p0.X + p1.X) - (_touchLastPts[0].X + _touchLastPts[1].X)) / 2;
                double dy = ((p0.Y + p1.Y) - (_touchLastPts[0].Y + _touchLastPts[1].Y)) / 2;

                double horizontalOffset = Math.Min(Math.Max(this.scrollHost.HorizontalOffset - dx, 0.0), this.scrollHost.ScrollableWidth);
                double verticalOffset = Math.Min(Math.Max(this.scrollHost.VerticalOffset - dy, 0.0), this.scrollHost.ScrollableHeight);

                this.scrollHost.ScrollToHorizontalOffset(horizontalOffset);
                this.scrollHost.ScrollToVerticalOffset(verticalOffset);
                */


                _touchLastPts[index] = p0;
                _inZooming = true;
            }
            e.Handled = true;
        }

        protected override void OnPreviewStylusUp(StylusEventArgs e)
        {
            base.OnPreviewStylusUp(e);

            if (_numTouches > 0)
            {
                if (e.StylusDevice.Id == _touchId[0])
                {
                    _touchId[0] = _touchId[1];
                    _touchInitialPts[0] = _touchInitialPts[1];
                    _touchLastPts[0] = _touchLastPts[1];
                    _numTouches--;
                }
                else if (e.StylusDevice.Id == _touchId[1])
                {
                    _numTouches--;
                }
            }
            if (_numTouches == 0)
            {
                this.ReleaseStylusCapture();
            }
            e.Handled = true;
        }
        protected override void OnStylusSystemGesture(StylusSystemGestureEventArgs e)
        {
            e.Handled = true;
        }

        #endregion

    }

    /// <summary>
    /// Event handler for when photo changes state.
    /// </summary>
    /// <param name="sender">Event source.</param>
    /// <param name="e">Event args.</param>
    public delegate void PhotoStateChangedEventHandler(object sender, EventArgs e);
}
