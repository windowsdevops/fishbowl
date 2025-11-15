//-----------------------------------------------------------------------
// <copyright file="ViewManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The main class to which UI Pages and Controls are bound.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using System.Windows.Threading;
    using ClientManager.Data;
    using Contigo;
    using Standard;

    public enum ViewPage
    {
        Newsfeed,
        Friends,
        Profile,
        Photos,
    }

    /// <summary>
    /// ViewManager provides a UI friendly object model and a navigation system for FacebookData.
    /// </summary>
    public class ViewManager : INotifyPropertyChanged
    {
        // Organizing to see whether I can split this from the ViewManager class...
        #region Navigation logic 

        public static object GetNavigationContent(DependencyObject obj)
        {
            return (object)obj.GetValue(NavigationContentProperty);
        }

        public static void SetNavigationContent(DependencyObject obj, object value)
        {
            obj.SetValue(NavigationContentProperty, value);
        }

        // Using a DependencyProperty as the backing store for NavigationContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NavigationContentProperty = DependencyProperty.RegisterAttached(
            "NavigationContent",
            typeof(object),
            typeof(ViewManager),
            new UIPropertyMetadata(null));

        public void NavigateToContent(object sender)
        {
            object content = GetNavigationContent((DependencyObject)sender);
            Assert.IsNotNull(content);

            if (content != null)
            {
                NavigationCommands.NavigateToContentCommand.Execute(content);
            }
        }

        private Navigator _currentNavigatorStore;
        /// <summary>Gets or sets the current navigator.</summary>
        public Navigator CurrentNavigator
        {
            get { return _currentNavigatorStore; }
            set
            {
                _currentNavigatorStore = value;
                _NotifyPropertyChanged("CurrentNavigator");

                Navigator root = value;
                while (root.Parent != null)
                {
                    root = root.Parent;
                }

                if (root != CurrentRootNavigator)
                {
                    CurrentRootNavigator = root;
                }
            }
        }

        private Navigator _currentRootNavigatorStore;
        public Navigator CurrentRootNavigator
        {
            get { return _currentRootNavigatorStore; }
            private set
            {
                _currentRootNavigatorStore = value;
                _NotifyPropertyChanged("CurrentRootNavigator");
            }
        }

        /// <summary>
        /// Navigate to object on a new navigation. Content state information for this navigation should be entered in the journal
        /// When navigating by command, the command should specify what mode of navigation it initiates - next, previous, etc.
        /// </summary>
        /// <param name="navigator">The navigator to navigate.</param>
        /// <param name="mode">The ScePhotoNavigationMode to use when navigating.</param>
        public void NavigateByCommand(Navigator navigator)
        {
            this._NavigateNew(navigator, NavigationMode.New);
        }

        /// <summary>
        /// Perform new navigation.
        /// </summary>
        /// <param name="navigator">Navigator which is the target of new navigation.</param>
        /// <param name="mode">Navigation mode, whether back, forward, etc.</param>
        private void _NavigateNew(Navigator navigator, NavigationMode mode)
        {
            if (navigator != null)
            {
                ViewManagerNavigatedEventArgs args = null;
                object content = _GetNavigatorContent(navigator);
                CustomContentState customContent = null;
                if (navigator.IncludeInJournal)
                {
                    customContent = new _ViewContentState(navigator);
                }

                if (Object.ReferenceEquals(navigator, this.CurrentNavigator))
                {
                    // Navigating to the same content. Args should show Refresh mode since this should not be separate journal entry.
                    // Ignore navigation mode passed in this case
                    args = new ViewManagerNavigatedEventArgs(content, NavigationMode.Refresh, customContent, CurrentNavigator, navigator);
                }
                else
                {
                    // Raise event indicating new navigation
                    args = new ViewManagerNavigatedEventArgs(content, mode, customContent, CurrentNavigator, navigator);
                }

                CurrentNavigator = navigator;
                _OnNavigated(args);
            }
        }

        /// <summary>
        /// Navigation may also take place through the journal when the application is hosted in a NavigationWindow, as is normal.
        /// On navigation through journal, the ViewContentState object gives the path of the navigation target.
        /// </summary>
        /// <remarks>
        /// When navigation takes place through the journal, the navigation mode reported by the navigaiton service is the platform's
        /// NavigationMode. It must be converted into ScePhoto's custom navigation mode type here.
        /// </remarks>
        /// <param name="path">The journal path to use.</param>
        /// <param name="mode">The journal navigation mode (NavigationMode.Back or .Forward).</param>
        private void _NavigateByJournal(string path, NavigationMode mode)
        {
            // Journal navigation must be back or forward
            if (mode == NavigationMode.Back || mode == NavigationMode.Forward)
            {
                NavigationMode readerNavigationMode = (mode == NavigationMode.Back) ? NavigationMode.Back : NavigationMode.Forward;
                if (!string.IsNullOrEmpty(path))
                {
                    Navigator currentNavigator = MasterNavigator.GetNavigatorFromPath(path);
                    if (currentNavigator != null)
                    {
                        // Current navigator found, raise Navigating event
                        object content = this._GetNavigatorContent(currentNavigator);
                        CustomContentState customContent = null;
                        if (currentNavigator.IncludeInJournal)
                        {
                            customContent = new _ViewContentState(currentNavigator); 
                        }
                        var args = new ViewManagerNavigatedEventArgs(content, readerNavigationMode, customContent, this.CurrentNavigator, currentNavigator);
                        CurrentNavigator = currentNavigator;
                        _OnNavigated(args);
                    }
                    else
                    {
                        // Object is gone. Return error
                        var error = new MissingItemError(path);
                        var args = new ViewManagerNavigatedEventArgs(error, readerNavigationMode, null, this.CurrentNavigator, null);
                        _OnNavigated(args);
                    }
                }
            }
        }

        /// <summary>
        /// Current navigator is reinstated.
        /// </summary>
        /// <remarks>
        /// Navigate by refresh always has Refresh navigation mode.
        /// </remarks>
        public void NavigateByRefresh()
        {
            _NavigateNew(CurrentNavigator, NavigationMode.Refresh);
        }

        /// <summary>
        /// Gets content that should be displayed for a given navigator. In most cases, this is just the Navigator.Content object, except for 
        /// PhotoNavigators where is a photo-document wrapper class.
        /// </summary>
        /// <param name="navigator">Navigator whose content must be retrieved.</param>
        /// <returns>The object that forms the content of the Navigator.</returns>
        private object _GetNavigatorContent(Navigator navigator)
        {
            object content = null;
            if (navigator != null)
            {
                var photoNavigator = navigator as PhotoNavigator;
                var albumNavigator = navigator as PhotoAlbumNavigator;
                var slideShowNavigator = navigator as SlideShowNavigator;
                var contactNavigator = navigator as ContactNavigator;
                if (photoNavigator != null)
                {
                    var photo = photoNavigator.Content as FacebookPhoto;
                    content = photo;

                    // If photo navigator's parent is a photo album, set active photo album to this value
                    var parent = photoNavigator.Parent as PhotoAlbumNavigator;
                    if (parent != null)
                    {
                        this.ActivePhotoAlbum = parent.Content as FacebookPhotoAlbum;
                    }
                    else
                    {
                        this.ActivePhotoAlbum = null;
                    }

                    this.ActivePhoto = photo;
                }
                else if (albumNavigator != null)
                {
                    FacebookPhotoAlbum album = albumNavigator.Content as FacebookPhotoAlbum;
                    content = album;
                    this.ActivePhotoAlbum = album;
                    this.ActivePhoto = null;
                }
                else if (slideShowNavigator != null)
                {
                    SlideShow slideShow = slideShowNavigator.Content as SlideShow;
                    content = slideShow;
                    this.ActivePhotoAlbum = slideShow.Album.Content as FacebookPhotoAlbum;
                    this.ActivePhoto = null;
                }
                else if (contactNavigator != null)
                {
                    FacebookContact contact = contactNavigator.Content as FacebookContact;
                    content = contact;
                }
                else
                {
                    // Return navigator's content 
                    content = navigator.Content;
                }
            }

            return content;
        }

        /// <summary>
        /// Raise Navigated event with event args indicating content and type of navigation.
        /// </summary>
        /// <param name="e">ViewManagerNavigatedEventArgs providing information about the navigation.</param>
        private void _OnNavigated(ViewManagerNavigatedEventArgs e)
        {
            var handler = Navigated;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Handler for a dialogs loaded event. Focuses the dialog once loaded.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Information about the event.</param>
        private static void Dialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            UserControl dialog = sender as UserControl;
            if (dialog != null)
            {
                dialog.Loaded -= Dialog_OnLoaded;
                dialog.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        /// <summary>
        /// Default handler for a dialogs KeyDown event.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">EventArgs describing the key event.</param>
        private static void Dialog_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Escape:
                    {
                        UserControl dialog = sender as UserControl;
                        if (dialog != null)
                        {
                            ServiceProvider.ViewManager.EndDialog(dialog);
                        }

                        break;
                    }

                default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// Custom content state for view navigators. Stores the navigator's path so it can be serialized in the journal and looked up on journal navigation.
        /// </summary>
        [Serializable]
        private class _ViewContentState : CustomContentState
        {
            private string _path;

            /// <summary>
            /// Initializes a new instance of the ViewContentState class.
            /// Creates ViewContentState on a navigation, given a Navigator that forms the target of that navigation. The constructor
            /// initializes the path and the name for the navigator, which appear in the navigation journal.
            /// </summary>
            /// <param name="navigator">The <see cref="Navigator"/> for which thie content state is created. ViewContentState
            /// is a CustomContentState, based on navigators, which uses the <see cref="Navigator.Path"/> property to save to the journal.</param>
            public _ViewContentState(Navigator navigator)
            {
                if (navigator != null)
                {
                    Assert.IsTrue(navigator.IncludeInJournal);
                    _path = navigator.Path;
                }
            }

            /// <summary>
            /// Gets the entry name that appears in the navigation journal for this navigator's content.
            /// </summary>
            public override string JournalEntryName { get { return _path; } }

            public override void Replay(NavigationService navigationService, NavigationMode mode)
            {
                ServiceProvider.ViewManager._NavigateByJournal(_path, mode);
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The current dialog.
        /// </summary>
        private FrameworkElement _dialog;

        /// <summary>
        /// The active photo album.
        /// </summary>
        private FacebookPhotoAlbum _activePhotoAlbum;

        /// <summary>
        /// The active photo.
        /// </summary>
        private FacebookPhoto _activePhoto;

        #endregion

        private ViewPage _startPage = ViewPage.Newsfeed;

        public event RequestNavigateEventHandler ExternalNavigationRequested
        {
            add { NavigationCommands.NavigateToContentCommand.ExternalNavigationRequested += value; }
            remove { NavigationCommands.NavigateToContentCommand.ExternalNavigationRequested -= value; }
        }

        public ViewManager(FacebookService facebookService, string[] parameters, Dispatcher dispatcher)
        {
            Verify.IsNotNull(facebookService, "facebookService");

            ProcessCommandLineArgs(parameters);

            NavigationCommands = new NavigationCommands(this);
            ActionCommands = new ActionCommands(this);
            // _startPage may get set by processCommandLineArgs
            MasterNavigator = new MasterNavigator(this, facebookService, _startPage, dispatcher);
            FacebookAppId = facebookService.ApplicationId;
            
            facebookService.PropertyChanged += facebookService_PropertyChanged;
        }

        void facebookService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string proxiedPropertyName = null;
            switch (e.PropertyName)
            {
#if FACEBOOK_HAS_GRANTED_INBOX_PERMISSIONS
#error remove this
#else
                case "UnreadMessageCount": proxiedPropertyName = "UnreadMessageCount"; break;
#endif
                case "MeContact": proxiedPropertyName = "MeContact"; break;
                case "ContactSortOrder": proxiedPropertyName = "ActiveContactSortOrder"; break;
                case "PhotoAlbumSortOrder": proxiedPropertyName = "ActivePhotoAlbumSortOrder"; break;
            }

            if (proxiedPropertyName != null)
            {
                _NotifyPropertyChanged(proxiedPropertyName);
            }
        }

        public event EventHandler<ViewManagerNavigatedEventArgs> Navigated;
        public event PropertyChangedEventHandler PropertyChanged;

        public string FacebookAppId { get; private set; }
        public NavigationCommands NavigationCommands { get; private set; }
        public ActionCommands ActionCommands { get; private set; }
        public MasterNavigator MasterNavigator { get; private set; }

        /// <summary>
        /// Gets the currently active modal dialog.
        /// </summary>
        public FrameworkElement Dialog
        {
            get
            {
                return _dialog;
            }

            private set
            {
                _dialog = value;
                _NotifyPropertyChanged("Dialog");
            }
        }

        public FacebookPhotoAlbum ActivePhotoAlbum
        {
            get
            {
                return _activePhotoAlbum;
            }

            private set
            {
                if (_activePhotoAlbum != value)
                {
                    _activePhotoAlbum = value;
                    _NotifyPropertyChanged("ActivePhotoAlbum");
                }
            }
        }

        public FacebookPhoto ActivePhoto
        {
            get
            {
                return _activePhoto;
            }

            private set
            {
                _activePhoto = value;
                _NotifyPropertyChanged("ActivePhoto");
            }
        }

        public FacebookPhotoAlbumCollection PhotoAlbums
        {
            get
            {
                return ServiceProvider.FacebookService.PhotoAlbums;
            }
        }

        public FacebookContactCollection Friends
        {
            get
            {
                return ServiceProvider.FacebookService.Friends;
            }
        }

        public FacebookContactCollection OnlineFriends
        {
            get
            {
                return ServiceProvider.FacebookService.OnlineFriends;
            }
        }

        public FacebookContact MeContact
        {
            get
            {
                return ServiceProvider.FacebookService.MeContact;
            }
        }

        public FacebookCollection<Notification> Notifications
        {
            get
            {
                return ServiceProvider.FacebookService.Notifications;
            }
        }

#if FACEBOOK_HAS_GRANTED_INBOX_PERMISSIONS

        public FacebookCollection<MessageNotification> Inbox
        {
            get
            {
                return ServiceProvider.FacebookService.InboxNotifications;
            }
        }
#else
        public int UnreadMessageCount
        {
            get
            {
                return ServiceProvider.FacebookService.UnreadMessageCount;
            }
        }
#endif

        public ActivityPostCollection NewsFeed
        {
            get
            {
                return ServiceProvider.FacebookService.NewsFeed;
			}
		}

        public FacebookCollection<ActivityFilter> NewsFeedFilters
        {
            get
            {
                return ServiceProvider.FacebookService.ActivityFilters;
            }
        }

        public ActivityFilter ActiveNewsFeedFilter
        {
            get
            {
                return ServiceProvider.FacebookService.NewsFeedFilter;
            }
        }

        public PhotoAlbumSortOrder ActivePhotoAlbumSortOrder
        {
            get
            {
                return ServiceProvider.FacebookService.PhotoAlbumSortOrder;
            }
        }

        public ContactSortOrder ActiveContactSortOrder
        {
            get
            {
                return ServiceProvider.FacebookService.ContactSortOrder;
            }
        }

        #region Public Methods

        /// <summary>
        /// Displays dialogPage and blocks input to the main application until EndDialog is called.
        /// </summary>
        /// <param name="sourceDialog">The dialog to show.</param>
        public void ShowDialog(FrameworkElement sourceDialog)
        {
            if (sourceDialog == null)
            {
                throw new ArgumentNullException("sourceDialog");
            }

            if (Dialog != null)
            {
                throw new InvalidOperationException("A dialog is already active.");
            }

            sourceDialog.Loaded += Dialog_OnLoaded; // Focus the dialog once it is loaded
            sourceDialog.KeyDown += Dialog_OnKeyDown; // Dismiss the dialog if escape is pressed

            Dialog = sourceDialog;
        }

        /// <summary>
        /// Closes dialogPage if it is the current dialogPage.
        /// </summary>
        /// <param name="sourceDialog">The dialog to close.</param>
        public virtual void EndDialog(FrameworkElement sourceDialog)
        {
            if (sourceDialog != null)
            {
                if (object.ReferenceEquals(sourceDialog, this.Dialog))
                {
                    Dialog.Loaded -= Dialog_OnLoaded;
                    Dialog.KeyDown -= Dialog_OnKeyDown;
                    Dialog = null;
                }
            }
        }

        public FacebookPhotoAlbum CreatePhotoAlbum(string name, string description, string location)
        {
            return ServiceProvider.FacebookService.CreatePhotoAlbum(name, description, location);
        }

        public FacebookPhoto AddPhotoToAlbum(FacebookPhotoAlbum album, string caption, string imageFile)
        {
            return ServiceProvider.FacebookService.AddPhotoToAlbum(album, caption, imageFile);
        }

        public FacebookPhoto AddPhotoToApplicationAlbum(string caption, string imageFile)
        {
            return ServiceProvider.FacebookService.AddPhotoToApplicationAlbum(caption, imageFile);
        }

        public SearchResults DoSearch(string query)
        {
            return ServiceProvider.FacebookService.SearchIndex.DoSearch(query);
        }

        public void AddTagToPhoto(FacebookPhoto photo, FacebookContact contact, Point offset)
        {
            ServiceProvider.FacebookService.AddPhotoTag(photo, contact, offset);
        }

        public void UpdateStatus(string newStatus)
        {
            ServiceProvider.FacebookService.UpdateStatus(newStatus);
        }

        public void UpdateStatus(string newStatus, string uri)
        {
            ServiceProvider.FacebookService.UpdateStatus(newStatus, uri);
        }

        /// <summary>
        /// Processes command line args from ServiceProvider.
        /// </summary>
        /// <param name="commandLineArgs">Command line arguments passed as string collection.</param>
        public void ProcessCommandLineArgs(IList<string> commandLineArgs)
        {
            // This gets called in the constructor to setup initial state
            // as well as through the SingleInstance handler.
            // Don't assume that 
            bool hasStarted = NavigationCommands != null;

            if (commandLineArgs != null && commandLineArgs.Count > 0)
            {
                int argIndex = 0;
                while (argIndex < commandLineArgs.Count)
                {
                    string commandSwitch = commandLineArgs[argIndex].ToLowerInvariant();
                    if (commandSwitch.StartsWith("-uri:") || commandSwitch.StartsWith("/uri:"))
                    {
                        // Not going to support starting from one of these right now.
                        // Only switch if already loaded.
                        if (hasStarted)
                        {
                            Uri uri;
                            if (Uri.TryCreate(commandSwitch.Substring("-uri:".Length), UriKind.Absolute, out uri))
                            {
                                NavigationCommands.NavigateToContentCommand.Execute(uri);
                            }
                        }
                    }
                    else
                    {
                        switch (commandSwitch)
                        {
                            case "-minimode":
                            case "/minimode":
                                if (hasStarted)
                                {
                                    NavigationCommands.NavigateHomeCommand.Execute(this);
                                }
                                else
                                {
                                    _startPage = ViewPage.Newsfeed;
                                }
                                break;

                            case "-newsfeed":
                            case "/newsfeed":
                                if (hasStarted)
                                {
                                    NavigationCommands.NavigateHomeCommand.Execute(this);
                                }
                                else
                                {
                                    _startPage = ViewPage.Newsfeed;
                                }
                                break;
                            case "-albums":
                            case "/albums":
                                if (hasStarted)
                                {
                                    NavigationCommands.NavigatePhotoAlbumsCommand.Execute(this);
                                }
                                else
                                {
                                    _startPage = ViewPage.Photos;
                                }
                                break;
                            case "-profile":
                            case "/profile":
                                if (hasStarted)
                                {
                                    NavigationCommands.NavigateProfileCommand.Execute(this);
                                }
                                else
                                {
                                    _startPage = ViewPage.Profile;
                                }
                                break;
                            case "-friends":
                            case "/friends":
                                if (hasStarted)
                                {
                                    NavigationCommands.NavigateFriendsCommand.Execute(this);
                                }
                                else
                                {
                                    _startPage = ViewPage.Friends;
                                }
                                break;
                            case "-exit":
                            case "/exit":
                                Application.Current.MainWindow.Close();
                                break;
                        }
                    }
                    ++argIndex;
                }
            }
        }

        #endregion

        /// <summary>
        /// Raise property changed event.
        /// </summary>
        /// <param name="propertyName">Name of the property which has changed.</param>
        private void _NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// EventArgs providing information about a ViewManager navigation event.
    /// </summary>
    public class ViewManagerNavigatedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the ViewManagerNavigatedEventArgs class.
        /// </summary>
        /// <param name="content">Content to which navigation has taken place.</param>
        /// <param name="navigationMode">Navigation mode.</param>
        /// <param name="contentStateToSave">CustomContentState to be persisted to the navigation journal for this navigation.</param>
        /// <param name="oldNavigator">Navigator for previously navigated content before navigation took place.</param>
        /// <param name="newNavigator">Navigator for current content after navigation.</param>
        public ViewManagerNavigatedEventArgs(object content, NavigationMode navigationMode, CustomContentState contentStateToSave, Navigator oldNavigator, Navigator newNavigator)
        {
            Content = content;
            NavigationMode = navigationMode;
            ContentStateToSave = contentStateToSave;
            OldNavigator = oldNavigator;
            NewNavigator = newNavigator;
        }

        /// <summary>
        /// Gets the actual content target of this navigation.
        /// </summary>
        public object Content { get; private set; }

        /// <summary>
        /// Gets the navigation mode - Back or Forward (through journal), New or Refresh. Only New warrants a new journal entry.
        /// </summary>
        public NavigationMode NavigationMode { get; private set; }

        /// <summary>
        /// Gets the content state to be saved to the journal on new navigations.
        /// </summary>
        public CustomContentState ContentStateToSave { get; private set; }

        /// <summary>
        /// Gets the navigator for old content.
        /// </summary>
        public Navigator OldNavigator { get; private set; }

        /// <summary>
        /// Gets the navigator for new content.
        /// </summary>
        public Navigator NewNavigator { get; private set; }
    }
}