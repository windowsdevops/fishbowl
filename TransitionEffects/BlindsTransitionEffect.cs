//-----------------------------------------------------------------------
// <copyright file="BlindsTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for blinds transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Blinds transition effect
    /// </summary>
    public class BlindsTransitionEffect : TransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the BlindsTransitionEffect class.
        /// </summary>
        public BlindsTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Blinds.fx.ps");
            this.PixelShader = shader;
        }
    }
}
