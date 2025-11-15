
namespace Contigo
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Standard;
    using System.Globalization;

    public sealed class FacebookPhotoAlbum : INotifyPropertyChanged, IFacebookObject, IMergeable<FacebookPhotoAlbum>, IComparable<FacebookPhotoAlbum>
    {
        #region Sort Delegates

        private static readonly Comparison<FacebookPhotoAlbum> _defaultComparison = (album1, album2) => album1.CompareTo(album2);
        private static readonly Comparison<FacebookPhotoAlbum> _ascendingByTitleComparison = (album1, album2) => album1._lowerTitleSmallString.CompareTo(album2._lowerTitleSmallString);
        private static readonly Comparison<FacebookPhotoAlbum> _ascendingByCreationComparison = (album1, album2) => album1.Created.CompareTo(album2.Created);
        private static readonly Comparison<FacebookPhotoAlbum> _ascendingByUpdateComparison = (album1, album2) => album1.LastModified.CompareTo(album2.LastModified);
        private static readonly Comparison<FacebookPhotoAlbum> _ascendingByFriendComparison = (album1, album2) =>
        {
            int ret = 0;
            if (album1.Owner == null)
            {
                ret = album2.Owner == null ? 0 : -1;
            }
            else
            {
                ret = album1.Owner.CompareTo(album2.Owner);
            }

            if (ret == 0)
            {
                ret = _descendingByUpdateComparison(album1, album2);
            }
            return ret;
        };
        private static readonly Comparison<FacebookPhotoAlbum> _ascendingByInterestLevelComparison = (album1, album2) =>
        {
            int ret = 0;
            if (album1.Owner == null)
            {
                ret = album2.Owner == null ? 0 : -1;
            }
            else
            {
                ret = album1.Owner.InterestLevel.CompareTo(album2.Owner.InterestLevel);
            }

            if (ret == 0)
            {
                ret = _descendingByFriendComparison(album1, album2);
            }
            return ret;
        };
        private static readonly Comparison<FacebookPhotoAlbum> _descendingByTitleComparison = (album1, album2) => album2._lowerTitleSmallString.CompareTo(album1._lowerTitleSmallString);
        private static readonly Comparison<FacebookPhotoAlbum> _descendingByCreationComparison = (album1, album2) => album2.Created.CompareTo(album1.Created);
        private static readonly Comparison<FacebookPhotoAlbum> _descendingByUpdateComparison = (album1, album2) => album2.LastModified.CompareTo(album1.LastModified);
        private static readonly Comparison<FacebookPhotoAlbum> _descendingByFriendComparison = (album1, album2) =>
        {
            int ret = 0;
            if (album2.Owner == null)
            {
                ret = album1.Owner == null ? 0 : -1;
            }
            else
            {
                ret = album2.Owner.CompareTo(album1.Owner);
            }

            if (ret == 0)
            {
                ret = _ascendingByUpdateComparison(album2, album1);
            }
            return ret;
        };
        private static readonly Comparison<FacebookPhotoAlbum> _descendingByInterestLevelComparison = (album1, album2) =>
        {
            int ret = 0;
            if (album1.Owner == null)
            {
                ret = album2.Owner == null ? 0 : -1;
            }
            else
            {
                ret = album2.Owner.InterestLevel.CompareTo(album1.Owner.InterestLevel);
            }

            if (ret == 0)
            {
                ret = _ascendingByFriendComparison(album1, album2);
            }
            return ret;
        };


        internal static Comparison<FacebookPhotoAlbum> GetComparison(PhotoAlbumSortOrder value)
        {
            switch (value)
            {
                case PhotoAlbumSortOrder.AscendingByCreation: return _ascendingByCreationComparison;
                case PhotoAlbumSortOrder.AscendingByFriend: return _ascendingByFriendComparison;
                case PhotoAlbumSortOrder.AscendingByTitle: return _ascendingByTitleComparison;
                case PhotoAlbumSortOrder.AscendingByUpdate: return _ascendingByUpdateComparison;
                case PhotoAlbumSortOrder.AscendingByInterestLevel: return _ascendingByInterestLevelComparison;
                case PhotoAlbumSortOrder.DescendingByCreation: return _descendingByCreationComparison;
                case PhotoAlbumSortOrder.DescendingByFriend: return _descendingByFriendComparison;
                case PhotoAlbumSortOrder.DescendingByTitle: return _descendingByTitleComparison;
                case PhotoAlbumSortOrder.DescendingByUpdate: return _descendingByUpdateComparison;
                case PhotoAlbumSortOrder.DescendingByInterestLevel: return _descendingByInterestLevelComparison;
                case PhotoAlbumSortOrder.None: return _defaultComparison;
                default: Assert.Fail(); return _defaultComparison;
            }
        }

        #endregion


        private FacebookPhoto _coverPic;
        private FacebookPhotoCollection _photos;

        private SmallString _location;
        private SmallString _title;
        private SmallString _lowerTitleSmallString;
        private SmallString _description;
        private SmallString _coverPicPid;
        private SmallString _ownerId;
        private SmallUri _link;
        private DateTime _created;
        private DateTime _lastModified;
        // Used too frequently to be a small string.
        private string _albumId;

        internal FacebookPhotoAlbum(FacebookService service)
        {
            Assert.IsNotNull(service);
            SourceService = service;
        }
        
        public string OwnerId
        {
            get { return _ownerId.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _ownerId)
                {
                    _ownerId = newValue;
                    _NotifyPropertyChanged("OwnerId");
                    _UpdateOwner();
                }
            }
        }

        public string AlbumId
        {
            get { return _albumId ?? ""; }
            internal set
            {
                if (value != (_albumId ?? ""))
                {
                    _albumId = value;
                    _NotifyPropertyChanged("AlbumId");
                }
            }
        }

        public string Location
        {
            get { return _location.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _location)
                {
                    _location = newValue;
                    _NotifyPropertyChanged("Location");
                }
            }
        }

        public string Title
        {
            get { return _title.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _title)
                {
                    _title = newValue;
                    _lowerTitleSmallString = new SmallString(value.ToLower(CultureInfo.CurrentCulture));
                    _NotifyPropertyChanged("Title");
                }
            }
        }

        public string Description
        {
            get { return _description.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _description)
                {
                    _description = newValue;
                    _NotifyPropertyChanged("Description");
                }
            }
        }

        public Uri Link
        {
            get { return _link.GetUri(); }
            internal set
            {
                var newValue = new SmallUri(value);
                if (newValue != _link)
                {
                    _link = newValue;
                    _NotifyPropertyChanged("Link");
                }
            }
        }

        internal string CoverPicPid
        {
            get { return _coverPicPid.GetString(); }
            set
            {
                var newValue = new SmallString(value);
                if (_coverPicPid != newValue)
                {
                    _coverPicPid = newValue;
                    _UpdateCoverPic();
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

        public DateTime LastModified
        {
            get { return _lastModified; }
            internal set
            {
                if (_lastModified != value)
                {
                    _lastModified = value;
                    _NotifyPropertyChanged("LastModified");
                }
            }
        }

        internal MergeableCollection<FacebookPhoto> RawPhotos { get; set; }

        public FacebookPhotoCollection Photos
        {
            get
            {
                if (_photos == null)
                {
                    Assert.IsNotNull(RawPhotos);
                    _photos = new FacebookPhotoCollection(RawPhotos, SourceService);
                }

                return _photos;
            }
        }

        public FacebookPhoto CoverPic
        {
            get
            {
                if (_coverPic == null)
                {
                    _coverPic = (from photo in Photos where photo.PhotoId == CoverPicPid select photo).FirstOrDefault();
                }
                return _coverPic;
            }
        }

        private void _UpdateCoverPic()
        {
            if (_coverPic != null)
            {
                _coverPic = null;
                _NotifyPropertyChanged("CoverPic");
            }
        }

        public FacebookPhoto FirstPhoto
        {
            get
            {
                if (Photos.Count > 0)
                {
                    return Photos[0];
                }
                return null; 
            }
        }

        public FacebookPhoto SecondPhoto
        {
            get
            {
                if (Photos.Count > 1)
                {
                    return Photos[1];
                }
                return null;
            }
        }

        public FacebookPhoto ThirdPhoto
        {
            get
            {
                if (Photos.Count > 2)
                {
                    return Photos[2];
                }
                return null;
            }
        }
        
        public FacebookContact Owner { get; private set; }

        private void _UpdateOwner()
        {
            if (_ownerId == default(SmallString))
            {
                return;
            }

            SourceService.GetUserAsync(_ownerId.GetString(), _OnGetOwnerCompleted);
        }

        private void _OnGetOwnerCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                return;
            }

            Owner = e.UserState as FacebookContact;
            _NotifyPropertyChanged("Owner"); 
        }

        /// <remarks>
        /// This is a synchronous operation.
        /// </remarks>
        public void SaveToFolder(string path)
        {
            if (!path.EndsWith("\\"))
            {
                path = path + "\\";
            }

            Utility.EnsureDirectory(path);

            for (int i = 0; i < this.Photos.Count; i++)
            {
                string cachePath = this.Photos[i].Image.GetCachePath(FacebookImageDimensions.Big);
                if (cachePath == null)
                {
                    // log error
                    break;
                }

                File.Copy(cachePath, Path.Combine(path, string.Format("{0:D3}.jpg", i + 1)));
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

        #region System.Object Overrides

        public override bool Equals(object obj)
        {
            return Equals(obj as FacebookPhotoAlbum);
        }

        public override int GetHashCode()
        {
            return _albumId.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}, {1} photos", Title, Photos.Count);
        }

        #endregion

        #region IFacebookObject Members

        FacebookService IFacebookObject.SourceService { get; set; }

        private FacebookService SourceService
        {
            get { return ((IFacebookObject)this).SourceService; }
            set { ((IFacebookObject)this).SourceService = value; }
        }

        #endregion

        #region IMergeable<FacebookPhotoAlbum> Members

        string IMergeable<FacebookPhotoAlbum>.FKID
        {
            get { return AlbumId; }
        }

        void IMergeable<FacebookPhotoAlbum>.Merge(FacebookPhotoAlbum other)
        {
            Verify.IsNotNull(other, "other");
            if (other.AlbumId != this.AlbumId)
            {
                throw new InvalidOperationException("Can't merge two albums with different album Ids.");
            }

            if (object.ReferenceEquals(this, other))
            {
                return;
            }

            CoverPicPid = other.CoverPicPid;
            Created = other.Created;
            Description = other.Description;
            LastModified = other.LastModified;
            Link = other.Link;
            Location = other.Location;
            OwnerId = other.OwnerId;
            RawPhotos.Merge(other.RawPhotos, false);
            _NotifyPropertyChanged("FirstPhoto");
            _NotifyPropertyChanged("SecondPhoto");
            _NotifyPropertyChanged("ThirdPhoto");
            Title = other.Title;
        }

        #endregion

        #region IEquatable<FacebookPhotoAlbum> Members

        public bool Equals(FacebookPhotoAlbum other)
        {
            if (other == null)
            {
                return false;
            }

            return other.AlbumId == this.AlbumId;
        }

        #endregion

        #region IComparable<FacebookPhotoAlbum> Members

        public int CompareTo(FacebookPhotoAlbum other)
        {
            if (other == null)
            {
                return 1;
            }

            // Albums by default should sort most recently updated first.
            return -LastModified.CompareTo(other.LastModified);
        }

        #endregion

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void VerifyAccess()
        {
            SourceService.Dispatcher.VerifyAccess();
        }

    }
}
