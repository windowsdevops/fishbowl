//-----------------------------------------------------------------------
// <copyright file="PhotoZoomFactorToBitmapScalingModeConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//      Converter to convert the PhotoViewerControl.ZoomFactorProperty
//      to the appropriate BitmapScalingMode for best image quality.
// </summary>
//-----------------------------------------------------------------------
namespace FacebookClient
{
    using System;
    using System.Windows.Data;
    using System.Windows.Media;

    /// <summary>
    ///     Converter for the ZoomFactor of the Photo to the appropriate 
    ///     BitmapScalingMode for best image quality.
    /// </summary>
    public class PhotoZoomFactorToBitmapScalingModeConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Converts to select the appropriate BitmapScaling mode based on the 
        /// PhotoViewercontrol.ZoomFactorProperty. The default and best quality
        /// for images at 1.0 scale is BitmapScalingMode.Unspecified. 
        /// =============================================================================
        /// |                               | ZoomFactor &lt; 1 |    ZoomFactor &gt; 1  |
        /// =============================================================================
        /// | ZoomFactor is not animating   | Fant              | NearestNeighbor       |
        /// | TODO: ZoomFactor is animating | LowQuality        | NearestNeighbor       |
        /// =============================================================================
        /// </summary>
        /// <param name="value">A double containing the ZoomFactor.</param>
        /// <param name="targetType">The current type.</param>
        /// <param name="parameter">Unused parameter.</param>
        /// <param name="culture">Unused culture.</param>
        /// <returns>The appropriate BitmapScalingMode.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BitmapScalingMode scalingMode = BitmapScalingMode.Unspecified;
            double imageZoomFactor = 1;

            if (value != null)
            {
                imageZoomFactor = (double)value;

                if (imageZoomFactor < 1)
                {
                    scalingMode = BitmapScalingMode.Fant;
                }
                else if (imageZoomFactor > 1)
                {
                    scalingMode = BitmapScalingMode.NearestNeighbor;
                }
                else 
                {
                    scalingMode = BitmapScalingMode.Unspecified;
                }
            }

            return scalingMode;
        }

        /// <summary>
        /// ConvertBack is not needed and unimplemented. 
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <param name="targetType">The targetType.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The current culture.</param>
        /// <returns>The converted value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
