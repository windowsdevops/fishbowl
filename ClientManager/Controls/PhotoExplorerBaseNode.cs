//-----------------------------------------------------------------------
// <copyright file="PhotoExplorerBaseNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The base class from which other Photo Explorer Nodes derive.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System.Collections.ObjectModel;
    using ClientManager.View;
    using Contigo;
    using System;

    /// <summary>
    /// The base type for all nodes displayed by the photo explorer.
    /// </summary>
    public class PhotoExplorerBaseNode
    {
        #region Fields
        /// <summary>
        /// Nodes the are related in some way to the current node.
        /// </summary>
        private ObservableCollection<PhotoExplorerBaseNode> relatedNodes = new ObservableCollection<PhotoExplorerBaseNode>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the PhotoExplorerBaseNode class.
        /// </summary>
        /// <param name="name">The name to display for the node.</param>
        public PhotoExplorerBaseNode(object content, string name)
        {
            this.Content = content;
            this.Name = name;
        } 

        #region Properties
        /// <summary>
        /// Gets the collection of nodes that are related in some way to the current node.
        /// </summary>
        public virtual ObservableCollection<PhotoExplorerBaseNode> RelatedNodes
        {
            get { return this.relatedNodes; }
        }

        /// <summary>
        /// Gets the name of the current node.
        /// </summary>
        public string Name { get; private set; }

        public object Content { get; private set; }

        #endregion

        protected void SetRelatedNodes()
        {
            this.relatedNodes.Clear();

            object[] objects = ServiceProvider.FacebookService.SearchIndex.GetRelevantFacebookObjects(this.Content);
            int count = 0;

            for (int i = 0; i < objects.Length && i < PhotoExplorerControl.MaximumDisplayedPhotos; i++, count++)
            {
                this.relatedNodes.Add(CreateNodeFromObject(objects[i]));
            }

            string[] words = ServiceProvider.FacebookService.SearchIndex.PruneCommonWords(ServiceProvider.FacebookService.SearchIndex.GetWordsFromObject(this.Content));
            for (int i = 0; i < words.Length && count < PhotoExplorerControl.MaximumDisplayedPhotos; i++, count++)
            {
                this.relatedNodes.Add(new PhotoExplorerTagNode(words[i]));
            }
        }

        public static PhotoExplorerBaseNode CreateNodeFromObject(object data)
        {
            if (data is FacebookPhoto)
            {
                return new PhotoExplorerPhotoNode((FacebookPhoto)data);
            }
            else if (data is FacebookPhotoAlbum)
            {
                return new PhotoExplorerAlbumNode((FacebookPhotoAlbum)data);
            }
            else if (data is FacebookContact)
            {
                return new PhotoExplorerContactNode((FacebookContact)data);
            }
            else if (data is ActivityPost)
            {
                return new PhotoExplorerPostNode((ActivityPost)data);
            }
            else if (data is ActivityComment)
            {
                return new PhotoExplorerCommentNode((ActivityComment)data);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
