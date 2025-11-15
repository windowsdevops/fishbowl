using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FacebookClient
{
    public class AnimationHelper
    {
        internal static void FadeOut(FrameworkElement element)
        {
            FadeOut(element, 0, TimeSpan.FromMilliseconds(300));
        }

        internal static void FadeOut(FrameworkElement element, double to, TimeSpan timeSpan)
        {
            FadeOut(element, to, timeSpan, TimeSpan.Zero);
        }

        internal static void FadeOut(FrameworkElement element, double to, TimeSpan timeSpan, TimeSpan beginTime)
        {
            FadeOut(element, to, timeSpan, TimeSpan.Zero, Visibility.Hidden);
        }

        internal static void FadeOut(FrameworkElement element, double to, TimeSpan timeSpan, TimeSpan beginTime, Visibility visibility)
        {
            var timeline = new DoubleAnimation(to, new Duration(timeSpan));
            timeline.BeginTime = beginTime;
            timeline.Completed += delegate
            {
                if (to == 0)
                {
                    element.Visibility = visibility;
                }
            };
            element.BeginAnimation(FrameworkElement.OpacityProperty, timeline);
        }

        internal static void FadeIn(FrameworkElement element)
        {
            FadeIn(element, 1, TimeSpan.FromMilliseconds(300));
        }

        internal static void FadeIn(FrameworkElement element, double to, TimeSpan timeSpan)
        {
            FadeIn(element, to, timeSpan, TimeSpan.Zero);
        }

        internal static void FadeIn(FrameworkElement element, double to, TimeSpan timeSpan, TimeSpan beginTime)
        {
            element.Visibility = Visibility.Visible;

            var timeline = new DoubleAnimation(to, new Duration(timeSpan));
            timeline.BeginTime = beginTime;
            element.BeginAnimation(FrameworkElement.OpacityProperty, timeline);
        }

        internal static void Scale(ScaleTransform scaleTransform, double from, double to, TimeSpan timeSpan)
        {
            var timeline = new DoubleAnimation(from, to, new Duration(timeSpan));
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, timeline);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, timeline);
        }

        internal static void Translate(TranslateTransform translateTransform, bool isX, double from, double to, TimeSpan timeSpan)
        {
            var timeline = new DoubleAnimation(from, to, new Duration(timeSpan));
            timeline.AccelerationRatio = .5;
            
            if (isX)
            {
                translateTransform.BeginAnimation(TranslateTransform.XProperty, timeline);
            }
            else
            {
                translateTransform.BeginAnimation(TranslateTransform.YProperty, timeline);
            }
        }

        internal static void Rotate(RotateTransform rotateTransform, double from, double to, TimeSpan timeSpan)
        {
            var timeline = new DoubleAnimation(from, to, new Duration(timeSpan));
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, timeline);
        }

        internal static void Margin(FrameworkElement element, Thickness to, TimeSpan timeSpan)
        {
            var timeline = new ThicknessAnimation(to, new Duration(timeSpan));
            element.BeginAnimation(FrameworkElement.MarginProperty, timeline);
        }

        internal delegate void DelayDelegate();

        internal static void Delay(TimeSpan timeSpan, DelayDelegate delayDelegate)
        {
            var timer = new DispatcherTimer();
            timer.Interval = timeSpan;
            timer.Tick += delegate
            {
                timer.Stop();

                delayDelegate();
            };
            timer.Start();
        }
    }
}
