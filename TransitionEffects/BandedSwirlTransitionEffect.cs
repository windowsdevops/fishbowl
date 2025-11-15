//-----------------------------------------------------------------------
// <copyright file="BandedSwirlTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for banded swirl transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System;
    using System.Windows;
    using System.Windows.Media.Effects;

    /// <summary>
    /// BandedSwirl transition effect.
    /// </summary>
    public class BandedSwirlTransitionEffect : TransitionEffect
    {
        #region Fields

        /// <summary>
        /// DependencyProperty for <see cref="TwistAmount"/>  property
        /// </summary>
        public static readonly DependencyProperty TwistAmountProperty =
            DependencyProperty.Register("TwistAmount", typeof(double), typeof(BandedSwirlTransitionEffect), new UIPropertyMetadata(Math.PI / 4.0, PixelShaderConstantCallback(1)));

        /// <summary>
        /// DependencyProperty for <see cref="Frequency"/>  property
        /// </summary>
        public static readonly DependencyProperty FrequencyProperty =
            DependencyProperty.Register("Frequency", typeof(double), typeof(BandedSwirlTransitionEffect), new UIPropertyMetadata(50.0, PixelShaderConstantCallback(2)));

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the BandedSwirlTransitionEffect class.
        /// </summary>
        /// <param name="twist">Value of twist to be applied by the effect.</param>
        /// <param name="freq">Frequency applied by the effect.</param>
        public BandedSwirlTransitionEffect(double twist, double freq)
            : this()
        {
            this.TwistAmount = twist;
            this.Frequency = freq;
        }

        /// <summary>
        /// Initializes a new instance of the BandedSwirlTransitionEffect class.
        /// </summary>
        public BandedSwirlTransitionEffect()
        {
            UpdateShaderValue(TwistAmountProperty);
            UpdateShaderValue(FrequencyProperty);

            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/BandedSwirl.fx.ps");
            this.PixelShader = shader;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Twist value for this effect.
        /// </summary>
        public double TwistAmount
        {
            get { return (double)GetValue(TwistAmountProperty); }
            set { SetValue(TwistAmountProperty, value); }
        }

        /// <summary>
        /// Gets or sets frequency value for this effect.
        /// </summary>
        public double Frequency
        {
            get { return (double)GetValue(FrequencyProperty); }
            set { SetValue(FrequencyProperty, value); }
        }

        #endregion
    }
}
