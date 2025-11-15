//-----------------------------------------------------------------------
// <copyright file="LeastBrightTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for least bright transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Least bright transition effect.
    /// </summary>
    public class LeastBrightTransitionEffect : TransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the LeastBrightTransitionEffect class.
        /// </summary>
        public LeastBrightTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/LeastBright.fx.ps");
            this.PixelShader = shader;
        }
    }
}
