//-----------------------------------------------------------------------
// <copyright file="BulgeAndPinchEffect.cs" company="Microsoft">
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
    public class BulgeAndPinchEffect : ShaderEffect
    {
        /// <summary>
        /// The explict input for this pixel shader
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(BulgeAndPinchEffect), 0);

        /// <summary>
        /// This property is mapped to the corrosponding variable within the pixel shader
        /// </summary>
        public static readonly DependencyProperty RadiiProperty = DependencyProperty.Register("Radii", typeof(Size), typeof(BulgeAndPinchEffect), new UIPropertyMetadata(new Size(0.2, 0.2), PixelShaderConstantCallback(0)));

        /// <summary>
        /// This property is mapped to the corrosponding variable within the pixel shader
        /// </summary>
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register("Center", typeof(Point), typeof(BulgeAndPinchEffect), new UIPropertyMetadata(new Point(0.25, 0.25), PixelShaderConstantCallback(1)));
        
        /// <summary>
        /// This property is mapped to the corrosponding variable within the pixel shader
        /// </summary>
        public static readonly DependencyProperty BulgeMultiplierProperty = DependencyProperty.Register("BulgeMultiplier", typeof(Vector), typeof(BulgeAndPinchEffect), new UIPropertyMetadata(new Vector(1, 1), PixelShaderConstantCallback(2)));

        /// <summary>
        /// A refernce to the pixel shader used
        /// </summary>
        private static PixelShader pixelShader;

        /// <summary>
        /// The transform used when this Effect is applied
        /// </summary>
        private BulgeAndPinchEffectGeneralTransform generalTransform;

        /// <summary>
        /// Initializes static members of the BulgeAndPinchEffect class.
        /// </summary>
        static BulgeAndPinchEffect()
        {
            pixelShader = new PixelShader();
            pixelShader.UriSource = Global.MakePackUri("ShaderBytecode/bulgeAndPinch.fx.ps");
        }

        /// <summary>
        /// Initializes a new instance of the BulgeAndPinchEffect class.
        /// Creates an instance and updates the shader's variables to the default values
        /// </summary>
        public BulgeAndPinchEffect()
        {
            this.PixelShader = pixelShader;

            UpdateShaderValue(RadiiProperty);
            UpdateShaderValue(CenterProperty);
            UpdateShaderValue(BulgeMultiplierProperty);
            UpdateShaderValue(InputProperty);

            this.generalTransform = new BulgeAndPinchEffectGeneralTransform(this);
        }

        /// <summary>
        /// Gets or sets the Radii variable within the shader
        /// </summary>
        public Size Radii
        {
            get { return (Size)GetValue(RadiiProperty); }
            set { SetValue(RadiiProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Center variable within the shader
        /// </summary>
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the BulgeMultiplier variable within the shader
        /// </summary>
        public Vector BulgeMultiplier
        {
            get { return (Vector)GetValue(BulgeMultiplierProperty); }
            set { SetValue(BulgeMultiplierProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Input of shader
        /// </summary>
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        /// <summary>
        /// Gets the transform of this effect
        /// </summary>
        protected override GeneralTransform EffectMapping
        {
            get
            {
                return this.generalTransform;
            }
        }

        /// <summary>
        /// Implementation of the transform specific to the BulgeAndPinchEffect
        /// </summary>
        private class BulgeAndPinchEffectGeneralTransform : GeneralTransform
        {
            /// <summary>
            /// The effect instance
            /// </summary>
            private readonly BulgeAndPinchEffect currentEffect;

            /// <summary>
            /// Is inverse transform
            /// </summary>
            private bool transformIsInverse;

            /// <summary>
            /// The general transform for this effect
            /// </summary>
            private BulgeAndPinchEffectGeneralTransform inverseTransform;

            /// <summary>
            /// Initializes a new instance of the BulgeAndPinchEffectGeneralTransform class.
            /// </summary>
            /// <param name="eff">The BulgeAndPinchEffect</param>
            public BulgeAndPinchEffectGeneralTransform(BulgeAndPinchEffect eff)
            {
                this.currentEffect = eff;
            }

            /// <summary>
            /// Gets the inverse
            /// </summary>
            public override GeneralTransform Inverse
            {
                get
                {
                    // Cache this since it can get called often
                    if (this.inverseTransform == null)
                    {
                        this.inverseTransform = (BulgeAndPinchEffectGeneralTransform)this.Clone();
                        this.inverseTransform.transformIsInverse = !this.transformIsInverse;
                    }

                    return this.inverseTransform;
                }
            }

            /// <summary>
            /// This is a no-op because of the type of effect.
            /// </summary>
            /// <param name="rect">the input rect</param>
            /// <returns>no-op: returns the input rect</returns>
            public override Rect TransformBounds(Rect rect)
            {
                return rect;
            }

            /// <summary>
            /// In this particular case, the inverse transform is the same as the forward
            /// transform.
            /// </summary>
            /// <param name="source">input point</param>
            /// <param name="result">the resunt</param>
            /// <returns>true if successful</returns>
            public override bool TryTransform(Point source, out Point result)
            {
                bool pointInEllipse = IsPointInEllipse(source, this.currentEffect.Center, this.currentEffect.Radii);

                if (!pointInEllipse)
                {
                    // If outside the ellipse, just the identity.
                    result = source;
                }
                else
                {
                    // If inside the ellipse, reflect about the center in the x direction
                    double centerX = this.currentEffect.Center.X;
                    result = new Point(centerX + (centerX - source.X), source.Y);
                }

                return true;
            }

            /// <summary>
            /// Returns a new instance
            /// </summary>
            /// <returns>a new copy of itself</returns>
            protected override Freezable CreateInstanceCore()
            {
                return new BulgeAndPinchEffectGeneralTransform(this.currentEffect) { transformIsInverse = this.transformIsInverse };
            }

            /// <summary>
            /// Checks if point is in an ellipse
            /// </summary>
            /// <param name="pt">point to test</param>
            /// <param name="center">center of the ellipse</param>
            /// <param name="radii">radii of ellipse</param>
            /// <returns>true if so</returns>
            private static bool IsPointInEllipse(Point pt, Point center, Size radii)
            {
                Vector ray = pt - center;
                double rayPctX = ray.X / radii.Width;
                double rayPctY = ray.Y / radii.Height;

                // Normally would take sqrt() for length, but since we're comparing 
                // to 1.0, it doesn't matter.
                double pctLength = (rayPctX * rayPctX) + (rayPctY * rayPctY);

                return pctLength <= 1.0;
            }
        }
    }
}
