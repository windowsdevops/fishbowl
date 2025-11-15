//-----------------------------------------------------------------------
// <copyright file="ImageThumbnailControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control used to display a photo thumbnail in an album.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Contigo;

    public enum ImageType
    {
        Person,
        Other,
    }

    /// <summary>
    /// Control used to display a friend image thumbnail.
    /// </summary>
    public class ImageThumbnailControl : Control
    {
        /// <summary>
        /// A Dictionary cache of previously used linear gradient brushes.
        /// </summary>
        private static Dictionary<Color, LinearGradientBrush> gradientBrushes = new Dictionary<Color, LinearGradientBrush>();

        /// <summary>
        /// The pen used to draw the image border.
        /// </summary>
        private static Pen borderPen;

        /// <summary>
        /// The default photo for an anonymous user.
        /// </summary>
        private static ImageBrush _anonymousPersonBrush;
        private static ImageBrush _anonymousLandscapeBrush;

        /// <summary>
        /// The gradient (from the cache) for this photo.
        /// </summary>
        private LinearGradientBrush gradient;

        /// <summary>
        /// The brush used to draw the user's image.
        /// </summary>
        private ImageBrush userImage;

        /// <summary>
        /// The rect used to draw the ImageThumbnailControl.
        /// </summary>
        private Rect brushRect = Rect.Empty;

        /// <summary>Dependency Property backing store for FacebookImage.</summary>
        public static readonly DependencyProperty FacebookImageProperty = DependencyProperty.Register(
            "FacebookImage",
            typeof(FacebookImage),
            typeof(ImageThumbnailControl),
            new UIPropertyMetadata((d, e) => ((ImageThumbnailControl)d)._UpdateImage()));

        /// <summary>Gets or sets the photo to display.</summary>
        public FacebookImage FacebookImage
        {
            get { return (FacebookImage)GetValue(FacebookImageProperty); }
            set { SetValue(FacebookImageProperty, value); }
        }

        /// <summary>Dependency Property backing store for ImageSource.</summary>
        private static readonly DependencyPropertyKey _ImageSourcePropertyKey = DependencyProperty.RegisterReadOnly(
            "ImageSource",
            typeof(ImageSource),
            typeof(ImageThumbnailControl),
            new UIPropertyMetadata(null));

        public static readonly DependencyProperty ImageSourceProperty = _ImageSourcePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the actual image content to display.
        /// </summary>
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            protected set { SetValue(_ImageSourcePropertyKey, value); }
        }

        /// <summary>Dependency Property backing store for FacebookImage.</summary>
        public static readonly DependencyProperty FacebookImageDimensionsProperty = DependencyProperty.Register(
            "FacebookImageDimensions",
            typeof(FacebookImageDimensions?),
            typeof(ImageThumbnailControl),
            new UIPropertyMetadata(null, (d, e) => ((ImageThumbnailControl)d)._UpdateImage()));

        public FacebookImageDimensions? FacebookImageDimensions
        {
            get { return (FacebookImageDimensions?)GetValue(FacebookImageDimensionsProperty); }
            set { SetValue(FacebookImageDimensionsProperty, value); }
        }

        public static readonly DependencyProperty ImageTypeProperty = DependencyProperty.Register(
            "ImageType",
            typeof(ImageType),
            typeof(ImageThumbnailControl),
            new PropertyMetadata(ImageType.Person));

        public ImageType ImageType
        {
            get { return (ImageType)GetValue(ImageTypeProperty); }
            set { SetValue(ImageTypeProperty, value); }
        }

        /// <summary>
        /// SizeBasedOnContent Dependency Property
        /// </summary>
        public static readonly DependencyProperty SizeToContentProperty = DependencyProperty.Register(
            "SizeToContent",
            typeof(SizeToContent),
            typeof(ImageThumbnailControl),
            new UIPropertyMetadata(
                System.Windows.SizeToContent.Manual,
                (d, e) => ((ImageThumbnailControl)d)._UpdateImage()));


        public SizeToContent SizeToContent
        {
            get { return (SizeToContent)GetValue(SizeToContentProperty); }
            set { SetValue(SizeToContentProperty, value); }
        }
        /// <summary>
        /// Corner radius to use for rounded rects.
        /// </summary>
        public double CornerRadius { get; set; }

        /// <summary>
        /// The amount of padding to use when framing the Image.
        /// </summary>
        public double ImagePadding
        {
            get { return (double)GetValue(ImagePaddingProperty); }
            set { SetValue(ImagePaddingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImagePadding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImagePaddingProperty =
                DependencyProperty.Register(
                    "ImagePadding",
                    typeof(double),
                    typeof(ImageThumbnailControl),
                    new UIPropertyMetadata(0.0));

        /// <summary>
        /// Color to use in the gradient behind the user's photo.
        /// </summary>
        public Color ActivityColor
        {
            get { return (Color)GetValue(ActivityColorProperty); }
            set { SetValue(ActivityColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActivityColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActivityColorProperty =
                DependencyProperty.Register(
                    "ActivityColor",
                    typeof(Color),
                    typeof(ImageThumbnailControl),
                    new UIPropertyMetadata(Colors.White, OnActivityColorChanged));

        /// <summary>
        /// Initializes static members of the ImageThumbnailControl class.
        /// </summary>
        static ImageThumbnailControl()
        {
            Type myType = typeof(ImageThumbnailControl);

            // Override some property defaults.
            VerticalAlignmentProperty.OverrideMetadata(myType, new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
            HorizontalAlignmentProperty.OverrideMetadata(myType, new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
            RenderOptions.BitmapScalingModeProperty.OverrideMetadata(myType, new FrameworkPropertyMetadata(BitmapScalingMode.Linear));

            borderPen = new Pen(Brushes.Black, 1.0);
            borderPen.Freeze();

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.DecodePixelWidth = 100;
            bi.UriSource = new Uri(@"pack://application:,,,/Resources/Images/placeholderprofilephoto-square.png");
            bi.EndInit();

            _anonymousPersonBrush = new ImageBrush(bi);
            _anonymousPersonBrush.Freeze();

            bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.DecodePixelWidth = 100;
            bi.UriSource = new Uri(@"pack://application:,,,/Resources/Images/PlaceholderPhoto3.png");
            bi.EndInit();

            _anonymousLandscapeBrush = new ImageBrush(bi);
            _anonymousLandscapeBrush.Freeze();
        }

        /// <summary>
        /// Initializes a new instance of the PhotoThumbnailControl class.
        /// Adds a render transform for animating via XAML styles.
        /// </summary>
        public ImageThumbnailControl()
        {
            ScaleTransform thumbnailScaleTransform = new ScaleTransform(1.0, 1.0);
            TransformGroup thumbnailTransformGroup = new TransformGroup();
            thumbnailTransformGroup.Children.Add(thumbnailScaleTransform);
            this.RenderTransform = thumbnailTransformGroup;

            gradient = null;
            gradientBrushes.TryGetValue(this.ActivityColor, out gradient);

            if (gradient == null)
            {
                gradient = AddKnownGradient(this.ActivityColor);
            }
        }

        /// <summary>
        /// Updates the activity gradient behind the user photo.
        /// </summary>
        private static void OnActivityColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ImageThumbnailControl itc = sender as ImageThumbnailControl;

            Color newColor = (Color) e.NewValue;
            
            itc.gradient = null;
            gradientBrushes.TryGetValue(newColor, out itc.gradient);

            if (itc.gradient == null)
            {
                itc.gradient = AddKnownGradient(newColor);
            }
        }

        /// <summary>
        /// Invoked to render the ImageThumbnailControl.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.CornerRadius > 0)
            {
                drawingContext.DrawRoundedRectangle(this.gradient, borderPen, new Rect(0, 0, this.ActualWidth, this.ActualHeight), this.CornerRadius + 2, this.CornerRadius + 2);
            }
            else
            {
                drawingContext.DrawRectangle(this.gradient, borderPen, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            }

            double pad = this.ImagePadding;
            double width = this.ActualWidth - 2.0 * pad > 0.0 ? this.ActualWidth - 2.0 * pad : 0.0;
            double height = this.ActualHeight - 2.0 * pad > 0.0 ? this.ActualHeight - 2.0 * pad : 0.0;

            if (brushRect.Width != width || brushRect.Height != height)
            {
                brushRect = new Rect(pad, pad, width, height);
            }

            if (this.ImageSource != null && userImage == null)
            {
                userImage = new ImageBrush(this.ImageSource);

                if (this.ImageSource.Height > this.ImageSource.Width)
                {
                    userImage.Viewport = new Rect(0, 0, 1.0, this.ImageSource.Height / this.ImageSource.Width);
                }
                else if (this.ImageSource.Width > this.ImageSource.Height)
                {
                    userImage.Viewport = new Rect(0, 0, this.ImageSource.Width / this.ImageSource.Height, 1.0);
                }
            }

            drawingContext.DrawRoundedRectangle(
                userImage ?? (ImageType == ImageType.Person ? _anonymousPersonBrush : _anonymousLandscapeBrush),
                null,
                brushRect, 
                CornerRadius, 
                CornerRadius);
        }

        /// <summary>
        /// Adds a new known gradient to the linear gradient cache.
        /// </summary>
        private static LinearGradientBrush AddKnownGradient(Color color)
        {
            LinearGradientBrush lgb = null;
            lgb = new LinearGradientBrush();
            lgb.StartPoint = new Point(0, 0);
            lgb.EndPoint = new Point(0, 1);
            lgb.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            lgb.GradientStops.Add(new GradientStop(color, 1.0));
            lgb.Freeze();

            gradientBrushes.Add(color, lgb);
            return lgb;
        }

        /// <summary>
        /// Updates the content of the control to contain the image at Photo.ThumbnailUri.
        /// </summary>
        private void _UpdateImage()
        {
            if (FacebookImage != null && FacebookImageDimensions != null)
            {
                FacebookImage.GetImageAsync((FacebookImageDimensions)FacebookImageDimensions, OnGetImageSourceCompleted);
            }
            else
            {
                ImageSource = null;
            }
        }

        /// <summary>
        /// Sets the ImageSource of the control as soon as the asynchronous get is completed.
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected virtual void OnGetImageSourceCompleted(object sender, GetImageSourceCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                this.ImageSource = e.ImageSource;
                if (SizeToContent != SizeToContent.Manual)
                {
                    // Not bothering to detect more granular values for SizeToContent.
                    SetValue(WidthProperty, e.NaturalSize.Value.Width);
                    SetValue(HeightProperty, e.NaturalSize.Value.Height);
                }
                this.InvalidateVisual();
            }
            else
            {
                this.ImageSource = null;
            }
        }
    }
}

