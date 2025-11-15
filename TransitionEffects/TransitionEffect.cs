// <copyright file="TransitionEffect.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Base class for transition effects.
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    /// <summary>
    /// Base class for all transition effects - defines common properties and methods for transition effects.
    /// </summary>
    public abstract class TransitionEffect : ShaderEffect
    {
        #region Fields

        /// <summary>
        /// DependencyProperty for <see cref="Input"/> property.
        /// </summary>
        public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(TransitionEffect), 0, SamplingMode.NearestNeighbor);

        /// <summary>
        /// DependencyProperty for <see cref="OldImage"/> property.
        /// </summary>
        public static readonly DependencyProperty OldImageProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("OldImage", typeof(TransitionEffect), 1, SamplingMode.NearestNeighbor);

        /// <summary>
        /// DependencyProperty for <see cref="Progress"/> property.
        /// </summary>
        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register("Progress", typeof(double), typeof(TransitionEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of the TransitionEffect class.
        /// </summary>
        protected TransitionEffect()
        {
            this.UpdateShaderValue(InputProperty);
            this.UpdateShaderValue(OldImageProperty);
            this.UpdateShaderValue(ProgressProperty);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets transition's input.
        /// </summary>
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value of old image to be used in transition.
        /// </summary>
        public Brush OldImage
        {
            get { return (Brush)GetValue(OldImageProperty); }
            set { SetValue(OldImageProperty, value); }
        }

        /// <summary>
        /// Gets or sets transition's rate of progress
        /// </summary>
        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        #endregion
    }
}
