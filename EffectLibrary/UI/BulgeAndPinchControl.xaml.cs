//-----------------------------------------------------------------------
// <copyright file="BulgeAndPinchControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The partial-class for BulgeAndPinchControl.xaml which exposes the UI
//     for the BulgeAndPinchEffect.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using System.Windows;
    using EffectLibrary;

    /// <summary>
    ///     The partial-class for BulgeAndPinchControl.xaml which exposes the UI
    ///     for the BulgeAndPinchEffect.
    /// </summary>
    public partial class BulgeAndPinchControl : EffectExpander
    {
        /// <summary>
        /// Initializes a new instance of the BulgeAndPinchControl class.
        /// </summary>
        public BulgeAndPinchControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets Effect's Center property to match the mouse position.
        /// </summary>
        /// <param name="mousePosition">The position to set.</param>
        public void SetMousePosition(Point mousePosition)
        {
            if (TargetElement != null && TargetElement.Effect is BulgeAndPinchEffect)
            {
                ((BulgeAndPinchEffect)TargetElement.Effect).Center = mousePosition;
            }
        }

        /// <summary>
        /// Apply the Effect to the TargetElement.
        /// </summary>
        protected override void ApplyEffect()
        {
            if (TargetElement != null && TargetElement.Effect == null)
            {
                TargetElement.Effect = new BulgeAndPinchEffect();
            }
        }

        /// <summary>
        /// Remove the Effect from the TargetElement.
        /// </summary>
        protected override void RemoveEffect()
        {
            if (TargetElement != null)
            {
                TargetElement.Effect = null;
            }
        }
    }
}
