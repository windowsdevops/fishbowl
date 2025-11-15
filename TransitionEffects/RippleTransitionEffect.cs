//-----------------------------------------------------------------------
// <copyright file="RippleTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for ripple transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Ripple transition effect.
    /// </summary>
    public class RippleTransitionEffect : TransitionEffect
    {
        #region Fields

        /// <summary>
        /// DependencyProperty for <see cref="Frequency"/> property
        /// </summary>
        public static readonly DependencyProperty FrequencyProperty = DependencyProperty.Register("Frequency", typeof(double), typeof(RippleTransitionEffect), new UIPropertyMetadata(20.0, PixelShaderConstantCallback(1)));

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the RippleTransitionEffect class.
        /// </summary>
        /// <param name="freq">Riiple frequency</param>
        public RippleTransitionEffect(double freq)
            : this()
        {
            this.Frequency = freq;
        }

        /// <summary>
        /// Initializes a new instance of the RippleTransitionEffect class.
        /// </summary>
        public RippleTransitionEffect()
        {
            this.UpdateShaderValue(FrequencyProperty);

            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Ripple.fx.ps");
            this.PixelShader = shader;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets ripple frequency for this effect.
        /// </summary>
        public double Frequency
        {
            get { return (double)GetValue(FrequencyProperty); }
            set { SetValue(FrequencyProperty, value); }
        }

        #endregion
    }
}
