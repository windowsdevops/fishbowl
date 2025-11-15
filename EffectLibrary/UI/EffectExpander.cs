//-----------------------------------------------------------------------
// <copyright file="EffectExpander.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//      This is the base abstract classe inherited by the custom Effect
//      Controls for displaying UI for their custom ShaderEffect.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    ///     This is the base abstract classe inherited by the custom Effect
    ///     Controls for displaying UI for their custom ShaderEffect.
    /// </summary>
    public class EffectExpander : Expander
    {
        /// <summary>
        /// The IsEffectAppliedProperty DependencyProperty declaration. This is used to display the state of the Effect in the UI.
        /// </summary>
        public static readonly DependencyProperty IsEffectAppliedProperty = DependencyProperty.Register("IsEffectAppliedProperty", typeof(bool), typeof(EffectExpander), new PropertyMetadata(false, OnIsEffectAppliedChanged));

        /// <summary>
        /// The EffectNameProperty DependencyProperty declaration used to display the name of the Effect.
        /// </summary>
        public static readonly DependencyProperty EffectNameProperty = DependencyProperty.Register("EffectNameProperty", typeof(string), typeof(EffectExpander));

        /// <summary>
        /// The TargetElement DependencyProperty declaration. This is a reference to the element which the Effect will be applied to.
        /// </summary>
        public static readonly DependencyProperty TargetElementProperty = DependencyProperty.Register("TargetElement", typeof(FrameworkElement), typeof(EffectExpander), new UIPropertyMetadata(null));

        /// <summary>
        /// Initializes static members of the EffectExpander class.
        /// </summary>
        static EffectExpander()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EffectExpander), new FrameworkPropertyMetadata(typeof(EffectExpander)));
        }

        /// <summary>
        /// Gets or sets a value indicating whether an Effect has been applied.
        /// </summary>
        public bool IsEffectApplied
        {
            get
            {
                return (bool)GetValue(IsEffectAppliedProperty);
            }

            set
            {
                SetValue(IsEffectAppliedProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the EffectNameProperty DependencyProperty.
        /// </summary>
        public string EffectName
        {
            get
            {
                return (string)GetValue(EffectNameProperty);
            }

            set
            {
                SetValue(EffectNameProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the TargetElementProperty DependencyProperty.
        /// </summary>
        public FrameworkElement TargetElement
        {
            get { return (FrameworkElement)GetValue(TargetElementProperty); }
            set { SetValue(TargetElementProperty, value); }
        }

        /// <summary>
        /// This sets the element which the Effect is applied to.
        /// </summary>
        /// <param name="targetElement">A reference to an element.</param>
        public void SetTarget(FrameworkElement targetElement)
        {
            this.TargetElement = targetElement;
        }

        /// <summary>
        /// Called when an Effect is to be applied to the TargetElement.
        /// </summary>
        protected virtual void ApplyEffect()
        {
        }

        /// <summary>
        /// Called when an Effect is to be removed from the TargetElement.
        /// </summary>
        protected virtual void RemoveEffect()
        {
        }

        /// <summary>
        /// This event is raised when the IsEffectAppliedProperty's value has changed.
        /// </summary>
        /// <param name="d">The current object.</param>
        /// <param name="e">Any arguments.</param>
        private static void OnIsEffectAppliedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EffectExpander expander = (EffectExpander)d;

            if ((bool)e.NewValue == true)
            {
                expander.ApplyEffect();
            }
            else
            {
                expander.RemoveEffect();
            }
        }
    }
}
