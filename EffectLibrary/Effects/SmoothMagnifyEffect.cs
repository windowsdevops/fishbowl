//-----------------------------------------------------------------------
// <copyright file="SmoothMagnifyEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     WPF Extensible Effect
// </summary>
//-----------------------------------------------------------------------
namespace EffectLibrary
{
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    /// <summary>
    /// This is the implementation of an extensible framework ShaderEffect which loads
    /// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
    /// to registers as defined in the *.ps file being loaded below.
    /// </summary>
    public class SmoothMagnifyEffect : ShaderEffect
    {
        /// <summary>
        /// Gets or sets the Center variable within the shader.
        /// </summary>
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register("Center", typeof(Point), typeof(SmoothMagnifyEffect), new UIPropertyMetadata(new Point(0.5, 0.5), PixelShaderConstantCallback(0)));

        /// <summary>
        /// Gets or sets the InnerRadius variable within the shader.
        /// </summary>
        public static readonly DependencyProperty InnerRadiusProperty = DependencyProperty.Register("InnerRadius", typeof(double), typeof(SmoothMagnifyEffect), new UIPropertyMetadata(.2, PixelShaderConstantCallback(2)));

        /// <summary>
        /// Gets or sets the Magnification variable within the shader.
        /// </summary>
        public static readonly DependencyProperty MagnificationProperty = DependencyProperty.Register("Magnification", typeof(double), typeof(SmoothMagnifyEffect), new UIPropertyMetadata(2.0, PixelShaderConstantCallback(3)));

        /// <summary>
        /// Gets or sets the OuterRaduis variable within the shader.
        /// </summary>
        public static readonly DependencyProperty OuterRadiusProperty = DependencyProperty.Register("OuterRadius", typeof(double), typeof(SmoothMagnifyEffect), new UIPropertyMetadata(.27, PixelShaderConstantCallback(4)));

        /// <summary>
        /// Gets or sets the input brush used in the shader.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(SmoothMagnifyEffect), 0);

        /// <summary>
        /// The pixel shader instance.
        /// </summary>
        private static PixelShader pixelShader = new PixelShader();

        /// <summary>
        /// Initializes static members of the SmoothMagnifyEffect class.
        /// </summary>
        static SmoothMagnifyEffect()
        {
            pixelShader.UriSource = Global.MakePackUri("ShaderBytecode/smoothmagnify.fx.ps");
        }

        /// <summary>
        /// Initializes a new instance of the SmoothMagnifyEffect class.
        /// </summary>
        public SmoothMagnifyEffect()
        {
            this.PixelShader = pixelShader;

            UpdateShaderValue(CenterProperty);
            UpdateShaderValue(InnerRadiusProperty);
            UpdateShaderValue(OuterRadiusProperty);
            UpdateShaderValue(MagnificationProperty);
            UpdateShaderValue(InputProperty);
        }

        /// <summary>
        /// Gets or sets the center variable within the shader.
        /// </summary>
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the InnerRadius variable within the shader.
        /// </summary>
        public double InnerRadius
        {
           get { return (double)GetValue(InnerRadiusProperty); }
           set { SetValue(InnerRadiusProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Magnification variable within the shader.
        /// </summary>
        public double Magnification
        {
           get { return (double)GetValue(MagnificationProperty); }
           set { SetValue(MagnificationProperty, value); }
        }

        /// <summary>
        /// Gets or sets the OuterRadius variable within the shader.
        /// </summary>
        public double OuterRadius
        {
           get { return (double)GetValue(OuterRadiusProperty); }
           set { SetValue(OuterRadiusProperty, value); }
        }

        /// <summary>
        /// Gets or sets the input brush used within the shader.
        /// </summary>
        public Brush Input
        {
           get { return (Brush)GetValue(InputProperty); }
           set { SetValue(InputProperty, value); }
        }
    }
}
