namespace ClientManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contigo;
    using Standard;
    
    /// <summary>
    /// Navigators are used to provide journaling support for Facebook data. The navigation history
    /// is persisted to the navigation journal as strings indicating the path of objects. 
    /// Navigator objects provide a navigation layer that wraps data objects and exposes information
    /// necessary for journaling, such as a path for the wrapped object that provides enough information
    /// to retrieve the object when access by the journal, and APIs to retrieve a navigator, and its 
    /// wrapped content, from the journaled path.
    /// </summary>
    public abstract class Navigator : IEquatable<Navigator>
    {
        /// <summary>
        /// Extract first child path, i.e. text before first separator.
        /// </summary>
        /// <param name="path">The inital path.</param>
        /// <param name="remainder">The remainder of the path after extraction.</param>
        /// <returns>The text before the first separator.</returns>
        public static string ExtractFirstChildPath(string path, out string remainder)
        {
            string childPath = path;
            remainder = String.Empty;
            if (!string.IsNullOrEmpty(path))
            {
                int separatorIndex = path.IndexOf("/", StringComparison.Ordinal);
                if (separatorIndex >= 0 && separatorIndex < path.Length)
                {
                    // Separator was found, remainder of the path is passed to the matching child for further lookup
                    childPath = path.Substring(0, separatorIndex);
                    if (separatorIndex + 1 < path.Length)
                    {
                        remainder = path.Substring(separatorIndex + 1, path.Length - (separatorIndex + 1));
                    }
                }
            }

            if (!string.IsNullOrEmpty(childPath))
            {
                childPath = Uri.UnescapeDataString(childPath);
            }

            return childPath;
        }

        protected Navigator(object content, string guid, Navigator parent)
        {
            Verify.IsNotNull(content, "content");
            Verify.IsNeitherNullNorEmpty(guid, "guid");
            Content = content;

            Guid = guid;
            if (parent != null)
            {
                Parent = parent;
                Path = Parent.Path + "/" + Uri.EscapeDataString(Guid);
            }
            else
            {
                Path = Uri.EscapeDataString(Guid);
            }
        }
      
        /// <summary>Gets the actual data object that the navigator is used to locate.</summary>
        public object Content { get; private set; }
        /// <summary>Gets the path for the Navigator.  Navigators must provide a serializable path or locator for their object that can be stored in the navigation journal.</summary>
        public string Path { get; private set; }
        public string Guid { get; private set; }
        public Navigator Parent { get; private set; }

        public virtual bool IncludeInJournal { get { return true; } }

        // Tag property a parent can use to look up relative siblings.
        internal int ParentIndex { get; set; }

        protected virtual Navigator GetChildNavigatorWithGuid(string guid) { return null; }
        protected virtual Navigator GetPreviousChild(Navigator navigator) { return null; }
        protected virtual Navigator GetNextChild(Navigator navigator) { return null; }
        public virtual bool CanGetChildNavigatorWithContent(object content) { return GetChildNavigatorWithContent(content) != null; }
        public virtual Navigator GetChildNavigatorWithContent(object content) { return null; }
        public virtual Navigator FirstChild { get { return null; } }
        public virtual Navigator LastChild { get { return null; } }

        public Navigator FindChildNavigatorFromPath(string path)
        {
            Verify.IsNeitherNullNorEmpty(path, "path");

            // Extract the first guid in the path and match it to a child navigator. If there is no child separator,
            // treat the entire path as the child's guid
            string remainder;
            string childGuid = ExtractFirstChildPath(path, out remainder);
            Assert.IsNeitherNullNorEmpty(childGuid);

            Navigator childNavigator = GetChildNavigatorWithGuid(childGuid);
            if (childNavigator == null)
            {
                return null; 
            }

            if (!string.IsNullOrEmpty(remainder))
            {
                return childNavigator.FindChildNavigatorFromPath(remainder);
            }

            return childNavigator;
        }

        public Navigator NextSibling
        {
            get
            {
                if (Parent != null)
                {
                    return Parent.GetNextChild(this);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the previous sibling Navigator for this navigator.
        /// </summary>
        public Navigator PreviousSibling
        {
            get
            {
                if (Parent != null)
                {
                    return Parent.GetPreviousChild(this);
                }
                return null;
            }
        }
        
        #region Object overrides

        public override string ToString()
        {
            return Path.ToString() + ": [" + Content.ToString() + "]";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Navigator);
        }

        public override int GetHashCode()
        {
            if (Content == null)
            {
                return 0;
            }

            return Content.GetHashCode() + 5;
        }


        #endregion

        public virtual bool ShouldReplaceNavigatorInJournal(Navigator oldNavigator)
        {
            if (oldNavigator == null)
            {
                return false;
            }

            if (this.Parent == null || oldNavigator.Parent == null)
            {
                return false;
            }

            return oldNavigator.Parent.Equals(Parent);
        }

        #region IEquatable<Navigator> Members

        public bool Equals(Navigator other)
        {
            if (other == null)
            {
                return false;
            }

            // I think reference comparison is okay here.
            return other.Content == this.Content;
        }

        #endregion
    }

    #region Navigators for Facebook objects

    #region Leaf Navigators

    public class ContactNavigator : Navigator
    {
        public ContactNavigator(FacebookContact contact, Navigator parent)
            : this(contact, contact.UserId, parent)
        {}

        public ContactNavigator(FacebookContact contact, string guid, Navigator parent)
            : base(contact, guid, parent)
        { }
    }

    public class SearchNavigator : Navigator
    {
        public SearchNavigator(SearchResults searchResults)
            : base(searchResults, "[search]" + searchResults.SearchText, null)
        { }
    }

    public class PhotoNavigator : Navigator
    {
        public PhotoNavigator(FacebookPhoto photo, Navigator parent)
            : base(photo, photo.PhotoId, parent)
        { }
    }

    #endregion

    public class ContactCollectionNavigator : Navigator
    {
        private FacebookContactCollection _contacts;

        public ContactCollectionNavigator(FacebookContactCollection contacts, string guid, Navigator parent)
            : base(contacts, guid, parent)
        {
            _contacts = contacts;
        }

        protected override Navigator GetChildNavigatorWithGuid(string guid)
        {
            Verify.IsNeitherNullNorEmpty(guid, "guid");
            _contacts.VerifyAccess();

            for (int index = 0; index < _contacts.Count; ++index)
            {
                if (_contacts[index].UserId == guid)
                {
                    var navigator = new ContactNavigator(_contacts[index], this)
                    {
                        ParentIndex = index,
                    };
                    return navigator;
                }
            }

            return null;
        }

        public override bool CanGetChildNavigatorWithContent(object content)
        {
            _contacts.VerifyAccess();

            var contact = content as FacebookContact;
            if (contact == null)
            {
                return false;
            }

            return (from c in _contacts where c.Equals(contact) select c).Any();
        }

        public override Navigator GetChildNavigatorWithContent(object content)
        {
            _contacts.VerifyAccess();

            var contact = content as FacebookContact;
            if (contact == null)
            {
                return null;
            }

            for (int index = 0; index < _contacts.Count; ++index)
            {
                if (_contacts[index].Equals(contact))
                {
                    var navigator = new ContactNavigator(_contacts[index], this)
                    {
                        ParentIndex = index,
                    };
                    return navigator;
                }
            }

            return null;
        }

        public Navigator GetContactWithId(string id)
        {
            _contacts.VerifyAccess();

            for (int index = 0; index < _contacts.Count; ++index)
            {
                if (_contacts[index].UserId == id)
                {
                    var navigator = new ContactNavigator(_contacts[index], this)
                    {
                        ParentIndex = index,
                    };
                    return navigator;
                }
            }

            return null;
        }

        private int _FindChildIndex(Navigator navigator)
        {
            int index = -1;
            var contact = navigator.Content as FacebookContact;
            if (contact != null)
            {
                if (ParentIndex >= 0
                    && ParentIndex < _contacts.Count
                    && _contacts[ParentIndex].Equals(contact))
                {
                    index = ParentIndex;
                }
                else
                {
                    index = _contacts.IndexOf(contact);
                }
            }
            return index;
        }

        protected override Navigator GetPreviousChild(Navigator navigator)
        {
            Verify.IsNotNull(navigator, "navigator");
            Verify.AreEqual(navigator.Parent, this, "navigator", "The navigator is not a child of this navigator.");

            _contacts.VerifyAccess();

            int oldOffset = _FindChildIndex(navigator);
            if (oldOffset == -1)
            {
                return null;
            }

            if (oldOffset == 0)
            {
                return null;
            }

            return new ContactNavigator(_contacts[oldOffset-1], this);
        }

        protected override Navigator GetNextChild(Navigator navigator)
        {
            Verify.IsNotNull(navigator, "navigator");
            Verify.AreEqual(navigator.Parent, this, "navigator", "The navigator is not a child of this navigator.");

            _contacts.VerifyAccess();

            int oldOffset = _FindChildIndex(navigator);
            if (oldOffset == -1)
            {
                return null;
            }

            if (oldOffset == _contacts.Count - 1)
            {
                return null;
            }

            return new ContactNavigator(_contacts[oldOffset + 1], this);
        }

        public override Navigator FirstChild
        {
            get
            {
                _contacts.VerifyAccess();
                var contact = _contacts.FirstOrDefault();
                if (contact == null)
                {
                    return null;
                }

                return new ContactNavigator(contact, this);
            }
        }

        public override Navigator LastChild
        {
            get
            {
                _contacts.VerifyAccess();
                var contact = _contacts.LastOrDefault();
                if (contact == null)
                {
                    return null;
                }

                return new ContactNavigator(contact, this);
            }
        }
    }

    public class PhotoAlbumCollectionNavigator : Navigator
    {
        private FacebookPhotoAlbumCollection _albums;

        public PhotoAlbumCollectionNavigator(FacebookPhotoAlbumCollection albumCollection, string guid, Navigator parent)
            : base(albumCollection, guid, parent)
        {
            _albums = albumCollection;
        }

        protected override Navigator GetChildNavigatorWithGuid(string guid)
        {
            Verify.IsNeitherNullNorEmpty(guid, "guid");
            _albums.VerifyAccess();

            for (int index = 0; index < _albums.Count; ++index)
            {
                if (_albums[index].AlbumId == guid)
                {
                    var navigator = new PhotoAlbumNavigator(_albums[index], this)
                    {
                        ParentIndex = index,
                    };
                    return navigator;
                }
            }

            return null;
        }

        public override bool CanGetChildNavigatorWithContent(object content)
        {
            _albums.VerifyAccess();

            var photo = content as FacebookPhoto;
            var album = content as FacebookPhotoAlbum;

            if (photo != null)
            {
                album = photo.Album;
            }

            if (album == null)
            {
                return false;
            }

            foreach (var a in _albums)
            {
                if (a.Equals(album))
                {
                    if (photo != null)
                    {
                        return (from p in a.Photos where p.Equals(photo) select p).Any();
                    }
                    return true;
                }
            }

            return false;
        }

        public override Navigator GetChildNavigatorWithContent(object content)
        {
            _albums.VerifyAccess();

            var photo = content as FacebookPhoto;
            var album = content as FacebookPhotoAlbum;

            if (photo != null)
            {
                album = photo.Album;
            }

            if (album == null)
            {
                return null;
            }

            for (int index = 0; index < _albums.Count; ++index)
            {
                if (_albums[index].Equals(album))
                {
                    var navigator = new PhotoAlbumNavigator(_albums[index], this)
                    {
                        ParentIndex = index,
                    };

                    if (photo != null)
                    {
                        return navigator.GetChildNavigatorWithContent(photo);
                    }

                    return navigator;
                }
            }

            return null;
        }

        public Navigator GetPhotoWithId(string ownerId, string photoId)
        {
            Verify.IsNeitherNullNorEmpty(ownerId, "ownerId");
            Verify.IsNeitherNullNorEmpty(photoId, "photoId");
            _albums.VerifyAccess();

            for (int index = 0; index < _albums.Count; ++index)
            {
                if (_albums[index].OwnerId.Equals(ownerId))
                {
                    for (int index2 = 0; index2 < _albums[index].Photos.Count; ++index2)
                    {
                        if (_albums[index].Photos[index2].PhotoId.Equals(photoId))
                        {
                            return new PhotoNavigator(
                                _albums[index].Photos[index2],
                                new PhotoAlbumNavigator(_albums[index], this)
                                {
                                    ParentIndex = index,
                                })
                            {
                                ParentIndex = index2,
                            };
                        }
                    }
                }
            }
            return null;
        }

        private int _FindChildIndex(Navigator navigator)
        {
            int index = -1;
            var album = navigator.Content as FacebookPhotoAlbum;
            if (album != null)
            {
                if (ParentIndex >= 0
                    && ParentIndex < _albums.Count
                    && _albums[ParentIndex].Equals(album))
                {
                    index = ParentIndex;
                }
                else
                {
                    index = _albums.IndexOf(album);
                }
            }
            return index;
        }

        protected override Navigator GetPreviousChild(Navigator navigator)
        {
            Verify.IsNotNull(navigator, "navigator");
            Verify.AreEqual(navigator.Parent, this, "navigator", "The navigator is not a child of this navigator.");

            _albums.VerifyAccess();

            int oldOffset = _FindChildIndex(navigator);
            if (oldOffset == -1)
            {
                return null;
            }

            if (oldOffset == 0)
            {
                return null;
            }

            return new PhotoAlbumNavigator(_albums[oldOffset - 1], this);
        }

        protected override Navigator GetNextChild(Navigator navigator)
        {
            Verify.IsNotNull(navigator, "navigator");
            Verify.AreEqual(navigator.Parent, this, "navigator", "The navigator is not a child of this navigator.");

            _albums.VerifyAccess();

            int oldOffset = _FindChildIndex(navigator);
            if (oldOffset == -1)
            {
                return null;
            }

            if (oldOffset == _albums.Count - 1)
            {
                return null;
            }

            return new PhotoAlbumNavigator(_albums[oldOffset + 1], this);
        }

        public override Navigator FirstChild
        {
            get
            {
                _albums.VerifyAccess();
                FacebookPhotoAlbum album = _albums.FirstOrDefault();
                if (album == null)
                {
                    return null;
                }

                return new PhotoAlbumNavigator(album, this);
            }
        }

        public override Navigator LastChild
        {
            get
            {
                _albums.VerifyAccess();
                FacebookPhotoAlbum album = _albums.LastOrDefault();
                if (album == null)
                {
                    return null;
                }

                return new PhotoAlbumNavigator(album, this);
            }
        }
    }

    public class PhotoAlbumNavigator : Navigator
    {
        private FacebookPhotoAlbum _album;

        public PhotoAlbumNavigator(FacebookPhotoAlbum album, Navigator parent)
            : base(album, album.AlbumId, parent)
        {
            _album = album;
        }

        protected override Navigator GetChildNavigatorWithGuid(string guid)
        {
            Verify.IsNeitherNullNorEmpty(guid, "guid");
            _album.VerifyAccess();

            for (int index = 0; index < _album.Photos.Count; ++index)
            {
                if (_album.Photos[index].PhotoId == guid)
                {
                    var navigator = new PhotoNavigator(_album.Photos[index], this)
                    {
                        ParentIndex = index,
                    };
                    return navigator;
                }
            }

            return null;
        }

        public override bool CanGetChildNavigatorWithContent(object content)
        {
            _album.VerifyAccess();

            var photo = content as FacebookPhoto;
            if (photo == null)
            {
                return false;
            }

            return (from p in _album.Photos where p.Equals(photo) select p).Any();
        }

        public override Navigator GetChildNavigatorWithContent(object content)
        {
            _album.VerifyAccess();

            var photo = content as FacebookPhoto;
            if (photo == null)
            {
                return null;
            }

            for (int index = 0; index < _album.Photos.Count; ++index)
            {
                if (_album.Photos[index].Equals(photo))
                {
                    var navigator = new PhotoNavigator(_album.Photos[index], this)
                    {
                        ParentIndex = index,
                    };
                    return navigator;
                }
            }

            return null;
        }

        private int _FindChildIndex(Navigator navigator)
        {
            int index = -1;
            var photo = navigator.Content as FacebookPhoto;
            if (photo != null)
            {
                if (ParentIndex >= 0
                    && ParentIndex < _album.Photos.Count
                    && _album.Photos[ParentIndex].Equals(photo))
                {
                    index = ParentIndex;
                }
                else
                {
                    index = _album.Photos.IndexOf(photo);
                }
            }
            return index;
        }

        protected override Navigator GetPreviousChild(Navigator navigator)
        {
            Verify.IsNotNull(navigator, "navigator");
            Verify.AreEqual(navigator.Parent, this, "navigator", "The navigator is not a child of this navigator.");

            _album.VerifyAccess();

            int oldOffset = _FindChildIndex(navigator);
            if (oldOffset == -1)
            {
                return null;
            }

            if (oldOffset == 0)
            {
                return null;
            }

            return new PhotoNavigator(_album.Photos[oldOffset - 1], this);
        }

        protected override Navigator GetNextChild(Navigator navigator)
        {
            Verify.IsNotNull(navigator, "navigator");
            Verify.AreEqual(navigator.Parent, this, "navigator", "The navigator is not a child of this navigator.");

            _album.VerifyAccess();

            int oldOffset = _FindChildIndex(navigator);
            if (oldOffset == -1)
            {
                return null;
            }

            if (oldOffset == _album.Photos.Count - 1)
            {
                return null;
            }

            return new PhotoNavigator(_album.Photos[oldOffset + 1], this);
        }

        public override Navigator FirstChild
        {
            get
            {
                _album.VerifyAccess();
                FacebookPhoto photo = _album.Photos.FirstOrDefault();
                if (photo == null)
                {
                    return null;
                }

                return new PhotoNavigator(photo, this);
            }
        }

        public override Navigator LastChild
        {
            get
            {
                _album.VerifyAccess();
                FacebookPhoto photo = _album.Photos.LastOrDefault();
                if (photo == null)
                {
                    return null;
                }

                return new PhotoNavigator(photo, this);
            }
        }

    }

    public class AggregatePhotoAlbumNavigator : Navigator
    {
        private readonly List<FacebookPhoto> _photos;

        public AggregatePhotoAlbumNavigator(PhotoAlbumCollectionNavigator albums)
            : base(albums, "[aggregate albums]", null)
        {
            var albumCollection = albums.Content as FacebookPhotoAlbumCollection;
            _photos = (from album in albumCollection
                      from photo in album.Photos
                      select photo).ToList();
        }

        protected override Navigator GetChildNavigatorWithGuid(string guid)
        {
            Verify.IsNeitherNullNorEmpty(guid, "guid");

            for (int index = 0; index < _photos.Count; ++index)
            {
                if (_photos[index].PhotoId == guid)
                {
                    var navigator = new PhotoNavigator(_photos[index], this)
                    {
                        ParentIndex = index,
                    };
                    return navigator;
                }
            }

            return null;
        }

        public override bool CanGetChildNavigatorWithContent(object content)
        {
            var photo = content as FacebookPhoto;
            if (photo == null)
            {
                return false;
            }

            return (from p in _photos where p.Equals(photo) select p).Any();
        }

        public override Navigator GetChildNavigatorWithContent(object content)
        {
            var photo = content as FacebookPhoto;
            if (photo == null)
            {
                return null;
            }

            for (int index = 0; index < _photos.Count; ++index)
            {
                if (_photos[index].Equals(photo))
                {
                    var navigator = new PhotoNavigator(_photos[index], this)
                    {
                        ParentIndex = index,
                    };
                    return navigator;
                }
            }

            return null;
        }

        private int _FindChildIndex(Navigator navigator)
        {
            int index = -1;
            var photo = navigator.Content as FacebookPhoto;
            if (photo != null)
            {
                if (ParentIndex >= 0
                    && ParentIndex < _photos.Count
                    && _photos[ParentIndex].Equals(photo))
                {
                    index = ParentIndex;
                }
                else
                {
                    index = _photos.IndexOf(photo);
                }
            }
            return index;
        }

        protected override Navigator GetPreviousChild(Navigator navigator)
        {
            Verify.IsNotNull(navigator, "navigator");
            Verify.AreEqual(navigator.Parent, this, "navigator", "The navigator is not a child of this navigator.");

            int oldOffset = _FindChildIndex(navigator);
            if (oldOffset == -1)
            {
                return null;
            }

            if (oldOffset == 0)
            {
                return null;
            }

            return new PhotoNavigator(_photos[oldOffset - 1], this);
        }

        public override Navigator FirstChild
        {
            get
            {
                FacebookPhoto photo = _photos.FirstOrDefault();
                if (photo == null)
                {
                    return null;
                }

                return new PhotoNavigator(photo, this);
            }
        }

        public override Navigator LastChild
        {
            get
            {
                FacebookPhoto photo = _photos.LastOrDefault();
                if (photo == null)
                {
                    return null;
                }

                return new PhotoNavigator(photo, this);
            }
        }
    }

    #endregion

}
