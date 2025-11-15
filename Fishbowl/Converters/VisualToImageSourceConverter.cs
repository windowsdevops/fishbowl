using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace FacebookClient
{
    public class VisualToImageSourceConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = value as Border;
            if (b == null)
            {
                return null;
            }

            var rtb = new RenderTargetBitmap((int)b.Width, (int)b.Height, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(b);
                dc.DrawRectangle(vb, null, new Rect(0, 0, b.Width, b.Height));
            }
            rtb.Render(dv);
            return rtb;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
