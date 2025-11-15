namespace ClientManager.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using Contigo;

    public class PhotoExplorerPostNode : PhotoExplorerBaseNode
    {
        public PhotoExplorerPostNode(ActivityPost post) 
            : base(post, string.Empty)
        {
            this.Post = post;
        }

        public ActivityPost Post { get; private set; }

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
