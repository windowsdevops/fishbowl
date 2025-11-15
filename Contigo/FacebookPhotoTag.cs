
namespace Contigo
{
    using System.ComponentModel;
    using System.Windows;
    using Standard;

    public class FacebookPhotoTag : IFacebookObject, INotifyPropertyChanged
    {
        private SmallString _text;
        private FacebookContact _contact;
        private bool _fetchingContact;
        private SmallString _contactId;
        private SmallString _photoId;

        internal FacebookPhotoTag(FacebookService service)
        {
            Verify.IsNotNull(service, "service");
            SourceService = service;
        }

        public FacebookContact Contact
        { 
            get
            {
                if (!_fetchingContact && _contact == null && !string.IsNullOrEmpty(ContactId))
                {
                    _fetchingContact = true;
                    SourceService.GetUserAsync(ContactId, _OnGetUserCompleted);
                }
                return _contact;
            }
            private set
            {
                _contact = value;
                _NotifyPropertyChanged("Contact");
            }
        }

        private void _OnGetUserCompleted(object sender, AsyncCompletedEventArgs args)
        {
            Contact = (FacebookContact)args.UserState;
            _fetchingContact = false;
        }

        public Point Offset { get; internal set; }

        internal string PhotoId
        {
            get { return _photoId.GetString(); }
            set { _photoId = new SmallString(value); }
        }

        internal string ContactId
        {
            get { return _contactId.GetString(); }
            set { _contactId = new SmallString(value); }
        }

        public string Text
        {
            get { return _text.GetString(); }
            internal set { _text = new SmallString(value); }
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

        private void _NotifyPropertyChanged(string propertyName)
        {
            Assert.IsNeitherNullNorEmpty(propertyName);

            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
