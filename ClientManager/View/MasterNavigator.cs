namespace ClientManager
{
    using System;
    using System.Collections.Specialized;
    using System.Windows.Threading;
    using ClientManager.View;
    using Contigo;
    using Standard;

    /// <summary>
    /// Handles top level navigation concepts.
    /// </summary>
    public sealed class MasterNavigator
    {
        private INotifyCollectionChanged _startupCollection;
        private ViewManager _viewManager;
        private LoginPage _loginPage;
        private LoadingPage _loadingPage;
        private Navigator[] _children;

        public Navigator LoginPageNavigator { get; private set; }
        public Navigator HomeNavigator { get; private set; }
        public Navigator ProfileNavigator { get; private set; }
        public Navigator FriendsNavigator { get; private set; }
        public Navigator PhotoAlbumsNavigator { get; private set; } 

        public MasterNavigator(ViewManager viewManager, FacebookService facebookService, ViewPage startupPage, Dispatcher dispatcher)
        {
            Verify.IsNotNull(viewManager, "viewManager");
            Verify.IsNotNull(facebookService, "facebookService");
            _viewManager = viewManager;
            
            HomeNavigator = new HomePage().GetNavigator(null, dispatcher);
            PhotoAlbumsNavigator = new PhotoAlbumCollectionNavigator(facebookService.PhotoAlbums, "Photo Albums", null);
            FriendsNavigator = new ContactCollectionNavigator(facebookService.Friends, "Friends", null);
            ProfileNavigator = new ContactNavigator(facebookService.MeContact, "Me", null);

            _children = new[] { HomeNavigator, PhotoAlbumsNavigator, FriendsNavigator, ProfileNavigator };

            Navigator startupNavigator = null;
            switch (startupPage)
            {
                case ViewPage.Friends:
                    startupNavigator = FriendsNavigator;
                    _startupCollection = facebookService.Friends;
                    break;
                case ViewPage.Newsfeed: 
                    startupNavigator = HomeNavigator;
                    _startupCollection = facebookService.NewsFeed;                         
                    break;
                case ViewPage.Photos:
                    startupNavigator = PhotoAlbumsNavigator;
                    _startupCollection = facebookService.PhotoAlbums;
                    break;
                case ViewPage.Profile:
                    startupNavigator = ProfileNavigator;
                    _startupCollection = null;
                    break;
            }

            _loadingPage = new LoadingPage(startupNavigator);
            if (_startupCollection != null)
            {
                _startupCollection.CollectionChanged += _SignalNewsfeedChanged;
            }
            else 
            {
                _loadingPage.Signal();
            }

            _loginPage = new LoginPage(facebookService.ApplicationId, _loadingPage.GetNavigator(), true);
            LoginPageNavigator = _loginPage.Navigator;
        }

        private void _SignalNewsfeedChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _startupCollection.CollectionChanged -= _SignalNewsfeedChanged;
            _loadingPage.Signal();
        }

        public Navigator GetNavigatorFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string searchPrefix = Uri.EscapeDataString("[search]");
            if (path.StartsWith(searchPrefix))
            {
                path = path.Substring(searchPrefix.Length);
                path = Uri.UnescapeDataString(path);
                return new SearchNavigator(_viewManager.DoSearch(path));
            }

            string childPath;
            string guid = Navigator.ExtractFirstChildPath(path, out childPath);

            foreach (var topNav in _children)
            {
                if (guid == topNav.Guid)
                {
                    if (string.IsNullOrEmpty(childPath))
                    {
                        return topNav;
                    }

                    return topNav.FindChildNavigatorFromPath(childPath);
                }
            }

            return null;
        }

        public bool CanGetNavigatorWithContent(object content)
        {
            foreach (var topNav in _children)
            {
                if (topNav.Content != null && topNav.Content == content)
                {
                    return true;
                }

                bool canCreateNav = topNav.CanGetChildNavigatorWithContent(content);
                if (canCreateNav)
                {
                    return true;
                }
            }
            return false;
        }


        public Navigator GetNavigatorWithContent(object content)
        {
            foreach (var topNav in _children)
            {
                if (topNav.Content != null && topNav.Content == content)
                {
                    return topNav;
                }

                Navigator contentNav = topNav.GetChildNavigatorWithContent(content);
                if (null != contentNav)
                {
                    return contentNav;
                }
            }
            return null;
        }
    }
}
