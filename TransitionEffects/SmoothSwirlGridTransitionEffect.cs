// <copyright file="SmoothSwirlGridTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for smooth swirl grid transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System;
    using System.Windows;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Smooth swirl grid transition effect.
    /// </summary>
    public class SmoothSwirlGridTransitionEffect : TransitionEffect
    {
        #region Fields

        /// <summary>
        /// DependencyProperty for <see cref="TwistAmount"/> property
        /// </summary>
        public static readonly DependencyProperty TwistAmountProperty = DependencyProperty.Register("TwistAmount", typeof(double), typeof(SmoothSwirlGridTransitionEffect), new UIPropertyMetadata(Math.PI, PixelShaderConstantCallback(1)));

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the SmoothSwirlGridTransitionEffect class.
        /// </summary>
        /// <param name="twist">Specified twist amount.</param>
        public SmoothSwirlGridTransitionEffect(double twist)
            : this()
        {
            this.TwistAmount = twist;
        }

        /// <summary>
        /// Initializes a new instance of the SmoothSwirlGridTransitionEffect class.
        /// </summary>
        public SmoothSwirlGridTransitionEffect()
        {
            this.UpdateShaderValue(TwistAmountProperty);

            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/SmoothSwirlGrid.fx.ps");
            this.PixelShader = shader;
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
