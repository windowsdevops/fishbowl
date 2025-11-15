//-----------------------------------------------------------------------
// <copyright file="BrickMasonControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The partial-class for BrickMasonControl.xaml which exposes the UI
//     for the BrickMasonEffect.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using EffectLibrary;

    /// <summary>
    ///   The partial-class for BrickMasonControl.xaml which exposes the UI
    ///   for the BrickMasonEffect.
    /// </summary>
    public partial class BrickMasonControl : EffectExpander
    {
        /// <summary>
        /// Initializes a new instance of the BrickMasonControl class.
        /// </summary>
        public BrickMasonControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Apply the Effect to the TargetElement.
        /// </summary>
        protected override void ApplyEffect()
        {
            if (TargetElement != null && TargetElement.Effect == null)
            {
                BrickMasonEffect fx = new BrickMasonEffect();

                TargetElement.Effect = fx;
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
