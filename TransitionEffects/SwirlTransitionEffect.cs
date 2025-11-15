// <copyright file="SwirlTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for swirl transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System;
    using System.Windows;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Swirl transition effect.
    /// </summary>
    public class SwirlTransitionEffect : TransitionEffect
    {
        #region Fields

        /// <summary>
        /// DependencyProperty for <see cref="TwistAmount"/> property
        /// </summary>
        public static readonly DependencyProperty TwistAmountProperty = DependencyProperty.Register("TwistAmount", typeof(double), typeof(SwirlTransitionEffect), new UIPropertyMetadata(Math.PI, PixelShaderConstantCallback(1)));

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the SwirlTransitionEffect class.
        /// </summary>
        /// <param name="twist">Specified twist amount.</param>
        public SwirlTransitionEffect(double twist)
            : this()
        {
            this.TwistAmount = twist;
        }

        /// <summary>
        /// Initializes a new instance of the SwirlTransitionEffect class.
        /// </summary>
        public SwirlTransitionEffect()
        {
            UpdateShaderValue(TwistAmountProperty);

            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Swirl.fx.ps");
            PixelShader = shader;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the twist factor for this effect.
        /// </summary>
        public double TwistAmount
        {
            get { return (double)GetValue(TwistAmountProperty); }
            set { SetValue(TwistAmountProperty, value); }
        }

        #endregion
    }
}
