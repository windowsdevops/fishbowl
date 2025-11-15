//-----------------------------------------------------------------------
// <copyright file="CrumbleTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for crumble transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Crumble transition effect.
    /// </summary>
    public class CrumbleTransitionEffect : CloudyTransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the CrumbleTransitionEffect class.
        /// </summary>
        public CrumbleTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Crumble.fx.ps");
            this.PixelShader = shader;
        }
    }
}
