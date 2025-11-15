//-----------------------------------------------------------------------
// <copyright file="BloodTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for blood transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Blood transition effect.
    /// </summary>
    public class BloodTransitionEffect : CloudyTransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the BloodTransitionEffect class.
        /// </summary>
        public BloodTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Blood.fx.ps");
            this.PixelShader = shader;
        }
    }
}
