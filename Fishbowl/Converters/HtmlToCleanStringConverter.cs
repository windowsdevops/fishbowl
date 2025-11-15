//-----------------------------------------------------------------------
// <copyright file="HtmlToCleanStringConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Cleans an HTML string for display without formatting.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows.Data;

    /// <summary>
    /// Cleans an HTML string for display without formatting.
    /// </summary>
    public class HtmlToCleanStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts an HTML string to a string without formatting.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The application culture.</param>
        /// <returns>A string free of (most) HTML formatting.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string cleanString = value as string;
            if (cleanString != null)
            {
                // Remove all HTML tags
                cleanString = Regex.Replace(cleanString, "<[^>]+>", String.Empty);

                // Remove common HTML formatting from the string
               cleanString = cleanString.Replace("&quot;", "\"");
               cleanString = cleanString.Replace("&amp;", "&");
               cleanString = cleanString.Replace("&apos;", "'");
               cleanString = cleanString.Replace("&lt;", "<");
               cleanString = cleanString.Replace("&gt;", ">");
            }

            return cleanString;
        }

        /// <summary>
        /// Converts an unencoded string to an HTML encoded string. Not implemented.
        /// </summary>
        /// <param name="value">The number of photos.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The application culture.</param>
        /// <returns>Throws a NotImplementedException.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
