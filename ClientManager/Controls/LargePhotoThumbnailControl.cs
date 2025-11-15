//-----------------------------------------------------------------------
// <copyright file="LargePhotoThumbnailControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control used to display a photo thumbnail in an album using the full-size image.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System.Windows.Media;
    using ClientManager.Data;
    using Contigo;
    using System;

    /// <summary>
    /// Control used to display a photo thumbnail in an album.
    /// </summary>
    public class LargePhotoThumbnailControl : PhotoBaseControl
    {
        /// <summary>
        /// Updates the content of the control to contain the image at Photo.ImageUri.
        /// </summary>
        protected override void OnUpdateContent()
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
    }
}

