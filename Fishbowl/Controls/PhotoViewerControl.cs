//-----------------------------------------------------------------------
// <copyright file="PhotoViewerControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control used to display a full photo.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using ClientManager;
    using ClientManager.Controls;
    using Contigo;
    using EffectLibrary;
    using FacebookClient.Controls;
    using Standard;

    /// <summary>
    /// Control used to display a full photo.
    /// </summary>
    [TemplatePart(Name = "PART_PhotoDisplay", Type = typeof(PhotoDisplayControl)),
     TemplatePart(Name = "PART_PhotoScrollViewer", Type = typeof(ScrollViewer)),
     TemplatePart(Name = "PART_FilmStrip", Type = typeof(FilmStripControl)),
     TemplatePart(Name = "PART_TagTargetElement", Type = typeof(TagTarget)),
     TemplatePart(Name = "PART_PhotoTaggerControl", Type = typeof(PhotoTaggerControl)),
     TemplatePart(Name = "PART_CommentsBorder", Type = typeof(FrameworkElement))]
    public class PhotoViewerControl : SizeTemplateControl
    {
        #region Fields

        public static readonly DependencyProperty FacebookPhotoProperty =
            DependencyProperty.Register("FacebookPhoto", typeof(FacebookPhoto), typeof(PhotoViewerControl));

        public FacebookPhoto FacebookPhoto
        {
            get { return (FacebookPhoto)GetValue(FacebookPhotoProperty); }
            set { SetValue(FacebookPhotoProperty, value); }
        }

        /// <summary>
        /// Dependency Property backing store for PhotoDescriptionVisibility.
        /// </summary>
        public static readonly DependencyProperty PhotoDescriptionVisibilityProperty =
            DependencyProperty.Register("PhotoDescriptionVisibility", typeof(Visibility), typeof(PhotoViewerControl), new UIPropertyMetadata(Visibility.Visible, new PropertyChangedCallback(OnPhotoDescriptionVisibilityChanged)));

        /// <summary>
        /// Dependency Property backing store for PhotoDescriptionVisibility.
        /// </summary>
        public static readonly DependencyProperty IsPhotoDescriptionVisibleProperty =
            DependencyProperty.Register("IsPhotoDescriptionVisible", typeof(bool), typeof(PhotoViewerControl), new UIPropertyMetadata(true, new PropertyChangedCallback(OnIsPhotoDescriptionVisibleChanged)));

        /// <summary>
        /// DependencyProperty backing store for DisplayPhotoAnimation.
        /// </summary>
        public static readonly DependencyProperty CommentsVisibleProperty =
            DependencyProperty.Register("CommentsVisible", typeof(bool), typeof(PhotoViewerControl), new UIPropertyMetadata(false, new PropertyChangedCallback(OnCommentsVisibleChanged)));

        /// <summary>
        /// DependencyProperty backing store for DisplayPhotoAnimation.
        /// </summary>
        public static readonly DependencyProperty TaggingPhotoProperty =
            DependencyProperty.Register("TaggingPhoto", typeof(bool), typeof(PhotoViewerControl), new UIPropertyMetadata(false, new PropertyChangedCallback(OnTaggingPhotoChanged)));

        /// <summary>
        /// RoutedCommand to toggle the display of the photo's description.
        /// </summary>
        private static RoutedCommand displayPhotoDescriptionCommand = new RoutedCommand("DisplayPhotoDescription", typeof(PhotoViewerControl));

        /// <summary>
        /// RoutedCommand to toggle the display of the photo's flow description.
        /// </summary>
        private static RoutedCommand displayPhotoFlowDescriptionCommand = new RoutedCommand("DisplayPhotoFlowDescription", typeof(PhotoViewerControl));

        /// <summary>
        /// RoutedCommand to apply an effect.
        /// </summary>
        private static RoutedCommand toggleEffectCommand = new RoutedCommand("ToggleEffect", typeof(PhotoViewerControl));

        /// <summary>
        /// RoutedCommand to explore a tag with the tag explorer.
        /// </summary>
        private static RoutedCommand exploreTagCommand = new RoutedCommand("ExploreTag", typeof(PhotoViewerControl));

        /// <summary>
        /// RoutedCommand to set the current photo as the desktop background.
        /// </summary>
        private static RoutedCommand setAsDesktopBackgroundCommand = new RoutedCommand("SetAsDesktopBackground", typeof(PhotoViewerControl));

        /// <summary>
        /// RoutedCommand to hide the tag target, or show it and position it accordingly.
        /// </summary>
        public static readonly RoutedCommand IsMouseOverTagCommand = new RoutedCommand("IsMouseOverTag", typeof(PhotoViewerControl));

        /// <summary>
        /// The PhotoDisplayControl which will display the photo image.
        /// </summary>
        private PhotoDisplayControl photoDisplay;

        /// <summary>
        /// The ScrollViewer that hosts the photo display.
        /// </summary>
        private ScrollViewer photoScrollViewer;

        /// <summary>
        /// The FilmStripControl that displays the thumbnails.
        /// </summary>
        private FilmStripControl filmStripControl;

        /// <summary>
        /// A reference to the tag element that highlights the tagged area.
        /// </summary>
        private TagTarget tagTarget;

        /// <summary>
        /// A reference to the photo tagger panel.
        /// </summary>
        private PhotoTaggerControl photoTagger;

        /// <summary>
        /// A reference to the element the has the photo comments control.
        /// </summary>
        private FrameworkElement commentsBorder;

        #endregion

        static PhotoViewerControl()
        {
            SaveAlbumCommand = new RoutedCommand("SaveAlbum", typeof(PhotoViewerControl));
            SavePhotoAsCommand = new RoutedCommand("SavePhotoAs", typeof(PhotoViewerControl));
        }
        
        /// <summary>
        /// Initializes a new instance of the PhotoViewerControl class.
        /// </summary>
        public PhotoViewerControl()
        {
            // Set the key commands for the photo viewer control.
            this.CommandBindings.Add(new CommandBinding(displayPhotoDescriptionCommand, new ExecutedRoutedEventHandler(OnDisplayPhotoDescriptionCommand)));
            this.CommandBindings.Add(new CommandBinding(exploreTagCommand, new ExecutedRoutedEventHandler(OnExploreTagCommand)));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, new ExecutedRoutedEventHandler(OnPrintCommand)));
            this.CommandBindings.Add(new CommandBinding(setAsDesktopBackgroundCommand, new ExecutedRoutedEventHandler(OnSetAsDesktopBackgroundCommand)));
            this.CommandBindings.Add(new CommandBinding(SavePhotoAsCommand, (sender, e) => ((PhotoViewerControl)sender)._OnSavePhotoCommand(e)));
            this.CommandBindings.Add(new CommandBinding(SaveAlbumCommand, new ExecutedRoutedEventHandler(OnSaveAlbumCommand)));

            this.CommandBindings.Add(new CommandBinding(System.Windows.Input.MediaCommands.TogglePlayPause, new ExecutedRoutedEventHandler(OnPlayCommandExecuted), new CanExecuteRoutedEventHandler(OnPlayCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(System.Windows.Input.MediaCommands.Play, new ExecutedRoutedEventHandler(OnPlayCommandExecuted), new CanExecuteRoutedEventHandler(OnPlayCommandCanExecute)));

            this.CommandBindings.Add(new CommandBinding(IsMouseOverTagCommand, new ExecutedRoutedEventHandler(OnIsMouseOverTagCommand)));

            this.MouseLeftButtonDown += new MouseButtonEventHandler(PhotoViewerControl_MouseLeftButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(PhotoViewerControl_MouseLeftButtonUp);

            this.KeyDown += new KeyEventHandler(OnKeyDown);
        }

        #region Properties
        /// <summary>
        /// Gets the RoutedCommand to toggle and Effect.
        /// </summary>
        public static RoutedCommand ToggleEffectCommand
        {
            get { return PhotoViewerControl.toggleEffectCommand; }
        }

        /// <summary>
        /// Gets the RoutedCommand to toggle the display of the photo's description.
        /// </summary>
        public static RoutedCommand DisplayPhotoDescriptionCommand
        {
            get { return PhotoViewerControl.displayPhotoDescriptionCommand; }
        }

        /// <summary>
        /// Gets the RoutedCommand to toggle the display of the photo's flow description.
        /// </summary>
        public static RoutedCommand DisplayPhotoFlowDescriptionCommand
        {
            get { return PhotoViewerControl.displayPhotoFlowDescriptionCommand; }
        }

        /// <summary>
        /// Gets the RoutedCommand to explore a tag with the photo explorer.
        /// </summary>
        public static RoutedCommand ExploreTagCommand
        {
            get { return PhotoViewerControl.exploreTagCommand; }
        }

        /// <summary>
        /// Gets the RoutedCommand to set the current photo as the desktop background.
        /// </summary>
        public static RoutedCommand SetAsDesktopBackgroundCommand
        {
            get { return PhotoViewerControl.setAsDesktopBackgroundCommand; }
        }

        public static RoutedCommand SavePhotoAsCommand { get; private set; }

        public static RoutedCommand SaveAlbumCommand { get; private set; }

        /// <summary>
        /// Gets the RoutedCommand to hide/show tag target in appropriate position over image.
        /// </summary>
        public static RoutedCommand IsMouseOverTagCommandCommand
        {
            get { return PhotoViewerControl.IsMouseOverTagCommand; }
        }

        /// <summary>
        /// Gets a value indicating whether the photo description is displayed or hidden/collapsed.
        /// </summary>
        public Visibility PhotoDescriptionVisibility
        {
            get { return (Visibility)GetValue(PhotoDescriptionVisibilityProperty); }
            protected set { SetValue(PhotoDescriptionVisibilityProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the photo description is displayed or hidden/collapsed.
        /// </summary>
        public bool IsPhotoDescriptionVisible
        {
            get { return (bool)GetValue(IsPhotoDescriptionVisibleProperty); }
            protected set { SetValue(IsPhotoDescriptionVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets animation used when initially displaying a photo.
        /// </summary>
        public bool CommentsVisible
        {
            get { return (bool)GetValue(CommentsVisibleProperty); }
            set { SetValue(CommentsVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets state for tagging photo.
        /// </summary>
        public bool TaggingPhoto
        {
            get { return (bool)GetValue(TaggingPhotoProperty); }
            set { SetValue(TaggingPhotoProperty, value); }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Sets up the rotation animations once the control template has been applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.photoDisplay = this.Template.FindName("PART_PhotoDisplay", this) as PhotoDisplayControl;
            this.photoScrollViewer = this.Template.FindName("PART_PhotoScrollViewer", this) as ScrollViewer;
            this.filmStripControl = this.Template.FindName("PART_FilmStrip", this) as FilmStripControl;

            this.tagTarget = this.Template.FindName("PART_TagTargetElement", this) as TagTarget;
            this.photoTagger = this.Template.FindName("PART_PhotoTaggerControl", this) as PhotoTaggerControl;
            this.commentsBorder = this.Template.FindName("PART_CommentsBorder", this) as FrameworkElement;

            this.photoScrollViewer.PreviewMouseWheel += new MouseWheelEventHandler(OnPhotoScrollViewerPreviewMouseWheel);
            this.photoTagger.TagsCanceledEvent += new TagsCanceledEventHandler(PhotoTagger_TagsCanceledEvent);
            this.photoTagger.TagsUpdatedEvent += new TagsUpdatedEventHandler(PhotoTagger_TagsUpdatedEvent);
            this.photoDisplay.PhotoStateChanged += new PhotoStateChangedEventHandler(PhotoDisplay_PhotoStateChanged);

            // Focus the control so that we can grab keyboard events.
            this.Focus();
        }

        public static void HandleScrollViewerMouseWheel(ScrollViewer scrollViewer, bool checkExtent, MouseWheelEventArgs e)
        {
            // This ScrollViewer will swallow mousewheel events even when it can't be scrolled down any more.
            // We workaround this by raising a new event in that case.

            if (!e.Handled)
            {
                if (!checkExtent || scrollViewer.ExtentHeight <= scrollViewer.ViewportHeight)
                {
                    e.Handled = true;
                    UIElement parent = (UIElement)scrollViewer.Parent;

                    parent.RaiseEvent(
                        new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                        {
                            RoutedEvent = UIElement.MouseWheelEvent,
                            Source = scrollViewer
                        });
                }
            }
        }

        private void OnPhotoScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleScrollViewerMouseWheel((ScrollViewer)sender, true, e);
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Toggles the visibility of the photo's description.
        /// </summary>
        protected void TogglePhotoDescriptionVisibility()
        {
            if (this.IsPhotoDescriptionVisible)
            {
                this.IsPhotoDescriptionVisible = false;
            }
            else
            {
                this.IsPhotoDescriptionVisible = true;
            }
        }

        /// <summary>
        /// Allows the photo to be zoomed in and out using the mouse wheel.
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                {
                    PhotoDisplayControl.ZoomPhotoInCommand.Execute(e, this.photoDisplay);
                }
                else if (e.Delta < 0)
                {
                    PhotoDisplayControl.ZoomPhotoOutCommand.Execute(e, this.photoDisplay);
                }

                e.Handled = true;
            }
            else
            {
                base.OnPreviewMouseWheel(e);
            }
        }

        /// <summary>
        /// Allows the photo to be fit to the current window size using the mouse wheel.
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    this.photoDisplay.FittingPhotoToWindow = true;
                }
            }
        }

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
        /// Command to toggle the display of the photo's description.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnDisplayPhotoDescriptionCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;
            if (photoViewer != null)
            {
                photoViewer.TogglePhotoDescriptionVisibility();
            }
        }

        /// <summary>
        /// Command to use the photo explorer to explore a tag.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnExploreTagCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;
            if (photoViewer != null)
            {
                ServiceProvider.ViewManager.NavigationCommands.NavigateSearchCommand.Execute("explore:" + e.Parameter.ToString());
            }
        }

        /// <summary>
        /// Command to print the current photo.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnPrintCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;
            if (photoViewer != null)
            {
                try
                {
                    string photoLocalPath = photoViewer.FacebookPhoto.Image.GetCachePath(FacebookImageDimensions.Big);

                    if (!string.IsNullOrEmpty(photoLocalPath))
                    {
                        photoLocalPath = System.IO.Path.GetFullPath(photoLocalPath);
                        object photoFile = photoLocalPath;
                        WIA.CommonDialog dialog = new WIA.CommonDialogClass();
                        dialog.ShowPhotoPrintingWizard(ref photoFile);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Sets the currently displayed photo as the desktop background.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnSetAsDesktopBackgroundCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;
            if (photoViewer != null)
            {
                string photoLocalPath = photoViewer.FacebookPhoto.Image.GetCachePath(FacebookImageDimensions.Big);

                if (!string.IsNullOrEmpty(photoLocalPath))
                {
                    string wallpaperPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FacebookClient\\Wallpaper") + Path.GetExtension(photoLocalPath);

                    try
                    {
                        // Clear the current wallpaper (releases lock on current bitmap)
                        NativeMethods.SystemParametersInfo(SPI.SETDESKWALLPAPER, 0, String.Empty, SPIF.UPDATEINIFILE | SPIF.SENDWININICHANGE);

                        // Delete the old wallpaper if it exists
                        if (File.Exists(wallpaperPath))
                        {
                            File.Delete(wallpaperPath);
                        }

                        if (!Directory.Exists(Path.GetDirectoryName(wallpaperPath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(wallpaperPath));
                        }

                        // Copy from temp location to my pictures
                        File.Copy(photoLocalPath, wallpaperPath);

                        // Set the desktop background
                        NativeMethods.SystemParametersInfo(SPI.SETDESKWALLPAPER, 0, wallpaperPath, SPIF.UPDATEINIFILE | SPIF.SENDWININICHANGE);
                    }
                    catch (Exception)
                    {
                        //ServiceProvider.Logger.Warning(exception.Message);
                    }
                }
            }
        }

        private void _OnSavePhotoCommand(ExecutedRoutedEventArgs e)
        {
            string photoLocalPath = FacebookPhoto.Image.GetCachePath(FacebookImageDimensions.Big);
            if (!string.IsNullOrEmpty(photoLocalPath))
            {
                string defaultFileName = Path.GetFileName(photoLocalPath);
                if (FacebookPhoto.Album != null)
                {
                    defaultFileName = FacebookPhoto.Album.Title + " (" + (FacebookPhoto.Album.Photos.IndexOf(FacebookPhoto) + 1) + ")";
                }

                var fileDialog = new System.Windows.Forms.SaveFileDialog();

                fileDialog.Filter = "Image Files|*.jpg;*.png;*.bmp;*.gif";
                fileDialog.DefaultExt = Path.GetExtension(photoLocalPath);
                fileDialog.FileName = defaultFileName;
                fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string imagePath = fileDialog.FileName;

                    try
                    {
                        // Copy the file to the save location, overwriting if necessary
                        File.Copy(photoLocalPath, imagePath, true);
                    }
                    catch (Exception)
                    {
                        //ServiceProvider.Logger.Warning(exception.Message);
                    }
                }
            }
        }


        /// <summary>
        /// Saves every photo in the currently displayed album to a user-provided location.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnSaveAlbumCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;
            if (photoViewer != null)
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderDialog.Description = "Choose where to save the album.";
                folderDialog.ShowNewFolderButton = true;
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    photoViewer.FacebookPhoto.Album.SaveToFolder(folderDialog.SelectedPath);
                }
            }
        }

        /// <summary>
        /// Handler for key press events that target the photo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                PhotoViewerControl photoViewerControl = sender as PhotoViewerControl;
                if (photoViewerControl != null)
                {
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                    {
                        switch (e.Key)
                        {
                            case Key.OemPlus:
                                PhotoDisplayControl.ZoomPhotoInCommand.Execute(null, photoViewerControl.photoDisplay);
                                e.Handled = true;
                                break;
                            case Key.OemMinus:
                                PhotoDisplayControl.ZoomPhotoOutCommand.Execute(null, photoViewerControl.photoDisplay);
                                e.Handled = true;
                                break;
                            case Key.D0:
                                PhotoDisplayControl.FitPhotoToWindowCommand.Execute(null, photoViewerControl.photoDisplay);
                                e.Handled = true;
                                break;
                            case Key.D:
                                photoViewerControl.TogglePhotoDescriptionVisibility();
                                e.Handled = true;
                                break;
                            default:
                                break;
                        }
                    }
                    else if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                    {
                        switch (e.Key)
                        {
                            case Key.Escape:
                                photoViewerControl.Focus();
                                e.Handled = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private Point GetRelativeMousePosition()
        {
            Point mousePosition = Mouse.GetPosition(this.photoDisplay.PhotoImage);
            mousePosition.X /= this.photoDisplay.PhotoImage.ActualWidth;
            mousePosition.Y /= this.photoDisplay.PhotoImage.ActualHeight;
            return mousePosition;
        }

        #endregion

        /// <summary>
        /// On mouse left button down capture mouse if user is tagging photo.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private void PhotoViewerControl_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (this.TaggingPhoto)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// On mouse left button up show tag target and menu.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private static void PhotoViewerControl_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;
            if (photoViewer != null)
            {
                if (photoViewer.TaggingPhoto)
                {
                    Point pt = e.GetPosition(photoViewer.photoDisplay.PhotoImage);
                    MatrixTransform mt = (MatrixTransform)photoViewer.photoDisplay.TransformToVisual(photoViewer.photoDisplay.PhotoImage);

                    photoViewer.tagTarget.Scale = Math.Sqrt(1 / mt.Matrix.M11);

                    double widthByTwo = photoViewer.tagTarget.Width / 2;
                    double heightByTwo = photoViewer.tagTarget.Height / 2;
                    double horizontalOffset = photoViewer.photoScrollViewer.HorizontalOffset * mt.Matrix.M11;
                    double verticalOffset = photoViewer.photoScrollViewer.VerticalOffset * mt.Matrix.M22;

                    Point transPt = new Point(widthByTwo * mt.Matrix.M11, heightByTwo * mt.Matrix.M22);

                    double left;
                    double top;

                    // Ensure tag target element does not go outside of horizontal boundry
                    if (pt.X < transPt.X)
                    {
                        left = transPt.X;
                    }
                    else if (pt.X > (photoViewer.photoDisplay.PhotoImage.ActualWidth - transPt.X))
                    {
                        left = photoViewer.photoDisplay.PhotoImage.ActualWidth - transPt.X;
                    }
                    else
                    {
                        left = pt.X;
                    }

                    // Ensure tag target element does not go outside of vertical boundry
                    if (pt.Y < transPt.Y)
                    {
                        top = transPt.Y;
                    }
                    else if (pt.Y > (photoViewer.photoDisplay.PhotoImage.ActualHeight - transPt.Y))
                    {
                        top = photoViewer.photoDisplay.PhotoImage.ActualHeight - transPt.Y;
                    }
                    else
                    {
                        top = pt.Y;
                    }

                    // Set new coordinates
                    transPt = photoViewer.photoDisplay.PhotoImage.TranslatePoint(
                        new Point(left + horizontalOffset, top + verticalOffset),
                        photoViewer.photoScrollViewer);
                    photoViewer.tagTarget.TransformPoint = transPt;
                    PhotoViewerControl.PlaceTaggerControl(photoViewer, transPt);

                    photoViewer.tagTarget.Visibility = Visibility.Visible;
                    photoViewer.photoTagger.Open();
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Place the photo tagger to the bottom left of given point.
        /// </summary>
        /// <param name="photoViewer">PhotoViewerControl which holds the tagger control.</param>
        /// <param name="topRightCorner">Top right corner point of tagger.</param>
        private static void PlaceTaggerControl(PhotoViewerControl photoViewer, Point topRightCorner)
        {
            double widthByTwo = photoViewer.tagTarget.Width / 2;
            double heightByTwo = photoViewer.tagTarget.Height / 2;

            double x = topRightCorner.X - photoViewer.photoTagger.Width - widthByTwo;
            double y = topRightCorner.Y - heightByTwo;

            if (topRightCorner.Y + photoViewer.photoTagger.Height > photoViewer.photoScrollViewer.ActualHeight)
            {
                y = photoViewer.photoScrollViewer.ActualHeight - photoViewer.photoTagger.Height - heightByTwo;
            }

            if (topRightCorner.X - widthByTwo < photoViewer.photoTagger.Width)
            {
                x = topRightCorner.X + widthByTwo;
            }

            photoViewer.photoTagger.TransformPoint = new Point(x, y);
        }

        /// <summary>
        /// Handler to change the control layout when FittingPhotoToWindow changes so that
        /// fit to window does indeed cause the photo to fit to window.
        /// </summary>
        /// <param name="newValue">The new FittingPhotoToWindow value.</param>
        protected static void OnCommentsVisibleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;

            if (photoViewer != null)
            {
                photoViewer.photoDisplay.CommentsVisible = (bool)e.NewValue;

                if ((bool)e.NewValue)
                {
                    // Fade in comments
                    ObjectAnimationUsingKeyFrames kf = new ObjectAnimationUsingKeyFrames();
                    kf.KeyFrames.Add(new DiscreteObjectKeyFrame(
                        Visibility.Visible,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
                    kf.Duration = new Duration(TimeSpan.FromMilliseconds(200));

                    photoViewer.commentsBorder.BeginAnimation(FrameworkElement.VisibilityProperty, kf, HandoffBehavior.SnapshotAndReplace);

                    DoubleAnimation db = new DoubleAnimation(0, 1,
                        new Duration(TimeSpan.FromMilliseconds(300)));
                    db.BeginTime = TimeSpan.FromMilliseconds(200);

                    photoViewer.commentsBorder.BeginAnimation(FrameworkElement.OpacityProperty, db, HandoffBehavior.SnapshotAndReplace);

                    photoViewer.TaggingPhoto = false;
                }
                else
                {
                    // Fade out comments
                    ObjectAnimationUsingKeyFrames kf = new ObjectAnimationUsingKeyFrames();
                    kf.KeyFrames.Add(new DiscreteObjectKeyFrame(
                        Visibility.Collapsed,
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                    kf.Duration = new Duration(TimeSpan.FromMilliseconds(0));

                    photoViewer.commentsBorder.BeginAnimation(FrameworkElement.VisibilityProperty, kf, HandoffBehavior.SnapshotAndReplace);

                    DoubleAnimationUsingKeyFrames db = new DoubleAnimationUsingKeyFrames();
                    db.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));

                    photoViewer.commentsBorder.BeginAnimation(FrameworkElement.OpacityProperty, db, HandoffBehavior.SnapshotAndReplace);
                }
            }
        }

        /// <summary>
        /// Handler to ensure that photo description button is consistant with model.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        protected static void OnPhotoDescriptionVisibilityChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;

            if (photoViewer != null)
            {
                photoViewer.IsPhotoDescriptionVisible = (((Visibility)e.NewValue) == Visibility.Visible);
            }
        }

        /// <summary>
        /// Handler to ensure that photo description button is consistant with model.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        protected static void OnIsPhotoDescriptionVisibleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;

            if (photoViewer != null)
            {
                if ((bool)e.NewValue)
                {
                    photoViewer.PhotoDescriptionVisibility = Visibility.Visible;
                }
                else
                {
                    photoViewer.PhotoDescriptionVisibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Handler to change the control layout when FittingPhotoToWindow changes so that
        /// fit to window does indeed cause the photo to fit to window.
        /// </summary>
        /// <param name="newValue">The new FittingPhotoToWindow value.</param>
        protected static void OnTaggingPhotoChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;

            if (photoViewer != null)
            {
                if ((bool)e.NewValue)
                {
                    photoViewer.photoDisplay.PhotoImage.Cursor = Cursors.Cross;

                    // Stop commenting
                    if (photoViewer.CommentsVisible)
                    {
                        photoViewer.CommentsVisible = false;
                    }
                }
                else
                {
                    photoViewer.photoDisplay.PhotoImage.Cursor = Cursors.Arrow;
                    photoViewer.tagTarget.Visibility = Visibility.Collapsed;
                    photoViewer.photoTagger.Close();
                }
            }
        }

        /// <summary>
        /// Photo tagging has been canceled.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private void PhotoTagger_TagsCanceledEvent(object sender, EventArgs e)
        {
            if (this.TaggingPhoto)
            {
                this.TaggingPhoto = false;
            }
        }

        /// <summary>
        /// Photo has been tagged.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private void PhotoTagger_TagsUpdatedEvent(object sender, TagsUpdatedArgs e)
        {
            if (this.TaggingPhoto)
            {
                this.TaggingPhoto = false;

                FacebookPhoto photo = (FacebookPhoto)this.DataContext;

                if (photo != null)
                {
                    Point pt = this.tagTarget.TransformPoint;

                    MatrixTransform mt = (MatrixTransform)this.photoDisplay.TransformToVisual(this.photoDisplay.PhotoImage);

                    pt = new Point(
                        (this.photoScrollViewer.TranslatePoint(
                            this.tagTarget.TransformPoint, this.photoDisplay.PhotoImage).X -
                            this.photoScrollViewer.HorizontalOffset * mt.Matrix.M11) /
                            this.photoDisplay.PhotoImage.ActualWidth,
                        (this.photoScrollViewer.TranslatePoint(
                            this.tagTarget.TransformPoint, this.photoDisplay.PhotoImage).Y -
                            this.photoScrollViewer.VerticalOffset * mt.Matrix.M22) /
                            this.photoDisplay.PhotoImage.ActualHeight
                        );

                    // Add photo tag
                    ClientManager.ServiceProvider.ViewManager.AddTagToPhoto(
                        photo,
                        e.SelectedContact,
                        pt);
                }
            }
        }

        /// <summary>
        /// Tag target scale has changed, reposition.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private void PhotoDisplay_PhotoStateChanged(object sender, EventArgs e)
        {
            if (this.TaggingPhoto)
            {
                this.TaggingPhoto = false;
            }

            if (this.CommentsVisible)
            {
                this.CommentsVisible = false;
            }

            this.tagTarget.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hide the tag target, or show it and position it accordingly.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private static void OnIsMouseOverTagCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoViewerControl photoViewer = sender as PhotoViewerControl;

            if (photoViewer != null && !photoViewer.TaggingPhoto)
            {
                if (e.Parameter == null)
                {
                    // Hide tag target
                    photoViewer.tagTarget.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Position tag and show
                    Point pt = (Point)e.Parameter;

                    if (pt != null)
                    {
                        MatrixTransform mt = (MatrixTransform)photoViewer.photoDisplay.TransformToVisual(photoViewer.photoDisplay.PhotoImage);

                        photoViewer.tagTarget.Scale = Math.Sqrt(1 / mt.Matrix.M11);

                        double widthByTwo = photoViewer.tagTarget.Width / 2;
                        double heightByTwo = photoViewer.tagTarget.Height / 2;

                        //Point transPt = mt.Transform(new Point(widthByTwo, heightByTwo));
                        Point transPt = new Point(widthByTwo * mt.Matrix.M11, heightByTwo * mt.Matrix.M22);

                        // Convert point from percentage to pixels
                        pt = new Point(pt.X * photoViewer.photoDisplay.PhotoImage.ActualWidth +
                            photoViewer.photoScrollViewer.HorizontalOffset * mt.Matrix.M11,
                            pt.Y * photoViewer.photoDisplay.PhotoImage.ActualHeight +
                            photoViewer.photoScrollViewer.VerticalOffset * mt.Matrix.M22);

                        double left;
                        double top;

                        // Ensure tag target element does not go outside of photo boundry
                        if (pt.X < transPt.X)
                        {
                            left = transPt.X;
                        }
                        else if (pt.X > (photoViewer.photoDisplay.PhotoImage.ActualWidth - transPt.X))
                        {
                            left = photoViewer.photoDisplay.PhotoImage.ActualWidth - transPt.X;
                        }
                        else
                        {
                            left = pt.X;
                        }

                        // Ensure tag target element does not go outside of photo boundry
                        if (pt.Y < transPt.Y)
                        {
                            top = transPt.Y;
                        }
                        else if (pt.Y > (photoViewer.photoDisplay.PhotoImage.ActualHeight - transPt.Y))
                        {
                            top = photoViewer.photoDisplay.PhotoImage.ActualHeight - transPt.Y;
                        }
                        else
                        {
                            top = pt.Y;
                        }

                        // Set new coordinates
                        transPt = photoViewer.photoDisplay.PhotoImage.TranslatePoint(new Point(left, top), photoViewer.photoScrollViewer);
                        photoViewer.tagTarget.TransformPoint = transPt;

                        photoViewer.tagTarget.Visibility = Visibility.Visible;
                    }
                }
            }
        }
    }
}