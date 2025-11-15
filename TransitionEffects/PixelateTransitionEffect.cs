//-----------------------------------------------------------------------
// <copyright file="PixelateTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for pixelate transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Pixelate transition effect.
    /// </summary>
    public class PixelateTransitionEffect : TransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the PixelateTransitionEffect class.
        /// </summary>
        public PixelateTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Pixelate.fx.ps");
            this.PixelShader = shader;
        }
    }
}
