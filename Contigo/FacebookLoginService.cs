
namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Windows;
    using Microsoft.Json.Serialization;
    using Standard;

    /// <summary>
    /// Extended permissions that the app can request beyond what Facebook normally allows.
    /// </summary>
    /// <remarks>
    /// According to facebook some of these are supersets of of others.  This relationship isn't reflected in the enum values.
    /// </remarks>
    [Flags]
    public enum Permissions
    {
        None = 0,

        // PublishStream is a superset of status_update, photo_upload, video_upload, create_note, and share_item extended permissions.
        PublishStream   = 0x0001,
        
        ReadStream      = 0x0002,
        Email           = 0x0004,
        ReadMailbox     = 0x0008,
        OfflineAccess   = 0x0010,
        CreateEvent     = 0x0020,
        RsvpEvent       = 0x0040,
        Sms             = 0x0080,
        StatusUpdate    = 0x0100,
        PhotoUpload     = 0x0200,
        VideoUpload     = 0x0400,
        CreateNote      = 0x0800,
        ShareItem       = 0x1000,
    }
    
    public delegate void GetPermissionsAsyncCallback(object sender, GetImageSourceCompletedEventArgs e);

    public class FacebookLoginService : IDisposable
    {
        private DispatcherPool _requestDispatcher = new DispatcherPool("LoginService WebRequests", 1);
        private readonly string _cachePath;

        private FacebookWebApi _facebookApi;

        public string SessionKey { get; private set; }
        public string UserId { get; private set; }
        public string AppId { get; private set; }
        public string SessionSecret { get; private set; }

        public bool HasCachedSessionInfo { get; private set; }

        public FacebookLoginService(string appId)
        {
            Verify.IsNeitherNullNorEmpty(appId, "appId");
            AppId = appId;

            _cachePath = _GenerateCachePath(AppId);
            _TryGetCachedSession();
        }

        private static string _GenerateCachePath(string appId)
        {
            Assert.IsNeitherNullNorEmpty(appId);
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Facebook\") + appId + @"\SESSION_CACHE";
        }

        public static void ClearCachedCredentials(string appId)
        {
            _DeleteUserLoginCookie();
            string cachePath = _GenerateCachePath(appId);
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
        }

        public void ClearCachedCredentials()
        {
            ClearCachedCredentials(AppId);
            SessionKey = null;
            UserId = null;
        }

        private bool _TryGetCachedSession()
        {
            if (File.Exists(_cachePath))
            {
                string[] sessionInfo = File.ReadAllLines(_cachePath);
                if (sessionInfo.Length != 3)
                {
                    return false;
                }

                this.UserId = sessionInfo[0];
                this.SessionKey = sessionInfo[1];
                this.SessionSecret = sessionInfo[2];

                _facebookApi = new FacebookWebApi(AppId, SessionKey, UserId, SessionSecret);

                HasCachedSessionInfo = true;
                return true;
            }
            HasCachedSessionInfo = false;
            return false;
        }

        /// <summary>
        /// Start a new session by connecting to Facebook.
        /// </summary>
        /// <param name="authenticationToken">The authentication token to use.</param>
        public void InitiateNewSession(Uri sessionResponse)
        {
            string sessionPrefix = "session=";
            var badSessionInfo = new ArgumentException("The session response does not contain connection information.", "sessionResponse");

            Verify.IsNotNull(sessionResponse, "sessionResponse");

            if (!string.IsNullOrEmpty(SessionKey))
            {
                throw new InvalidOperationException("This object already has a session.");
            }

            string jsonSessionInfo = sessionResponse.Query;
            jsonSessionInfo = Utility.UrlDecode(jsonSessionInfo);
            int startIndex = jsonSessionInfo.IndexOf(sessionPrefix);
            if (-1 == startIndex)
            {
                throw badSessionInfo;
            }

            jsonSessionInfo = jsonSessionInfo.Substring(startIndex + sessionPrefix.Length);
            if (jsonSessionInfo.Length == 0 || jsonSessionInfo[0] != '{')
            {
                throw badSessionInfo;
            }

            int curlyCount = 1;
            for (int i = 1; i < jsonSessionInfo.Length; ++i)
            {
                if (jsonSessionInfo[i] == '{')
                {
                    ++curlyCount;
                }
                if (jsonSessionInfo[i] == '}')
                {
                    --curlyCount;
                }

                if (curlyCount == 0)
                {
                    jsonSessionInfo = jsonSessionInfo.Substring(0, i + 1);
                    break;
                }
            }

            if (curlyCount != 0)
            {
                throw badSessionInfo;
            }

            var serializer = new JsonSerializer(typeof(object));
            var sessionMap = (IDictionary<string, object>)serializer.Deserialize(jsonSessionInfo);

            object sessionKey;
            object userId;
            object secret;

            if (!sessionMap.TryGetValue("session_key", out sessionKey)
                || !sessionMap.TryGetValue("uid", out userId)
                || !sessionMap.TryGetValue("secret", out secret))
            {
                throw badSessionInfo;
            }

            SessionKey = sessionKey.ToString();
            UserId = userId.ToString();
            SessionSecret = secret.ToString();

            Utility.EnsureDirectory(_cachePath);
            using (TextWriter tw = new StreamWriter(_cachePath, false))
            {
                tw.WriteLine(UserId);
                tw.WriteLine(SessionKey);
                tw.WriteLine(SessionSecret);
            }

            _facebookApi = new FacebookWebApi(AppId, SessionKey, UserId, SessionSecret);
            HasCachedSessionInfo = true;
        }

        /// <summary>Get the URI to host in a WebBrowser to allow a Facebook user to log into this application.</summary>
        /// <param name="authenticationToken"></param>
        /// <returns></returns>
        public Uri GetLoginUri(string successUri, string deniedUri, Permissions requiredPermissions)
        {
            return FacebookWebApi.GetLoginUri(AppId, successUri, deniedUri, requiredPermissions);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Utility.SafeDispose(ref _requestDispatcher);
        }

        #endregion

        public void GetMissingPermissionsAsync(Permissions permissions, AsyncCompletedEventHandler callback)
        {
            Assert.IsNotNull(_facebookApi);
            Assert.IsNotNull(callback);

            // Otherwise, why bother calling this?
            Assert.AreNotEqual(Permissions.None, permissions);

            _requestDispatcher.QueueRequest((arg) =>
            {
                Exception ex = null;
                Permissions missingPermissions = Permissions.None;
                try
                {
                    missingPermissions = _facebookApi.GetMissingPermissions(permissions);
                }
                catch (Exception e)
                {
                    ex = e;
                }
                callback(this, new AsyncCompletedEventArgs(ex, false, missingPermissions));
            }, null);
        }

        // For signout, we need to delete all cookies for these Urls.
        // (Based on empirical observation; there may be more later we need to clean to ensure logout)
        static readonly Uri FaceBookLoginUrl1 = new Uri("https://ssl.facebook.com/desktopapp.php");
        static readonly Uri FaceBookLoginUrl2 = new Uri("https://login.facebook.com/login.php");

        private static void _DeleteUserLoginCookie()
        {
            _DeleteEveryCookie(FaceBookLoginUrl1);
            _DeleteEveryCookie(FaceBookLoginUrl2);
        }

        private static void _DeleteEveryCookie(Uri url)
        {
            string cookie = string.Empty;
            try
            {
                // Get every cookie (Expiration will not be in this response)
                cookie = Application.GetCookie(url);
            }
            catch (Win32Exception)
            {
                // "no more data is available" ... happens randomly so ignore it.
            }
            if (!string.IsNullOrEmpty(cookie))
            {
                // This may change eventually, but seems quite consistent for Facebook.com.
                // ... they split all values w/ ';' and put everything in foo=bar format.
                string[] values = cookie.Split(';');

                foreach (string s in values)
                {
                    if (s.IndexOf('=') > 0)
                    {
                        // Sets value to null with expiration date of yesterday.
                        _DeleteSingleCookie(s.Substring(0, s.IndexOf('=')).Trim(), url);
                    }
                }
            }
        }

        private static void _DeleteSingleCookie(string name, Uri url)
        {
            try
            {
                // Calculate "one day ago"
                DateTime expiration = DateTime.UtcNow - TimeSpan.FromDays(1);
                // Format the cookie as seen on FB.com.  Path and domain name are important factors here.
                string cookie = String.Format("{0}=; expires={1}; path=/; domain=.facebook.com", name, expiration.ToString("R"));
                // Set a single value from this cookie (doesnt work if you try to do all at once, for some reason)
                Application.SetCookie(url, cookie);
            }
            catch (Exception exc)
            {
                Assert.Fail(exc + " seen deleting a cookie.  If this is reasonable, add it to the list.");
            }
        }

    }
}
