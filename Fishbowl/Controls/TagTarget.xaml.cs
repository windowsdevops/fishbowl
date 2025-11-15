using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FacebookClient.Controls
{
    /// <summary>
    /// Interaction logic for TagTarget.xaml
    /// </summary>
    public partial class TagTarget : UserControl
    {
        /// <summary>
        /// Dependency Property backing store for PhotoZoomFactor.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(TagTarget),
                new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnScaleChanged)));

        private const double DEFAULT_SIZE = 100;

        public TagTarget()
        {
            this.InitializeComponent();

            this.Width = DEFAULT_SIZE;
            this.Height = DEFAULT_SIZE;
            this.Margin = new Thickness(-this.Width/2);
        }

        /// <summary>
        /// Gets the scale of control.
        /// </summary>
        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set
            {
                // Enforce lower limit
                if (value < 0)
                {
                    SetValue(ScaleProperty, 0);
                    return;
                }

                SetValue(ScaleProperty, value);
            }
        }

        /// <summary>
        /// Gets the transform point of control.
        /// </summary>
        public Point TransformPoint
        {
            get
            {
                TranslateTransform tf = (TranslateTransform)this.RenderTransform;
                return new Point(tf.X, tf.Y);
            }
            set
            {
                TranslateTransform tf = new TranslateTransform(value.X, value.Y);
                this.RenderTransform = tf;
            }
        }

        /// <summary>
        /// Handler to change the control layout when FittingPhotoToWindow changes so that
        /// fit to window does indeed cause the photo to fit to window.
        /// </summary>
        /// <param name="newValue">The new FittingPhotoToWindow value.</param>
        protected static void OnScaleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TagTarget tag = (TagTarget)sender;

            if (tag != null)
            {
                double factor = (double)e.NewValue;

                // Set size according to zoom factor
                tag.Width = DEFAULT_SIZE * factor;
                tag.Height = DEFAULT_SIZE * factor;
                tag.Margin = new Thickness(-tag.Width / 2);
            }
        }
    }
}
