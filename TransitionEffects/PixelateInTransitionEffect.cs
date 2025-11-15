//-----------------------------------------------------------------------
// <copyright file="PixelateInTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for pixelate in transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Pixelate in transition effect.
    /// </summary>
    public class PixelateInTransitionEffect : TransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the PixelateInTransitionEffect class.
        /// </summary>
        public PixelateInTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/PixelateIn.fx.ps");
            this.PixelShader = shader;
        }
    }
}
