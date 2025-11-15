namespace ClientManager.View
{
    using System;
    using System.Windows;
    using System.Windows.Media.Animation;
    using System.Windows.Controls;

    public partial class AnimatedSwooshes : UserControl
    {
        private double lastOpacityValue = 0;

        public AnimatedSwooshes()
        {
            InitializeComponent();

            this.Loaded += delegate
            {
                var timeline = (Storyboard)Swoosh1.FindResource("animation");
                timeline.Begin(Swoosh1, true);

                timeline = (Storyboard)Swoosh2.FindResource("animation");
                timeline.Begin(Swoosh2, true);
            };
        }

        public void Pause()
        {
            this.ThrottleAnimation();
        }

        private void ThrottleAnimation()
        {
            var timeline = this.Swoosh1.FindResource("animation") as Storyboard;
            timeline.Pause(this.Swoosh1);

            timeline = this.Swoosh2.FindResource("animation") as Storyboard;
            timeline.Pause(this.Swoosh2);

            //this.lastOpacityValue = this.Opacity;
            //this.Fade(0);
        }

        private void UnthrottleAnimation()
        {
            var timeline = this.Swoosh1.FindResource("animation") as Storyboard;
            timeline.Resume(this.Swoosh1);

            timeline = this.Swoosh2.FindResource("animation") as Storyboard;
            timeline.Resume(this.Swoosh2);

            this.Fade(this.lastOpacityValue);
        }

        public void Fade(double opacity)
        {
            var timeline = new DoubleAnimation(opacity, new Duration(TimeSpan.FromMilliseconds(400)));
            this.BeginAnimation(FrameworkElement.OpacityProperty, timeline);
        }
    }
}