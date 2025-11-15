namespace ClientManager.Controls
{
    using System;
using ClientManager.View;
using Contigo;
    using System.Collections.ObjectModel;

    public class PhotoExplorerContactNode : PhotoExplorerBaseNode
    {
        public PhotoExplorerContactNode(FacebookContact contact)
            : base(contact, contact.Name)
        {
            Contact = contact;
        }

        public FacebookContact Contact { get; private set; }

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
