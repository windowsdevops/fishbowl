//-----------------------------------------------------------------------
// <copyright file="SmoothMagnifyControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The partial-class for SmoothMagnifyControl.xaml which exposes the UI
//     for the SmoothMagnifyEffect.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using EffectLibrary;

    /// <summary>
    ///    The partial-class for SmoothMagnifyControl.xaml which exposes the UI
    ///    for the SmoothMagnifyEffect.
    /// </summary>
    public partial class SmoothMagnifyControl : EffectExpander
    {
        /// <summary>
        /// The original value of the SmoothMagnifyEffect.MagnificationProperty.
        /// </summary>
        private double originalMagnification;

        /// <summary>
        /// Initializes a new instance of the SmoothMagnifyControl class.
        /// </summary>
        public SmoothMagnifyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets Effect's Center property to match the mouse position.
        /// </summary>
        /// <param name="mousePosition">The position to set.</param>
        public void SetMousePosition(Point mousePosition)
        {
            if (TargetElement != null && TargetElement.Effect is SmoothMagnifyEffect)
            {
                (TargetElement.Effect as SmoothMagnifyEffect).Center = mousePosition;
            }
        }

        /// <summary>
        /// Apply the Effect to the TargetElement.
        /// </summary>
        protected override void ApplyEffect()
        {
            if (TargetElement != null && !(TargetElement.Effect is SmoothMagnifyEffect))
            {
                SmoothMagnifyEffect fx = new SmoothMagnifyEffect();
                Point mousePosition = Mouse.GetPosition(this.TargetElement);

                mousePosition.X /= this.TargetElement.ActualWidth;
                mousePosition.Y /= this.TargetElement.ActualHeight;
                fx.Center = mousePosition;

                DoubleAnimation zoomInAnimation = new DoubleAnimation(fx.Magnification, new Duration(TimeSpan.FromSeconds(0.5)));
                zoomInAnimation.FillBehavior = FillBehavior.HoldEnd;
                zoomInAnimation.Completed += new EventHandler(this.OnZoomInAnimationCompleted);

                this.originalMagnification = fx.Magnification;
                fx.Magnification = 1;
                TargetElement.Effect = fx;
                (TargetElement.Effect as SmoothMagnifyEffect).BeginAnimation(SmoothMagnifyEffect.MagnificationProperty, zoomInAnimation, HandoffBehavior.SnapshotAndReplace);
            }
        }

        /// <summary>
        /// Remove the Effect from the TargetElement.
        /// </summary>
        protected override void RemoveEffect()
        {
            if (TargetElement != null && TargetElement.Effect is SmoothMagnifyEffect)
            {
                DoubleAnimation zoomOutAnimation = new DoubleAnimation(1, new Duration(TimeSpan.FromSeconds(0.5)));
                zoomOutAnimation.FillBehavior = FillBehavior.HoldEnd;
                zoomOutAnimation.Completed += new EventHandler(this.OnZoomOutAnimationCompleted);

                TargetElement.Effect.BeginAnimation(SmoothMagnifyEffect.MagnificationProperty, zoomOutAnimation, HandoffBehavior.SnapshotAndReplace);
            }
        }

        /// <summary>
        /// Removes the animation from the Effect once the animation has completed so that
        /// the value can be modified by the user. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Params for this event</param>
        private void OnZoomInAnimationCompleted(object sender, EventArgs e)
        {
            if (TargetElement != null && TargetElement.Effect is SmoothMagnifyEffect)
            {
                (TargetElement.Effect as SmoothMagnifyEffect).BeginAnimation(SmoothMagnifyEffect.MagnificationProperty, null);
                (TargetElement.Effect as SmoothMagnifyEffect).Magnification = this.originalMagnification;
            }
        }

        /// <summary>
        /// Removes the animation from the Effect once the animation has completed so that
        /// the value can be modified by the user.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Params for this event</param>
        private void OnZoomOutAnimationCompleted(object sender, EventArgs e)
        {
            if (TargetElement != null && TargetElement.Effect is SmoothMagnifyEffect)
            {
                TargetElement.Effect.BeginAnimation(SmoothMagnifyEffect.MagnificationProperty, null);
                TargetElement.Effect = null;
            }
        }
    }
}
