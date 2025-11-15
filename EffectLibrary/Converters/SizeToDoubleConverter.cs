//-----------------------------------------------------------------------
// <copyright file="SizeToDoubleConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Converts a Size to a double by returning the Width property of the Size
// </summary>
//-----------------------------------------------------------------------

namespace EffectControls
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Converts a Size to a double. 
    /// </summary>
    public class SizeToDoubleConverter : IValueConverter
    {
        /// <summary>
        /// Converts a Size to a double by returning the Width property of the Size
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double val = ((Size)value).Width;
            return val;
        }

        /// <summary>
        /// Converts a double to a Size by setting both the Width and the Height of the Size to the value of the double
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Size s = new Size((double)value, (double)value);
            return s;
        }
    }
}
