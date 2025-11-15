//-----------------------------------------------------------------------
// <copyright file="PhotoSlideShowControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control used to display a slideshow of photo's with transitions.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;
    using ClientManager;
    using ClientManager.Data;
    using ClientManager.View;
    using TransitionEffects;
    using Contigo;

    /// <summary>
    /// Control used to display a slideshow of photo's with transitions.
    /// </summary>
    [TemplatePart(Name = "PART_PhotoHost", Type = typeof(Decorator))]
    public class PhotoSlideShowControl : Control
    {
        #region Fields
        /// <summary>
        /// Dependency property for <see cref="SlideShow"/> property.
        /// </summary>
        public static readonly DependencyProperty SlideShowProperty =
            DependencyProperty.Register(
                "SlideShow",
                typeof(SlideShow),
                typeof(PhotoSlideShowControl),
                new UIPropertyMetadata(null, OnPhotoSlideSlideShowChanged));

        /// <summary>
        /// DependencyPropertyKey for <see cref="Paused"/> property.
        /// </summary>
        private static readonly DependencyPropertyKey PausedPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "Paused",
                        typeof(bool),
                        typeof(PhotoSlideShowControl),
                        new FrameworkPropertyMetadata(false));

        /// <summary>
        /// DependencyProperty for <see cref="Paused"/> property.
        /// </summary>
        public static readonly DependencyProperty PausedProperty =
                PausedPropertyKey.DependencyProperty;

        /// <summary>
        /// Transition effect used by this control.
        /// </summary>
        private static TransitionEffect enterTransitionEffect = new FadeTransitionEffect();

        /// <summary>
        /// Control hosting the current slide show image.
        /// </summary>
        private SimplePhotoViewerControl currentChild;

        /// <summary>
        /// Control that temporarily hosts the old slide show image upon transition to the next image.
        /// </summary>
        private SimplePhotoViewerControl oldChild;

        /// <summary>
        /// Decorator that hosts photo controls.
        /// </summary>
        private Decorator photoHost;

        /// <summary>
        /// Timer to control interval between transitions.
        /// </summary>
        private DispatcherTimer timer;

        /// <summary>
        /// PRNG used to select the next transition to be applied.
        /// </summary>
        private Random rand = new Random();



        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PhotoSlideShowControl class.
        /// </summary>
        public PhotoSlideShowControl()
        {
            this.currentChild = new SimplePhotoViewerControl();
            this.oldChild = new SimplePhotoViewerControl();

            this.timer = new DispatcherTimer(TimeSpan.FromSeconds(6), DispatcherPriority.Input, this.OnTimerTick, Dispatcher);
            this.timer.Stop();

            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;


            this.InputBindings.Add(new InputBinding(MediaCommands.Stop, new KeyGesture(Key.Escape)));
            this.InputBindings.Add(new InputBinding(MediaCommands.NextTrack, new KeyGesture(Key.Right)));
            this.InputBindings.Add(new InputBinding(MediaCommands.PreviousTrack, new KeyGesture(Key.Left)));
            this.CommandBindings.Add(new CommandBinding(System.Windows.Input.MediaCommands.TogglePlayPause, new ExecutedRoutedEventHandler(OnPlayPauseCommandExecuted), new CanExecuteRoutedEventHandler(OnPlayPauseCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(System.Windows.Input.MediaCommands.Pause, new ExecutedRoutedEventHandler(OnPauseCommandExecuted), new CanExecuteRoutedEventHandler(OnPauseCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(System.Windows.Input.MediaCommands.Play, new ExecutedRoutedEventHandler(OnResumeCommandExecuted), new CanExecuteRoutedEventHandler(OnResumeCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(System.Windows.Input.MediaCommands.NextTrack, new ExecutedRoutedEventHandler(OnNextSlideCommandExecuted), new CanExecuteRoutedEventHandler(OnNextSlideCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(System.Windows.Input.MediaCommands.PreviousTrack, new ExecutedRoutedEventHandler(OnPreviousSlideCommandExecuted), new CanExecuteRoutedEventHandler(OnPreviousSlideCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(System.Windows.Input.MediaCommands.Stop, new ExecutedRoutedEventHandler(OnStopCommandExecuted), new CanExecuteRoutedEventHandler(OnStopCommandCanExecute)));
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets SlideShow object displayed in this control.
        /// </summary>
        public SlideShow SlideShow
        {
            get { return (SlideShow)GetValue(SlideShowProperty); }
            set { SetValue(SlideShowProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether slide show is in paused mode or not.
        /// </summary>
        public bool Paused
        {
            get { return (bool)GetValue(PausedProperty); }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// OnApplyTemplate override
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.photoHost = this.Template.FindName("PART_PhotoHost", this) as Decorator;
            this.photoHost.Child = this.currentChild;

            if (this.photoHost != null && this.SlideShow != null)
            {
                this.StartTimer();
                this.Cursor = Cursors.None;
            }
        }

        #endregion

        #region Private Methods



        /// <summary>
        ///
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            this.Cursor = Cursors.Arrow;
            
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
          //  this.Cursor = Cursors.Hand;  // Mouse is "moving" even when it's not.  Gotta look into this.

        }



        /// <summary>
        /// Dependency property changed handler for SlideShowProperty.
        /// </summary>
        /// <param name="d">Dependency object for which DP change has occurred.</param>
        /// <param name="e">EventArgs describing property change.</param>
        private static void OnPhotoSlideSlideShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PhotoSlideShowControl c = d as PhotoSlideShowControl;
            SlideShow pss = e.NewValue as SlideShow;
            if (pss == null || pss.CurrentPhoto == null)
            {
                c.currentChild.FacebookPhoto = null;
                c.oldChild.FacebookPhoto = null;
                c.timer.Stop();
                c.SetValue(PausedPropertyKey, false);
            }
            else
            {
                c.currentChild.FacebookPhoto = pss.CurrentPhoto.Content as FacebookPhoto;
                c.oldChild.FacebookPhoto = pss.NextPhoto.Content as FacebookPhoto;

                if (c.photoHost != null)
                {
                    c.StartTimer();
                }
            }
        }

        /// <summary>
        /// Can execute handler for TogglePlayPause command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPlayPauseCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow.Paused)
                {
                    OnResumeCommandCanExecute(sender, e);
                }
                else
                {
                    OnPauseCommandCanExecute(sender, e);
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Executed event handler for TogglePlayPause command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPlayPauseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow.Paused)
                {
                    OnResumeCommandExecuted(sender, e);
                }
                else
                {
                    OnPauseCommandExecuted(sender, e);
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Can execute handler for Pause command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPauseCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    e.CanExecute = !slideShow.Paused;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Executed event handler for pause command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPauseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    slideShow.StopTimer();
                    slideShow.ClearValue(FrameworkElement.CursorProperty);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Can execute handler for resume command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnResumeCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    e.CanExecute = slideShow.Paused;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Executed event handler for resume command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnResumeCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    slideShow.StartTimer();
                    slideShow.Cursor = Cursors.None;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Can execute handler for next slide command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnNextSlideCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    // Since slide show wraps around, this can always execute
                    e.CanExecute = true;
                    e.Handled = true;
                }
            }
        }

    

        /// <summary>
        /// Executed event handler for next slide command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnNextSlideCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    // Stop the timer, change the photo, move to the next photo and restart timer
                    slideShow.MoveNext();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Can execute handler for previous slide command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPreviousSlideCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    // Since slide show wraps around, this can always execute
                    e.CanExecute = true;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Executed event handler for previous slide command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPreviousSlideCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    // Stop the timer, change the photo, move to the next photo and restart timer
                    slideShow.MovePrevious();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Can execute handler for stop command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnStopCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    // Slide show can always stop and navigate to current photo
                    e.CanExecute = true;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Executed event handler for stop command
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnStopCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoSlideShowControl slideShow = sender as PhotoSlideShowControl;
                if (slideShow != null)
                {
                    // Stop the timer, change the photo, move to the next photo and restart timer
                    slideShow.NavigateToPhoto();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Loaded override, attaches listener for DataManager's GetTextDocumentCompleted event.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Event args describing the event.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Full screen the application when viewing the slideshow
            ((MainWindow)Application.Current.MainWindow).SetSlideshowViewingMode(true);
            this.Focus();
        }

        /// <summary>
        /// Loaded override, detaches listener for DataManager's GetTextDocumentCompleted event.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Event args describing the event.</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.currentChild.Effect = null;
            this.timer.Stop();
            this.SetValue(PausedPropertyKey, false);

            // Revert to a normal window after the slideshow completes.
            ((MainWindow)Application.Current.MainWindow).SetSlideshowViewingMode(false);
        }

        /// <summary>
        /// Swaps control displaying current photo with the control for the next photo, enabling transition.
        /// </summary>
        private void SwapChildren()
        {
            SimplePhotoViewerControl temp = this.currentChild;
            this.currentChild = this.oldChild;
            this.oldChild = temp;
            this.currentChild.Width = double.NaN;
            this.currentChild.Height = double.NaN;
            if (this.photoHost != null)
            {
                this.photoHost.Child = this.currentChild;
            }

            this.oldChild.Effect = null;
        }

        /// <summary>
        /// Starts timer and resets Paused property
        /// </summary>
        private void StartTimer()
        {
            this.timer.Start();
            this.SetValue(PausedPropertyKey, false);
            this.Cursor = Cursors.None;
        }

        /// <summary>
        /// Stops timer and sets Paused property
        /// </summary>
        private void StopTimer()
        {
            this.timer.Stop();
            this.SetValue(PausedPropertyKey, true);
            this.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Applies a random transition effect between current and next slide show images
        /// </summary>
        private void ApplyTransitionEffect()
        {
            DoubleAnimation da = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(600)), FillBehavior.HoldEnd);
            da.AccelerationRatio = 0.5;
            da.DecelerationRatio = 0.5;
            da.Completed += new EventHandler(this.TransitionCompleted);
            enterTransitionEffect.BeginAnimation(TransitionEffect.ProgressProperty, da);

            VisualBrush vb = new VisualBrush(this.oldChild);
            vb.Viewbox = new Rect(0, 0, this.oldChild.ActualWidth, this.oldChild.ActualHeight);
            vb.ViewboxUnits = BrushMappingMode.Absolute;
            this.oldChild.Width = this.oldChild.ActualWidth;
            this.oldChild.Height = this.oldChild.ActualHeight;
            this.oldChild.Measure(new Size(this.oldChild.ActualWidth, this.oldChild.ActualHeight));
            this.oldChild.Arrange(new Rect(0, 0, this.oldChild.ActualWidth, this.oldChild.ActualHeight));

            enterTransitionEffect.OldImage = vb;
            this.currentChild.Effect = enterTransitionEffect;
        }

        /// <summary>
        /// Advances to next photo. This action stops the timer and puts the slide show in paused mode, slide changes now only take place
        /// through user-initiated action.
        /// </summary>
        private void MoveNext()
        {
            if (!this.Paused)
            {
                this.StopTimer();
            }

            if (this.SlideShow != null)
            {
                this.SlideShow.MoveNext();
            }

            this.ChangePhoto(false);    
        }

        /// <summary>
        /// Goes back to previous photo. This action stops the timer and puts the slide show in paused mode, slide changes now only take place
        /// through user-initiated action.
        /// </summary>
        private void MovePrevious()
        {
            if (!this.Paused)
            {
                this.StopTimer();
            }

            if (this.SlideShow != null)
            {
                this.SlideShow.MovePrevious();
            }

            this.ChangePhoto(false);
        }

        /// <summary>
        /// Stops slide show and navigates to the currently displayed photo.
        /// </summary>
        private void NavigateToPhoto()
        {
            this.timer.Stop();
            this.SetValue(PausedPropertyKey, false);
            PhotoNavigator photo = (PhotoNavigator)this.SlideShow.CurrentPhoto;
            if (ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.CanExecute(photo))
            {
                ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.Execute(photo);
            }
        }

        /// <summary>
        /// Handler for timer tick - initiates transition to next photo.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args describing the event.</param>
        private void OnTimerTick(object sender, EventArgs e)
        {

            this.Cursor = Cursors.None;
            this.ChangePhoto(true);
            if (this.SlideShow != null)
            {
                this.SlideShow.MoveNext();
            }
        }

        /// <summary>
        /// If applyTransitionEffect is true, initiates transition animation to next photo. If false, assumes that next photo has been
        /// selected by manually advancing the slide show, and just displays the current photo.
        /// </summary>
        /// <param name="applyTransitionEffect">If true, transition animation and effects are initiated.</param>
        private void ChangePhoto(bool applyTransitionEffect)
        {

            if (this.SlideShow != null && !this.oldChild.ImageDownloadInProgress)
            {
                if (applyTransitionEffect)
                {
                    this.SwapChildren();
                    this.ApplyTransitionEffect();
                }
                else
                {    
                    // Apply the current slide show content. 
                    // Load the old child with the next photo so it will advance to the next photo if the user resumes play.
                    this.currentChild.FacebookPhoto = SlideShow.CurrentPhoto.Content as FacebookPhoto;
                    this.oldChild.FacebookPhoto = SlideShow.NextPhoto.Content as FacebookPhoto;
                }
            }
        }

        /// <summary>
        /// Handler for slide transition completed.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Event args describing the event.</param>
        private void TransitionCompleted(object sender, EventArgs e)
        {
            this.currentChild.Effect = null;
            if (this.SlideShow != null)
            {
                this.oldChild.FacebookPhoto = SlideShow.NextPhoto.Content as FacebookPhoto;
            }
        }

        #endregion
    }
}
