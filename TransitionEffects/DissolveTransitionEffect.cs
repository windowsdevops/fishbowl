//-----------------------------------------------------------------------
// <copyright file="DissolveTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for dissolve transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Dissolve transition effect.
    /// </summary>
    public class DissolveTransitionEffect : RandomizedTransitionEffect
    {
        #region Fields

        /// <summary>
        /// DependencyProperty for <see cref="NoiseImage"/> property
        /// </summary>
        protected static readonly DependencyProperty NoiseImageProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("NoiseImage", typeof(DissolveTransitionEffect), 2, SamplingMode.Bilinear);

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the DissolveTransitionEffect class.
        /// </summary>
        public DissolveTransitionEffect()
        {
            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/Disolve.fx.ps");
            this.PixelShader = shader;

            this.NoiseImage = new ImageBrush(new BitmapImage(TransitionUtilities.MakePackUri("Images/noise.png")));
            this.UpdateShaderValue(NoiseImageProperty);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Brush to be used as noise image for this transition.
        /// </summary>
        protected Brush NoiseImage
        {
            get { return (Brush)GetValue(NoiseImageProperty); }
            set { SetValue(NoiseImageProperty, value); }
        }

        #endregion
    }
}
