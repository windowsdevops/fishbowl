//-----------------------------------------------------------------------
// <copyright file="StretchBox.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//      The Decorator used within a ContentControl's ControlTemplate to cause
//      its ContentPresenter to stretch horizontally.
// </summary>
//-----------------------------------------------------------------------
namespace EffectControls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// The Decorator used within a ContentControl's ControlTemplate to cause
    /// its ContentPresenter to stretch horizontally.
    /// </summary>
    public class StretchBox : Decorator
    {
        /// <summary>
        /// Initializes a new instance of the StretchBox class.
        /// </summary>
        public StretchBox()
        {
            this.Loaded += new RoutedEventHandler(this.StretchBox_Loaded);
        }

        /// <summary>
        /// The Loaded event handler which forces the ContentPresenter to stretch horizontally.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void StretchBox_Loaded(object sender, RoutedEventArgs e)
        {
            for (ContentPresenter contentPresenter = VisualTreeHelper.GetParent(this) as ContentPresenter;
                 contentPresenter != null;
                 contentPresenter = VisualTreeHelper.GetParent(contentPresenter) as ContentPresenter)
            {
                contentPresenter.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
        }
    }
}
