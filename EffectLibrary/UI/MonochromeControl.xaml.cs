//-----------------------------------------------------------------------
// <copyright file="MonochromeControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The partial-class for MonochromeControl.xaml which exposes the UI
//     for the MonochromeEffect.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using EffectLibrary;

    /// <summary>
    ///     The partial-class for MonochromeControl.xaml which exposes the UI
    ///     for the MonochromeEffect.
    /// </summary>
    public partial class MonochromeControl : EffectExpander
    {
        /// <summary>
        /// Initializes a new instance of the MonochromeControl class.
        /// </summary>
        public MonochromeControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Apply the Effect to the TargetElement.
        /// </summary>
        protected override void ApplyEffect()
        {
            if (TargetElement != null && TargetElement.Effect == null)
            {
                TargetElement.Effect = new MonochromeEffect();
            }
        }

        /// <summary>
        /// Remove the Effect from the TargetElement.
        /// </summary>
        protected override void RemoveEffect()
        {
            if (TargetElement != null)
            {
                TargetElement.Effect = null;
            }
        }
    }
}
