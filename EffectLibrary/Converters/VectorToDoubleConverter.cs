//-----------------------------------------------------------------------
// <copyright file="VectorToDoubleConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//      Converts a Vector to a double by returning the X property of the Vector.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Converts a Vector to a double. 
    /// </summary>
    public class VectorToDoubleConverter : IValueConverter
    {
        /// <summary>
        /// Converts a Vector to a double by returning the X property of the Vector
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double val = ((Vector)value).X;
            return val;
        }

        /// <summary>
        /// Converts a double back to a Vector
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Vector v = new Vector((double)value, (double)value);
            return v;
        }
    }
}
