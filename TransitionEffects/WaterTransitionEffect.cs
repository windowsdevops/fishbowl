// <copyright file="WaterTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for water transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Water transition effect.
    /// </summary>
    public class WaterTransitionEffect : CloudyTransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the WaterTransitionEffect class.
        /// </summary>
        public WaterTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Water.fx.ps");
            this.PixelShader = shader;
        }
    }
}
