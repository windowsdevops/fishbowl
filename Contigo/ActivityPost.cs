namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Standard;

    public class ActivityPost : IFacebookObject, INotifyPropertyChanged, IMergeable<ActivityPost>, IComparable<ActivityPost>
    {
        private FacebookContact _actor;
        private FacebookContact _target;
        private bool _isActorUpdateInProgress;
        private bool _isTargetUpdateInProgress;
        private bool _gettingMoreComments;
        private ActivityCommentCollection _comments;
        private FacebookContactCollection _likers;
        private SmallString _message;
        private SmallString _actorUserId;
        private SmallString _targetUserId;
        private SmallString _postId;
        private SmallUri _likeUri;
        private MergeableCollection<FacebookContact> _mergeableLikers;
        private ActivityPostAttachment _attachment;
        private DateTime _created;
        private DateTime _updated;
        private bool _canLike;
        private bool _hasLiked;
        private int _likedCount;
        private bool _canComment;
        private bool _canRemoveComments;
        private int _commentCount;

        // When merging, make sure that we don't drop comments if all were requested.
        private bool _hasGottenMoreComments;


        // We don't actually populate the data in the constructor.  Instead letting the service do it.
        internal ActivityPost(FacebookService service)
        {
            Verify.IsNotNull(service, "service");
            SourceService = service;
        }

        public ActivityPostAttachment Attachment
        {
            get { return _attachment; }
            internal set
            {
                if (_attachment != value)
                {
                    _attachment = value;
                    _NotifyPropertyChanged("Attachment");
                }
            }
        }

        public DateTime Created
        {
            get { return _created; }
            internal set
            {
                if (_created != value)
                {
                    _created = value;
                    _NotifyPropertyChanged("Created");
                }
            }
        }

        public DateTime Updated
        {
            get { return _updated; }
            internal set
            {
                if (_updated != value)
                {
                    _updated = value;
                    _NotifyPropertyChanged("Updated");
                }
            }
        }
        
        public string Message
        {
            get { return _message.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (_message != newValue)
                {
                    _message = newValue;
                    _NotifyPropertyChanged("Message");
                }
            }
        }

        internal string ActorUserId
        {
            get { return _actorUserId.GetString(); }
            set { _actorUserId = new SmallString(value); }
        }
        
        internal string TargetUserId
        {
            get { return _targetUserId.GetString(); }
            set { _targetUserId = new SmallString(value); }
        }

        public bool CanLike
        {
            get { return _canLike; }
            internal set
            {
                if (_canLike != value)
                {
                    _canLike = value;
                    _NotifyPropertyChanged("CanLike");
                }
            }
        }

        public bool HasLiked
        {
            get { return _hasLiked; }
            internal set
            {
                if (_hasLiked != value)
                {
                    _hasLiked = value;
                    _NotifyPropertyChanged("HasLiked");
                }
            }
        }

        public int LikedCount
        {
            get { return _likedCount; }
            internal set
            {
                if (value < 0)
                {
                    value = 0;
                }

                if (_likedCount != value)
                {
                    _likedCount = value;
                    _NotifyPropertyChanged("LikedCount");
                }
            }
        }

        internal MergeableCollection<string> RawPeopleWhoLikeThisIds { get; set; }

        public FacebookContactCollection PeopleWhoLikeThis
        {
            get
            {
                if (RawPeopleWhoLikeThisIds == null)
                {
                    return null;
                }

                if (_likers == null)
                {
                    _mergeableLikers = new MergeableCollection<FacebookContact>();
                    _likers = new FacebookContactCollection(_mergeableLikers, SourceService, false);
                    _likers.CollectionChanged += (sender, e) => _NotifyPropertyChanged("PeopleWhoLikeThis");
                    foreach (string uid in RawPeopleWhoLikeThisIds)
                    {
                        SourceService.GetUserAsync(uid, _OnGetUserCompleted);
                    }
                }
                return _likers;
            }
        }

        private void _OnGetUserCompleted(object sender, AsyncCompletedEventArgs args)
        {
            if (args.Error != null || args.Cancelled)
            {
                return;
            }

            var contact = (FacebookContact)args.UserState;
            Assert.IsNotNull(contact);
            _mergeableLikers.Add(contact);
        }

        public bool CanComment
        {
            get { return _canComment; }
            internal set
            {
                if (_canComment != value)
                {
                    _canComment = value;
                    _NotifyPropertyChanged("CanComment");
                }
            }
        }

        public bool CanRemoveComments
        {
            get { return _canRemoveComments; }
            internal set
            {
                if (_canRemoveComments != value)
                {
                    _canRemoveComments = value;
                    _NotifyPropertyChanged("CanRemoveComments");
                }
            }
        }

        public int CommentCount
        {
            get { return _commentCount; }
            internal set
            {
                if (_commentCount != value)
                {
                    _commentCount = value;
                    _NotifyPropertyChanged("CommentCount");
                }
            }
        }

        public bool HasMoreComments { get { return CommentCount != Comments.Count; } }

        internal MergeableCollection<ActivityComment> RawComments { get; set; }

        public ActivityCommentCollection Comments
        {
            get
            {
                if (_comments == null)
                {
                    if (this.RawComments == null)
                    {
                        return null;
                    }

                    _comments = new ActivityCommentCollection(RawComments, SourceService);
                }

                return _comments;
            }
        }

        internal string PostId
        {
            get { return _postId.GetString(); }
            set { _postId = new SmallString(value); }
        }

        public Uri LikeUrl
        {
            get { return _likeUri.GetUri(); }
            internal set
            {
                var newValue = new SmallUri(value);
                if (_likeUri != newValue)
                {
                    _likeUri = newValue;
                    _NotifyPropertyChanged("LikeUri");
                }
            }
        }

        public FacebookContact Actor
        {
            get
            {
                if (_actor == null && !_isActorUpdateInProgress)
                {
                    _isActorUpdateInProgress = true;
                    SourceService.GetUserAsync(this.ActorUserId, _OnGetActorCompleted);
                                    }

                return _actor;
            }
        }

        public FacebookContact Target
        {
            get
            {
                if (this.TargetUserId == string.Empty)
                {
                    return null;
                }

                if (_target == null && !_isTargetUpdateInProgress)
                {
                    _isTargetUpdateInProgress = true;
                    SourceService.GetUserAsync(this.TargetUserId, _OnGetTargetCompleted);
                }

                return _target;
            }
        }

        internal void GetMoreComments()
        {
            _hasGottenMoreComments = true;
            if (HasMoreComments && !_gettingMoreComments)
            {
                _gettingMoreComments = true;
                SourceService.GetCommentsForPostAsync(this, _OnGetCommentsCompleted);
            }
        }

        private void _OnGetCommentsCompleted(object sender, AsyncCompletedEventArgs args)
        {
            var comments = (IEnumerable<ActivityComment>)args.UserState;
            RawComments.Merge(comments, false);
            CommentCount = RawComments.Count;
            _NotifyPropertyChanged("CommentCount");
            _NotifyPropertyChanged("HasMoreComments");
            _gettingMoreComments = false;
        }

        private void _OnGetActorCompleted(object sender, AsyncCompletedEventArgs args)
        {
            _actor = (FacebookContact)args.UserState;
            _NotifyPropertyChanged("Actor");
            _isActorUpdateInProgress = false;
        }

        private void _OnGetTargetCompleted(object sender, AsyncCompletedEventArgs args)
        {
            _target = (FacebookContact)args.UserState;
            _NotifyPropertyChanged("Target");
            _isTargetUpdateInProgress = false;
        }

        private void _NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region IFacebookObject Members

        FacebookService IFacebookObject.SourceService { get; set; }

        private FacebookService SourceService
        {
            get { return ((IFacebookObject)this).SourceService; }
            set { ((IFacebookObject)this).SourceService = value; }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public override string ToString()
        {
            return this.Message + " @" + Created + ", Updated @" + Updated;
        }

        #region IMergeable<ActivityPost> Members

        string IMergeable<ActivityPost>.FKID
        {
            get { return PostId; }
        }

        void IMergeable<ActivityPost>.Merge(ActivityPost other)
        {
            Verify.IsNotNull(other, "other");
            Verify.AreEqual(PostId, other.PostId, "other", "Can't merge two ActivityPosts with different Ids.");

            ActorUserId = other.ActorUserId;
            Attachment = other.Attachment;
            CanComment = other.CanComment;
            CanLike = other.CanLike;
            CanRemoveComments = other.CanRemoveComments;
            CommentCount = other.CommentCount;
            Created = other.Created;
            HasLiked = other.HasLiked;
            LikedCount = other.LikedCount;
            LikeUrl = other.LikeUrl;
            Message = other.Message;
            RawComments.Merge(other.RawComments, false);
            if (_hasGottenMoreComments)
            {
                GetMoreComments();
            }
            else
            {
                _NotifyPropertyChanged("HasMoreComments");
            }
            // TODO: This isn't going to quite work...
            RawPeopleWhoLikeThisIds.Merge(other.RawPeopleWhoLikeThisIds, false);
            TargetUserId = other.TargetUserId;
            Updated = other.Updated;
        }

        #endregion

        #region IEquatable<ActivityPost> Members

        public bool Equals(ActivityPost other)
        {
            if (other == null)
            {
                return false;
            }

            return other.PostId == this.PostId;
        }

        #endregion

        #region IComparable<ActivityPost> Members

        public int CompareTo(ActivityPost other)
        {
            if (other == null)
            {
                return 1;
            }

            // Sort ActivityPosts newest first.
            return -Created.CompareTo(other.Created);
        }

        #endregion
    }
}
