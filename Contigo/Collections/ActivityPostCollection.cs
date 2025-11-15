namespace Contigo
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using Standard;

    public class ActivityPostCollection : FacebookCollection<ActivityPost>
    {
        private static bool _IsInteresting(FacebookContact contact)
        {
            return contact.InterestLevel > .1;
        }

        private readonly MergeableCollection<ActivityPost> _filteredCollection;
        private readonly MergeableCollection<ActivityPost> _rawCollection;
        private readonly Dictionary<FacebookContact, bool> _interestMap; 

        internal ActivityPostCollection(MergeableCollection<ActivityPost> sourceCollection, FacebookService service, bool filterable)
            : base(sourceCollection, service)
        {
            if (filterable)
            {
                _interestMap = new Dictionary<FacebookContact, bool>();
                _rawCollection = sourceCollection;
                _filteredCollection = new MergeableCollection<ActivityPost>(from post in sourceCollection where _IsInteresting(post.Actor) select post, false);
                base.ReplaceSourceCollection(_filteredCollection);

                foreach (var contact in from p in sourceCollection select p.Actor)
                {
                    _interestMap.Add(contact, _IsInteresting(contact));
                    contact.PropertyChanged += _OnContactPropertyChanged;
                }

                sourceCollection.CollectionChanged += _OnRawCollectionChanged;
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

        void _OnRawCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            bool needsUpdate = false;
            if (e.NewItems != null)
            {
                needsUpdate = (from ActivityPost post in e.NewItems where _IsInteresting(post.Actor) select post).Any();
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var contact in _interestMap.Keys)
                {
                    contact.PropertyChanged -= _OnContactPropertyChanged;
                    needsUpdate |= _IsInteresting(contact);
                }

                _interestMap.Clear();
            }

            if (e.OldItems != null)
            {
                foreach (var contact in from ActivityPost post in e.OldItems select post.Actor)
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
            _filteredCollection.Merge(from post in _rawCollection where _IsInteresting(post.Actor) select post, false);

            foreach (var contact in from p in _rawCollection select p.Actor)
            {
                if (!_interestMap.ContainsKey(contact))
                {
                    _interestMap.Add(contact, _IsInteresting(contact));
                    contact.PropertyChanged += _OnContactPropertyChanged;
                }
            }

        }
    }
}