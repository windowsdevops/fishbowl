//-----------------------------------------------------------------------
// <copyright file="PhotoExplorerTagNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Photo Explorer node containing a tag.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using ClientManager.Data;
    using ClientManager.View;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Contigo;

    /// <summary>
    /// A Photo Explorer node containing a tag.
    /// </summary>
    public class PhotoExplorerTagNode : PhotoExplorerBaseNode
    {
        #region Fields
        /// <summary>
        /// The Id of the tag this node represents.
        /// </summary>
        private string _tag;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the PhotoExplorerTagNode class.
        /// </summary>
        /// <param name="tagId">The short integer identifier of the tag this node represents.</param>
        public PhotoExplorerTagNode(string tag)
            : base(null, tag)
        {
            _tag = tag;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets the related photo nodes for this tag.
        /// </summary>
        public override ObservableCollection<PhotoExplorerBaseNode> RelatedNodes
        {
            get
            {
                if (base.RelatedNodes.Count == 0)
                {
                    SearchResults searchResults = ServiceProvider.ViewManager.DoSearch(this.Name);
                    for (int i = 0; i < searchResults.Count && i < PhotoExplorerControl.MaximumDisplayedPhotos; i++)
                    {
                        base.RelatedNodes.Add(PhotoExplorerBaseNode.CreateNodeFromObject(searchResults[i]));
                    }
                }

                return base.RelatedNodes;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates a new PhotoExplorerTagNode given a tag's text.
        /// </summary>
        /// <param name="tag">The tag text.</param>
        /// <returns>A new PhotoExplorerTagNode.</returns>
        public static PhotoExplorerTagNode CreateTagNodeFromTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return null;
            }

            return new PhotoExplorerTagNode(tag);
        }
        #endregion
    }
}
