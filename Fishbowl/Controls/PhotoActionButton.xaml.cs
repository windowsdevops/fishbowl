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
    /// Interaction logic for PhotoActionButton.xaml
    /// </summary>
    public partial class PhotoActionButton : Button
    {
        /// <summary>
        /// Dependency Property backing store for PhotoZoomFactor.
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(PhotoActionButton), new UIPropertyMetadata(false));

        public PhotoActionButton()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets a value indicating whether button is active.
        /// </summary>
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        private void PhotoActionButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsActive = !this.IsActive;
        }
    }
}
