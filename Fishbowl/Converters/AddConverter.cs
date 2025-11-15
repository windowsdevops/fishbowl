namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    public class AddConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double doubleValue, doubleParameter;

            if (value is string)
            {
                doubleValue = double.Parse((string)value);
            }
            else if (value is double)
            {
                doubleValue = (double)value;
            }
            else if (value is int)
            {
                doubleValue = (int)value;
            }
            else
            {
                throw new ArgumentException("Unsupported type.");
            }

            if (parameter is string)
            {
                doubleParameter = double.Parse((string)parameter);
            }
            else if (parameter is double)
            {
                doubleParameter = (double)parameter;
            }
            else if (parameter is int)
            {
                doubleParameter = (int)parameter;
            }
            else
            {
                throw new ArgumentException("Unsupported type.");
            }

            return doubleValue + doubleParameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}
