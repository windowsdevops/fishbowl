namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using Standard;

    public enum OnlinePresence
    {
        Unknown,
        Active,
        Idle,
        Offline,
    }

    public class Location
    {
        private SmallString _city;
        private SmallString _state;
        private SmallString _country;

        public string City
        {
            get { return _city.GetString(); }
            internal set { _city = new SmallString(value); }
        }

        public string State
        {
            get { return _state.GetString(); }
            internal set { _state = new SmallString(value); }
        }

        public string Country
        {
            get { return _country.GetString(); }
            internal set { _country = new SmallString(value); }
        }
        
        public int? ZipCode { get; internal set; }

        public bool IsEmpty
        {
            get
            {
                return _city == default(SmallString)
                    && _state == default(SmallString)
                    && _country == default(SmallString)
                    && ZipCode == null;
            }
        }
    }

    public class EducationInfo
    {
        private SmallString _name;
        private SmallString _degree;
        private SmallString _concentrations;

        public int? Year { get; internal set; }
        public string Name
        {
            get { return _name.GetString(); }
            internal set { _name = new SmallString(value); }
        }

        public string Concentrations 
        {
            get { return _concentrations.GetString(); }
            internal set { _concentrations = new SmallString(value); } 
        }

        public string Degree
        { 
            get { return _degree.GetString(); }
            internal set { _degree = new SmallString(value); }
        }
    }

    public class WorkInfo
    {
        private SmallString _name;
        private SmallString _description;
        private SmallString _position;
        private SmallString _startDate;
        private SmallString _endDate;

        public Location Location { get; internal set; }

        public string CompanyName
        {
            get { return _name.GetString(); }
            internal set { _name = new SmallString(value); }
        }

        public string Description
        {
            get { return _description.GetString(); }
            internal set { _description = new SmallString(value); }
        }

        public string Position
        {
            get { return _position.GetString(); }
            internal set { _position = new SmallString(value); }
        }

        public string StartDate
        {
            get { return _startDate.GetString(); }
            internal set { _startDate = new SmallString(value); }
        }

        public string EndDate
        {
            get { return _endDate.GetString(); }
            internal set { _endDate = new SmallString(value); }
        }
    }

    public class HighSchoolInfo
    {
        private SmallString _name;
        private SmallString _name2;

        public string Name
        {
            get { return _name.GetString(); }
            internal set { _name = new SmallString(value); }
        }

        public string Name2
        {
            get { return _name2.GetString(); }
            internal set { _name2 = new SmallString(value); }
        }
        public int? GraduationYear { get; internal set; }
        //internal string Id { get; set; }
        //internal string Id2 { get; set; }
    }

    /* TODO
    public enum AffiliationType
    {
        College,
        HighSchool,
        Work,
        Region,
    }
    
    public class Affiliation
    {
        public AffiliationType AffiliationType { get; internal set; }
        public int Year { get; internal set; }
        public string Name { get; internal set; }
        internal string NetworkId { get; set; }
        public string Status { get; internal set; }
    }

    public enum RelationType
    {
        Mother,
        Father,
        Sibling,
        Sister,
        Brother,
        Child,
        Son,
        Daughter,
        MutualFriend,
    }

    public class Relation
    {
        public FacebookContact Contact { get; internal set; }
        internal string UserId { set; get; }
        public RelationType RelationshipType { get; internal set; }
        public string Birthday { get; internal set; }
    }
    */

    public sealed class FacebookContact : IFacebookObject, INotifyPropertyChanged, IMergeable<FacebookContact>, IComparable<FacebookContact>
    {
        #region Sort Delegates

        private static readonly OnlinePresence[] _PresenceSortOrder = new OnlinePresence[] { OnlinePresence.Active, OnlinePresence.Idle, OnlinePresence.Offline, OnlinePresence.Unknown };
        private static readonly OnlinePresence[] _ReversePresenceSortOrder = new OnlinePresence[] { OnlinePresence.Offline, OnlinePresence.Idle, OnlinePresence.Active, OnlinePresence.Unknown };

        private static readonly Comparison<FacebookContact> _defaultComparison = (contact1, contact2) => contact1.CompareTo(contact2);
        private static readonly Comparison<FacebookContact> _ascendingByDisplayNameComparison = (contact1, contact2) => contact1._lowerNameSmallString.CompareTo(contact2._lowerNameSmallString);
        private static readonly Comparison<FacebookContact> _ascendingByLastNameComparison = _defaultComparison;
        private static readonly Comparison<FacebookContact> _ascendingByBirthdayComparison = (contact1, contact2) =>
        {
            if (contact1.NextBirthday == contact2.NextBirthday)
            {
                return _defaultComparison(contact1, contact2);
            }

            TimeSpan span1 = contact1.NextBirthday - DateTime.Now.Date;
            TimeSpan span2 = contact2.NextBirthday - DateTime.Now.Date;
            if (span1.Ticks < 0)
            {
                return span2.Ticks < 0 ? 0 : 1;
            }
            else if (span2.Ticks < 0)
            {
                return -1;
            }

            return span1.CompareTo(span2);
        };
        private static readonly Comparison<FacebookContact> _ascendingByRecentActivityComparison = (contact1, contact2) =>
        {
            int ret = 0;
            if (contact1.StatusMessage == null)
            {
                ret = contact2.StatusMessage == null ? 0 : 1;
            }
            else if (contact2.StatusMessage == null)
            {
                ret = -1;
            }
            else
            {
                ret = contact1.StatusMessage.Created.CompareTo(contact2.StatusMessage.Created);
            }

            if (ret == 0)
            {
                ret = _defaultComparison(contact1, contact2);
            }

            return ret;
        };
        private static readonly Comparison<FacebookContact> _ascendingByInterestLevelComparison = (contact1, contact2) =>
        {
            int ret = contact1.InterestLevel.CompareTo(contact2.InterestLevel);
            if (ret == 0)
            {
                ret = _defaultComparison(contact1, contact2);
            }
            return ret;
        };
        private static readonly Comparison<FacebookContact> _ascendingByOnlinePresenceComparison = (contact1, contact2) =>
        {
            if (contact1.OnlinePresence == contact2.OnlinePresence)
            {
                return _defaultComparison(contact1, contact2);
            }

            Assert.AreEqual(Enum.GetValues(typeof(OnlinePresence)).Length, _PresenceSortOrder.Length);
            foreach (var presenceValue in _PresenceSortOrder)
            {
                if (presenceValue == contact1.OnlinePresence)
                {
                    return -1;
                }

                if (presenceValue == contact2.OnlinePresence)
                {
                    return 1;
                }
            }

            // Bad online presence?
            Assert.Fail();
            return 0;
        };

        private static readonly Comparison<FacebookContact> _descendingByDisplayNameComparison = (contact1, contact2) => contact2._lowerNameSmallString.CompareTo(contact1._lowerNameSmallString);
        private static readonly Comparison<FacebookContact> _descendingByLastNameComparison = (contact1, contact2) => _defaultComparison(contact2, contact1);
        // Don't actually want the inverse of _ascendingByBirthdayComparison.
        // Anyone without a birthday will go to the back of the list in either ascending or descending.
        private static readonly Comparison<FacebookContact> _descendingByBirthdayComparison = (contact1, contact2) =>
        {
            if (contact1.NextBirthday == contact2.NextBirthday)
            {
                return _defaultComparison(contact1, contact2);
            }

            TimeSpan span1 = contact1.NextBirthday - DateTime.Now.AddDays(1);
            TimeSpan span2 = contact2.NextBirthday - DateTime.Now.AddDays(1);
            if (span1.Ticks < 0)
            {
                return span2.Ticks < 0 ? 0 : -1;
            }
            else if (span2.Ticks < 0)
            {
                return 1;
            }

            return span1.CompareTo(span2);
        };
        private static readonly Comparison<FacebookContact> _descendingByRecentActivityComparison = (contact1, contact2) =>
        {
            int ret = 0;
            if (contact1.StatusMessage == null)
            {
                ret = contact2.StatusMessage == null ? 0 : 1;
            }
            else if (contact2.StatusMessage == null)
            {
                ret = -1;
            }
            else
            {
                ret = contact2.StatusMessage.Created.CompareTo(contact1.StatusMessage.Created);
            }

            if (ret == 0)
            {
                ret = _defaultComparison(contact1, contact2);
            }

            return ret;
        };
        private static readonly Comparison<FacebookContact> _descendingByInterestLevelComparison = (contact1, contact2) =>
        {
            int ret = contact2.InterestLevel.CompareTo(contact1.InterestLevel);
            if (ret == 0)
            {
                ret = _defaultComparison(contact1, contact2);
            }
            return ret;
        };
        private static readonly Comparison<FacebookContact> _descendingByOnlinePresenceComparison = (contact1, contact2) =>
        {
            if (contact1.OnlinePresence == contact2.OnlinePresence)
            {
                return _defaultComparison(contact1, contact2);
            }

            Assert.AreEqual(Enum.GetValues(typeof(OnlinePresence)).Length, _ReversePresenceSortOrder.Length);
            foreach (var presenceValue in _PresenceSortOrder)
            {
                if (presenceValue == contact1.OnlinePresence)
                {
                    return -1;
                }

                if (presenceValue == contact2.OnlinePresence)
                {
                    return 1;
                }
            }

            // Bad online presence?
            Assert.Fail();
            return 0;
        };


        internal static Comparison<FacebookContact> GetComparison(ContactSortOrder value)
        {
            switch (value)
            {
                case ContactSortOrder.AscendingByBirthday: return _ascendingByBirthdayComparison;
                case ContactSortOrder.AscendingByDisplayName: return _ascendingByDisplayNameComparison;
                case ContactSortOrder.AscendingByInterestLevel: return _ascendingByInterestLevelComparison;
                case ContactSortOrder.AscendingByLastName: return _ascendingByLastNameComparison;
                case ContactSortOrder.AscendingByOnlinePresence: return _ascendingByOnlinePresenceComparison;
                case ContactSortOrder.AscendingByRecentActivity: return _ascendingByRecentActivityComparison;
                case ContactSortOrder.DescendingByBirthday: return _descendingByBirthdayComparison;
                case ContactSortOrder.DescendingByDisplayName: return _descendingByDisplayNameComparison;
                case ContactSortOrder.DescendingByInterestLevel: return _descendingByInterestLevelComparison;
                case ContactSortOrder.DescendingByLastName: return _descendingByLastNameComparison;
                case ContactSortOrder.DescendingByOnlinePresence: return _descendingByOnlinePresenceComparison;
                case ContactSortOrder.DescendingByRecentActivity: return _descendingByRecentActivityComparison;
                default: Assert.Fail(); return _defaultComparison;
            }
        }

        #endregion

        internal static readonly double DefaultInterestLevel = .5;

        private ActivityPostCollection _recentActivity;
        private FacebookPhotoAlbumCollection _photoAlbums;

        // Facebook data fields
        private SmallString _aboutMe;
        private SmallString _activities;
        //private List<Affiliation> _affiliations;
        private SmallString _birthday;
        private SmallString _msBirthday;
        private SmallString _books;
        private SmallString _firstName;
        private SmallString _interests;
        private SmallString _lastName;
        private SmallString _name;
        private SmallString _quotes;
        private SmallUri _profileUri;
        private ActivityPost _statusMessage;
        private OnlinePresence _onlinePresence;
        private DateTime _profileUpdateTime;
        private DateTime _nextBirthdayDate;
        private Location _currentLocation;
        private Location _hometownLocation;
        private HighSchoolInfo _hsInfo;
        private SmallString _movies;
        private SmallString _music;
        private SmallString _relationshipStatus;
        private SmallString _religion;
        private SmallString _sex;
        private SmallString _tv;
        private SmallString _website;

        private bool _hasData;
        private double? _nullableInterestLevel;
        private DateTime _lastActivitySync = DateTime.MinValue;

        private SmallString _lowerNameSmallString;
        private SmallString _sortStringSmallString;

        internal FacebookContact(FacebookService service)
        {
            SourceService = service;
            RawRecentActivity = new MergeableCollection<ActivityPost>(null);
        }

        public DateTime ProfileUpdateTime
        {
            get { return _profileUpdateTime; }
            internal set
            {
                if (_profileUpdateTime != value)
                {
                    _profileUpdateTime = value;
                    _NotifyPropertyChanged("ProfileUpdateTime");
                }
            }
        }

        public Location CurrentLocation
        {
            get { return _currentLocation; }
            internal set 
            {
                _currentLocation = value;
                _NotifyPropertyChanged("CurrentLocation");
            }
        }

        public Location Hometown
        {
            get { return _hometownLocation; }
            internal set
            {
                _hometownLocation = value;
                _NotifyPropertyChanged("Hometown");
            }
        }

        public HighSchoolInfo HighSchoolInfo
        {
            get { return _hsInfo; }
            internal set
            {
                _hsInfo = value;
                _NotifyPropertyChanged("HighSchoolInfo");
            }
        }

        public ActivityPost StatusMessage 
        {
            get { return _statusMessage; }
            internal set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    _NotifyPropertyChanged("StatusMessage");
                }
            }
        }

        // Not a SmallString because this property is frequently accessed for identity operations.
        // Does not raise change notifications because it should never be changing.
        public string UserId { get; internal set; }

        public string AboutMe
        {
            get { return _aboutMe.GetString(); }
            internal set
            { 
                var newValue = new SmallString(value);
                if (newValue != _aboutMe)
                {
                    _aboutMe = newValue;
                    _NotifyPropertyChanged("AboutMe");
                }
            }
        }

        public string Activities
        {
            get { return _activities.GetString(); }
            internal set
            { 
                var newValue = new SmallString(value);
                if (newValue != _activities)
                {
                    _activities = newValue;
                    _NotifyPropertyChanged("Activities");
                }
            }
        }

        public string Birthday
        {
            get { return _birthday.GetString(); }
            internal set
            { 
                var newValue = new SmallString(value);
                if (newValue != _birthday)
                {
                    _birthday = newValue;
                    _NotifyPropertyChanged("Birthday");
                }
            }
        }

        internal string MachineSafeBirthday
        {
            get { return _msBirthday.GetString(); }
            set
            {
                var newValue = new SmallString(value);
                if (newValue != _msBirthday)
                {
                    _msBirthday = newValue;

                    try
                    {
                        // Facebook gives back an American version of this string regardless of culture.
                        // It sometimes returns a year, but not always.
                        string[] date = value.Split('/');

                        // If it's not MM/DD or MM/DD/YYYY then I don't know what it's giving me.
                        if (date.Length >= 2 && date.Length <= 3)
                        {
                            int month;
                            int day;
                            if (int.TryParse(date[0], out month) && month >= 1 && month <= 12
                                // Not bothering additional verification of the correct number of days per month.  Assuming FB has some sanitization...
                                && int.TryParse(date[1], out day) && day >= 1 && day <= 31)
                            {
                                if (month == 2 && day == 29)
                                {
                                    // For the sake of this, treat Feb29 as Mar1
                                    month = 3;
                                    day = 1;
                                }
                                var birthday = new DateTime(DateTime.Now.Year, month, day);
                                if (birthday < DateTime.Now)
                                {
                                    birthday = birthday.AddYears(1);
                                }

                                NextBirthday = birthday;
                            }
                        }
                    }
                    // If we get bad data for a date then just clear the value.
                    catch
                    {
                        NextBirthday = default(DateTime);
                        throw;
                    }
                }
            }
        }

        public DateTime NextBirthday
        {
            get { return _nextBirthdayDate; }
            private set
            {
                if (value != _nextBirthdayDate)
                {
                    _nextBirthdayDate = value;
                    _NotifyPropertyChanged("NextBirthday");
                }
            }
        }

        public string Books
        {
            get { return _books.GetString(); }
            internal set
            { 
                var newValue = new SmallString(value);
                if (newValue != _books)
                {
                    _books = newValue;
                    _NotifyPropertyChanged("Books");
                }
            }
        }

        public string FirstName
        {
            get { return _firstName.GetString(); }
            internal set
            { 
                var newValue = new SmallString(value);
                if (newValue != _firstName)
                {
                    _firstName = newValue;
                    _NotifyPropertyChanged("FirstName");
                    _UpdateSortStrings();
                }
            }
        }

        public string LastName
        {
            get { return _lastName.GetString(); }
            internal set
            { 
                var newValue = new SmallString(value);
                if (newValue != _lastName)
                {
                    _lastName = newValue;
                    _NotifyPropertyChanged("LastName");
                    _UpdateSortStrings();
                }
            }
        }

        public string Interests
        {
            get { return _interests.GetString(); }
            internal set
            { 
                var newValue = new SmallString(value);
                if (newValue != _interests)
                {
                    _interests = newValue;
                    _NotifyPropertyChanged("Interests");
                }
            }
        }

        public string Name
        {
            get { return _name.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _name)
                {
                    _name = newValue;
                    _NotifyPropertyChanged("Name");
                    _UpdateSortStrings();
                }
            }
        }

        public string Quotes
        {
            get { return _quotes.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _quotes)
                {
                    _quotes = newValue;
                    _NotifyPropertyChanged("Quotes");
                }
            }
        }

        public string Movies
        {
            get { return _movies.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _movies)
                {
                    _movies = newValue;
                    _NotifyPropertyChanged("Movies");
                }
            }
        }

        public string Music
        {
            get { return _music.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _music)
                {
                    _music = newValue;
                    _NotifyPropertyChanged("Music");
                }
            }
        }

        public string TV
        {
            get { return _tv.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _tv)
                {
                    _tv = newValue;
                    _NotifyPropertyChanged("TV");
                }
            }
        }

        public string Religion
        {
            get { return _religion.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _religion)
                {
                    _religion = newValue;
                    _NotifyPropertyChanged("Religion");
                }
            }
        }

        public string RelationshipStatus
        {
            get { return _relationshipStatus.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _relationshipStatus)
                {
                    _relationshipStatus = newValue;
                    _NotifyPropertyChanged("RelationshipStatus");
                }
            }
        }

        public string Sex
        {
            get { return _sex.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _sex)
                {
                    _sex = newValue;
                    _NotifyPropertyChanged("Sex");
                }
            }
        }

        public string Website
        {
            get { return _website.GetString(); }
            internal set
            {
                var newValue = new SmallString(value);
                if (newValue != _website)
                {
                    _website = newValue;
                    _NotifyPropertyChanged("Website");
                }
            }
        }

        public Uri ProfileUri
        {
            get { return _profileUri.GetUri(); }
            internal set
            {
                var newValue = new SmallUri(value);
                if (newValue != _profileUri)
                {
                    _profileUri = newValue;
                    _NotifyPropertyChanged("ProfileUri");
                }
            }
        }

        public FacebookImage Image
        {
            get; 
            internal set; 
        }

        public OnlinePresence OnlinePresence
        {
            get { return _onlinePresence; }
            internal set
            {
                if (_onlinePresence != value)
                {
                    _onlinePresence = value;
                    _NotifyPropertyChanged("OnlinePresence");
                }
            }
        }

        public bool HasData
        {
            get { return _hasData; }
            set
            {
                if (value != _hasData)
                {
                    _hasData = value;
                    _NotifyPropertyChanged("HasData");
                }
            }
        }
        
        public FacebookPhotoAlbumCollection PhotoAlbums
        {
            get
            {
                if (_photoAlbums == null)
                {
                    _photoAlbums = new FacebookPhotoAlbumCollection(SourceService.RawPhotoAlbums, SourceService, this);
                }

                return _photoAlbums;
            }
        }

        internal MergeableCollection<ActivityPost> RawRecentActivity { get; private set; }

        public ActivityPostCollection RecentActivity
        {
            get
            {
                if (_recentActivity == null)
                {
                    _recentActivity = new ActivityPostCollection(RawRecentActivity, SourceService, false);
                }

                if (DateTime.Now - _lastActivitySync > TimeSpan.FromMinutes(5))
                {
                    UpdateRecentActivity();
                }

                return _recentActivity;
            }
        }

        internal void UpdateRecentActivity()
        {
            SourceService.GetActivityPostsByUserAsync(UserId, _OnGetRecentActivityCompleted);
        }

        private void _OnGetRecentActivityCompleted(object sender, AsyncCompletedEventArgs args)
        {
            var posts = (IEnumerable<ActivityPost>)args.UserState;
            RawRecentActivity.Merge(posts, false);
        }

        private void _NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void _UpdateSortStrings()
        {
            _lowerNameSmallString = new SmallString(Name.ToLower(CultureInfo.CurrentCulture));

            string sortName = (LastName.ToLower(CultureInfo.CurrentCulture) + " " + FirstName.ToLower(CultureInfo.CurrentCulture)).Trim();
            if (string.IsNullOrEmpty(sortName))
            {
                _sortStringSmallString = _lowerNameSmallString;
            }
            else
            {
                _sortStringSmallString = new SmallString(sortName);
            }
        }

        /// <summary>
        /// A percentage value to indicate how much information we should pull for a person.
        /// </summary>
        public double InterestLevel
        {
            get { return _nullableInterestLevel ?? DefaultInterestLevel; }
            set
            {
                double boundedValue = Math.Min(1, Math.Max(value, 0));
                if (_nullableInterestLevel != boundedValue)
                {
                    _nullableInterestLevel = boundedValue;
                    SourceService.TagAsInteresting(this);
                    _NotifyPropertyChanged("InterestLevel");
                }
            }
        }

        public IList<EducationInfo> EducationHistory { get; internal set; }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region System.Object overrides

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FacebookContact);
        }

        public override int GetHashCode()
        {
            if (UserId == null)
            {
                return 0;
            }
            return UserId.GetHashCode();
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

        #region IMergeable<FacebookContact> Members

        string IMergeable<FacebookContact>.FKID { get { return UserId; } }

        void IMergeable<FacebookContact>.Merge(FacebookContact other) { Merge(other); }

        internal void Merge(FacebookContact other)
        {
            if (other == null)
            {
                UserId = null;
                return;
            }

            if (object.ReferenceEquals(this, other))
            {
                return;
            }

            // Special case the empty MeContact.
            if (!string.IsNullOrEmpty(UserId))
            {
                if (UserId != other.UserId)
                {
                    throw new InvalidOperationException("Can't merge two different contacts.");
                }
            }
            else
            {
                UserId = other.UserId;
            }

            AboutMe = other.AboutMe;
            Activities = other.Activities;
            Birthday = other.Birthday;
            Books = other.Books;
            CurrentLocation = other.CurrentLocation;
            Hometown = other.Hometown;
            HighSchoolInfo = other.HighSchoolInfo;
            FirstName = other.FirstName;
            HasData = other.HasData;
            MergeImage(other.Image);
            Interests = other.Interests;
            LastName = other.LastName;
            MachineSafeBirthday = other.MachineSafeBirthday;
            Name = other.Name;
            OnlinePresence = other.OnlinePresence;
            // Photos gets merged independent of the contact.
            ProfileUpdateTime = other.ProfileUpdateTime;
            ProfileUri = other.ProfileUri;
            Quotes = other.Quotes;
            StatusMessage = other.StatusMessage;
            Movies = other.Movies;
            Music = other.Music;
            RelationshipStatus = other.RelationshipStatus;
            Religion = other.Religion;
            Sex = other.Sex;
            TV = other.TV;
            Website = other.Website;

            // Only merge InterestLevel if it's been explicitly set.
            if (other._nullableInterestLevel.HasValue)
            {
                this.InterestLevel = other.InterestLevel;
            }
        }

        internal bool MergeImage(FacebookImage otherImage)
        {
            bool modified = false;
            if (Image != null)
            {
                if (Image.SafeMerge(otherImage))
                {
                    Image = Image.Clone();
                    modified = true;
                }
            }
            else
            {
                Image = otherImage;
                modified = true;
            }
            _NotifyPropertyChanged("Image");

            return modified;
        }

        #endregion

        #region IEquatable<FacebookContact> Members

        public bool Equals(FacebookContact other)
        {
            if (other == null)
            {
                return false;
            }

            return other.UserId == UserId;
        }

        #endregion

        #region IComparable<FacebookContact> Members

        public int CompareTo(FacebookContact other)
        {
            if (other == null)
            {
                return 1;
            }

            return _sortStringSmallString.CompareTo(other._sortStringSmallString);
        }

        #endregion

    }
}