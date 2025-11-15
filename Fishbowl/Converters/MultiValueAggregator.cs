namespace FacebookClient
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class MultiValueAggregator : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return new object[] { values[0], values[1] };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return (object[])value;
        }
    }
}
