
namespace Contigo
{
    using System;
    using System.ComponentModel;
    using Standard;

    public class Notification : IFacebookObject, INotifyPropertyChanged, IMergeable<Notification>
    {
        internal Notification(FacebookService service)
        {
            Verify.IsNotNull(service, "service");
            SourceService = service;
        }

        private SmallString _title;
        private SmallString _description;
        private SmallUri _href;
        private SmallString _notificationId;
        private SmallString _senderId;
        private SmallString _recipientId;
        // private int _appId;

        private SmallString _titleText;
        private SmallString _descriptionText;
        private DateTime _created;
        private DateTime _updated;
        private bool _hidden;
        private bool _unread;
        private FacebookContact _sender;
        private bool _isSenderUpdateInProgress;

        internal string NotificationId
        {
            get { return _notificationId.GetString(); }
            set
            {
                Assert.IsDefault(_notificationId);
                _notificationId = new SmallString(value);
            }
        }

        internal string SenderId
        {
            get { return _senderId.GetString(); }
            set
            {
                SmallString newValue = new SmallString(value);
                if (_senderId != newValue)
                {
                    _senderId = newValue;
                    _NotifyPropertyChanged("SenderId");
                }
            }
        }

        public FacebookContact Sender
        {
            get
            {
                if (_sender == null && !_isSenderUpdateInProgress)
                {
                    _isSenderUpdateInProgress = true;
                    SourceService.GetUserAsync(SenderId, _OnGetSenderCompleted);
                }

                return _sender;
            }
            internal set { _sender = value; }
        }

        private void _OnGetSenderCompleted(object sender, AsyncCompletedEventArgs args)
        {
            _sender = (FacebookContact)args.UserState;
            _NotifyPropertyChanged("Sender");
            _isSenderUpdateInProgress = false;
        }

        internal string RecipientId
        {
            get { return _recipientId.GetString(); }
            set
            {
                SmallString newValue = new SmallString(value);
                if (_recipientId != newValue)
                {
                    _recipientId = newValue;
                    _NotifyPropertyChanged("RecipientId");
                }
            }
        }

        public string TitleText
        {
            get
            {
                if (_titleText == default(SmallString))
                {
                    return _title.GetString();
                }
                return _titleText.GetString();
            }
            set
            {
                SmallString newValue = new SmallString(value);
                if (_titleText != newValue)
                {
                    _titleText = newValue;
                    _NotifyPropertyChanged("TitleText");
                    if (_titleText == default(SmallString) || _title == default(SmallString))
                    {
                        // It didn't necessarily, but it's worth checking.
                        _NotifyPropertyChanged("Title");
                    }
                }
            }
        }

        public string DescriptionText 
        {
            get 
            {
                if (_descriptionText == default(SmallString))
                {
                    return _description.GetString();
                }
                return _descriptionText.GetString(); 
            }
            set
            {
                SmallString newValue = new SmallString(value);
                if (_descriptionText != newValue)
                {
                    _descriptionText = newValue;
                    _NotifyPropertyChanged("DescriptionText");
                    if (_descriptionText == default(SmallString) || _title == default(SmallString))
                    {
                        // It didn't necessarily, but it's worth checking.
                        _NotifyPropertyChanged("Description");
                    }
                }
            }
        }

        public DateTime Created
        {
            get { return _created; }
            internal set
            {
                if (value != _created)
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
                if (value != _updated)
                {
                    _updated = value;
                    _NotifyPropertyChanged("Updated");
                }
            }
        }

        public bool IsUnread
        {
            get { return _unread; }
            internal set
            {
                if (value != _unread)
                {
                    _unread = value;
                    _NotifyPropertyChanged("IsUnread");
                }
            }
        }

        public bool IsHidden
        {
            get { return _hidden; }
            internal set
            {
                if (value != _hidden)
                {
                    _hidden = value;
                    _NotifyPropertyChanged("IsHidden");
                }
            }
        }

        public string Title
        {
            get
            {
                if (_title == default(SmallString))
                {
                    return _titleText.GetString();
                }
                return _title.GetString();
            }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _title)
                {
                    _title = newValue;
                    _NotifyPropertyChanged("Title");
                    if (_titleText == default(SmallString) || _title == default(SmallString))
                    {
                        // It didn't necessarily, but it's worth checking.
                        _NotifyPropertyChanged("TitleText");
                    }

                }
            }
        }

        public string Description
        {
            get
            {
                if (_description == default(SmallString))
                {
                    return _descriptionText.GetString();
                }
                return _description.GetString();
            }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _description)
                {
                    _description = newValue;
                    _NotifyPropertyChanged("Description");
                    if (_descriptionText == default(SmallString) || _title == default(SmallString))
                    {
                        // It didn't necessarily, but it's worth checking.
                        _NotifyPropertyChanged("DescriptionText");
                    }
                }
            }
        }

        public Uri Link
        {
            get { return _href.GetUri(); }
            internal set
            {
                var newValue = new SmallUri(value);
                if (newValue != _href)
                {
                    _href = newValue;
                    _NotifyPropertyChanged("Link");
                }
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

        #region Object Overrides

        public override bool Equals(object obj)
        {
            return Equals(obj as Notification);
        }

        public override int GetHashCode()
        {
            return NotificationId.GetHashCode();
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(TitleText))
            {
                return "(Empty Notification)"; 
            }

            if (string.IsNullOrEmpty(DescriptionText))
            {
                return TitleText;
            }
            return TitleText + " - " + DescriptionText;
        }

        #endregion

        #region IMergeable<Notification> Members

        string IMergeable<Notification>.FKID
        {
            get
            {
                Assert.IsNeitherNullNorEmpty(NotificationId);
                return NotificationId.ToString(); 
            }
        }

        void IMergeable<Notification>.Merge(Notification other)
        {
            Verify.IsNotNull(other, "other");
            Verify.AreEqual(NotificationId, other.NotificationId, "other", "This can only be merged with a Notification with the same Id.");

            Created = other.Created;
            Description = other.Description;
            DescriptionText = other.DescriptionText;
            IsHidden = other.IsHidden;
            IsUnread = other.IsUnread;
            Link = other.Link;
            RecipientId = other.RecipientId;
            SenderId = other.SenderId;
            Title = other.Title;
            TitleText = other.TitleText;
            Updated = other.Updated;
        }

        #endregion

        #region IEquatable<Notification> Members

        public bool Equals(Notification other)
        {
            if (other == null)
            {
                return false; 
            }
            return NotificationId == other.NotificationId;
        }

        #endregion

        #region INotifyPropertyChanged Members

        private void _NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public class FriendRequestNotification : Notification
    {
        private const string _friendRequestFormat = "<div><a href=\"{0}\">{1}</a> wants to be your friend!</div>";
        private const string _friendRequestTextFormat = "{0} wants to be your friend!";

        internal FriendRequestNotification(FacebookService service, string userId)
            : base(service)
        {
            Created = default(DateTime);
            Updated = default(DateTime);
            IsHidden = false;
            IsUnread = true;
            NotificationId = "FriendRequest_" + userId;
            RecipientId = service.UserId;
            SenderId = userId;
            Title = string.Format(_friendRequestFormat, "http://facebook.com/profile.php?id=" + userId, "Someone");
            TitleText = string.Format(_friendRequestTextFormat, "Someone");
            service.GetUserAsync(userId, _UpdateTitle);
            Link = new Uri("http://facebook.com/profile.php?id=" + userId);
            //this.Description = "";
        }

        private void _UpdateTitle(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                return;
            }

            Sender = e.UserState as FacebookContact;
            if (!string.IsNullOrEmpty(Sender.Name))
            {
                Title = string.Format(_friendRequestFormat, Sender.ProfileUri, Sender.Name);
                TitleText = string.Format(_friendRequestTextFormat, Sender.Name);
                Link = Sender.ProfileUri;
            }
        }

        public override string ToString()
        {
            if (Sender != null)
            {
                return Sender.Name + " wants to be your friend.";
            }
            return "Someone wants to be your friend.";
        }
    }

    // public class GroupInviteRequestNotification : Notification {}
    // public class EventInviteRequestNotification : Notification {}

    public class MessageNotification : Notification, IMergeable<MessageNotification>
    {
        internal MessageNotification(FacebookService service)
            : base(service)
        {}

        #region IMergeable<MessageNotification> Members

        string IMergeable<MessageNotification>.FKID
        {
            get { return ((IMergeable<Notification>)this).FKID; }
        }

        void IMergeable<MessageNotification>.Merge(MessageNotification other)
        {
        }

        #endregion

        #region IEquatable<MessageNotification> Members

        public bool Equals(MessageNotification other)
        {
            return ((IEquatable<Notification>)this).Equals(other);
        }

        #endregion
    }
}
