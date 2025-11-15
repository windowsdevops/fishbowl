//-----------------------------------------------------------------------
// <copyright file="DropFadeTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for drop fade transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Drop fade transition effect.
    /// </summary>
    public class DropFadeTransitionEffect : CloudyTransitionEffect
    {
        /// <summary>
        /// Initializes a new instance of the DropFadeTransitionEffect class.
        /// </summary>
        public DropFadeTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/DropFade.fx.ps");
            this.PixelShader = shader;
        }
    }
}
