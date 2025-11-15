//-----------------------------------------------------------------------
// <copyright file="RotationAngleToSimplePositiveAngleConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     IValueConverter to convert an angle, in degrees, to a 'simple' angle.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Windows.Data;

    /// <summary>
    /// IValueConverter to convert an angle, in degrees, to a 'simple' angle, that is, an angle which lies between 0 and 360 degrees.
    /// </summary>
    public class RotationAngleToSimplePositiveAngleConverter : IValueConverter
    {
        /// <summary>
        /// Converts an angle, in degrees, to a 'simple' angle, that is, an angle which lies between 0 and 360 degrees.
        /// </summary>
        /// <param name="value">The original angle, in degrees.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The application culture.</param>
        /// <returns>An angle between 0 and 360 degrees.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object convertedValue = null;
            try
            {
                double angle = System.Convert.ToDouble(value, culture);
                angle = angle % 360;
                if (angle < 0)
                {
                    angle += 360.0;
                }

                convertedValue = angle;
            }
            catch (InvalidCastException)
            {
                //ClientManager.ServiceProvider.Logger.Warning(e.Message);
            }

            return convertedValue;
        }

        /// <summary>
        /// Converts back to the original angle.  Not implemented.
        /// </summary>
        /// <param name="value">The simple angle, in degrees.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The application culture.</param>
        /// <returns>An angle in degrees.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
