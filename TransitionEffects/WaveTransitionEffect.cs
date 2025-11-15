// <copyright file="WaveTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for wave transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Wave transition effect.
    /// </summary>
    public class WaveTransitionEffect : TransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the WaveTransitionEffect class.
        /// </summary>
        public WaveTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Wave.fx.ps");
            this.PixelShader = shader;
        }
    }
}
