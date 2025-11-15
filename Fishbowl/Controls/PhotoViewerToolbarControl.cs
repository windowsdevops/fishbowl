//-----------------------------------------------------------------------
// <copyright file="PhotoViewerToolbarControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Custom toolbar control.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;

    /// <summary>
    /// Custom toolbar control that focuses a control when a contained button is clicked.
    /// </summary>
    public class PhotoViewerToolbarControl : ItemsControl
    {
        /// <summary>
        /// Dependency Property backing store for FocusTarget.
        /// </summary>
        public static readonly DependencyProperty FocusTargetProperty =
            DependencyProperty.Register("FocusTarget", typeof(IInputElement), typeof(PhotoViewerToolbarControl), new UIPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the target to switch focus to on button presses.
        /// </summary>
        public IInputElement FocusTarget
        {
            get { return (IInputElement)GetValue(FocusTargetProperty); }
            set { SetValue(FocusTargetProperty, value); }
        }

        /// <summary>
        /// Refocuses the photo viewer control when the button is pressed via *mouse* click.
        /// </summary>
        /// <param name="e">Event arguments describing the mouse event.</param>
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            ButtonBase button = e.Source as ButtonBase;
            if (button != null && this.FocusTarget != null)
            {
                // ManuallyAddContact raise the event on the button since focusing another control breaks the original event routing.
                MouseButtonEventArgs mouseEventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);
                mouseEventArgs.RoutedEvent = Mouse.MouseUpEvent;
                button.RaiseEvent(mouseEventArgs);

                this.FocusTarget.Focus();
            }
        }
    }
}
