namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;
    using Contigo;

    class FilterToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var filter = (ActivityFilter)value;

            if (filter != null)
            {
                switch (filter.FilterType)
                {
                    case "newsfeed":
                        return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/feed_icon.png"));
                    case "network":
                        return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/network_icon.png"));
                    case "application":
                        switch (filter.Name)
                        {
                            case "Status Updates":
                                return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/status_icon.png"));
                            case "Photos":
                                return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/photos_icon.png"));
                            case "Links":
                                return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/links_icon.png"));
                            case "Video":
                                return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/video_icon.png"));
                            case "Notes":
                                return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/notes_icon.png"));
                            default:
                                return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/feed_icon.png"));
                        }
                    default:
                        return new BitmapImage(new Uri(@"pack://application:,,,/FishBowl;component/Resources/Images/Icons/feed_icon.png"));
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
