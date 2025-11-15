//-----------------------------------------------------------------------
// <copyright file="ZoomBlurEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     WPF Extensible Effect
// </summary>
//-----------------------------------------------------------------------
namespace EffectLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    /// <summary>
    /// This is the implementation of an extensible framework ShaderEffect which loads
    /// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
    /// to registers as defined in the *.ps file being loaded below.
    /// </summary>
    public class ZoomBlurEffect : ShaderEffect
    {
        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ZoomBlurEffect), 0);

        /// <summary>
        /// This property is mapped to the CenterX variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty CenterXProperty = DependencyProperty.Register("CenterX", typeof(double), typeof(ZoomBlurEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the CenterY variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty CenterYProperty = DependencyProperty.Register("CenterY", typeof(double), typeof(ZoomBlurEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));

        /// <summary>
        /// This property is mapped to the BlurAmount variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty BlurAmountProperty = DependencyProperty.Register("BlurAmount", typeof(double), typeof(ZoomBlurEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(2)));

        /// <summary>
        /// A refernce to the pixel shader used.
        /// </summary>
        private static PixelShader pixelShader = new PixelShader();

        /// <summary>
        /// Initializes static members of the ZoomBlurEffect class.
        /// </summary>
        static ZoomBlurEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderBytecode/ZoomBlur.fx.ps");
        }

        /// <summary>
        /// Initializes a new instance of the ZoomBlurEffect class.
        /// </summary>
        public ZoomBlurEffect()
        {
            this.PixelShader = pixelShader;
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(CenterXProperty);
            UpdateShaderValue(CenterYProperty);
            UpdateShaderValue(BlurAmountProperty);
        }

        /// <summary>
        /// Gets or sets the Input shader sampler.
        /// </summary>
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        /// <summary>
        /// Gets or sets the CenterX variable within the shader.
        /// </summary>
        public double CenterX
        {
            get { return (double)GetValue(CenterXProperty); }
            set { SetValue(CenterXProperty, value); }
        }

        /// <summary>
        /// Gets or sets the CenterY variable within the shader.
        /// </summary>
        public double CenterY
        {
            get { return (double)GetValue(CenterYProperty); }
            set { SetValue(CenterYProperty, value); }
        }

        /// <summary>
        /// Gets or sets the BlurAmount variable within the shader.
        /// </summary>
        public double BlurAmount
        {
            get { return (double)GetValue(BlurAmountProperty); }
            set { SetValue(BlurAmountProperty, value); }
        }
    }
}
