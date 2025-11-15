//-----------------------------------------------------------------------
// <copyright file="RadialBlurTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for radial blur transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Radial blur transition effect.
    /// </summary>
    public class RadialBlurTransitionEffect : TransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the RadialBlurTransitionEffect class.
        /// </summary>
        public RadialBlurTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/RadialBlur.fx.ps");
            this.PixelShader = shader;
        }
    }
}
