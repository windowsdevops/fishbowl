namespace ClientManager.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Contigo;

    public sealed class FacebookImageControl : Control
    {
        public static readonly DependencyProperty FacebookImageProperty = DependencyProperty.Register(
            "FacebookImage",
            typeof(FacebookImage),
            typeof(FacebookImageControl),
            new UIPropertyMetadata((d, e) => ((FacebookImageControl)d)._UpdateImage()));

        public FacebookImage FacebookImage
        {
            get { return (FacebookImage)GetValue(FacebookImageProperty); }
            set { SetValue(FacebookImageProperty, value); }
        }

        private static readonly DependencyPropertyKey _ImageSourcePropertyKey = DependencyProperty.RegisterReadOnly(
            "ImageSource",
            typeof(ImageSource),
            typeof(FacebookImageControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ImageSourceProperty = _ImageSourcePropertyKey.DependencyProperty;

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            private set { SetValue(_ImageSourcePropertyKey, value); }
        }

        public static readonly DependencyProperty FacebookImageDimensionsProperty = DependencyProperty.Register(
            "FacebookImageDimensions",
            typeof(FacebookImageDimensions?),
            typeof(FacebookImageControl),
            new UIPropertyMetadata(null, (d, e) => ((FacebookImageControl)d)._UpdateImage()));

        public FacebookImageDimensions? FacebookImageDimensions
        {
            get { return (FacebookImageDimensions?)GetValue(FacebookImageDimensionsProperty); }
            set { SetValue(FacebookImageDimensionsProperty, value); }
        }

        public FacebookImageControl()
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, 1));

            RenderTransform = transformGroup;
        }

        private void _UpdateImage()
        {
            if (FacebookImage != null && FacebookImageDimensions != null)
            {
                FacebookImage.GetImageAsync((FacebookImageDimensions)FacebookImageDimensions, _OnGetImageSourceCompleted);
            }
            else
            {
                ImageSource = null;
            }
        }

        private void _OnGetImageSourceCompleted(object sender, GetImageSourceCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                ImageSource = e.ImageSource;
            }
            else
            {
                ImageSource = null;
            }
        }
    }
}

