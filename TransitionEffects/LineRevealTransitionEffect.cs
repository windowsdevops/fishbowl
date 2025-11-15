//-----------------------------------------------------------------------
// <copyright file="LineRevealTransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Code for line reveal transition effect
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Line reveal transition effect.
    /// </summary>
    public class LineRevealTransitionEffect : TransitionEffect
    {
        #region Fields

        /// <summary>
        /// DependencyProperty for <see cref="LineOrigin"/> property
        /// </summary>
        public static readonly DependencyProperty LineOriginProperty = DependencyProperty.Register("LineOrigin", typeof(Point), typeof(LineRevealTransitionEffect), new UIPropertyMetadata(new Point(-0.2, -0.2), PixelShaderConstantCallback(1)));

        /// <summary>
        /// DependencyProperty for <see cref="LineNormal"/> property
        /// </summary>
        public static readonly DependencyProperty LineNormalProperty = DependencyProperty.Register("LineNormal", typeof(Vector), typeof(LineRevealTransitionEffect), new UIPropertyMetadata(new Vector(1.0, 1.0), PixelShaderConstantCallback(2)));

        /// <summary>
        /// DependencyProperty for <see cref="LineOffset"/> property
        /// </summary>
        public static readonly DependencyProperty LineOffsetProperty = DependencyProperty.Register("LineOffset", typeof(Vector), typeof(LineRevealTransitionEffect), new UIPropertyMetadata(new Vector(1.4, 1.4), PixelShaderConstantCallback(3)));

        /// <summary>
        /// DependencyProperty for <see cref="FuzzyAmount"/> property
        /// </summary>
        public static readonly DependencyProperty FuzzyAmountProperty = DependencyProperty.Register("FuzzyAmount", typeof(double), typeof(LineRevealTransitionEffect), new UIPropertyMetadata(0.2, PixelShaderConstantCallback(4)));

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the LineRevealTransitionEffect class.
        /// </summary>
        /// <param name="origin">Line origin.</param>
        /// <param name="normal">Vector representing line's normal.</param>
        /// <param name="offset">Line offset.</param>
        /// <param name="fuzziness">Fuzziness factor for this effect.</param>
        public LineRevealTransitionEffect(Point origin, Vector normal, Vector offset, double fuzziness)
            : this()
        {
            this.LineOrigin = origin;
            this.LineNormal = normal;
            this.LineOffset = offset;
            this.FuzzyAmount = fuzziness;
        }

        /// <summary>
        /// Initializes a new instance of the LineRevealTransitionEffect class.
        /// </summary>
        public LineRevealTransitionEffect()
        {
            UpdateShaderValue(LineOriginProperty);
            UpdateShaderValue(LineNormalProperty);
            UpdateShaderValue(LineOffsetProperty);
            UpdateShaderValue(FuzzyAmountProperty);

            PixelShader shader = new PixelShader();
            shader.UriSource = TransitionUtilities.MakePackUri("Shaders/LineReveal.fx.ps");
            PixelShader = shader;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the line's origin.
        /// </summary>
        public Point LineOrigin
        {
            get { return (Point)GetValue(LineOriginProperty); }
            set { SetValue(LineOriginProperty, value); }
        }

        /// <summary>
        /// Gets or sets a vector that represents the line's normal.
        /// </summary>
        public Vector LineNormal
        {
            get { return (Vector)GetValue(LineNormalProperty); }
            set { SetValue(LineNormalProperty, value); }
        }

        /// <summary>
        /// Gets or sets a vector representing line offset.
        /// </summary>
        public Vector LineOffset
        {
            get { return (Vector)GetValue(LineOffsetProperty); }
            set { SetValue(LineOffsetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the fuzziness factor for the effect.
        /// </summary>
        public double FuzzyAmount
        {
            get { return (double)GetValue(FuzzyAmountProperty); }
            set { SetValue(FuzzyAmountProperty, value); }
        }

        #endregion
    }
}
