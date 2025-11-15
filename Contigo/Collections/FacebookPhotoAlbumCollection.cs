namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using Standard;

    // These can be set on the FacebookService.
    public enum PhotoAlbumSortOrder
    {
        None,
        AscendingByCreation,
        DescendingByCreation,
        AscendingByFriend,
        DescendingByFriend,
        AscendingByInterestLevel,
        DescendingByInterestLevel,
        AscendingByUpdate,
        DescendingByUpdate,
        AscendingByTitle,
        DescendingByTitle,
    }

    public class FacebookPhotoAlbumCollection : FacebookCollection<FacebookPhotoAlbum>
    {
        private bool _IsInteresting(FacebookContact contact)
        {
            Assert.IsNotNull(contact);
            return contact.InterestLevel > .1;
        }

        // Avoid problems with the actual Owner reference being async.
        private bool _IsOwnerInteresting(FacebookPhotoAlbum album)
        {
            Assert.IsNotNull(album);
            if (album.Owner != null)
            {
                return _IsInteresting(album.Owner);
            }

            return ((IFacebookObject)album).SourceService.GetInterestLevelForUserId(album.OwnerId) > .1;
        }

        private readonly FacebookContact _owner;
        private readonly MergeableCollection<FacebookPhotoAlbum> _filteredCollection;
        private readonly MergeableCollection<FacebookPhotoAlbum> _rawCollection;
        private readonly Dictionary<FacebookContact, bool> _interestMap;
        private readonly object _localLock = new object();

        internal FacebookPhotoAlbumCollection(MergeableCollection<FacebookPhotoAlbum> sourceCollection, FacebookService service, FacebookContact owner)
            : base(sourceCollection, service)
        {
            VerifyAccess();

            if (owner != null)
            {
                _rawCollection = sourceCollection;
                _owner = owner;
                _filteredCollection = new MergeableCollection<FacebookPhotoAlbum>(from album in sourceCollection where album.OwnerId == owner.UserId select album, false);
                base.ReplaceSourceCollection(_filteredCollection);
                sourceCollection.CollectionChanged += _OwnerFilterOnRawCollectionChanged;
            }
            // A null owner implicitly means that the album collection is filterable 
            else
            {
                _interestMap = new Dictionary<FacebookContact, bool>();

                _rawCollection = sourceCollection;
                _filteredCollection = new MergeableCollection<FacebookPhotoAlbum>(false);
                base.ReplaceSourceCollection(_filteredCollection);

                foreach (var album in sourceCollection)
                {
                    album.PropertyChanged += _OnAlbumOwnerUpdated;
                    if (album.Owner != null)
                    {
                        lock (_localLock)
                        {
                            if (!_interestMap.ContainsKey(album.Owner))
                            {
                                _interestMap.Add(album.Owner, _IsOwnerInteresting(album));
                                album.Owner.PropertyChanged += _OnContactPropertyChanged;
                            }
                        }
                        album.PropertyChanged -= _OnAlbumOwnerUpdated;

                        if (_IsOwnerInteresting(album))
                        {
                            _filteredCollection.Add(album);
                        }
                    }
                }

                sourceCollection.CollectionChanged += _InterestFilterOnRawCollectionChanged;
            }
        }

        private void _OnAlbumOwnerUpdated(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Owner")
            {
                var album = (FacebookPhotoAlbum)sender;
                album.PropertyChanged -= _OnAlbumOwnerUpdated;

                lock (_localLock)
                {
                    if (!_interestMap.ContainsKey(album.Owner))
                    {
                        _interestMap.Add(album.Owner, _IsOwnerInteresting(album));
                        album.Owner.PropertyChanged += _OnContactPropertyChanged;
                    }
                }

                if (_IsOwnerInteresting(album))
                {
                    _filteredCollection.Add(album);
                }
            }
        }

        private void _OnContactPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "InterestLevel")
            {
                var contact = (FacebookContact)sender;
                if (_IsInteresting(contact) != _interestMap[contact])
                {
                    _interestMap[contact] = _IsInteresting(contact);
                    _UpdateCollection();
                }
            }
        }

        void _InterestFilterOnRawCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            bool needsUpdate = false;
            if (e.NewItems != null)
            {
                foreach (FacebookPhotoAlbum album in e.NewItems)
                {
                    album.PropertyChanged += _OnAlbumOwnerUpdated;
                    if (album.Owner != null)
                    {
                        lock (_localLock)
                        {
                            if (!_interestMap.ContainsKey(album.Owner))
                            {
                                _interestMap.Add(album.Owner, _IsOwnerInteresting(album));
                                album.Owner.PropertyChanged += _OnContactPropertyChanged;
                                needsUpdate = true;
                            }
                        }
                        album.PropertyChanged -= _OnAlbumOwnerUpdated;
                        if (_IsOwnerInteresting(album))
                        {
                            needsUpdate = true;
                        }
                    }
                    else
                    {
                        needsUpdate = true;
                    }
                }

                needsUpdate = (from FacebookPhotoAlbum album in e.NewItems where _IsOwnerInteresting(album) select album).Any();
            }

            if (e.OldItems != null)
            {
                foreach (var contact in from FacebookPhotoAlbum album in e.OldItems select album.Owner)
                {
                    if (_interestMap.ContainsKey(contact))
                    {
                        _interestMap.Remove(contact);
                        contact.PropertyChanged -= _OnContactPropertyChanged;
                    }

                    needsUpdate = needsUpdate || _IsInteresting(contact);
                }
            }

            if (needsUpdate)
            {
                _UpdateCollection();
            }
        }

        private void _UpdateCollection()
        {
            _filteredCollection.Merge(from album in _rawCollection where album.Owner == null || _IsOwnerInteresting(album) select album, false);

            foreach (var contact in from album in _rawCollection where album.Owner != null select album.Owner)
            {
                if (!_interestMap.ContainsKey(contact))
                {
                    _interestMap.Add(contact, _IsInteresting(contact));
                    contact.PropertyChanged += _OnContactPropertyChanged;
                }
            }
        }

        void _OwnerFilterOnRawCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            bool needsUpdate = false;
            if (e.NewItems != null)
            {
                needsUpdate = (from FacebookPhotoAlbum album in e.NewItems where album.OwnerId == _owner.UserId select album).Any();
            }

            if (!needsUpdate && e.OldItems != null)
            {
                needsUpdate = (from FacebookPhotoAlbum album in e.OldItems where album.OwnerId == _owner.UserId select album).Any();
            }

            if (needsUpdate)
            {
                _filteredCollection.Merge(from album in _rawCollection where album.OwnerId == _owner.UserId select album, false);
            }
        }
    }
}
