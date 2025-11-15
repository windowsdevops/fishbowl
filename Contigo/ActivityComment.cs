
namespace Contigo
{
    using System;
    using System.ComponentModel;
    using Standard;

    public class ActivityComment : INotifyPropertyChanged, IFacebookObject, IMergeable<ActivityComment>, IComparable<ActivityComment>
    {
        internal enum Type : byte
        {
            Unknown,
            ActivityPost,
            Photo
        }

        private FacebookContact _fromUser;
        private SmallString _fromUserId;
        private SmallString _commentId;
        private SmallString _text;
        private DateTime _timestamp;
        private bool _isFromUserUpdateInProgress;

        internal ActivityComment(FacebookService service)
        {
            Verify.IsNotNull(service, "service");
            SourceService = service;
        }

        internal global::Contigo.ActivityComment.Type CommentType { get; set; }

        internal string FromUserId
        {
            get { return _fromUserId.GetString(); }
            set
            {
                var newValue = new SmallString(value);
                if (newValue != _fromUserId)
                {
                    _fromUserId = new SmallString(value);
                    _UpdateFromUser();
                }
            }
        }

        public DateTime Time
        {
            get { return _timestamp; }
            internal set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    _NotifyPropertyChanged("Time");
                }
            }
        }

        public string Text
        {
            get { return _text.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (_text != newValue)
                {
                    _text = new SmallString(value);
                    _NotifyPropertyChanged("Text");
                }
            }
        }

        internal string CommentId
        {
            get { return _commentId.GetString(); }
            set { _commentId = new SmallString(value); }
        }

        internal ActivityPost Post { get; set; }

        public FacebookContact FromUser
        {
            get
            {
                if (_fromUser == null && !_isFromUserUpdateInProgress)
                {
                    _UpdateFromUser();
                }

                return _fromUser;
            }
        }

        private void _UpdateFromUser()
        {
            _isFromUserUpdateInProgress = true;
            SourceService.GetUserAsync(FromUserId, _OnGetFromUserCompleted);
        }

        private void _OnGetFromUserCompleted(object sender, AsyncCompletedEventArgs args)
        {
            _fromUser = (FacebookContact)args.UserState;
            _NotifyPropertyChanged("FromUser");
            _isFromUserUpdateInProgress = false;
        }

        public bool CanRemove
        {
            get
            {
                return IsMine && _commentId != default(SmallString) && CommentType == Type.ActivityPost;
            }
        }

        public bool IsMine
        {
            get
            {
                return FromUserId == SourceService.UserId;
            }
        }

        private void _NotifyPropertyChanged(string propertyName)
        {
            Assert.IsNeitherNullNorEmpty(propertyName);

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IFacebookObject Members

        FacebookService IFacebookObject.SourceService { get; set; }

        private FacebookService SourceService
        {
            get { return ((IFacebookObject)this).SourceService; }
            set { ((IFacebookObject)this).SourceService = value; }
        }

        #endregion

        #region IMergeable<ActivityComment> Members

        string IMergeable<ActivityComment>.FKID { get { return CommentId; } }

        void IMergeable<ActivityComment>.Merge(ActivityComment other)
        {}

        #endregion

        #region IEquatable<ActivityComment> Members

        public bool Equals(ActivityComment other)
        {
            if (other == null)
            {
                return false;
            }

            return this.CommentId == other.CommentId;
        }

        #endregion

        #region IComparable<ActivityComment> Members

        public int CompareTo(ActivityComment other)
        {
            // sort oldest first.  This is opposite many other Facebook types.
            if (other == null)
            {
                return -1;
            }

            return this.Time.CompareTo(other.Time);
        }

        #endregion
    }
}
