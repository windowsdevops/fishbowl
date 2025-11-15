//-----------------------------------------------------------------------
// <copyright file="InvertColorControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The partial-class for InvertColorControl.xaml which exposes the UI
//     for the InvertColorEffect.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using EffectLibrary;

    /// <summary>
    ///    The partial-class for InvertColorControl.xaml which exposes the UI
    ///    for the InvertColorEffect.
    /// </summary>
    public partial class InvertColorControl : EffectExpander
    {
        /// <summary>
        /// Initializes a new instance of the InvertColorControl class.
        /// </summary>
        public InvertColorControl()
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
                TargetElement.Effect = new InvertColorEffect();
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
