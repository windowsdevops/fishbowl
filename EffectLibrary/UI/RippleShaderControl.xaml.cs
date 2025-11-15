//-----------------------------------------------------------------------
// <copyright file="RippleShaderControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The partial-class for RippleShaderControl.xaml which exposes the UI
//     for the RippleEffect.
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
    ///     The partial-class for RippleShaderControl.xaml which exposes the UI
    ///     for the RippleEffect.
    /// </summary>
    public partial class RippleShaderControl : EffectExpander
    {
        /// <summary>
        /// Initializes a new instance of the RippleShaderControl class..
        /// </summary>
        public RippleShaderControl()
        {
            InitializeComponent();
            this.Unloaded += new RoutedEventHandler(this.OnUnloaded);
        }

        /// <summary>
        /// Updates properties on the Effect.
        /// </summary>
        public void Update()
        {
            if (TargetElement != null && TargetElement.Effect is RippleEffect)
            {
                ((RippleEffect)TargetElement.Effect).Phase += 0.2;
            }   
        }

        /// <summary>
        /// Sets Effect's Center property to match the mouse position.
        /// </summary>
        /// <param name="mousePosition">The position to set.</param>
        public void SetMousePosition(Point mousePosition)
        {
            if (TargetElement != null && TargetElement.Effect is RippleEffect)
            {
                ((RippleEffect)TargetElement.Effect).Center = mousePosition;
            }
        }

        /// <summary>
        /// Apply the Effect to the TargetElement.
        /// </summary>
        protected override void ApplyEffect()
        {
            if (TargetElement != null && !(TargetElement.Effect is RippleEffect))
            {
                RippleEffect fx = new RippleEffect();
                Point mousePosition = Mouse.GetPosition(TargetElement);

                mousePosition.X /= TargetElement.ActualWidth;
                mousePosition.Y /= TargetElement.ActualHeight;
                fx.Center = mousePosition;
                TargetElement.Effect = fx;

                DoubleAnimation da = new DoubleAnimation(0, 10 * System.Math.PI, new Duration(new TimeSpan(0, 0, 5)));
                da.RepeatBehavior = RepeatBehavior.Forever;

                fx.BeginAnimation(RippleEffect.PhaseProperty, da, HandoffBehavior.SnapshotAndReplace);
            }
        }

        /// <summary>
        /// Remove the Effect from the TargetElement.
        /// </summary>
        protected override void RemoveEffect()
        {
            if (TargetElement != null && TargetElement.Effect is RippleEffect)
            {
                TargetElement.Effect.BeginAnimation(RippleEffect.PhaseProperty, null);
                TargetElement.Effect = null;
            }
        }

        /// <summary>
        /// Perform any clean-up work when this element is unloaded.
        /// </summary>
        /// <param name="sender">The sender of this event.</param>
        /// <param name="e">Params for this event.</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.RemoveEffect();
        }
    }
}
