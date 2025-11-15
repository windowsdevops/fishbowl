//-----------------------------------------------------------------------
// <copyright file="NavigationCommands.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Navigation commands exposed by ViewManager.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.View
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Navigation;
    using Contigo;
    using Standard;

    /// <summary>
    /// Navigation commands exposed by <see cref="ViewManager"/>. Includes commands for navigating to specific photos or albums,
    /// next/previous navigation, navigating to a Search album, navigating by Guid or by <see cref="Navigator"/> and others. 
    /// </summary>
    public sealed class NavigationCommands
    {
        public NavigationCommands(ViewManager viewManager)
        {
            Assert.IsNotNull(viewManager);
            foreach (PropertyInfo publicInstanceProperty in typeof(NavigationCommands).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                ConstructorInfo cons = publicInstanceProperty.PropertyType.GetConstructor(new[] { typeof(ViewManager) });
                Assert.IsNotNull(cons);
                object command = cons.Invoke(new object[] { viewManager });
                Assert.AreEqual(command.GetType().Name, publicInstanceProperty.Name);
                publicInstanceProperty.SetValue(this, command, null);
            }
        }

        public NavigateToContentCommand NavigateToContentCommand { get; private set; }

        public NavigateToNextCommand NavigateToNextCommand { get; private set; }
        public NavigateToPriorCommand NavigateToPriorCommand { get; private set; }
        public NavigateToParentCommand NavigateToParentCommand { get; private set; }
        public NavigateToNextSiblingCommand NavigateToNextSiblingCommand { get; private set; }
        public NavigateToPriorSiblingCommand NavigateToPriorSiblingCommand { get; private set; }
        public NavigateToBeginningCommand NavigateToBeginningCommand { get; private set; }
        public NavigateToEndCommand NavigateToEndCommand { get; private set; }

        public NavigateLoginCommand NavigateLoginCommand { get; private set; }
        public NavigateHomeCommand NavigateHomeCommand { get; private set; }
        public NavigateProfileCommand NavigateProfileCommand { get; private set; }
        public NavigateFriendsCommand NavigateFriendsCommand { get; private set; }
        public NavigatePhotoAlbumsCommand NavigatePhotoAlbumsCommand { get; private set; }
        public NavigateSearchCommand NavigateSearchCommand { get; private set; }
        public NavigateSlideShowCommand NavigateSlideShowCommand { get; private set; }
    }

    public abstract class NavigationCommand : ViewCommand
    {
        protected NavigationCommand(ViewManager viewManager) : base(viewManager) 
        {}

        protected sealed override void ExecuteInternal(object parameter)
        {
            PerformNavigate(parameter);
        }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        /// <remarks>
        /// PerformNavigate is a navigation-specific method that NavigationCommand subclasses may override to customize behavior
        /// instead of overriding ExecuteInternal on ViewCommand. This is useful because the NavigationCommand class overrides
        /// ExecuteInternal to add special interaction logic with transitions, it is not desirable to remove this logic by overriding.
        /// </remarks>
        protected abstract void PerformNavigate(object parameter);
    }

    public sealed class NavigateToContentCommand : NavigationCommand
    {
        public NavigateToContentCommand(ViewManager viewManager)
            : base(viewManager)
        {}

        private object _FindNavigatorFromString(string stringContent)
        {
            Uri maybeProfileUri;
            if (Uri.TryCreate(stringContent, UriKind.Absolute, out maybeProfileUri) && maybeProfileUri.Host.ToLowerInvariant().Contains("facebook.com"))
            {
                string id = null;
                // Maybe this is a straight-up profile.php?id=####
                // Not supporting username links right now, but Facebook is also not giving them in notifications right now.
                if (maybeProfileUri.LocalPath.ToLowerInvariant().Equals("/profile.php"))
                {
                    string idPart = null;
                    foreach (string part in new[] { "?id=", "&id=" })
                    {
                        if (maybeProfileUri.Query.ToLowerInvariant().Contains(part))
                        {
                            idPart = part;
                            break;
                        }
                    }

                    if (idPart != null)
                    {
                        id = maybeProfileUri.Query.ToLower();
                        id = maybeProfileUri.Query.Substring(id.IndexOf(idPart) + idPart.Length);
                        // Strip out the rest of the query
                        id = id.Split('&')[0];
                    }
                }

                if (!string.IsNullOrEmpty(id))
                {
                    var me = (FacebookContact)ViewManager.MasterNavigator.ProfileNavigator.Content;
                    if (me.UserId.Equals(id))
                    {
                        return ViewManager.MasterNavigator.ProfileNavigator;
                    }

                    Navigator nav = ((ContactCollectionNavigator)ViewManager.MasterNavigator.FriendsNavigator).GetContactWithId(id);
                    if (nav != null)
                    {
                        return nav;
                    }
                }

                if (maybeProfileUri.LocalPath.ToLowerInvariant().Equals("/photo.php"))
                {
                    string userId = null;
                    string photoId = null;

                    foreach (string part in maybeProfileUri.Query.Split('?', '&'))
                    {
                        if (part.ToLowerInvariant().StartsWith("id="))
                        {
                            userId = part.Substring("id=".Length);
                        }
                        else if (part.ToLowerInvariant().StartsWith("pid="))
                        {
                            photoId = part.Substring("pid=".Length);
                        }
                    }

                    if (userId != null && photoId != null)
                    {
                        // Need to do additional parsing here.
                        // If id is 32 bits then PID = ((id << 32) | pid)
                        // If id is 64 bits then PID = id + " _ " + pid
                        ulong userIdLong;
                        if (ulong.TryParse(userId, out userIdLong))
                        {
                            if (userIdLong == (ulong)(uint)userIdLong)
                            {
                                uint photoIdInt;
                                if (uint.TryParse(photoId, out photoIdInt))
                                {
                                    id = ((userIdLong << 32) | (ulong)photoIdInt).ToString();
                                }
                            }
                            else
                            {
                                id = userId + "_" + photoId;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(id))
                    {
                        Navigator nav = ((PhotoAlbumCollectionNavigator)ViewManager.MasterNavigator.PhotoAlbumsNavigator).GetPhotoWithId(userId, id);
                        if (nav != null)
                        {
                            return nav;
                        }
                    }
                }
            }
            return null;
        }

        private object _GetNavigatableContent(object content)
        {
            string stringContent = content as string;
            if (stringContent == null)
            {
                var uriContent = content as Uri;
                if (uriContent != null)
                {
                    stringContent = uriContent.OriginalString;
                }
            }

            if (!string.IsNullOrEmpty(stringContent))
            {
                if (stringContent == "[CurrentNavigator]")
                {
                    return _GetExternalNavigatorLocation(ViewManager.CurrentNavigator.Content) ?? new Uri("http://facebook.com");
                }

                // I want to make notifications work with people links, but not walk every navigatable object
                // looking for compatible URLs.  This is a simple heuristic that's mostly working most of the time,
                // and it allows me to generally stop quickly.
                object o = _FindNavigatorFromString(stringContent);
                if (o != null)
                {
                    return o;
                }
            }

            var post = content as ActivityPost;
            if (post != null)
            {
                content = post.Actor;
            }
            var comment = content as ActivityComment;
            if (comment != null)
            {
                content = comment.FromUser;
            }

            var attachment = content as ActivityPostAttachment;
            if (attachment != null)
            {
                if (attachment.Type == ActivityPostAttachmentType.Video)
                {
                    return attachment.VideoSource;
                }
                if (attachment.Type == ActivityPostAttachmentType.Photos)
                {
                    return attachment.Photos.FirstOrDefault();
                }
                if (attachment.Type == ActivityPostAttachmentType.Links)
                {
                    if (attachment.Links.Count > 0)
                    {
                        return attachment.Links[0].Link;
                    }
                }
                return attachment.Link;
            }

            return content;
        }


        protected override bool CanExecuteInternal(object parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            if (parameter is Navigator
                || parameter is string
                || parameter is Uri)
            {
                return true;
            }

            parameter = _GetNavigatableContent(parameter);

            if (parameter is Navigator
                || parameter is string 
                || parameter is Uri)
            {
                return true;
            }

            Assert.IsNotNull(parameter);

            // Try to find a navigator that matches this parameter object.
            return ViewManager.MasterNavigator.CanGetNavigatorWithContent(parameter) || _GetExternalNavigatorLocation(parameter) != null;
        }

        protected override void PerformNavigate(object parameter)
        {
            Verify.IsNotNull(parameter, "parameter");

            parameter = _GetNavigatableContent(parameter);
            Assert.IsNotNull(parameter);

            var stringContent = parameter as string;
            if (stringContent != null)
            {
                Uri uriMaybe;
                if (!Uri.TryCreate(stringContent, UriKind.Absolute, out uriMaybe))
                {
                    Process.Start(new ProcessStartInfo(stringContent));
                    return;
                }
                parameter = uriMaybe;
            }

            var uriContent = parameter as Uri;
            if (uriContent != null)
            {
                _OnExternalNavigationRequested(this, uriContent);
                return;
            }

            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                navigator = ViewManager.MasterNavigator.GetNavigatorWithContent(parameter);
            }

            // If we aren't able to find a navigator with the specified content, then try to get a URI from it.
            if (navigator == null)
            {
                uriContent = _GetExternalNavigatorLocation(parameter) ?? new Uri("http://facebook.com");
                _OnExternalNavigationRequested(this, uriContent);
                return;
            }

            if (navigator != null)
            {
                ViewManager.NavigateByCommand(navigator);
            }
        }

        private Uri _GetExternalNavigatorLocation(object content)
        {
            var contact = content as FacebookContact;
            if (contact != null)
            {
                return contact.ProfileUri;
            }

            var photo = content as FacebookPhoto;
            if (photo != null)
            {
                return photo.Link;
            }

            var album = content as FacebookPhotoAlbum;
            if (album != null)
            {
                return album.Link;
            }

            return null;
        }

        public event RequestNavigateEventHandler ExternalNavigationRequested;

        private void _OnExternalNavigationRequested(object sender, Uri uri)
        {
            var handler = ExternalNavigationRequested;
            if (handler != null)
            {
                handler(sender, new RequestNavigateEventArgs(uri, null));
            }
        }

        public bool CanFindInternalNavigator(object parameter, out Uri externalNavigationUri)
        {
            externalNavigationUri = null;
            Verify.IsNotNull(parameter, "parameter");

            parameter = _GetNavigatableContent(parameter);
            Assert.IsNotNull(parameter);

            if (parameter is string)
            {
                Uri uri;
                if (Uri.TryCreate((string)parameter, UriKind.Absolute, out uri))
                {
                    externalNavigationUri = uri;
                }
                return false;
            }

            if (parameter is Uri)
            {
                externalNavigationUri = (Uri)parameter;
                return false;
            }

            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                navigator = ViewManager.MasterNavigator.GetNavigatorWithContent(parameter);
            }

            // If we aren't able to find a navigator with the specified content, then try to get a URI from it.
            if (navigator == null)
            {
                externalNavigationUri = _GetExternalNavigatorLocation(parameter) ?? new Uri("http://facebook.com");
                return false;
            }

            return true;
        }
    }

    public sealed class NavigateToNextCommand : NavigationCommand
    {
        public NavigateToNextCommand(ViewManager viewManager)
            : base(viewManager) 
        {}

        private static Navigator _GetNextNavigator(Navigator navigator)
        {
            Assert.IsNotNull(navigator);

            if (navigator.FirstChild != null)
            {
                return navigator.FirstChild;
            }

            if (navigator.NextSibling != null)
            {
                return navigator.NextSibling;
            }

            if (navigator.Parent != null)
            {
                return navigator.Parent.NextSibling;
            }

            return null;
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            Verify.IsNotNull(parameter, "parameter");
            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                return false;
            }

            return _GetNextNavigator(navigator) != null;
        }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            Verify.IsNotNull(parameter, "parameter");

            var navigator = parameter as Navigator;
            if (navigator != null)
            {
                navigator = _GetNextNavigator(navigator);
                if (null != navigator)
                {
                    ViewManager.NavigateByCommand(navigator);
                }
            }
        }
    }

    public sealed class NavigateToPriorCommand : NavigationCommand
    {
        public NavigateToPriorCommand(ViewManager viewManager)
            : base(viewManager) 
        {
        }

        private static Navigator _GetPriorNavigator(Navigator navigator)
        {
            Assert.IsNotNull(navigator);

            if (navigator.PreviousSibling != null)
            {
                if (navigator.PreviousSibling.LastChild != null)
                {
                    return navigator.PreviousSibling.LastChild;
                 }
                return navigator.PreviousSibling;
            }

            return navigator.Parent;
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            Verify.IsNotNull(parameter, "parameter");
            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                return false;
            }

            return _GetPriorNavigator(navigator) != null;
        }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            Verify.IsNotNull(parameter, "parameter");

            var navigator = parameter as Navigator;
            if (navigator != null)
            {
                navigator = _GetPriorNavigator(navigator);
                if (null != navigator)
                {
                    ViewManager.NavigateByCommand(navigator);
                }
            }
        }
    }

    public sealed class NavigateToParentCommand : NavigationCommand
    {
        public NavigateToParentCommand(ViewManager viewManager)
            : base(viewManager) 
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            Verify.IsNotNull(parameter, "parameter");
            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                return false;
            }

            return navigator.Parent != null;
        }

        protected override void PerformNavigate(object parameter)
        {
            Verify.IsNotNull(parameter, "parameter");
            var navigator = parameter as Navigator;
            if (navigator != null)
            {
                navigator = navigator.Parent;
                if (navigator != null)
                {
                    ViewManager.NavigateByCommand(navigator);
                }
            }
        }
    }

    public sealed class NavigateToNextSiblingCommand : NavigationCommand
    {
        public NavigateToNextSiblingCommand(ViewManager viewManager)
            : base(viewManager) 
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                navigator = ServiceProvider.ViewManager.CurrentNavigator;
                Assert.IsNotNull(navigator);
            }

            return navigator.NextSibling != null;
        }

        protected override void PerformNavigate(object parameter)
        {
            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                navigator = ServiceProvider.ViewManager.CurrentNavigator;
                Assert.IsNotNull(navigator);
            }

            navigator = navigator.NextSibling;
            if (navigator != null)
            {
                ViewManager.NavigateByCommand(navigator);
            }
        }
    }

    public sealed class NavigateToPriorSiblingCommand : NavigationCommand
    {
        public NavigateToPriorSiblingCommand(ViewManager viewManager)
            : base(viewManager) 
        {}

        protected override bool CanExecuteInternal(object parameter)
        {
            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                navigator = ServiceProvider.ViewManager.CurrentNavigator;
                Assert.IsNotNull(navigator);
            }

            return navigator.PreviousSibling != null;
        }

        protected override void PerformNavigate(object parameter)
        {
            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                navigator = ServiceProvider.ViewManager.CurrentNavigator;
                Assert.IsNotNull(navigator);
            }

            navigator = navigator.PreviousSibling;
            if (navigator != null)
            {
                ViewManager.NavigateByCommand(navigator);
            }
        }
    }
    
    /// <summary>
    /// Given a <see cref="PhotoSlideShowNavigator"/>, navigates to the PhotoSlideShow represented by that navigator.
    /// If not given a <see cref="PhotoSlideShowNavigator"/>, it will navigate to a slideshow based on the current Album. 
    /// </summary>
    public sealed class NavigateSlideShowCommand : NavigationCommand
    {
        /// <summary>
        /// Initializes a new instance of the NavigateToSlideShowCommand class.
        /// </summary>
        /// <param name="viewManager">
        /// ViewManager associated with ViewCommand subclasses.
        /// </param>
        public NavigateSlideShowCommand(ViewManager viewManager)
            : base(viewManager)
        {}

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            SlideShow slideShow = null;
            var photoSlideShowNavigator = parameter as SlideShowNavigator;

            if (photoSlideShowNavigator == null)
            {
                var albumNavigator = parameter as PhotoAlbumNavigator;
                if (albumNavigator == null)
                {
                    albumNavigator = ServiceProvider.ViewManager.CurrentNavigator as PhotoAlbumNavigator;
                }

                if (albumNavigator != null && albumNavigator.FirstChild != null)
                {
                    slideShow = new SlideShow(albumNavigator);
                    photoSlideShowNavigator = new SlideShowNavigator(slideShow);
                }
            }

            if (photoSlideShowNavigator == null)
            {
                var photoNavigator = ServiceProvider.ViewManager.CurrentNavigator as PhotoNavigator;
                if (photoNavigator != null)
                {
                    slideShow = new SlideShow(photoNavigator);
                    photoSlideShowNavigator = new SlideShowNavigator(slideShow);
                }
            }

            if (photoSlideShowNavigator == null)
            {
                if (ServiceProvider.FacebookService.IsOnline)
                {
                    var allAlbumsNavigator = ServiceProvider.ViewManager.MasterNavigator.PhotoAlbumsNavigator;
                    if (allAlbumsNavigator.FirstChild != null)
                    {
                        slideShow = new SlideShow((PhotoAlbumCollectionNavigator)allAlbumsNavigator);
                        photoSlideShowNavigator = new SlideShowNavigator(slideShow);
                    }
                }
            }

            if (photoSlideShowNavigator != null)
            {
                ViewManager.NavigateByCommand(photoSlideShowNavigator);
            }
        }
    }

    public sealed class NavigateLoginCommand : NavigationCommand
    {
        public NavigateLoginCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override void PerformNavigate(object parameter)
        {
            ViewManager.NavigateByCommand(ViewManager.MasterNavigator.LoginPageNavigator);
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            return !ServiceProvider.FacebookService.IsOnline;
        }
    }

    public sealed class NavigateHomeCommand : NavigationCommand
    {
        public NavigateHomeCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            ViewManager.NavigateByCommand(ViewManager.MasterNavigator.HomeNavigator);
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            return ServiceProvider.FacebookService.IsOnline;
        }
    }

    /// <summary>
    /// Navigates to the profile page.
    /// </summary>
    public sealed class NavigateProfileCommand : NavigationCommand
    {
        public NavigateProfileCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            ViewManager.NavigateByCommand(ViewManager.MasterNavigator.ProfileNavigator);
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            return ServiceProvider.FacebookService.IsOnline;
        }
    }

    /// <summary>
    /// Navigates to the friends page.
    /// </summary>
    public sealed class NavigateFriendsCommand : NavigationCommand
    {
        public NavigateFriendsCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            ViewManager.NavigateByCommand(ViewManager.MasterNavigator.FriendsNavigator);
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            return ServiceProvider.FacebookService.IsOnline;
        }
    }

    /// <summary>
    /// Navigates to friends photo albums.
    /// </summary>
    public sealed class NavigatePhotoAlbumsCommand : NavigationCommand
    {
        public NavigatePhotoAlbumsCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            ViewManager.NavigateByCommand(ViewManager.MasterNavigator.PhotoAlbumsNavigator);
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            return ServiceProvider.FacebookService.IsOnline;
        }
    }

    public sealed class NavigateToBeginningCommand : NavigationCommand
    {
        public NavigateToBeginningCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            Navigator nav = ViewManager.CurrentNavigator.Parent;
            if (nav != null)
            {
                ViewManager.NavigateByCommand(nav.FirstChild);
            }
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            return ViewManager.CurrentNavigator.Parent != null;
        }
    }

    public sealed class NavigateToEndCommand : NavigationCommand
    {
        public NavigateToEndCommand(ViewManager viewManager)
            : base(viewManager) 
        {}

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            Verify.IsNotNull(parameter, "parameter");

            var navigator = parameter as Navigator;
            if (navigator != null)
            {
                navigator = navigator.Parent;
                if (navigator != null)
                {
                    ViewManager.NavigateByCommand(navigator.LastChild);
                }
            }
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            var navigator = parameter as Navigator;
            if (navigator == null)
            {
                return false;
            }
            
            return navigator.Parent != null;
        }
    }

    /// <summary>
    /// Given search text, queries for a <see cref="Navigator"/> for a Search photo album for that text, and navigates to the Search photo album.
    /// This may result in the creation of a new Search photo album and Navigator for this search term, a consequence of navigation.
    /// </summary>
    public sealed class NavigateSearchCommand : NavigationCommand
    {
        /// <summary>
        /// Initializes a new instance of the SearchCommand class.
        /// </summary>
        /// <param name="viewManager">
        /// ViewManager associated with ViewCommand subclasses.
        /// </param>
        public NavigateSearchCommand(ViewManager viewManager) : base(viewManager) 
        { 
        }

        /// <summary>
        /// Navigation-specific logic that can be overridden by subclasses of NavigationCommand to provide custom navigation behavior.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for NavigationCommand.
        /// </param>
        protected override void PerformNavigate(object parameter)
        {
            string searchText = parameter as string;
            if (!string.IsNullOrEmpty(searchText))
            {
                // TODO:
                // Generate search photo album and get navigator
                //FacebookPhotoAlbum searchPhotoAlbum = ViewManager.GenerateSearchPhotoAlbum(searchText);
                //PhotoAlbumNavigator searchNavigator = ViewManager.MasterNavigator.GetSearchNavigator(searchPhotoAlbum);
                //ViewManager.NavigateByCommand(searchNavigator, ScePhotoNavigationMode.Normal);

                SearchResults searchResults = ServiceProvider.ViewManager.DoSearch(searchText);
                SearchNavigator navigator = new SearchNavigator(searchResults);
                ViewManager.NavigateByCommand(navigator);
            }
        }
    }
}
