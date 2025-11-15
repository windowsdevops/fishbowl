//-----------------------------------------------------------------------
// <copyright file="ImageThumbnailControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    public class NewsFeedItemOverlay : FrameworkElement
    {
        private static int itemCount;
        private static LinearGradientBrush layerOne;
        private static LinearGradientBrush layerTwo;
        private static LinearGradientBrush layerThree;

        //private static RadialGradientBrush shineOne;
        
        //private static RadialGradientBrush shineTwo;
        
        //private static RadialGradientBrush shineBorder;
        
        //private static Pen shineBorderPen;

        static NewsFeedItemOverlay()
        {
            if (itemCount % 2 == 0)
            {
                Type ownerType = typeof(NewsFeedItemOverlay);
                HorizontalAlignmentProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(HorizontalAlignment.Stretch));
                VerticalAlignmentProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
                IsHitTestVisibleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));

                layerOne = new LinearGradientBrush()
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
                    GradientStops = new GradientStopCollection()
                    {
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#f3f2f2"), Offset = 0.0 } },
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#f3f2f2"), Offset = 0.9 } },
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#00f3f2f2"), Offset = 1.0 } },
                    },
                };
                layerOne.Freeze();

                layerTwo = new LinearGradientBrush()
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
                    GradientStops = new GradientStopCollection()
                    {
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#FFFFFFFF"), Offset = 0.0 } },
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#FFFFFFFF"), Offset = 0.9 } },
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#00FFFFFF"), Offset = 1.0 } },
                    },
                };
                layerTwo.Freeze();

                layerThree = new LinearGradientBrush()
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
                    GradientStops = new GradientStopCollection()
                    {
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#f3f2f2"), Offset = 0.0 } },
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#f3f2f2"), Offset = 0.9 } },
                        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#00f3f2f2"), Offset = 1.0 } },
                    },
                };
                layerThree.Freeze();
            }

            //shineOne = new RadialGradientBrush()
            //{
            //    ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            //    Center = new Point(1.095, 1.16),
            //    GradientOrigin = new Point(1.095, 1.16),
            //    GradientStops = new GradientStopCollection()
            //    {
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#00000000"), Offset = 1.0 } },
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#FFFF0000"), Offset = 0.388 } },
            //    },
            //};
            //shineOne.Freeze();

            //shineTwo = new RadialGradientBrush()
            //{
            //    ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            //    Center = new Point(-0.095, 1.16),
            //    GradientOrigin = new Point(-0.095, 1.16),
            //    GradientStops = new GradientStopCollection()
            //    {
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#00000000"), Offset = 1.0 } },
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#4CFFFFFF"), Offset = 0.388 } },
            //    },
            //};
            //shineTwo.Freeze();

            //shineBorder = new RadialGradientBrush()
            //{
            //    ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            //    Center = new Point(0.46, 0.026),
            //    GradientOrigin = new Point(0.448, 0.0),
            //    RadiusX = 0.7,
            //    RadiusY = 0.25,
            //    GradientStops = new GradientStopCollection()
            //    {
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#00000000"), Offset = 1.0 } },
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#22FFFFFF"), Offset = 0.4 } },
            //    },
            //};
            //shineBorder.Freeze();

            //var shineBorderStroke = new RadialGradientBrush()
            //{
            //    ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            //    RadiusX = 0.707,
            //    RadiusY = 0.707,
            //    GradientStops = new GradientStopCollection()
            //    {
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#00000000"), Offset = 0.733 } },
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#FFFFFFFF"), Offset = 1.0 } },
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#FFFFFFFF") } },
            //        { new GradientStop { Color = (Color) ColorConverter.ConvertFromString("#15232323"), Offset = 0.293 } },
            //    },
            //};
            //shineBorderStroke.Freeze();

            //shineBorderPen = new Pen()
            //{
            //    Brush = shineBorderStroke,      
            //};
            //shineBorderPen.Freeze();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (itemCount % 2 == 0)
            {
                drawingContext.DrawRectangle(layerOne, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
                drawingContext.DrawRectangle(layerTwo, null, new Rect(0, 1, this.ActualWidth, this.ActualHeight - 2));
                drawingContext.DrawRectangle(layerThree, null, new Rect(0, 2, this.ActualWidth, this.ActualHeight - 4));
            }

            itemCount++;

            //drawingContext.DrawRoundedRectangle(shineOne, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight), 5.0, 5.0);
            //drawingContext.DrawRoundedRectangle(shineTwo, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight), 5.0, 5.0);
            //drawingContext.DrawRoundedRectangle(shineBorder, shineBorderPen, new Rect(0, 0, this.ActualWidth, this.ActualHeight), 5.0, 5.0);
        }
    }
}
