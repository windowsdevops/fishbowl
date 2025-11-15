// <copyright file="SwirlGridTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for swirl grid transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System;
    using System.Windows;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Swirl grid transition effect.
    /// </summary>
    public class SwirlGridTransitionEffect : TransitionEffect
    {
        #region Fields

        /// <summary>
        /// DependencyProperty for <see cref="TwistAmount"/> property
        /// </summary>
        public static readonly DependencyProperty TwistAmountProperty = DependencyProperty.Register("TwistAmount", typeof(double), typeof(SwirlGridTransitionEffect), new UIPropertyMetadata(Math.PI, PixelShaderConstantCallback(1)));

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the SwirlGridTransitionEffect class.
        /// </summary>
        /// <param name="twist">Specified twist amount.</param>
        public SwirlGridTransitionEffect(double twist)
            : this()
        {
            this.TwistAmount = twist;
        }

        /// <summary>
        /// Initializes a new instance of the SwirlGridTransitionEffect class.
        /// </summary>
        public SwirlGridTransitionEffect()
        {
            this.UpdateShaderValue(TwistAmountProperty);

            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/SwirlGrid.fx.ps");
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
