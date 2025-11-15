//-----------------------------------------------------------------------
// <copyright file="DateTimeToStringConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//      Converts a Date and Time into a string for display.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Windows.Data;
    using Standard;

    /// <summary>
    /// Converts a Date and Time into a string for display.
    /// </summary>
    public class DateTimeToStringConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Assert.IsNotNull(culture);
            Assert.IsNotNull(value);

            var dateBuilder = new StringBuilder();
            var dtValue = (DateTime)value;

            DateTime now = DateTime.Now;
            bool isToday = now.Date == dtValue.Date;
            bool wasYesterday = now.Subtract(TimeSpan.FromDays(1)).Date == dtValue.Date;

            if (isToday || wasYesterday)
            {
                dateBuilder.Append(dtValue.ToString("t", culture));
                if (wasYesterday)
                {
                    dateBuilder.Append(" yesterday");
                }
            }
            else
            {
                dateBuilder.Append(dtValue.ToString("g"));
            }

            return dateBuilder.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
