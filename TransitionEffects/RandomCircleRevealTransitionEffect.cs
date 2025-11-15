//-----------------------------------------------------------------------
// <copyright file="RandomCircleRevealTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for random circle reveal transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Random circle reveal transition effect.
    /// </summary>
    public class RandomCircleRevealTransitionEffect : CloudyTransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the RandomCircleRevealTransitionEffect class.
        /// </summary>
        public RandomCircleRevealTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/RandomCircleReveal.fx.ps");
            this.PixelShader = shader;
        }
    }
}
