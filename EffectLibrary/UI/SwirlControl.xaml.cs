//-----------------------------------------------------------------------
// <copyright file="SwirlControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The partial-class for SwirlControl.xaml which exposes the UI
//     for the SwirlEffect ShaderEffect.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using EffectLibrary;

    /// <summary>
    ///     The partial-class for SwirlControl.xaml which exposes the UI
    ///     for the SwirlEffect ShaderEffect.
    /// </summary>
    public partial class SwirlControl : EffectExpander
    {
        /// <summary>
        /// Initializes a new instance of the SwirlControl class.
        /// </summary>
        public SwirlControl()
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
                SwirlEffect fx = new SwirlEffect();

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
