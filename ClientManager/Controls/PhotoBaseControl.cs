//-----------------------------------------------------------------------
// <copyright file="PhotoBaseControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Base class for controls that display images.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ClientManager.Data;
    using Contigo;

    /// <summary>
    /// Base class for controls that display images.  Performs an asynchronous postback to get the image
    /// so as not to block the UI thread.
    /// </summary>
    public abstract class PhotoBaseControl : Control
    {
        protected PhotoBaseControl()
        {
        }

        /// <summary>Indicates whether a content update is already pending.</summary>
        private bool _contentUpdatePending;

        /// <summary>Dependency Property backing store for FacebookPhoto.</summary>
        public static readonly DependencyProperty FacebookPhotoProperty = DependencyProperty.Register(
            "FacebookPhoto",
            typeof(FacebookPhoto),
            typeof(PhotoBaseControl),
            new UIPropertyMetadata(
                (d, e) => ((PhotoBaseControl)d).InvalidateContent()));

        /// <summary>Gets or sets the photo to display.</summary>
        public FacebookPhoto FacebookPhoto
        {
            get { return (FacebookPhoto)GetValue(FacebookPhotoProperty); }
            set { SetValue(FacebookPhotoProperty, value); }
        }

        /// <summary>Dependency Property backing store for ImageSource.</summary>
        private static readonly DependencyPropertyKey _ImageSourcePropertyKey = DependencyProperty.RegisterReadOnly(
            "ImageSource",
            typeof(ImageSource), 
            typeof(PhotoBaseControl),
            new UIPropertyMetadata(null));

        public static readonly DependencyProperty ImageSourceProperty = _ImageSourcePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the actual image content to display.
        /// </summary>
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            protected set { SetValue(_ImageSourcePropertyKey, value); }
        } 

        /// <summary>
        /// Gets a value indicating whether an image download is in progress.
        /// </summary>
        public bool ImageDownloadInProgress { get; set; }

        #region Protected Methods
        /// <summary>
        /// Starts the asynchronous update progress.  Needs to be overriden by child classes.
        /// </summary>
        protected virtual void OnUpdateContent()
        {
            FacebookPhoto photo = FacebookPhoto;
            if (photo != null)
            {
                ImageDownloadInProgress = true;
                photo.Image.GetImageAsync(FacebookImageDimensions.Big, OnGetImageSourceCompleted);
            }
            else
            {
                ImageSource = null;
            }
        }

        /// <summary>
        /// Invalidates the content of the control and starts an asynchronous content update.
        /// </summary>
        protected virtual void InvalidateContent()
        {
            if (!this._contentUpdatePending)
            {
                this.ImageSource = null;
                if (this.ImageDownloadInProgress)
                {
                    //ServiceProvider.AsyncWebGetter.CancelAsync(this);
                }

                this._contentUpdatePending = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(this.UpdateContent), null);
            }
        }

        /// <summary>
        /// Sets the ImageSource of the control as soon as the asynchronous get is completed.
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected virtual void OnGetImageSourceCompleted(object sender, GetImageSourceCompletedEventArgs e)
        {
            if (this.FacebookPhoto != null)
            {
                if (e.UserState == this.FacebookPhoto.Image)
                {
                    ImageDownloadInProgress = false;
                    if (e.Error == null && !e.Cancelled)
                    {
                        this.ImageSource = e.ImageSource;
                    }
                    else
                    {
                        this.ImageSource = null;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Updates the control content.
        /// </summary>
        /// <param name="arg">Callback argument.</param>
        /// <returns>Always null.</returns>
        private object UpdateContent(object arg)
        {
            this._contentUpdatePending = false;
            this.OnUpdateContent();
            return null;
        }
    }
}
