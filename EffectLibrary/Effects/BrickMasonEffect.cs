//-----------------------------------------------------------------------
// <copyright file="BrickMasonEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     WPF Extensible Effect
// </summary>
//-----------------------------------------------------------------------
namespace EffectLibrary
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    /// <summary>
    /// This is the implementation of an extensible framework ShaderEffect which loads
    /// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
    /// to registers as defined in the *.ps file being loaded below.
    /// </summary>
    public class BrickMasonEffect : ShaderEffect
    {
        /// <summary>
        /// This property is mapped to the BrickCounts variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty BrickCountsProperty = DependencyProperty.Register("BrickCounts", typeof(Size), typeof(BrickMasonEffect), new UIPropertyMetadata(new Size(20, 10), PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the MortarPixelSize variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty MortarPixelSizeProperty = DependencyProperty.Register("MortarPixelSize", typeof(Vector), typeof(BrickMasonEffect), new UIPropertyMetadata(new Vector(1, 1), PixelShaderConstantCallback(1)));
        
        /// <summary>
        /// This property is mapped to the MortarColor variable within the pixel shader.
        /// </summary>
        public static readonly DependencyProperty MortarColorProperty = DependencyProperty.Register("MortarColor", typeof(Color), typeof(BrickMasonEffect), new UIPropertyMetadata(Colors.Black, PixelShaderConstantCallback(2)));

        /// <summary>
        /// The explict input for this pixel shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(BrickMasonEffect), 0);

        /// <summary>
        /// The loaded pixel shader.
        /// </summary>
        private static PixelShader pixelShader = new PixelShader();

        /// <summary>
        /// Initializes static members of the BrickMasonEffect class.
        /// </summary>
        static BrickMasonEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderBytecode/brickMason.fx.ps");
        }

        /// <summary>
        /// Initializes a new instance of the BrickMasonEffect class.
        /// Creates an instance and updates the shader's variables to the default values.
        /// </summary>
        public BrickMasonEffect()
        {
            this.PixelShader = pixelShader;
            this.DdxUvDdyUvRegisterIndex = 6;

            UpdateShaderValue(BrickCountsProperty);
            UpdateShaderValue(MortarPixelSizeProperty);
            UpdateShaderValue(MortarColorProperty);
            UpdateShaderValue(InputProperty);
        }

        /// <summary>
        /// Gets or sets the BrickCounts variable within the shader.
        /// </summary>
        public Size BrickCounts
        {
            get { return (Size)GetValue(BrickCountsProperty); }
            set { SetValue(BrickCountsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the MortarPixelSize variable within the shader.
        /// </summary>
        public Vector MortarPixelSize
        {
            get { return (Vector)GetValue(MortarPixelSizeProperty); }
            set { SetValue(MortarPixelSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the MortarColor variable within the shader.
        /// </summary>
        public Color MortarColor
        {
            get { return (Color)GetValue(MortarColorProperty); }
            set { SetValue(MortarColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Input shader sampler.
        /// </summary>
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
    }
}
