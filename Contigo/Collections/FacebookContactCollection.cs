
namespace Contigo
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using Standard;

    public enum ContactSortOrder
    {
        AscendingByDisplayName,
        DescendingByDisplayName,
        AscendingByLastName,
        DescendingByLastName,
        AscendingByBirthday,
        DescendingByBirthday,
        AscendingByRecentActivity,
        DescendingByRecentActivity,
        AscendingByInterestLevel,
        DescendingByInterestLevel,
        AscendingByOnlinePresence,
        DescendingByOnlinePresence,
    }

    public class FacebookContactCollection : FacebookCollection<FacebookContact>
    {
        private readonly MergeableCollection<FacebookContact> _filteredCollection;
        private readonly MergeableCollection<FacebookContact> _rawCollection;
        private readonly Dictionary<FacebookContact, bool> _onlineMap;

        private static bool _IsOnline(FacebookContact contact)
        {
            return contact.OnlinePresence == OnlinePresence.Active
                || contact.OnlinePresence == OnlinePresence.Idle;
        }

        internal FacebookContactCollection(MergeableCollection<FacebookContact> sourceCollection, FacebookService service, bool includeOnlyOnlineContacts)
            : base(sourceCollection, service)
        {
            if (includeOnlyOnlineContacts)
            {
                _onlineMap = new Dictionary<FacebookContact, bool>();
                _rawCollection = sourceCollection;
                _filteredCollection = new MergeableCollection<FacebookContact>(from buddy in sourceCollection where _IsOnline(buddy) select buddy, true);
                _filteredCollection.CustomComparison = FacebookContact.GetComparison(ContactSortOrder.AscendingByOnlinePresence);
                base.ReplaceSourceCollection(_filteredCollection);

                foreach (var buddy in sourceCollection)
                {
                    _onlineMap.Add(buddy, _IsOnline(buddy));
                    buddy.PropertyChanged += _OnContactPropertyChanged;
                }

                sourceCollection.CollectionChanged += _OnRawCollectionChanged;
            }
        }

        private void _OnContactPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "OnlinePresence")
            {
                var contact = (FacebookContact)sender;
                if (_IsOnline(contact) != _onlineMap[contact])
                {
                    _onlineMap[contact] = _IsOnline(contact);
                    _UpdateCollection();
                }
            }
        }

        void _OnRawCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            bool needsUpdate = false;
            if (e.NewItems != null)
            {
                needsUpdate = (from FacebookContact contact in e.NewItems where _IsOnline(contact) select contact).Any();
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var contact in _onlineMap.Keys)
                {
                    contact.PropertyChanged -= _OnContactPropertyChanged;
                    needsUpdate |= _IsOnline(contact);
                }

                _onlineMap.Clear();
            }

            if (e.OldItems != null)
            {
                foreach (FacebookContact contact in e.OldItems)
                {
                    if (_onlineMap.ContainsKey(contact))
                    {
                        _onlineMap.Remove(contact);
                        contact.PropertyChanged -= _OnContactPropertyChanged;
                    }

                    needsUpdate = needsUpdate || _IsOnline(contact);
                }
            }

            if (needsUpdate)
            {
                _UpdateCollection();
            }
        }

        private void _UpdateCollection()
        {
            _filteredCollection.Merge(from contact in _rawCollection where _IsOnline(contact) select contact, false);

            foreach (var contact in _rawCollection)
            {
                if (!_onlineMap.ContainsKey(contact))
                {
                    _onlineMap.Add(contact, _IsOnline(contact));
                    contact.PropertyChanged += _OnContactPropertyChanged;
                }
            }
        }
    }
}
