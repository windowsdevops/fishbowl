
namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using Standard;

    public class NotificationCountControl : Control
    {
        public NotificationCountControl()
        {
            Visibility = Visibility.Collapsed;
            LayoutUpdated += (sender, e) => _UpdateImageSource();
        }

        public static readonly DependencyProperty DisplayCountProperty = DependencyProperty.Register(
            "DisplayCount",
            typeof(int), 
            typeof(NotificationCountControl),
            new UIPropertyMetadata(0,
                (d, e) => ((NotificationCountControl)d)._OnDisplayCountChanged()));

        public int DisplayCount
        {
            get { return (int)GetValue(DisplayCountProperty); }
            set { SetValue(DisplayCountProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
            "ImageSource", 
            typeof(ImageSource), 
            typeof(NotificationCountControl),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the ImageSource property.  This dependency property 
        /// indicates ....
        /// </summary>
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        private void _OnDisplayCountChanged()
        {
            if (DisplayCount != 0)
            {
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Collapsed;
                ImageSource = null;
            }
        }

        private void _UpdateImageSource()
        {
            if (ActualWidth == 0 || ActualHeight == 0)
            {
                return;
            }

            ImageSource = Utility.GenerateBitmapSource(this, 16, 16);
        }
    }
}
