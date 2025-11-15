
namespace Contigo
{
    using System;
    using Standard;

    public enum ActivityPostAttachmentType
    {
        None,

        /// <summary>An attachment exists, but does not contain rich media.</summary>
        Simple,

        Photos,
        Links,
        Video,
    }

    public class ActivityPostAttachment
    {
        private SmallString _name;
        private SmallString _caption;
        private SmallString _description;
        private SmallString _properties;
        private SmallUri _videoSource;
        private SmallUri _link;

        public ActivityPostAttachment(ActivityPost post)
        {
            Verify.IsNotNull(post, "post");
            Post = post;
        }

        public ActivityPostAttachmentType Type { get; internal set; }

        public string Name
        {
            get { return _name.GetString(); }
            internal set { _name = new SmallString(value); }
        }

        public Uri Link
        {
            get { return _link.GetUri(); }
            internal set { _link = new SmallUri(value); }
        }

        public string Caption
        {
            get { return _caption.GetString(); }
            internal set { _caption = new SmallString(value); }
        }

        public string Description
        {
            get { return _description.GetString(); }
            internal set { _description = new SmallString(value); }
        }

        public string Properties
        {
            get { return _properties.GetString(); }
            internal set { _properties = new SmallString(value); }
        }

        public ActivityPost Post { get; private set; }

        public FacebookCollection<FacebookPhoto> Photos { get; internal set; }

        public FacebookCollection<FacebookImageLink> Links { get; internal set; }

        public FacebookImage VideoPreviewImage { get; internal set; }

        public Uri VideoSource
        {
            get { return _videoSource.GetUri(); }
            set { _videoSource = new SmallUri(value); }
        }

        public FacebookImage Icon { get; internal set; }
    }
}
