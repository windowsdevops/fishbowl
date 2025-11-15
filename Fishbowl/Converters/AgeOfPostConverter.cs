using System;
using System.Windows.Data;
using System.Globalization;

namespace FacebookClient
{
    public class AgeOfPostConverter: IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Converts a DateTime into a string.
        /// </summary>
        /// <param name="value">The DateTime to convert.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The conversion parameter.</param>
        /// <param name="culture">The conversion culture.</param>
        /// <returns>A string representation of the provided date and time.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string dateTimeString = string.Empty;
            DateTime inputDateTime = DateTime.MinValue;

            if (value != null)
            {
                if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out inputDateTime))
                {
                    var diff = DateTime.Now - inputDateTime;
                    if (diff.TotalDays >= ( 1.0 - .5/24 ))
                    {
                        if (diff.TotalDays > 1.25)
                            dateTimeString = string.Format("{0} days ago", RoundToHalf(diff.TotalDays));
                        else
                            dateTimeString = "1 day ago";
                    }
                    else if (diff.TotalHours > 8)
                        dateTimeString = string.Format("{0} hours ago", RoundToHalf(diff.TotalHours));
                    else if (diff.TotalHours > 1)
                        dateTimeString = string.Format("{0} hours {1} minutes ago", diff.Hours, diff.Minutes);
                    else
                        dateTimeString = string.Format("{0} minutes ago", diff.Minutes);
                }
            }

            return dateTimeString;
        }

        private double RoundToHalf(double p)
        {
            int doubleValue = (int)( (p+.25) * 2 );
            return (double)doubleValue / 2.0;
        }

        /// <summary>
        /// Converts a date and time string into a DateTime object.  Not implemented.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The conversion parameter.</param>
        /// <param name="culture">The conversion culture.</param>
        /// <returns>A DateTime object of the provided string.  Not implemented.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    
}
