namespace Contigo
{
    using Standard;

    public class ActivityCommentCollection : FacebookCollection<ActivityComment>
    {
        internal ActivityCommentCollection(MergeableCollection<ActivityComment> rawCollection, FacebookService service)
            : base(rawCollection, service)
        { }
    }
}
