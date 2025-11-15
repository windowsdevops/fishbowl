namespace Contigo
{
    using Standard;
    using System.Collections.Generic;

    // Currently this is always a static list.
    public class FacebookPhotoTagCollection : FacebookCollection<FacebookPhotoTag>
    {
        internal static FacebookPhotoTagCollection CreateStaticCollection(IEnumerable<FacebookPhotoTag> tags)
        {
            return new FacebookPhotoTagCollection(tags);
        }

        private FacebookPhotoTagCollection(IEnumerable<FacebookPhotoTag> tags)
            : base(tags ?? new FacebookPhotoTag[0])
        {}

        internal FacebookPhotoTagCollection(MergeableCollection<FacebookPhotoTag> tags, FacebookService service)
            : base(tags, service)
        {}
    }
}
