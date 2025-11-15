namespace ClientManager.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using Contigo;

    public class PhotoExplorerAlbumNode : PhotoExplorerBaseNode
    {
        public PhotoExplorerAlbumNode(FacebookPhotoAlbum album)
            : base(album, string.Empty)
        {
            this.Album = album;
        }

        public FacebookPhotoAlbum Album { get; private set; }

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
