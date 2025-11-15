namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Media3D;
    using System.Windows.Threading;
    using ClientManager;

    public class ScaleScrollViewer : ScrollViewer
    {
        private static TimeSpan s_animationDuration = TimeSpan.FromMilliseconds(500);
        private static KeySpline s_animationSpline = new KeySpline(0.05, 0.9, 0.75, 1.0);
        private static double s_lineHeight = 16.0;

        private FrameworkElement content;
        private ScaleTransform scaleTransform;
        private ScrollBar scrollBar;
        private bool disableAnimation;

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register("Scale", typeof(double), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(1.0, new PropertyChangedCallback(ScaleChanged), new CoerceValueCallback(CoerceScale)));
        public static readonly DependencyProperty MinScaleProperty = DependencyProperty.Register("MinScale", typeof(double), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(0.1, new PropertyChangedCallback(MinMaxScaleChanged)));
        public static readonly DependencyProperty MaxScaleProperty = DependencyProperty.Register("MaxScale", typeof(double), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(4.0, new PropertyChangedCallback(MinMaxScaleChanged)));
        public static readonly DependencyProperty ScaleIncrementProperty = DependencyProperty.Register("ScaleIncrement", typeof(double), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(0.1));
        public static readonly DependencyProperty MouseWheelScrollDeltaProperty = DependencyProperty.Register("MouseWheelScrollDelta", typeof(double), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(48.0));
        public static readonly DependencyProperty FinalVerticalOffsetProperty = DependencyProperty.Register("FinalVerticalOffset", typeof(double), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnFinalVerticalOffsetChanged)));
        public static readonly DependencyProperty ActualVerticalOffsetProperty = DependencyProperty.Register("ActualVerticalOffset", typeof(double), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnActualVerticalOffsetChanged)));
        public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register("IsAnimationEnabled", typeof(bool), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register("LineHeight", typeof(double), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(s_lineHeight));
        public static readonly DependencyProperty NavigateRightCommandProperty = DependencyProperty.Register("NavigateRightCommand", typeof(ICommand), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty NavigateRightCommandParameterProperty = DependencyProperty.Register("NavigateRightCommandParameter", typeof(object), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty FocusOnLoadProperty = DependencyProperty.Register("FocusOnLoad", typeof(bool), typeof(ScaleScrollViewer),
            new FrameworkPropertyMetadata(false));

        public static readonly RoutedCommand ZoomInCommand = new RoutedCommand("ZoomIn", typeof(ScaleScrollViewer));
        public static readonly RoutedCommand ZoomOutCommand = new RoutedCommand("ZoomOut", typeof(ScaleScrollViewer));
        public static RoutedCommand ToggleScrollingAnimationCommand = new RoutedCommand("ToggleScrollingAnimation", typeof(ScaleScrollViewer));

        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        public double MinScale
        {
            get { return (double)GetValue(MinScaleProperty); }
            set { SetValue(MinScaleProperty, value); }
        }

        public double MaxScale
        {
            get { return (double)GetValue(MaxScaleProperty); }
            set { SetValue(MaxScaleProperty, value); }
        }

        public double ScaleIncrement
        {
            get { return (double)GetValue(ScaleIncrementProperty); }
            set { SetValue(ScaleIncrementProperty, value); }
        }

        public double MouseWheelScrollDelta
        {
            get { return (double)GetValue(MouseWheelScrollDeltaProperty); }
            set { SetValue(MouseWheelScrollDeltaProperty, value); }
        }

        public double FinalVerticalOffset
        {
            get { return (double)GetValue(FinalVerticalOffsetProperty); }
            set { SetValue(FinalVerticalOffsetProperty, value); }
        }

        public double ActualVerticalOffset
        {
            get { return (double)GetValue(ActualVerticalOffsetProperty); }
            set { SetValue(ActualVerticalOffsetProperty, value); }
        }

        public bool IsAnimationEnabled
        {
            get { return (bool)GetValue(IsAnimationEnabledProperty); }
            set { SetValue(IsAnimationEnabledProperty, value); }
        }

        public double LineHeight
        {
            get { return (double)GetValue(LineHeightProperty); }
            set { SetValue(LineHeightProperty, value); }
        }

        public ICommand NavigateRightCommand
        {
            get { return (ICommand)GetValue(NavigateRightCommandProperty); }
            set { SetValue(NavigateRightCommandProperty, value); }
        }

        public object NavigateRightCommandParameter
        {
            get { return GetValue(NavigateRightCommandParameterProperty); }
            set { SetValue(NavigateRightCommandParameterProperty, value); }
        }

        public bool FocusOnLoad
        {
            get { return (bool)GetValue(FocusOnLoadProperty); }
            set { SetValue(FocusOnLoadProperty, value); }
        }

        public ScaleScrollViewer()
        {
            this.CommandBindings.Add(new CommandBinding(ZoomInCommand, new ExecutedRoutedEventHandler(OnZoomInCommand),
                new CanExecuteRoutedEventHandler(OnZoomInCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(ZoomOutCommand, new ExecutedRoutedEventHandler(OnZoomOutCommand),
                new CanExecuteRoutedEventHandler(OnZoomOutCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(ToggleScrollingAnimationCommand, new ExecutedRoutedEventHandler(OnToggleScrollingAnimationCommand)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.scrollBar = this.Template.FindName("PART_VerticalScrollBar2", this) as ScrollBar;

            if (this.FocusOnLoad)
            {
                try
                {
                    this.Focus();
                }
                // Happens if the host window is not visible.
                catch (System.ComponentModel.Win32Exception){ }
            }
        }

        /// <summary>
        /// Toggles scrolling animation
        /// </summary>
        protected void ToggleScrollingAnimation()
        {
            if (this.IsAnimationEnabled)
            {
                this.IsAnimationEnabled = false;
            }
            else
            {
                this.IsAnimationEnabled = true;
            }
        }

        private static void ScaleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ScaleScrollViewer ssv = (ScaleScrollViewer)sender;
            double scale = (double)args.NewValue;

            if (ssv.scaleTransform != null)
            {
                ssv.scaleTransform.ScaleX = scale;
                ssv.scaleTransform.ScaleY = scale;
            }
        }

        private static object CoerceScale(DependencyObject sender, object value)
        {
            ScaleScrollViewer ssv = (ScaleScrollViewer)sender;
            double scale = (double)value;

            if (scale > ssv.MaxScale)
            {
                scale = ssv.MaxScale;
            }

            if (scale < ssv.MinScale)
            {
                scale = ssv.MinScale;
            }

            return scale;
        }

        private static void MinMaxScaleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ScaleScrollViewer ssv = (ScaleScrollViewer)sender;
            ssv.CoerceValue(ScaleScrollViewer.ScaleProperty);
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            this.content = newContent as FrameworkElement;

            if (content != null)
            {
                this.scaleTransform = new ScaleTransform(this.Scale, this.Scale);
                this.content.LayoutTransform = this.scaleTransform;                
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (content != null)
                {
                    if (e.Delta > 0)
                    {
                        if (ZoomInCommand.CanExecute(null, this))
                        {
                            ZoomInCommand.Execute(null, this);
                        }
                    }
                    else
                    {
                        if (ZoomOutCommand.CanExecute(null, this))
                        {
                            ZoomOutCommand.Execute(null, this);
                        }
                    }
                }
            }
            else
            {
                if (ScrollInfo != null)
                {
                    if (e.Delta < 0)
                    {
                        this.FinalVerticalOffset = Math.Min(this.FinalVerticalOffset + this.MouseWheelScrollDelta, this.ScrollableHeight);
                    }
                    else
                    {
                        this.FinalVerticalOffset = Math.Max(this.FinalVerticalOffset - this.MouseWheelScrollDelta, 0.0);
                    }
                }
            }

            e.Handled = true;
            base.OnPreviewMouseWheel(e);
        }

        private bool _inPromotion;
        private bool _isPanning;
        private bool _isInInertia;
        private Point _initialPoint;
        private Point _currentPoint;
        private Point _lastPoint;
        private double _initialVerticalOffset;
        private DispatcherTimer _inertiaTimer;
        private double _friction = 0.98;
        private double _velocity;

        /// <summary>
        /// Command to toggle scrolling animation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnToggleScrollingAnimationCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ScaleScrollViewer scaleScrollViewer = sender as ScaleScrollViewer;
            if (scaleScrollViewer != null)
            {
                scaleScrollViewer.ToggleScrollingAnimation();
            }
        }


        private bool IgnoredControls(DependencyObject o)
        {
            return o is ScrollBar ||
                   o is TextBox ||
                   o is RichTextBox ||
                   o is Button ||
                   o is RepeatButton;
        }


        private bool CanPan(DependencyObject o)
        {
            while ( o != null && !IgnoredControls(o) && ( o is Visual || o is Visual3D))
                o = VisualTreeHelper.GetParent(o);

            return !IgnoredControls(o);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                && e.MiddleButton == MouseButtonState.Pressed)
            {
                this.Scale = 1.0;
            }

            if (_inPromotion)
            {
                base.OnPreviewMouseDown(e);
            }
            else
            {
                if (CanPan( e.MouseDevice.DirectlyOver as DependencyObject ))
                {
                    e.MouseDevice.Capture(this);
                    e.Handled = true;
                    _initialPoint = e.GetPosition(this);
                    _currentPoint = _initialPoint;
                    _lastPoint = _initialPoint;
                    _isPanning = false;
                    _initialVerticalOffset = VerticalOffset;
                }
                else
                {
                    _inPromotion = true;
                }
            }
        }

        protected override void  OnPreviewMouseMove(MouseEventArgs e)
        {
            if (_inPromotion)
                base.OnPreviewMouseMove(e);
            else if (e.MouseDevice.Captured == this)
            {
                e.Handled = true;
                _lastPoint = _currentPoint;
                _currentPoint = e.GetPosition(this);

                if (!_isPanning)
                {
                    if ((_currentPoint - _initialPoint).Length > 3.0)
                    {
                        _isPanning = true;
                        _inertiaTimer = new DispatcherTimer();
                        _inertiaTimer.Interval = TimeSpan.FromMilliseconds(20);
                        _inertiaTimer.Tick += OnInertiaTimerTick;
                        _inertiaTimer.Start();
                    }
                }
                else
                {
                    ScrollToVerticalOffset(_initialVerticalOffset - _currentPoint.Y + _initialPoint.Y);
                }
            }
        }

        void OnInertiaTimerTick(object sender, EventArgs e)
        {
            if (_isPanning)
            {
                _velocity = _currentPoint.Y - _lastPoint.Y;

            }
            else if (_isInInertia)
            {
                if (Math.Abs(_velocity) > 1.0)
                {
                    _currentPoint.Y += _velocity;
                    ScrollToVerticalOffset(_initialVerticalOffset - _currentPoint.Y + _initialPoint.Y); 
                    _velocity *= _friction;
                }
                else
                {
                    _inertiaTimer.Stop();
                    _inertiaTimer = null;
                    _isInInertia = false;
                }
            }
            // This line needed to keep the scrollbar in sync with the viewer.
            this.scrollBar.SetValue(ScrollBar.ValueProperty, this.VerticalOffset);
        }

        protected override void  OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            if (_inPromotion)
            {
                base.OnPreviewMouseUp(e);
                _inPromotion = false;
           }
            else if (e.MouseDevice.Captured == this)
            {
                e.MouseDevice.Capture(null);
                e.Handled = true;
                if (!_isPanning)
                {
                    _inPromotion = true;
                    Standard.SendMouseInput.Click();
                }
                else
                {
                    _isInInertia = true;
                    _currentPoint = e.GetPosition(this);
                        //TODO: Initiate Inertia
                    //Point p = e.GetPosition(this);

                    //if  (p.Y > _initialPoint.Y)
                    //{
                    //    this.FinalVerticalOffset = Math.Min(_initialVerticalOffset - p.Y + _initialPoint.Y - this.MouseWheelScrollDelta, this.ScrollableHeight);
                    //}
                    //else
                    //{
                    //    this.FinalVerticalOffset = Math.Max(_initialVerticalOffset - p.Y + _initialPoint.Y + this.MouseWheelScrollDelta, 0.0);
                    //}
                }
            }
            _isPanning = false;
        }
    
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // In this function, move the slider rather than the FinalVerticalOffset, to leverage logic in the slider
            // to coerce values < min and > max

            if (!(e.OriginalSource is FacebookClient.CommandTextBox))
            {
                Key key = e.Key;
                if (key == Key.Space)
                {
                    key = (ModifierKeys.None != (Keyboard.Modifiers & ModifierKeys.Shift)) ? Key.PageUp : Key.PageDown;
                }

                switch (key)
                {
                    case Key.Up:
                        this.scrollBar.Value -= this.scrollBar.SmallChange;
                        e.Handled = true;
                        break;
                    case Key.Down:
                        this.scrollBar.Value += this.scrollBar.SmallChange;
                        e.Handled = true;
                        break;
                    case Key.PageUp:
                        this.scrollBar.Value -= this.scrollBar.LargeChange;
                        e.Handled = true;
                        break;
                    case Key.PageDown:
                        this.scrollBar.Value += this.scrollBar.LargeChange;
                        e.Handled = true;
                        break;
                    case Key.End:
                        this.FinalVerticalOffset = this.ScrollableHeight;
                        e.Handled = true;
                        break;
                    case Key.Home:
                        this.FinalVerticalOffset = 0;
                        e.Handled = true;
                        break;
                    case Key.OemPlus:
                        if (ModifierKeys.None != (Keyboard.Modifiers & ModifierKeys.Control))
                        {
                            if (content != null)
                            {
                                if (ZoomInCommand.CanExecute(null, this))
                                {
                                    ZoomInCommand.Execute(null, this);
                                }
                            }
                        }
                        e.Handled = true;
                        break;

                    case Key.OemMinus:
                        if (ModifierKeys.None != (Keyboard.Modifiers & ModifierKeys.Control))
                        {
                            if (content != null)
                            {
                                if (ZoomOutCommand.CanExecute(null, this))
                                {
                                    ZoomOutCommand.Execute(null, this);
                                }
                            }
                        }
                        e.Handled = true;
                        break;
                }
            }

            base.OnPreviewKeyDown(e);
        }
        
        private static void OnFinalVerticalOffsetChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            double newValue = (double)e.NewValue;
            ScaleScrollViewer ssv = sender as ScaleScrollViewer;

            if (ssv.disableAnimation || !ssv.IsAnimationEnabled)
            {
                ssv.disableAnimation = false;
                ssv.BeginAnimation(ScaleScrollViewer.ActualVerticalOffsetProperty, null);
                ssv.ActualVerticalOffset = newValue;
            }
            else
            {
                DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
                animation.KeyFrames.Add(new SplineDoubleKeyFrame(newValue, s_animationDuration, s_animationSpline));
                ssv.BeginAnimation(ScaleScrollViewer.ActualVerticalOffsetProperty, animation);
            }
        }

        private static void OnActualVerticalOffsetChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ScaleScrollViewer ssv = sender as ScaleScrollViewer;
            ssv.ScrollToVerticalOffset(ssv.ActualVerticalOffset);
        }

        private void OnZoomInCommand(object sender, ExecutedRoutedEventArgs e)
        {
            double coefficient = 1.0 + this.ScaleIncrement;
            double scale = this.Scale * coefficient;
            this.Scale = scale;
        }

        private void OnZoomInCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.Scale < this.MaxScale;
        }

        private void OnZoomOutCommand(object sender, ExecutedRoutedEventArgs e)
        {
            double coefficient = 1.0 - this.ScaleIncrement;
            double scale = this.Scale * coefficient;
            this.Scale = scale;
        }

        private void OnZoomOutCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.Scale > this.MinScale;
        } 
    }
}
