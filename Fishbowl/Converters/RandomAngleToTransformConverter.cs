namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;

    public class RandomAngleToTransformConverter : IValueConverter
    {
        private static Random random = new Random(Environment.TickCount);

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var group = new TransformGroup();
            group.Children.Add(new RotateTransform(random.Next(-10, 11)));
            group.Children.Add(new TranslateTransform(random.Next(-10, 11), random.Next(-10, 11)));
            return group;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
