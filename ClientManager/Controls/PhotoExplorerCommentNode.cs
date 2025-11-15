namespace ClientManager.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using Contigo;

    public class PhotoExplorerCommentNode : PhotoExplorerBaseNode
    {
        public PhotoExplorerCommentNode(ActivityComment comment)
            : base(comment, string.Empty)
        {
            this.Comment = comment;
        }

        public ActivityComment Comment { get; private set; }

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
