//-----------------------------------------------------------------------
// <copyright file="CircularBlurTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for circular blur effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Circular blur transition effect.
    /// </summary>
    public class CircularBlurTransitionEffect : TransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the CircularBlurTransitionEffect class.
        /// </summary>
        public CircularBlurTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/CircularBlur.fx.ps");
            this.PixelShader = shader;
        }
    }
}
