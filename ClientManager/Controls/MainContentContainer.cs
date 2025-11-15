//-----------------------------------------------------------------------
// <copyright file="MainContentContainer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Container element for the primary content of a ScePhoto application.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using System.Windows.Threading;
    using ClientManager.View;

    public class ContentChangedEventArgs : EventArgs
    {
        public ContentChangedEventArgs(object oldContent, object newContent)
        {
            OldContent = oldContent;
            NewContent = newContent;
        }

        public object OldContent { get; private set; }
        public object NewContent { get; private set; }
    }

    /// <summary>Container element for the primary content of the application.</summary>
    public class MainContentContainer : ContentControl
    {
        public event EventHandler<ContentChangedEventArgs> ContentChanged;

        private CustomContentState _contentStateToSave;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Virtual handler for loaded event.
        /// </summary>
        private void OnLoaded()
        {
            ServiceProvider.ViewManager.Navigated += this.OnViewManagerNavigated;
            NavigationService navigationService = NavigationService.GetNavigationService(this);
            if (navigationService != null)
            {
                navigationService.Navigating += this.OnNavigationServiceNavigating;
            }

            if (ServiceProvider.ViewManager.CurrentNavigator != null)
            {
                ServiceProvider.ViewManager.NavigateByRefresh();
            }

        }

        /// <summary>
        /// Virtual handler for unloaded event.
        /// </summary>
        protected void OnUnloaded()
        {
            ServiceProvider.ViewManager.Navigated -= this.OnViewManagerNavigated;
            NavigationService navigationService = NavigationService.GetNavigationService(this);
            if (navigationService != null)
            {
                navigationService.Navigating -= this.OnNavigationServiceNavigating;
            }
        }

        /// <summary>
        /// Does actual navigation - adds journal entry if necessary, etc.
        /// </summary>
        /// <param name="e">Details of the navigation event.</param>
        protected void DoNavigation(ViewManagerNavigatedEventArgs e)
        {
            if (_contentStateToSave != null && this.IsNewNavigation(e.NavigationMode))
            {
                NavigationService navigationService = NavigationService.GetNavigationService(this);
                if (navigationService != null)
                {
                    CustomContentState contentStateToSave = _contentStateToSave;
                    _contentStateToSave = null;
                    if (!e.NewNavigator.ShouldReplaceNavigatorInJournal(e.OldNavigator))
                    {
                        navigationService.AddBackEntry(contentStateToSave);
                    }
                }
            }

            if (e.ContentStateToSave != null)
            {
                _contentStateToSave = e.ContentStateToSave;
            }

            Content = e.Content;
        }

        /// <summary>
        /// Determines whether this container considers a navigation to be "new" or not. New navigations cause a back entry
        /// to be added to the journal's back stack.
        /// </summary>
        /// <param name="navigationMode">Mode of the navigation, determines if it will be to a new Navigator or not.</param>
        /// <returns>True if the navigation mode represents a "new" navigation, i.e. not from the journal stack.</returns>
        private bool IsNewNavigation(NavigationMode navigationMode)
        {
            return (navigationMode != NavigationMode.Back) && 
                   (navigationMode != NavigationMode.Forward) &&
                   (navigationMode != NavigationMode.Refresh);
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            var handler = ContentChanged;
            if (handler != null)
            {
                handler(this, new ContentChangedEventArgs(oldContent, newContent));
            }
        }

        /// <summary>
        /// When navigating via the journal, the last viewed content state has not been
        /// saved to the journal. We can listen for the navigating event on the navigation service to store the content state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Details of the cancelled navigation.</param>
        private void OnNavigationServiceNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (_contentStateToSave != null)
            {
                e.ContentStateToSave = _contentStateToSave;
                _contentStateToSave = null;
            }
        }

        /// <summary>
        /// Event handler for ViewManager's navigated event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Details of the navigation.</param>
        private void OnViewManagerNavigated(object sender, ViewManagerNavigatedEventArgs e)
        {
            this.DoNavigation(e);
        }

        /// <summary>
        /// Handler for unloaded event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Details of the unload.</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.OnUnloaded();
        }

        /// <summary>
        /// Handler for loaded event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Details of the load.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.OnLoaded();
        }
    }
}
