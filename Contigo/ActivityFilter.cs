namespace Contigo
{
    using System.ComponentModel;
    using Standard;
    using System;

    public class ActivityFilter : IFacebookObject, INotifyPropertyChanged, IMergeable<ActivityFilter>, IComparable<ActivityFilter>
    {
        internal ActivityFilter(FacebookService service)
        {
            Verify.IsNotNull(service, "service");
            SourceService = service;
        }

        private SmallString _key { get; set; }
        private SmallString _name { get; set; }
        private SmallString _filterType { get; set; }
        private int _rank;
        private bool _isVisible;

        // Not raising property change notifications because this should never change.
        public string Key
        {
            get { return _key.GetString(); }
            internal set { _key = new SmallString(value); }
        }

        public string Name
        {
            get { return _name.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (_name != newValue)
                {
                    _name = newValue;
                    _NotifyPropertyChanged("Name");
                }
            }
        }

        public string FilterType
        {
            get { return _filterType.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (_filterType != newValue)
                {
                    _filterType = newValue;
                    _NotifyPropertyChanged("FilterType");
                }
            }
        }

        public int Rank
        {
            get { return _rank; }
            internal set
            {
                if (_rank != value)
                {
                    _rank = value;
                    _NotifyPropertyChanged("Rank");
                }
            }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
            internal set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    _NotifyPropertyChanged("IsVisible");
                }
            }
        }

        // Let Merge raise the notifications because of the SafeMerge functionality.
        public FacebookImage Icon { get; internal set; }

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
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IMergeable<ActivityFilter> Members

        string IMergeable<ActivityFilter>.FKID
        {
            get 
            {
                Assert.IsNeitherNullNorEmpty(Key);
                return Key; 
            }
        }

        void IMergeable<ActivityFilter>.Merge(ActivityFilter other)
        {
            Verify.IsNotNull(other, "other");
            if (other.Key != this.Key)
            {
                throw new InvalidOperationException("Cannot merge filters with different Keys.");
            }

            Name = other.Name;
            FilterType = other.FilterType;
            Rank = other.Rank;
            IsVisible = other.IsVisible;
            if (Icon != null)
            {
                if (Icon.SafeMerge(other.Icon))
                {
                    Icon = Icon.Clone();
                }
            }
            else
            {
                Icon = other.Icon;
            }
            _NotifyPropertyChanged("Icon");
        }

        #endregion

        #region IEquatable<ActivityFilter> Members

        public bool Equals(ActivityFilter other)
        {
            if (other == null)
            {
                return false;
            }

            return _key.Equals(other._key);
        }

        #endregion

        #region IComparable<ActivityFilter> Members

        public int CompareTo(ActivityFilter other)
        {
            if (other == null)
            {
                return 1;
            }

            return Rank.CompareTo(other.Rank);
        }

        #endregion
    }
}
