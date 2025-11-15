//-----------------------------------------------------------------------
// <copyright file="BandedSwirlControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The partial-class for BandedSwirlControl.xaml which exposes the UI
//     for the BandedSwirlEffect.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using System.Windows;
    using System.Windows.Input;
    using EffectLibrary;

    /// <summary>
    ///     The partial-class for BandedSwirlControl.xaml which exposes the UI
    ///     for the BandedSwirlEffect.
    /// </summary>
    public partial class BandedSwirlControl : EffectExpander
    {
        /// <summary>
        /// Initializes a new instance of the BandedSwirlControl class.
        /// </summary>
        public BandedSwirlControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets Effect's Center property to match the mouse position.
        /// </summary>
        /// <param name="mousePosition">The position to set.</param>
        public void SetMousePosition(Point mousePosition)
        {
            if (TargetElement != null && TargetElement.Effect is BandedSwirlEffect)
            {
                ((BandedSwirlEffect)TargetElement.Effect).Center = mousePosition;
            }
        }

        /// <summary>
        /// Apply the Effect to the TargetElement.
        /// </summary>
        protected override void ApplyEffect()
        {
            if (this.TargetElement != null && this.TargetElement.Effect == null)
            {
                BandedSwirlEffect fx = new BandedSwirlEffect();
                Point mousePosition = Mouse.GetPosition(this.TargetElement);

                mousePosition.X /= this.TargetElement.ActualWidth;
                mousePosition.Y /= this.TargetElement.ActualHeight;
                fx.Center = mousePosition;
                this.TargetElement.Effect = fx;
            }          
        }

        /// <summary>
        /// Remove the Effect from the TargetElement.
        /// </summary>
        protected override void RemoveEffect()
        {
            if (this.TargetElement != null)
            {
                this.TargetElement.Effect = null;
            }
        }
    }
}
