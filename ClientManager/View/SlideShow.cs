//-----------------------------------------------------------------------
// <copyright file="PhotoSlideShow.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Represents a single image, along with all of its associated attributes.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using ClientManager.View;
    using Standard;
    using Contigo;
    using System.Windows.Threading;
    
    /// <summary>
    /// Represents a slideshow of images.
    /// </summary>
    public class SlideShow
    {
        public SlideShow(PhotoAlbumCollectionNavigator albumCollectionNavigator)
        {
            Verify.IsNotNull(albumCollectionNavigator, "albumCollectionNavigator");
            if (albumCollectionNavigator.FirstChild == null)
            {
                throw new ArgumentException("Cannot create a slideshow with no photos.", "albumCollectionNavigator");
            }
            
            Album = new AggregatePhotoAlbumNavigator(albumCollectionNavigator);
            CurrentPhoto = Album.FirstChild;
        }

        /// <summary>
        /// Initializes a new instance of the PhotoSlideShow class.
        /// </summary>
        /// <param name="albumNavigator">Navigator for PhotoAlbum in the slide show.</param>
        public SlideShow(PhotoAlbumNavigator albumNavigator)
        {
            Verify.IsNotNull(albumNavigator, "albumNavigator");
            if (albumNavigator.FirstChild == null)
            {
                throw new ArgumentException("Cannot create a slideshow with no photos.", "albumCollectionNavigator");
            }

            Album = albumNavigator;
            CurrentPhoto = Album.FirstChild;
        }

        /// <summary>
        /// Initializes a new instance of the PhotoSlideShow class.
        /// </summary>
        /// <param name="photoNavigator">Navigator for a Photo object, slide show is created for the album containing this Photo.</param>
        public SlideShow(PhotoNavigator photoNavigator)
            : this(photoNavigator.Parent as PhotoAlbumNavigator)
        {
            CurrentPhoto = photoNavigator;
        }

        public Navigator CurrentPhoto { get; private set; }
        public Navigator Album { get; private set; }

        public Navigator NextPhoto
        {
            get
            {
                Navigator next = CurrentPhoto.NextSibling;
                if (next == null)
                {
                    next = CurrentPhoto.Parent.FirstChild;
                }
                return next;
            }
        }

        public Navigator PreviousPhoto
        {
            get
            {
                Navigator prior = CurrentPhoto.PreviousSibling;
                if (prior == null)
                {
                    prior = CurrentPhoto.Parent.LastChild;
                }
                return prior;
            }
        }


        /// <summary>
        /// Advance to next photo.
        /// </summary>
        public void MoveNext()
        {
            CurrentPhoto = NextPhoto;
        }

        public void MovePrevious()
        {
            CurrentPhoto = PreviousPhoto;
        }
    }

    /// <summary>
    /// PhotoSlideShowNavigator wraps a PhotoSlideShow object.
    /// </summary>
    public class SlideShowNavigator : Navigator
    {
        /// <summary>
        /// Initializes a new instance of the PhotoSlideShowNavigator class.
        /// </summary>
        /// <param name="photoSlideShow">photoSlideShow that is navigator's content.</param>
        public SlideShowNavigator(SlideShow slideShow)
            : base(slideShow, "[slideshow]", null)
        {}

        public override bool IncludeInJournal { get { return false; } }
    }
}
