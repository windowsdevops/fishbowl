//-----------------------------------------------------------------------
// <copyright file="PhotoAlbumControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control that displays album UI.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    using ClientManager;
    using ClientManager.Controls;
    using ClientManager.Data;
    using Contigo;
    using System.Globalization;
    using System.IO;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using FacebookClient.Controls;
    using Standard;

    /// <summary>
    /// Control that displays album UI.
    /// </summary>
    public class PhotoAlbumControl : SizeTemplateControl
    {
        public static readonly DependencyProperty PhotoAlbumProperty = DependencyProperty.Register(
            "PhotoAlbum", 
            typeof(FacebookPhotoAlbum), 
            typeof(PhotoAlbumControl),
            new FrameworkPropertyMetadata(null));

        public FacebookPhotoAlbum PhotoAlbum
        {
            get { return (FacebookPhotoAlbum)GetValue(PhotoAlbumProperty); }
            set { SetValue(PhotoAlbumProperty, value); }
        }
        
        static PhotoAlbumControl()
        {
            SaveAlbumCommand = new RoutedCommand("SaveAlbum", typeof(PhotoAlbumControl));
        }

        public PhotoAlbumControl()
        {
            this.CommandBindings.Add(new CommandBinding(SaveAlbumCommand, (sender, e) => ((PhotoAlbumControl)sender)._SaveAlbum()));
        }

        public static RoutedCommand SaveAlbumCommand { get; private set; }

        #region Protected Methods

        /// <summary>
        /// Focuses the control when initialized and sets up handlers so that the control is refocused when the album changes.
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            this.Focus();
            this.Loaded += new RoutedEventHandler(this.OnPhotoAlbumControlLoaded);
            this.Unloaded += new RoutedEventHandler(this.OnPhotoAlbumControlUnloaded);   
        }

        /// <summary>
        /// PhotoAlbumControl has special handling for directional keys since the behavior depends on the element currently in focus.
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                {
                    switch (e.Key)
                    {
                        //case Key.Enter:
                        //    this.OnEnterKeyPress(e);
                        //    break;
                        case Key.Escape:
                            this.OnEscapeKeyPress(e);
                            break;
                        default:
                            break;
                    }
                }
            }

            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// On Enter key, enter tab mode.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        private void OnEnterKeyPress(KeyEventArgs e)
        {
            // Move focus only if there is keyboard focus on this control, but not within it (otherwise handled as tab or traversal requests)
            // Also, ensure that no element has mouse capture, focus should not move while the mouse is captured
            if (this.IsKeyboardFocused && Mouse.Captured == null)
            {
                this.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Saves every photo in the currently displayed album to a user-provided location.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private void _SaveAlbum()
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.Description = "Choose where to save the album.";
            folderDialog.ShowNewFolderButton = true;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PhotoAlbum.SaveToFolder(folderDialog.SelectedPath);
            }
        }


        /// <summary>
        /// If keyboard focus is within, move focus to the main control to get out of directional navigation mode.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        private void OnEscapeKeyPress(KeyEventArgs e)
        {
            // Move focus only if there is keyboard focus within 
            // Also, ensure that no element has mouse  capture, focus should not move while the mouse is captured
            if (!IsKeyboardFocused && IsKeyboardFocusWithin && Mouse.Captured == null)
            {
                this.Focus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Establishes a handler for ViewManager's property changed so that it can refocus the control when the photo album changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnPhotoAlbumControlLoaded(object sender, RoutedEventArgs e)
        {
            ServiceProvider.ViewManager.PropertyChanged += new PropertyChangedEventHandler(this.OnViewManagerPropertyChanged);
        }

        /// <summary>
        /// Focuses the PhotoAlbumControl when the current photo album changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the event.</param>
        private void OnViewManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActivePhotoAlbum")
            {
                this.Focus();
            }
        }

        /// <summary>
        /// Removes the handler from ViewManager's property changed event when the control is unloaded.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private void OnPhotoAlbumControlUnloaded(object sender, RoutedEventArgs e)
        {
            ServiceProvider.ViewManager.PropertyChanged -= new PropertyChangedEventHandler(this.OnViewManagerPropertyChanged);
        }
        #endregion
    }
}