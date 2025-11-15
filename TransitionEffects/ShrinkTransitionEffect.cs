// <copyright file="ShrinkTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for shrink transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Shrink transition effect.
    /// </summary>
    public class ShrinkTransitionEffect : TransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the ShrinkTransitionEffect class.
        /// </summary>
        public ShrinkTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Shrink.fx.ps");
            this.PixelShader = shader;
        }
    }
}
