//-----------------------------------------------------------------------
// <copyright file="RotateCrumbleTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for rotate crumble transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows.Media.Effects;

    /// <summary>
    /// Rotate crumble transition effect.
    /// </summary>
    public class RotateCrumbleTransitionEffect : CloudyTransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the RotateCrumbleTransitionEffect class.
        /// </summary>
        public RotateCrumbleTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/RotateCrumble.fx.ps");
            this.PixelShader = shader;
        }
    }
}
