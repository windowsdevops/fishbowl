//-----------------------------------------------------------------------
// <copyright file="VisibilityToBooleanConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Class to convert a Visibility value to a boolean indicating whether or not the element can be seen.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Class to convert a Visibility value to a boolean indicating whether or not the element can be seen.
    /// </summary>
    public class VisibilityToBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Converts a Visibility value to a boolean indicating whether or not the element can be seen.
        /// </summary>
        /// <param name="value">The original Visibility.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The application culture.</param>
        /// <returns>A boolean indicating whether or not the element can be seen.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible ? true : false;
        }

        /// <summary>
        /// Converts back by returning the boolean value.
        /// </summary>
        /// <param name="value">The visibility value.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The application culture.</param>
        /// <returns>The provided value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}
