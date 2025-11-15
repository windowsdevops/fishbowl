//-----------------------------------------------------------------------
// <copyright file="ApplicationInputHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Global input handler for the application.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows;
    using ClientManager;
    using ClientManager.View;
    using Standard;

    /// <summary>
    /// Global input handler for the application. Global input handler receives the OnKeyDown event from the main page.
    /// If no control or other UI has handled the event on the route, global application key handling can take over.
    /// It can then take make "application-wide" decisions about what action take based on
    /// application state that individual controls may not have, or if users do not want to customize controls to handle key input.
    /// </summary>
    public static class ApplicationInputHandler
    {
        static Point _lastposition;

        /// <summary>
        /// Application-level navigation handler for key down - checks if key event impacts navigation and takes
        /// necessary action.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        public static void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled && !(e.OriginalSource is TextBox) && !(e.OriginalSource is WebBrowser) && (ServiceProvider.ViewManager.Dialog == null))
            {
                bool handled = false;
                switch (e.KeyboardDevice.Modifiers)
                {
                    case ModifierKeys.None:
                        // Shortcut keys without modifiers generally initiate navigation, handled here
                        switch (e.Key)
                        {
                            case Key.Left:
                                _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToPriorCommand);
                                handled = true;
                                break;
                            case Key.Space:
                            case Key.Right:
                                _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToNextCommand);
                                handled = true;
                                break;
                            case Key.Up:
                            case Key.PageUp:
                                if (!(e.OriginalSource is FlowDocumentPageViewer))
                                {
                                    _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToPriorSiblingCommand);
                                    handled = true;
                                }
                                break;
                            case Key.Down:
                            case Key.PageDown:
                                if (!(e.OriginalSource is FlowDocumentPageViewer))
                                {
                                    _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToNextSiblingCommand);
                                    handled = true;
                                }
                                break;
                            case Key.Home:
                                _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToBeginningCommand);
                                handled = true;
                                break;
                            case Key.End:
                                _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToEndCommand);
                                handled = true;
                                break;
                            default:
                                break;
                        }
                        break;
                    case ModifierKeys.Control:
                        switch (e.Key)
                        {
                            // Alt-Up means navigate to parent in explorer.
                            case Key.Up:
                                _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToParentCommand);
                                handled = true;
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
                e.Handled = handled;
            }
        }

        public static void OnPreviewStylusSystemGesture(StylusSystemGestureEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.SystemGesture == SystemGesture.Flick)
                {
                    Point _newposition;
                    _newposition = e.GetPosition(Application.Current.MainWindow.Content as UIElement);

                    if (_newposition.X > _lastposition.X)
                    {
                        _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToNextCommand);
                        e.Handled = true;
                    }
                    else
                    {
                        _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToPriorCommand);
                        e.Handled = true;
                    }
                }
            }       
        }

        public static void OnPreviewStylusMove(StylusEventArgs e)
        {
            if (!e.Handled)
                _lastposition = e.GetPosition(Application.Current.MainWindow.Content as UIElement);

        }


        /// <summary>
        /// Application-wide handler for MouseWheel event.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        public static void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!e.Handled && (ServiceProvider.ViewManager.Dialog == null))
            {
                // I think this is wrong.  Not changing it for now for risk of breaking something unknown...
                // Try removing all but the ctrl handlers for this at some time when we can afford more risk.
                if (!(e.OriginalSource is FlowDocumentPageViewer
                        || e.OriginalSource is Paragraph
                        || e.OriginalSource is FlowDocument
                        || e.OriginalSource is Run
                        || Keyboard.IsKeyDown(Key.LeftCtrl)
                        || Keyboard.IsKeyDown(Key.RightCtrl)
                    ))

                {
                    if (DoubleUtilities.LessThan(e.Delta, 0))
                    {
                        _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToNextCommand);
                    }
                    else if (DoubleUtilities.GreaterThan(e.Delta, 0))
                    {
                        _SafeExecuteCommand(ServiceProvider.ViewManager.NavigationCommands.NavigateToPriorCommand);
                    }

                    e.Handled = true;
                }
            }
        }

        private static void _SafeExecuteCommand(ViewCommand command)
        {
            Assert.IsNotNull(command);
            if (command.CanExecute(ServiceProvider.ViewManager.CurrentNavigator))
            {
                command.Execute(ServiceProvider.ViewManager.CurrentNavigator);
            }
        }
    }
}
