//-----------------------------------------------------------------------
// <copyright file="SearchViewControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Interaction logic for SearchViewControl.xaml.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using ClientManager;
    using ClientManager.Controls;
    using ClientManager.Data;
    using ClientManager.View;
    using Contigo;

    /// <summary>
    /// Interaction logic for SearchViewControl.xaml.
    /// </summary>
    public partial class SearchViewControl : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Command to switch the search control to list view.
        /// </summary>
        private static RoutedCommand switchToListViewCommand = new RoutedCommand("SwitchToListView", typeof(SearchViewControl));

        /// <summary>
        /// Command to switch the search control to the photo explorer view.
        /// </summary>
        private static RoutedCommand switchToPhotoExplorerCommand = new RoutedCommand("SwitchToPhotoExplorer", typeof(SearchViewControl));

        /// <summary>
        /// The current display mode.
        /// </summary>
        private SearchViewMode displayMode = SearchViewMode.ListView;

        /// <summary>
        /// Animation for the list view's opacity property.
        /// </summary>
        private DoubleAnimation listViewOpacityAnimation = new DoubleAnimation();

        /// <summary>
        /// Animation for the list view's transform (scale) property.
        /// </summary>
        private DoubleAnimation listViewTransformAnimation = new DoubleAnimation();

        /// <summary>
        /// Animation for the photo explorer's opacity.
        /// </summary>
        private DoubleAnimation photoExplorerAnimation = new DoubleAnimation();

        /// <summary>
        /// Initializes a new instance of the SearchViewControl class.
        /// </summary>
        public SearchViewControl()
        {
            InitializeComponent();
            
            this.CommandBindings.Add(new CommandBinding(switchToListViewCommand, new ExecutedRoutedEventHandler(this.OnSwitchToListViewCommand), new CanExecuteRoutedEventHandler(this.OnSwitchToListViewCanExecute)));
            this.CommandBindings.Add(new CommandBinding(switchToPhotoExplorerCommand, new ExecutedRoutedEventHandler(this.OnSwitchToPhotoExplorerCommand), new CanExecuteRoutedEventHandler(this.OnSwitchToPhotoExplorerCanExecute)));

            this.SetupAnimations();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(this.OnSearchViewControlDataContextChanged);

           // SwitchToPhotoExplorerCommand.Execute(null, this);
        }

        /// <summary>
        /// Represents the search view's current display mode.
        /// </summary>
        private enum SearchViewMode
        {
            /// <summary>
            /// A listing of photos is displayed.
            /// </summary>
            ListView,
            
            /// <summary>
            /// The photo explorer is displayed.
            /// </summary>
            PhotoExplorer
        }

        /// <summary>
        /// Gets the command to switch the search control to list view.
        /// </summary>
        public static RoutedCommand SwitchToListViewCommand
        {
            get { return switchToListViewCommand; }
        }

        /// <summary>
        /// Gets the command to switch the search control to the photo explorer view.
        /// </summary>
        public static RoutedCommand SwitchToPhotoExplorerCommand
        {
            get { return switchToPhotoExplorerCommand; }
        }

        /// <summary>
        /// Moves focus to search results.
        /// </summary>
        public void MoveFocusToSearchResults()
        {
            // Focus on results view
            if (this.SearchListView.Visibility == Visibility.Visible && this.SearchListView.IsEnabled)
            {
                this.SearchListView.Focus();
                this.SearchListView.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
            else if (this.PhotoExplorer.Visibility == Visibility.Visible && this.PhotoExplorer.IsEnabled)
            {
                this.PhotoExplorer.Focus();
                this.PhotoExplorer.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        /// <summary>
        /// Returns special automation peer for this element.
        /// </summary>
        /// <returns>AutomationPeer for this element.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SearchViewControlAutomationPeer(this);
        }

        /// <summary>
        /// Catches the Alt+S keyboard command and switches the display mode.
        /// </summary>
        /// <param name="e">Arguments describing the key event.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
                {
                    if (e.Key == Key.System && e.SystemKey == Key.S)
                    {
                        if (this.displayMode == SearchViewMode.ListView)
                        {
                            switchToPhotoExplorerCommand.Execute(null, this);
                        }
                        else if (this.displayMode == SearchViewMode.PhotoExplorer)
                        {
                            switchToListViewCommand.Execute(null, this);
                        }

                        e.Handled = true;
                    }
                }
            }

            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        /// <summary>
        /// When the data context changes, updates the center node of the photo explorer if it is currently displayed, 
        /// and switches to the explorer if a tag search query beginning with 'explore:' is entered.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnSearchViewControlDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SearchResults searchResults = this.DataContext as SearchResults;

            if (searchResults != null)
            {
                if (searchResults.SearchText.StartsWith("explore:", StringComparison.OrdinalIgnoreCase))
                {
                    //searchResults.Title = searchResults.SearchText.Substring(8);
                    this.OnSwitchToPhotoExplorerCommand(null, null);
                }
                else
                {
                    //searchResults.Title = searchResults.SearchText;
                    if (this.displayMode == SearchViewMode.PhotoExplorer)
                    {
                        this.SetPhotoExplorerCenterNodeToQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Switches to the list view of the search.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnSwitchToListViewCommand(object sender, ExecutedRoutedEventArgs e)
        {
            this.displayMode = SearchViewMode.ListView;

            this.photoExplorerAnimation.To = 0;
            this.PhotoExplorerGrid.BeginAnimation(OpacityProperty, this.photoExplorerAnimation);

            this.SearchListView.Visibility = Visibility.Visible;
            this.listViewOpacityAnimation.To = 1;
            this.SearchListView.BeginAnimation(OpacityProperty, this.listViewOpacityAnimation);
            this.listViewTransformAnimation.To = 1;
            this.ListViewTransform.BeginAnimation(ScaleTransform.ScaleXProperty, this.listViewTransformAnimation);
            this.ListViewTransform.BeginAnimation(ScaleTransform.ScaleYProperty, this.listViewTransformAnimation);
        }

        /// <summary>
        /// Determines whether the list view can be switched to (that is, is not currently being displayed).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnSwitchToListViewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.displayMode == SearchViewMode.PhotoExplorer;
        }

        /// <summary>
        /// Switches to the photo explorer view of the search.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnSwitchToPhotoExplorerCommand(object sender, ExecutedRoutedEventArgs e)
        {
            this.displayMode = SearchViewMode.PhotoExplorer;
            
            this.PhotoExplorerGrid.Visibility = Visibility.Visible;
            this.SetPhotoExplorerCenterNodeToQuery();
            this.photoExplorerAnimation.To = 1;
            this.PhotoExplorerGrid.BeginAnimation(OpacityProperty, this.photoExplorerAnimation);

            this.listViewOpacityAnimation.To = 0;
            this.SearchListView.BeginAnimation(OpacityProperty, this.listViewOpacityAnimation);
            this.listViewTransformAnimation.To = 0.85;
            this.ListViewTransform.BeginAnimation(ScaleTransform.ScaleXProperty, this.listViewTransformAnimation);
            this.ListViewTransform.BeginAnimation(ScaleTransform.ScaleYProperty, this.listViewTransformAnimation);
        }

        /// <summary>
        /// Determines whether the photo explorer can be switched to (that is, is not currently being displayed).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnSwitchToPhotoExplorerCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.displayMode == SearchViewMode.ListView;
        }

        /// <summary>
        /// Sets the center node of the photo explorer to the search query text, less any qualifiers.
        /// </summary>
        private void SetPhotoExplorerCenterNodeToQuery()
        {
            SearchResults searchResults = this.DataContext as SearchResults;

            if (searchResults != null)
            {
                if (searchResults.SearchText.StartsWith("tag:", StringComparison.OrdinalIgnoreCase) || searchResults.SearchText.StartsWith("explore:", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = searchResults.SearchText.Split(':');
                    this.PhotoExplorer.CenterNode = PhotoExplorerTagNode.CreateTagNodeFromTag(parts[1]);
                }
                else
                {
                    this.PhotoExplorer.CenterNode = new PhotoExplorerBaseNode(null, "search: " + searchResults.SearchText);

                    for (int i = 0; i < searchResults.Count && i < PhotoExplorerControl.MaximumDisplayedPhotos; i++)
                    {
                        this.PhotoExplorer.CenterNode.RelatedNodes.Add(PhotoExplorerBaseNode.CreateNodeFromObject(searchResults[i]));
                    }
                }
            }
        }

        /// <summary>
        /// Sets up the animation parameters and handlers.
        /// </summary>
        private void SetupAnimations()
        {
            this.listViewOpacityAnimation.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));
            this.listViewTransformAnimation.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));
            this.photoExplorerAnimation.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));

            this.listViewTransformAnimation.AccelerationRatio = 0.4;
            this.listViewTransformAnimation.DecelerationRatio = 0.2;

            this.listViewOpacityAnimation.Completed += new EventHandler(this.OnListViewAnimationCompleted);
            this.photoExplorerAnimation.Completed += new EventHandler(this.OnPhotoExplorerAnimationCompleted);
        }

        /// <summary>
        /// Collapses the photo explorer once its animation completes if it was fading out.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnPhotoExplorerAnimationCompleted(object sender, EventArgs e)
        {
            if (this.PhotoExplorer.Opacity == 0)
            {
                this.PhotoExplorer.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.PhotoExplorer.Focus();
            }
        }

        /// <summary>
        /// Collapses the list view once its animation completes if it was fading out.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnListViewAnimationCompleted(object sender, EventArgs e)
        {
            if (this.SearchListView.Opacity == 0)
            {
                this.SearchListView.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// AutomationPeer for SearchViewControl for UIAutomation accessibility.
        /// </summary>
        private class SearchViewControlAutomationPeer : FrameworkElementAutomationPeer
        {
            /// <summary>
            /// Initializes a new instance of the SearchViewControlAutomationPeer class.
            /// </summary>
            /// <param name="owner">The parent SearchViewControl.</param>
            public SearchViewControlAutomationPeer(SearchViewControl owner)
                : base(owner)
            {
            }

            /// <summary>
            /// Returns the automation control type.
            /// </summary>
            /// <returns>The AutomationControlType value "Pane".</returns>
            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.Pane;
            }

            /// <summary>
            /// Returns the class name.
            /// </summary>
            /// <returns>The string "SearchViewControl".</returns>
            protected override string GetClassNameCore()
            {
                return "SearchViewControl";
            }
        }
    }
}