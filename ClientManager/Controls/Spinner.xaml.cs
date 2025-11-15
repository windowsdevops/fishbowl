namespace ClientManager.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    public partial class Spinner : UserControl
    {
        public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
            "IsRunning",
            typeof(bool),
            typeof(Spinner),
            new FrameworkPropertyMetadata(
                false,
                (d, e) => ((Spinner)d)._OnIsRunningChanged()));

        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            set { SetValue(IsRunningProperty, value); }
        }

        private void _OnIsRunningChanged()
        {
            if (IsRunning)
            {
                _OnStart();
            }
            else
            {
                _OnStop();
            }
        }

        public Spinner()
        {
            InitializeComponent();
        }

        private void _OnStart()
        {
            spinner.Visibility = Visibility.Visible;

            var timeline = new DoubleAnimation(359, new Duration(TimeSpan.FromMilliseconds(650)));
            timeline.RepeatBehavior = RepeatBehavior.Forever;
            SpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, timeline);
        }

        private void _OnStop()
        {
            spinner.Visibility = Visibility.Hidden;
            SpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }
}