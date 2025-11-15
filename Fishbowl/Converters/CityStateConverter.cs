namespace FacebookClient
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using Contigo;

    public class CityStateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var contact = value as FacebookContact;
            if (contact != null)
            {
                Location cl = contact.CurrentLocation;

                bool includeComma = !string.IsNullOrEmpty(cl.State) && !string.IsNullOrEmpty(cl.City);
                return string.Format("{0}{1}{2}",
                    cl.City,
                    includeComma ? ", " : "",
                    cl.State);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
