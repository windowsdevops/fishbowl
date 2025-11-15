namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Collections;
    using System.Text.RegularExpressions;
    using System.Collections.Specialized;
    using System.Windows.Threading;
    using Standard;

    public class SearchIndex
    {
        private static Regex s_wordRegex = new Regex(@"[\w']+");

        private Dictionary<object, string[]> _forwardIndex = new Dictionary<object, string[]>();
        private Dictionary<string, List<object>> _invertedIndex = new Dictionary<string, List<object>>();
        private object _indexLock = new object();
        private FacebookService _service;

        internal SearchIndex(FacebookService service)
        {
            _service = service;

            // Sign up for collection change notifications for incremental index updates.
            //_service.Friends.CollectionChanged += new NotifyCollectionChangedEventHandler(OnFriendsCollectionChanged);
            //_service.NewsFeed.CollectionChanged += new NotifyCollectionChangedEventHandler(OnNewsFeedCollectionChanged);
            //_service.FriendsPhotoAlbums.CollectionChanged += new NotifyCollectionChangedEventHandler(OnAlbumsCollectionChanged);
            //_service.MyPhotoAlbums.CollectionChanged += new NotifyCollectionChangedEventHandler(OnAlbumsCollectionChanged);

            //foreach (ActivityPost post in _service.NewsFeed)
            //{
            //    post.Comments.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCommentsCollectionChanged);
            //}

            //foreach (FacebookPhotoAlbum album in _service.FriendsPhotoAlbums)
            //{
            //    album.Photos.CollectionChanged += new NotifyCollectionChangedEventHandler(OnPhotosCollectionChanged);
            //}

            //foreach (FacebookPhotoAlbum album in _service.MyPhotoAlbums)
            //{
            //    album.Photos.CollectionChanged += new NotifyCollectionChangedEventHandler(OnPhotosCollectionChanged);
            //}

            //BuildIndex();
        }

        public SearchResults DoSearch(string query)
        {
            BuildIndex(); // todo: temporary until we have incremental update.

            if (!string.IsNullOrEmpty(query))
            {
                lock (_indexLock)
                {
                    string[] words = StemWords(GetWords(query));
                    if (words.Length > 0 && _invertedIndex.ContainsKey(words[0]))
                    {
                        List<object> searchResults = new List<object>(_invertedIndex[words[0]]);

                        for (int i = 1; i < words.Length; i++)
                        {
                            if (!_invertedIndex.ContainsKey(words[i]))
                            {
                                return SearchResults.CreateEmpty(_service, query);
                            }

                            Intersect(searchResults, _invertedIndex[words[i]]);
                            if (searchResults.Count == 0)
                            {
                                return SearchResults.CreateEmpty(_service, query);
                            }
                        }

                        return new SearchResults(new MergeableSearchResultsCollection(searchResults), _service, query);
                    }
                }
            }

            return SearchResults.CreateEmpty(_service, query);
        }

        /// <summary>
        /// Rebuilds the search index. The old data will be replaced.
        /// </summary>
        private void BuildIndex()
        {
            lock (_indexLock)
            {
                // Build the forward index from all of the metadata.
                _forwardIndex.Clear();
                IndexContacts(_service.RawFriends);
                IndexPosts(_service.RawNewsFeed);
                IndexAlbums(_service.RawPhotoAlbums);

                // Build the inverted index from the forward index.
                _invertedIndex.Clear();
                foreach (object facebookObject in _forwardIndex.Keys)
                {
                    string[] words = _forwardIndex[facebookObject];
                    foreach (string word in words)
                    {
                        if (_invertedIndex.ContainsKey(word))
                        {
                            if (!_invertedIndex[word].Contains(facebookObject)) // :\
                            {
                                _invertedIndex[word].Add(facebookObject);
                            }
                        }
                        else
                        {
                            _invertedIndex.Add(word, new List<object>() { facebookObject });
                        }
                    }
                }
            }
        }

        private void UpdateInvertedIndex(object facebookObject, List<string> previousList)
        {

        }

        #region CollectionChanged handlers

        private void OnFriendsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnNewsFeedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnCommentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnAlbumsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnPhotosCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion CollectionChanged handlers

        public string[] GetWordsFromContact(FacebookContact contact)
        {
            if (contact == null)
            {
                return null;
            }

            return GetWords(contact.Name);
        }

        public string[] GetWordsFromPost(ActivityPost post)
        {
            if (post == null)
            {
                return null;
            }

            return GetWords(post.Message, (post.Actor != null) ? post.Actor.Name : "");
        }

        public string[] GetWordsFromComment(ActivityComment comment)
        {
            if (comment == null)
            {
                return null;
            }

            return GetWords(comment.Text, (comment.FromUser != null) ? comment.FromUser.Name : "");
        }

        public string[] GetWordsFromAlbum(FacebookPhotoAlbum album)
        {
            if (album == null)
            {
                return null;
            }

            return GetWords(album.Title, album.Description, album.Location, (album.Owner != null) ? album.Owner.Name : "");
        }

        public string[] GetWordsFromPhoto(FacebookPhoto photo)
        {
            return GetWordsFromPhoto(photo, photo.Album);
        }

        public string[] GetWordsFromPhoto(FacebookPhoto photo, FacebookPhotoAlbum album)
        {
            if (photo == null)
            {
                return null;
            }

            return GetWords(photo.Caption, (album.Owner != null) ? album.Owner.Name : "");
        }

        public string[] GetWordsFromTag(FacebookPhotoTag tag)
        {
            if (tag == null)
            {
                return null;
            }

            return GetWords(tag.Text, (tag.Contact != null) ? tag.Contact.Name : "");
        }

        public string[] GetWordsFromObject(object data)
        {
            if (data is FacebookContact)
            {
                return GetWordsFromContact((FacebookContact)data);
            }
            else if (data is ActivityPost)
            {
                return GetWordsFromPost((ActivityPost)data);
            }
            else if (data is ActivityComment)
            {
                return GetWordsFromComment((ActivityComment)data);
            }
            else if (data is FacebookPhotoAlbum)
            {
                return GetWordsFromAlbum((FacebookPhotoAlbum)data);
            }
            else if (data is FacebookPhoto)
            {
                return GetWordsFromPhoto((FacebookPhoto)data);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object[] GetRelevantFacebookObjects(object o)
        {
            List<object> objects = new List<object>();

            FacebookContact contact = o as FacebookContact;
            if (contact != null)
            {
                foreach (var post in _service.RawNewsFeed)
                {
                    if (post.ActorUserId == contact.UserId)
                    {
                        objects.Add(post);
                    }

                    foreach (var comment in post.Comments)
                    {
                        if (comment.FromUserId == contact.UserId)
                        {
                            objects.Add(comment);
                        }
                    }
                }

                foreach (var album in _service.RawPhotoAlbums)
                {
                    if (album.OwnerId == contact.UserId)
                    {
                        objects.Add(album);
                    }

                    foreach (var photo in album.Photos)
                    {
                        if (photo.OwnerId == contact.UserId)
                        {
                            objects.Add(photo);
                            continue;
                        }

                        foreach (var tag in photo.Tags)
                        {
                            if (tag.Contact != null && tag.Contact.UserId == contact.UserId)
                            {
                                objects.Add(photo);
                                goto next;
                            }
                        }

                    next: ;
                    }
                }
            }

            FacebookPhoto p = o as FacebookPhoto;
            if (p != null)
            {
                objects.Add(p.Album);

                if (p.Album.Owner != null)
                {
                    objects.Add(p.Album.Owner);
                }

                foreach (var tag in p.Tags)
                {
                    if (tag.Contact != null && !objects.Contains(tag.Contact))
                    {
                        objects.Add(tag.Contact);
                    }
                }
            }

            FacebookPhotoAlbum a = o as FacebookPhotoAlbum;
            if (a != null)
            {
                if (a.Owner != null)
                {
                    objects.Add(a.Owner);
                }

                foreach (var photo in a.Photos)
                {
                    objects.Add(photo);

                    foreach (var tag in photo.Tags)
                    {
                        if (tag.Contact != null && !objects.Contains(tag.Contact))
                        {
                            objects.Add(tag.Contact);
                        }
                    }
                }
            }

            ActivityPost activityPost = o as ActivityPost;
            if (activityPost != null)
            {
                if (activityPost.Actor != null)
                {
                    objects.Add(activityPost.Actor);
                }

                if (activityPost.Target != null)
                {
                    objects.Add(activityPost.Target);
                }

                foreach (var comment in activityPost.Comments)
                {
                    objects.Add(comment);
                }
            }

            ActivityComment activityComment = o as ActivityComment;
            if (activityComment != null)
            {
                if (activityComment.FromUser != null)
                {
                    objects.Add(activityComment.FromUser);
                }
            }

            objects.Sort(new SearchResultsComparer());
            return objects.ToArray();
        }

        private void IndexContacts(MergeableCollection<FacebookContact> friends)
        {
            lock (friends.SyncRoot)
            {
                foreach (FacebookContact friend in friends)
                {
                    string[] words = GetWordsFromContact(friend);
                    ReplaceForwardIndexItem(friend, words);
                }
            }
        }

        private void IndexPosts(MergeableCollection<ActivityPost> posts)
        {
            lock (posts.SyncRoot)
            {
                foreach (ActivityPost post in posts)
                {
                    string[] words = GetWordsFromPost(post);
                    ReplaceForwardIndexItem(post, words);
                    IndexComments(post.RawComments);
                }
            }
        }

        private void IndexComments(MergeableCollection<ActivityComment> comments)
        {
            lock (comments)
            {
                foreach (ActivityComment comment in comments)
                {
                    string[] words = GetWordsFromComment(comment);
                    ReplaceForwardIndexItem(comment, words);
                }
            }
        }

        private void IndexAlbums(MergeableCollection<FacebookPhotoAlbum> albums)
        {
            lock (albums.SyncRoot)
            {
                foreach (FacebookPhotoAlbum album in albums)
                {
                    string[] words = GetWordsFromAlbum(album);
                    ReplaceForwardIndexItem(album, words);
                    IndexPhotos(album);
                }
            }
        }

        private void IndexPhotos(FacebookPhotoAlbum album)
        {
            if (album.RawPhotos != null)
            {
                lock (album.RawPhotos.SyncRoot)
                {
                    foreach (FacebookPhoto photo in album.RawPhotos)
                    {
                        string[] words = GetWordsFromPhoto(photo, album);
                        ReplaceForwardIndexItem(photo, words);
                        IndexTags(photo);
                    }
                }
            }
        }

        private void IndexTags(FacebookPhoto photo)
        {
            if (photo.Tags != null)
            {
                foreach (FacebookPhotoTag tag in photo.Tags)
                {
                    string[] words = GetWordsFromTag(tag);
                    MergeForwardIndexItem(photo, words);
                }
            }
        }

        private void ReplaceForwardIndexItem(object key, string[] newValue)
        {
            newValue = StemWords(newValue);

            if (_forwardIndex.ContainsKey(key))
            {
                _forwardIndex.Remove(key);
            }

            if (newValue != null && newValue.Length > 0)
            {
                _forwardIndex.Add(key, newValue);
            }
        }

        private void MergeForwardIndexItem(object key, string[] newValue)
        {
            newValue = StemWords(newValue);

            if (newValue != null && newValue.Length > 0)
            {
                if (_forwardIndex.ContainsKey(key))
                {
                    string[] oldValue = _forwardIndex[key];
                    string[] mergedValues = new string[oldValue.Length + newValue.Length];
                    Array.Copy(oldValue, 0, mergedValues, 0, oldValue.Length);
                    Array.Copy(newValue, 0, mergedValues, oldValue.Length, newValue.Length);

                    _forwardIndex[key] = mergedValues;
                }
                else
                {
                    _forwardIndex.Add(key, newValue);
                }
            }
        }

        public static string[] GetWords(params string[] list)
        {
            List<string> words = new List<string>();
            if (list != null)
            {
                foreach (string item in list)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        string input = item.ToLower();
                        int idx = 0;

                        while (true)
                        {
                            Match match = s_wordRegex.Match(input, idx);
                            if (!match.Success)
                            {
                                break;
                            }

                            string word = input.Substring(match.Index, match.Length);
                            if (!words.Contains(word))
                            {
                                words.Add(word);
                            }

                            idx = match.Index + match.Length;
                        }
                    }
                }
            }

            return words.ToArray();
        }

        public static string[] StemWords(string[] words)
        {
            if (words == null)
            {
                return null;
            }

            List<string> stemmedWords = new List<string>(words.Length);
            Stemmer stemmer = new Stemmer();

            foreach (string word in words)
            {
                string stemmedWord = stemmer.Stem(word);
                if (!stemmedWords.Contains(stemmedWord))
                {
                    stemmedWords.Add(stemmedWord);
                }
            }

            return stemmedWords.ToArray();
        }

        private static string[] _commonWords = { 
            "a", 
            "an", 
            "and",
            "are",
            "at",
            "be",
            "but",
            "for",
            "from",
            "have",
            "he",          
            "her",
            "his", 
            "i",
            "is",
            "in",
            "it",
            "its",
            "it's",
            "i'm",
            "i've",
            "me",
            "my",
            "of",
            "on",
            "or",
            "our",
            "she",
            "the",
            "to",
            "that",
            "us",
            "was",
            "were",
            "with",
            "where",
            "you", 
            "your"};

        /// <remarks>
        /// Assumes lowercase input.
        /// </remarks>
        public string[] PruneCommonWords(string[] words)
        {
            List<string> prunedWords = new List<string>();

            foreach (string word in words)
            {
                if (!_commonWords.Contains(word))
                {
                    prunedWords.Add(word);
                }
            }

            return prunedWords.ToArray();
        }

        private void Intersect(List<object> first, List<object> second)
        {
            // todo: could do something more clever here. (though the sets we
            // are working with are expected to be reasonably small.)
            for (int i = 0; i < first.Count; i++)
            {
                if (!second.Contains(first[i]))
                {
                    first.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public class SearchResultsComparer : IComparer, IComparer<object>
    {
        #region IComparer Members

        private static DateTime? GetDateTimeFromObject(object o)
        {
            if (o is ActivityPost)
            {
                return ((ActivityPost)o).Created;
            }
            else if (o is ActivityComment)
            {
                return ((ActivityComment)o).Time;
            }
            else if (o is FacebookPhotoAlbum)
            {
                return ((FacebookPhotoAlbum)o).LastModified;
            }
            else if (o is FacebookPhoto)
            {
                return ((FacebookPhoto)o).Created;
            }

            return null;
        }

        public int Compare(object x, object y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null)
            {
                return -1;
            }
            else if (y == null)
            {
                return 1;
            }

            // Contacts go to the top of the list.
            if (x is FacebookContact && y is FacebookContact)
            {
                return ((FacebookContact)y).Name.CompareTo(((FacebookContact)x).Name);
            }
            else if (x is FacebookContact)
            {
                return -1;
            }
            else if (y is FacebookContact)
            {
                return 1;
            }

            // After contacts we sort based on time.
            DateTime firstTime = GetDateTimeFromObject(x) ?? default(DateTime);
            DateTime secondTime = GetDateTimeFromObject(y) ?? default(DateTime);
            return secondTime.CompareTo(firstTime);
        }

        #endregion

        #region IComparer<object> Members

        int IComparer<object>.Compare(object x, object y)
        {
            return ((IComparer)this).Compare(x, y);
        }

        #endregion
    }

    internal class MergeableSearchResultsCollection : MergeableCollection<object>
    {
        public MergeableSearchResultsCollection(IEnumerable<object> dataObjects)
            : base(dataObjects)
        {
        }
    }

    public class SearchResults : FacebookCollection<object>
    {
        private static IComparer s_comparer = new SearchResultsComparer();
        private ReadOnlyCollection<FacebookContact> _friends;
        private ReadOnlyCollection<ActivityPost> _posts;
        private ReadOnlyCollection<ActivityComment> _comments;
        private ReadOnlyCollection<FacebookPhotoAlbum> _albums;
        private ReadOnlyCollection<FacebookPhoto> _photos;

        internal SearchResults(MergeableCollection<object> rawCollection, FacebookService service, string searchText)
            : base(rawCollection, service)
        {
            this.SearchText = searchText;
        }

        internal static SearchResults CreateEmpty(FacebookService service, string searchText)
        {
            return new SearchResults(new MergeableSearchResultsCollection(null), service, searchText);
        }

        public string SearchText { get; private set; }

        public ReadOnlyCollection<FacebookContact> Friends
        {
            get 
            { 
                if (_friends == null)
                {
                    _friends = new ReadOnlyCollection<FacebookContact>(new List<FacebookContact>(this.OfType<FacebookContact>()));
                }

                return _friends;
            }
        }

        public ReadOnlyCollection<ActivityPost> Posts
        {
            get 
            {
                if (_posts == null)
                {
                    _posts = new ReadOnlyCollection<ActivityPost>(new List<ActivityPost>(this.OfType<ActivityPost>()));
                }

                return _posts;
            }
        }

        public ReadOnlyCollection<ActivityComment> Comments
        {
            get 
            {
                if (_comments == null)
                {
                    _comments = new ReadOnlyCollection<ActivityComment>(new List<ActivityComment>(this.OfType<ActivityComment>()));
                }

                return _comments;
            }
        }

        public ReadOnlyCollection<FacebookPhotoAlbum> Albums
        {
            get
            {
                if (_albums == null)
                {
                    _albums = new ReadOnlyCollection<FacebookPhotoAlbum>(new List<FacebookPhotoAlbum>(this.OfType<FacebookPhotoAlbum>()));
                }

                return _albums;
            }
        }

        public ReadOnlyCollection<FacebookPhoto> Photos
        {
            get
            {
                if (_photos == null)
                {
                    _photos = new ReadOnlyCollection<FacebookPhoto>(new List<FacebookPhoto>(this.OfType<FacebookPhoto>()));
                }

                return _photos;
            }
        }
    }
}
