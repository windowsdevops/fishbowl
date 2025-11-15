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

namespace FacebookClient
{
    public class IconNotificationButton : Button
    {

        public static DependencyProperty IsFlashingProperty = DependencyProperty.Register("IsFlashing", typeof(bool), typeof(IconNotificationButton));

        static IconNotificationButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IconNotificationButton), new FrameworkPropertyMetadata(typeof(IconNotificationButton)));
        }

        public bool IsFlashing
        {
            get
            {
                return (bool)GetValue(IsFlashingProperty);
            }
            set
            {
                SetValue(IsFlashingProperty, value);
            }
        }

    }
}
