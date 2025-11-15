//-----------------------------------------------------------------------
// <copyright file="PhotoExplorerPhotoNode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Photo Explorer node containing a photo.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using ClientManager.Data;
    using ClientManager.View;
    using Contigo;

    /// <summary>
    /// A Photo Explorer node containing a photo.
    /// </summary>
    public class PhotoExplorerPhotoNode : PhotoExplorerBaseNode
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the PhotoExplorerPhotoNode class.
        /// </summary>
        /// <param name="photoNavigator">The photo navigator to the photo this node represents.</param>
        public PhotoExplorerPhotoNode(FacebookPhoto photo)
            : base(photo, string.Empty)
        {
            this.Photo = photo;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the photo object that this node references.
        /// </summary>
        public FacebookPhoto Photo { get; private set; }

        #endregion

        /// <summary>
        /// Gets the related tag nodes for this photo.
        /// </summary>
        public override ObservableCollection<PhotoExplorerBaseNode> RelatedNodes
        {
            get
            {
                if (base.RelatedNodes.Count == 0)
                {
                    base.SetRelatedNodes();
                }

                return base.RelatedNodes;
            }
        }
    }
}
