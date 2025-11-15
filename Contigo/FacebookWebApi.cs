
namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Xml.Linq;
    using Microsoft.Json.Serialization;
    using Standard;

    using METHOD_MAP = System.Collections.Generic.SortedDictionary<string, string>;

    // This class is thread-safe.  It does not contain any mutable state.
    internal class FacebookWebApi : IDisposable
    {
        #region Fields

        private static readonly Uri _FacebookApiUri = new Uri(@"http://api.facebook.com/restserver.php");

        private const string _ExtendedPermissionColumns = "create_event, create_note, email, offline_access, photo_upload, publish_stream, read_mailbox, read_stream, rsvp_event, share_item, sms, status_update, video_upload";
        // affiliations: type: [college | high school | work | region], year, name, nid, status
        // current_location: city, state, country (well defined), zip (may be zero)
        // education_history: year (4 digit, may be blank), name, concentration (list), degree
        // hometown_location: city, state, country
        // hs_info: hs1_name, hs2_name, grad_year, hs1_id, hs2_id
        // pic* also has pic_with_logo* analagous column.
        // work_history: location, company_name, description, position, start_date, end_date
        // family: relationship (one of parent, mother, father, sibling, sister, brother, child, son, daughter), uid (optional), name (optional), birthday (if the relative is a child, this is the birthday the user entered)
        private const string _UserColumns = "about_me, activities, affiliations, allowed_restrictions, birthday, birthday_date, books, current_location, education_history, email_hashes, family, first_name, has_added_app, hometown_location, hs_info, interests, is_app_user, is_blocked, last_name, locale, meeting_for, meeting_sex, movies, music, name, notes_count, online_presence, pic, pic_big, pic_small, pic_square, political, profile_blurb, profile_update_time, profile_url, proxied_email, quotes, relationship_status, religion, sex, significant_other_id, status, timezone, tv, uid, username, verified, wall_count, website, work_history";
        private const string _AlbumColumns = "aid, cover_pid, owner, name, created, modified, description, location, link, size, visible";
        private const string _PhotoColumns = "pid, aid, owner, src, src_big, src_small, link, caption, created";
        // Facebook's documentation also mentions a "type" column that has been deprecated in favor of using the attachment to figure it out.
        private const string _StreamTableColumns = "post_id, viewer_id, app_id, source_id, updated_time, created_time, filter_key, attribution, actor_id, target_id, message, app_data, action_links, attachment, comments, likes, privacy";
        private const string _StreamCommentColumns = "post_id, id, fromid, time, text";
        private const string _StreamFilterColumns = "uid, filter_key, name, rank, icon_url, is_visible, type, value";
        private const string _PhotoTagColumns = "pid, subject, text, xcoord, ycoord, created";
        private const string _ProfileColumns = "id, name, url, pic, pic_square, pic_small, pic_big, type, username";
        private const string _ThreadColumns = "thread_id, folder_id, subject, recipients, updated_time, parent_message_id, parent_thread_id, message_count, snippet, snippet_author, object_id, viewer_id, unread";

        private const string _SelectFriendsClause = "(SELECT uid2 FROM friend WHERE uid1={0})";

        private const string _GetPermissionsQueryString = "SELECT " + _ExtendedPermissionColumns + " FROM permissions WHERE uid={0}";
        private const string _GetFriendsQueryString = "SELECT " + _UserColumns + " FROM user WHERE uid IN " + _SelectFriendsClause;
        private const string _GetFriendsOnlineStatusQueryString = "SELECT uid, online_presence FROM user WHERE uid IN " + _SelectFriendsClause;
        private const string _GetSingleUserQueryString = "SELECT " + _UserColumns + " FROM user WHERE uid={0}";
        private const string _GetSingleProfileInfoQueryString = "SELECT " + _ProfileColumns + " FROM profile WHERE id={0}";
        private const string _GetSingleUserAlbumsQueryString = "SELECT " + _AlbumColumns + " FROM album WHERE owner={0} ORDER BY modified DESC";
        private const string _GetFriendsAlbumsQueryString = "SELECT " + _AlbumColumns + " FROM album WHERE owner IN " + _SelectFriendsClause + " ORDER BY modified DESC LIMIT 200";
        private const string _GetPhotosFromSingleUserAlbumsQueryString = "SELECT " + _PhotoColumns + " FROM photo WHERE aid IN (SELECT aid FROM album WHERE owner={0}) ORDER BY modified DESC";
        private const string _GetPhotoTagsFromAlbumQueryString = "SELECT " + _PhotoTagColumns + " FROM photo_tag WHERE pid IN (SELECT pid FROM photo WHERE aid=\"{0}\")";
        // Can't use this because it returns a limit of 5000 items which results in a bunch of albums with no photos.
        //private const string _GetPhotosFromFriendsAlbumsQueryString = "SELECT " + _PhotoFields + " FROM photo WHERE aid IN (SELECT aid FROM album WHERE owner IN " + _SelectFriendsClause + " ORDER BY modified DESC)";
        private const string _GetPhotosFromAlbumQueryString = "SELECT " + _PhotoColumns + " FROM photo WHERE aid=\"{0}\"";
        private const string _GetFriendsPhotosQueryString = "SELECT " + _PhotoColumns + " FROM photo WHERE aid IN (SELECT aid FROM album WHERE owner IN " + _SelectFriendsClause + " ORDER BY modified DESC)";
        private const string _GetCommentorsQueryString = "SELECT " + _UserColumns + " FROM user WHERE uid IN (SELECT fromid FROM comment where post_id IN (SELECT post_id FROM stream WHERE filter_key in (SELECT filter_key FROM stream_filter WHERE uid={0} and type='newsfeed')))";
        private const string _GetStreamPostsQueryString = "SELECT " + _StreamTableColumns + " FROM stream WHERE source_id={0} LIMIT 20";
        private const string _GetFriendsRecentActivityString = "SELECT " + _StreamTableColumns + " FROM stream WHERE source_id IN " + _SelectFriendsClause;
        private const string _GetStreamCommentsQueryString = "SELECT " + _StreamCommentColumns + " FROM comment WHERE post_id IN (SELECT post_id FROM stream WHERE filter_key in (SELECT filter_key FROM stream_filter WHERE uid={0} and type='newsfeed'))";
        private const string _GetStreamFiltersQueryString = "SELECT " + _StreamFilterColumns + " FROM stream_filter where uid={0}";

        private const string _GetInboxThreadsQueryString = "SELECT " + _ThreadColumns + " FROM thread where folder_id=0";
        private const string _GetUnreadInboxThreadsQueryString = _GetInboxThreadsQueryString + " and unread != 0";

        private const string _GetPhotoTagsMultiQueryString = "SELECT " + _PhotoTagColumns + " FROM photo_tag WHERE pid IN (SELECT pid FROM #{0})";
        private const string _GetProfilesMultiQueryString = "SELECT " + _ProfileColumns + " FROM profile WHERE id in (SELECT uid FROM #{0}) ORDER BY id DESC";

        private readonly DataSerialization _serializer;

        /// <summary>
        /// A mapping of the Permissions enum to the extended permissions strings that we need to request from the server.
        /// </summary>
        // This map and the enum needs to be kept in sync with the query string for extended permissions.
        // If any of the Facebook fields become very volatile then we can dynamically generate strings
        // and maps in the static constructor.
        private static readonly Dictionary<Permissions, string> _PermissionLookup = new Dictionary<Permissions, string>
        {
            { Permissions.CreateEvent, "create_event" },
            { Permissions.CreateNote, "create_note" },
            { Permissions.Email, "email" },
            { Permissions.ReadMailbox, "read_mailbox" },
            { Permissions.OfflineAccess, "offline_access" },
            { Permissions.PhotoUpload, "photo_upload" },
            { Permissions.PublishStream, "publish_stream" },
            { Permissions.ReadStream, "read_stream" },
            { Permissions.RsvpEvent, "rsvp_event" },
            { Permissions.ShareItem, "share_item" },
            { Permissions.Sms, "sms" },
            { Permissions.StatusUpdate, "status_update" },
            { Permissions.VideoUpload, "video_upload" },
        };

        private readonly string _ApplicationId;
        private readonly string _SessionKey;
        private readonly string _Secret;
        private readonly string _UserId;
        private readonly FacebookService _Service;
        private bool _disposed;

        #endregion

        private void _Verify(bool requiresService)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("this");
            }

            if (requiresService && _Service == null)
            {
                throw new InvalidOperationException("Operation requires a valid FacebookService");
            }
        }

        #region Methods that don't require a session key

        public static Uri GetLoginUri(string appId, string successUri, string deniedUri, Permissions requiredPermissions)
        {
            string permissionsPart = "";

            if (requiredPermissions != Permissions.None)
            {
                bool isFirst = true;
                StringBuilder permBuilder = new StringBuilder();
                foreach (var pair in _PermissionLookup)
                {
                    if (Permissions.None == (pair.Key & requiredPermissions))
                    {
                        continue;
                    }

                    if (!isFirst)
                    {
                        // read_stream,publish_stream,offline_access
                        permBuilder.Append(",");
                    }
                    else
                    {
                        isFirst = false;
                    }

                    permBuilder.Append(pair.Value);
                }
                permissionsPart = permBuilder.ToString();
                Assert.IsNeitherNullNorEmpty(permissionsPart);
            }

            return new Uri(string.Format("http://www.facebook.com/login.php?api_key={0}&connect_display=popup&v=1.0&next={1}&cancel_url={2}&fbconnect=true&return_session=true{3}{4}",
                appId, 
                successUri, 
                deniedUri, 
                string.IsNullOrEmpty(permissionsPart) ? "" : "&req_perms=",
                permissionsPart));
        }

        /* Not currently used.
        public static string GetAuthenticationToken(string appId, string secret)
        {
            var createTokenMap = new METHOD_MAP
            {
                { "method", "facebook.auth.createToken" },
                { "api_key", appId },
                { "v", "1.0" },
            };

            string authResponse = SendRequest(createTokenMap, secret);
            return XDocument.Parse(authResponse).Root.Value;
        }

        public static void GetSession(string appId, string authToken, string secret, out string sessionKey, out string userId)
        {
            var getSession = new SortedDictionary<string, string>
            {
                { "method", "facebook.auth.getSession" },
                { "auth_token", authToken },
                { "api_key", appId },
                { "v", "1.0" },
            };

            string xml = SendRequest(getSession, secret);

            DataSerialization.DeserializeSessionInfo(xml, out sessionKey, out userId);
        }
        */

        #endregion

        static FacebookWebApi()
        {
            /*
            ServicePointManager.MaxServicePoints = 16;
            ServicePointManager.MaxServicePointIdleTime = new TimeSpan(0, 10, 0).Milliseconds;
            //ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.Expect100Continue = true;
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(_FacebookApiUri);
            */
        }

        public FacebookWebApi(string applicationId, string sessionKey, string userId, string secret)
        {
            _ApplicationId = applicationId;
            _SessionKey = sessionKey;
            _UserId = userId;
            _Secret = secret;
            _Service = null;
        }

        public FacebookWebApi(FacebookService service, string secret)
        {
            _ApplicationId = service.ApplicationId;
            _SessionKey = service.SessionKey;
            _UserId = service.UserId;
            _Secret = secret;
            _Service = service;
            _serializer = new DataSerialization(service);
        }

        private static string _SanitizeJsonString(string value)
        {
            Assert.IsNeitherNullNorEmpty(value);

            var sb = new StringBuilder(value.Length);
            char[] chars = value.ToCharArray();
            foreach (var c in chars)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\");
                        break;
                    case '/':
                        sb.Append("\\/");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        // JSON spec says any unicode character not in the above list
                        // is valid, but for the sake of debuggability and wire transfer
                        // just sanitize things that aren't normally ASCII printable.
                        if (c < 32 || c > 126)
                        {
                            sb.Append("\\u" + ((int)c).ToString("{0:x4}"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Posts an FQL query to facebook.
        /// </summary>
        /// <param name="query">The FQL query to send.</param>
        /// <returns>The results of the FQL query.</returns>
        private string _SendQuery(string query)
        {
            Assert.IsNotOnMainThread();

            Assert.IsNeitherNullNorEmpty(query);

            _Verify(true);

            var queryMap = new METHOD_MAP
            {
                { "method", "facebook.fql.query" },
                { "query", query },
            };

            return _SendRequest(queryMap);
        }


        private string _SendMultiQuery(IList<string> names, IList<string> queries)
        {
            Assert.IsNotOnMainThread();

            _Verify(true);

            Assert.IsNotNull(names);
            Assert.IsNotNull(queries);
            Assert.AreEqual(names.Count, queries.Count);

            var dict = names
                .Zip(queries, (key, value) => new KeyValuePair<string, string>(key, value))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            var serializer = new JsonSerializer(typeof(object));

            string multiquery = serializer.Serialize(dict);

            var queryMap = new METHOD_MAP
            {
                { "method", "fql.multiquery" },
                { "queries", multiquery },
            };

            return _SendRequest(queryMap);
        }

        /// <summary>
        /// Sends a request with the given parameters to the server.
        /// </summary>
        /// <param name="requestPairs">A dictionary of parameters describing the request.</param>
        /// <returns>The HTTP response as a string.</returns>
        /// <remarks>
        /// This will modify the dictionary parameter to include additional information about the request.
        /// </remarks>
        private string _SendRequest(IDictionary<string, string> requestPairs)
        {
            Assert.IsNotOnMainThread();

            _Verify(true);

            if (!requestPairs.ContainsKey("api_key"))
            {
                requestPairs.Add("api_key", _ApplicationId);
                requestPairs.Add("v", "1.0");
                requestPairs.Add("session_key", _SessionKey);
                // Need to signal that we're using the session secret instead.
                requestPairs.Add("ss", "1");
            }

            return SendRequest(requestPairs, _Secret);
        }


        private string _SendFileRequest(IDictionary<string, string> requestPairs, string filePath)
        {
            Assert.IsNotOnMainThread();

            _Verify(true);

            byte[] data = null;
            using (FileStream fs = File.Open(filePath, FileMode.Open))
            {
                data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
            }

            const string NewLine = "\r\n";
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);

            if (!requestPairs.ContainsKey("api_key"))
            {
                requestPairs.Add("api_key", _ApplicationId);
                requestPairs.Add("v", "1.0");
                requestPairs.Add("session_key", _SessionKey);
                // Need to signal that we're using the session secret instead.
                requestPairs.Add("ss", "1");
                requestPairs.Add("sig", _GenerateSignature(requestPairs, _Secret));
            }

            var builder = new StringBuilder();

            foreach (var pair in requestPairs)
            {
                builder
                    .Append("--").Append(boundary).Append(NewLine)
                    .Append("Content-Disposition: form-data; name=\"").Append(pair.Key).Append("\"").Append(NewLine)
                    .Append(NewLine)
                    .Append(pair.Value).Append(NewLine);
            }

            builder
                .Append("--").Append(boundary).Append(NewLine)
                .Append("Content-Disposition: form-data; filename=\"").Append("Sample.jpg").Append("\"").Append(NewLine)
                .Append("Content-Type: image/jpeg\r\n\r\n");

            byte[] bytes = Encoding.UTF8.GetBytes("\r\n" + "--" + boundary + "--" + "\r\n");
            byte[] buffer = Encoding.UTF8.GetBytes(builder.ToString());

            byte[] postData = null;
            using (MemoryStream stream = new MemoryStream((buffer.Length + data.Length) + bytes.Length))
            {
                stream.Write(buffer, 0, buffer.Length);
                stream.Write(data, 0, data.Length);
                stream.Write(bytes, 0, bytes.Length);
                postData = stream.GetBuffer();
            }

            var request = (HttpWebRequest)WebRequest.Create(_FacebookApiUri);
            // Use 1.0 because a significant number of proxies don't understand 1.1 (the default)
            request.ProtocolVersion = HttpVersion.Version10;

            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(postData, 0, postData.Length);
            }

            string result = null;
            using (WebResponse response = request.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                }
            }

            Exception e = _VerifyResult(result, builder.ToString());
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        private static string SendRequest(IDictionary<string, string> requestPairs, string secret)
        {
            Assert.IsNotOnMainThread();

            string requestData = _GenerateRequestData(requestPairs, secret);

            var request = (HttpWebRequest)WebRequest.Create(_FacebookApiUri);
            // Use 1.0 because a significant number of proxies don't understand 1.1 (the default)
            request.ProtocolVersion = HttpVersion.Version10;

            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            using (Stream requestStream = request.GetRequestStream())
            {
                using (var sw = new StreamWriter(requestStream))
                {
                    sw.Write(requestData);
                }
            }

            string result = null;
            using (WebResponse response = request.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                }
            }

            Exception e = _VerifyResult(result, requestData);
            if (e != null)
            {
                throw e;
            }

            return result;
        }

        /// <summary>Check the response of a Facebook server call for error information.</summary>
        /// <param name="xml"></param>
        /// <returns>
        /// If the request was an error, returns an exception that describes the failure.
        /// Otherwise this returns null.
        /// </returns>
        private static Exception _VerifyResult(string xml, string sourceXml)
        {
            try
            {
                return DataSerialization.DeserializeFacebookException(xml, sourceXml);
            }
            catch (InvalidOperationException) { }

            // Yay, couldn't convert to an exception :) return null.
            return null;
        }

        /// <summary>
        /// Converts the dictionary describing a server request into a string in the format that the Facebook servers require,
        /// including an encrypted signature key based on the application's secret.
        /// </summary>
        /// <param name="requestPairs"></param>
        /// <returns></returns>
        private static string _GenerateRequestData(IDictionary<string, string> requestPairs, string secret)
        {
            if (!requestPairs.ContainsKey("sig"))
            {
                requestPairs.Add("sig", _GenerateSignature(requestPairs, secret));
            }
            return requestPairs.Aggregate(new StringBuilder(), (sb, kv) => sb.AppendFormat("&{0}={1}", kv.Key, kv.Value)).ToString();
        }

        private static string _GenerateSignature(IDictionary<string, string> requestPairs, string secret)
        {
            // Need to build a hash with the secret to authenticate this URL.
            string fullRequest = requestPairs.Aggregate(
                new StringBuilder(),
                (sb, kv) => sb.AppendFormat("{0}={1}", kv.Key, kv.Value))
                .Append(secret)
                .ToString();

            return Utility.GetHashString(fullRequest);
        }

        #region Methods that don't require a FacebookService/SessionKey

        public Permissions GetMissingPermissions(Permissions extendedPermissions)
        {
            var queryMap = new METHOD_MAP
            {
                { "method", "facebook.fql.query" },
                { "query", string.Format(_GetPermissionsQueryString, _UserId) },
                { "api_key", _ApplicationId },
                { "v", "1.0" },
                { "ss", "1" },
                { "session_key", _SessionKey },
            };

            string result = Utility.FailableFunction(() => SendRequest(queryMap, _Secret));

            XDocument document = XDocument.Parse(result);

            Permissions deniedPermissions = Permissions.None;
            foreach (var pair in from pair in _PermissionLookup
                                 where (pair.Key & extendedPermissions) != Permissions.None
                                 select pair)
            {
                var permissionNode = (XElement)document.Root.Elements().Nodes<XElement>().First(node => ((XElement)node).Name.LocalName == pair.Value);
                Assert.Implies(permissionNode.Value != "0", permissionNode.Value == "1");
                if (permissionNode.Value == "0")
                {
                    deniedPermissions |= pair.Key;
                }
            }

            return deniedPermissions;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // Stop any new outbound web requests from this object.
            _disposed = true;
        }

        #endregion

        public List<ActivityFilter> GetActivityFilters()
        {
            string result = Utility.FailableFunction(() => _SendQuery(string.Format(_GetStreamFiltersQueryString, _UserId)));
            return _serializer.DeserializeFilterList(result);
        }

        public FacebookContact GetUser(string userId)
        {
            _Verify(true);

            var userMap = new METHOD_MAP
            {
                { "method", "users.GetInfo" },
                { "uids", userId },
                { "fields", _UserColumns },
            };

            string result = Utility.FailableFunction(10, () => _SendRequest(userMap));
            List<FacebookContact> contactList = _serializer.DeserializeUsersList(result);
            if (contactList.Count == 0)
            {
                throw new FacebookException("Unable to obtain information about the user.", null);
            }

            return contactList[0];
        }

        public List<MessageNotification> GetMailNotifications(bool includeRead)
        {
            _Verify(true);

            string query = includeRead
                ? _GetInboxThreadsQueryString 
                : _GetUnreadInboxThreadsQueryString;

            string result = Utility.FailableFunction(() => _SendQuery(query));
            List<MessageNotification> notifications = _serializer.DeserializeMessageQueryResponse(result);

            return notifications;
        }

        // This differs from Notifications.GetList in that it returns messages, pokes, shares, group and event invites, and friend requests.
        public void GetRequests(out List<Notification> friendRequests, out int unreadMessagesCount)
        {
            _Verify(true);

            var notificationMap = new METHOD_MAP
            {
                { "method", "Notifications.get" },
            };

            string result = Utility.FailableFunction(() => _SendRequest(notificationMap));
            _serializer.DeserializeNotificationsGetResponse(result, out friendRequests, out unreadMessagesCount);
        }

        public List<Notification> GetNotifications(bool includeRead)
        {
            _Verify(true);

            var notificationMap = new METHOD_MAP
            {
                { "method", "Notifications.getList" },
            };

            if (includeRead)
            {
                notificationMap.Add("include_read", "true");
            }

            string result = Utility.FailableFunction(() => _SendRequest(notificationMap));

            List<Notification> notifications = _serializer.DeserializeNotificationsListResponse(result);

            // Don't include hidden notifications in this result set.
            // Facebook also tends to leave stale notifications behind for activity posts that have been deleted.
            // If the title is empty, just don't include it.
            notifications.RemoveAll(n => n.IsHidden || string.IsNullOrEmpty(n.Title));

            return notifications;
        }

        public FacebookPhoto GetPhoto(string photoId)
        {
            _Verify(true);

            var photoMap = new METHOD_MAP
            {
                { "method", "photos.get" },
                { "pids", photoId },
            };

            string result = Utility.FailableFunction(() => _SendRequest(photoMap));

            List<FacebookPhoto> response = _serializer.DeserializePhotosGetResponse(result);

            FacebookPhoto photo = response.FirstOrDefault();
            Assert.IsNotNull(photo);

            return photo;
        }

        public List<FacebookPhotoTag> GetPhotoTags(string photoId)
        {
            var tagMap = new METHOD_MAP
            {
                { "method", "photos.getTags" },
                { "pids", photoId },
            };

            string response = Utility.FailableFunction(() => _SendRequest(tagMap));
            return _serializer.DeserializePhotoTagsList(response);
        }

        public List<FacebookPhotoTag> AddPhotoTag(string photoId, string userId, float x, float y)
        {
            Verify.IsNeitherNullNorWhitespace(userId, "userId");
            Verify.IsNeitherNullNorWhitespace(photoId, "photoId");

            x *= 100;
            y *= 100;
            var tagMap = new METHOD_MAP
            {
                { "method", "photos.addTag" },
                { "pid", photoId },
                { "tag_uid", userId },
                { "x", string.Format("{0:0.##}", x) },
                { "y", string.Format("{0:0.##}", y) },
            };

            string response = Utility.FailableFunction(() => _SendRequest(tagMap));

            return GetPhotoTags(photoId);
        }

        public FacebookPhotoAlbum CreateAlbum(string name, string description, string location)
        {
            _Verify(true);

            var createMap = new METHOD_MAP
            {
                { "method", "photos.createAlbum" },
                { "name", name },
            };

            if (!string.IsNullOrEmpty(description))
            {
                createMap.Add("description", description);
            }

            if (!string.IsNullOrEmpty(location))
            {
                createMap.Add("location", location);
            }

            string createAlbumResponse = Utility.FailableFunction(() => _SendRequest(createMap));

            FacebookPhotoAlbum album = _serializer.DeserializeUploadAlbumResponse(createAlbumResponse);
            album.RawPhotos = new MergeableCollection<FacebookPhoto>();
            return album;
        }

        public FacebookPhoto AddPhotoToAlbum(string albumId, string caption, string imageFile)
        {
            _Verify(true);

            var updateMap = new METHOD_MAP
            {
                { "method", "photos.upload" },
            };

            if (!string.IsNullOrEmpty(albumId))
            {
                updateMap.Add("aid", albumId);
            }

            if (!string.IsNullOrEmpty(caption))
            {
                updateMap.Add("caption", caption);
            }

            string response = Utility.FailableFunction(() => _SendFileRequest(updateMap, imageFile));
            return _serializer.DeserializePhotoUploadResponse(response);
        }

        public FacebookPhotoAlbum GetAlbum(string albumId)
        {
            Verify.IsNeitherNullNorEmpty(albumId, "albumId");

            var albumMap = new METHOD_MAP
            {
                { "method", "photos.getAlbums" },
                { "aids", albumId },
            };

            string response = Utility.FailableFunction(() => _SendRequest(albumMap));

            List<FacebookPhotoAlbum> albumsResponse = _serializer.DeserializeGetAlbumsResponse(response);

            Assert.IsFalse(albumsResponse.Count > 1);

            if (albumsResponse.Count == 0)
            {
                return null;
            }
            return albumsResponse[0];
        }

        public ActivityPost PublishStream(string targetId, string message)
        {
            var streamMap = new METHOD_MAP
            {
                { "method", "stream.publish" },
                { "message", message },
                { "target_id", targetId },
            };

            Utility.FailableFunction(() => _SendRequest(streamMap));

            // Return a proxy that looks close to what we expect the updated status to look like.
            // We'll replace it with the real one the next time we sync.
            return new ActivityPost(_Service)
            {
                ActorUserId = _UserId,
                TargetUserId = targetId,
                Attachment = null,
                CanComment = false,
                CanLike = false,
                CanRemoveComments = false,
                CommentCount = 0,
                Created = DateTime.Now,
                HasLiked = false,
                LikedCount = 0,
                LikeUrl = null,
                Message = message,
                PostId = "-1",
                RawComments = new MergeableCollection<ActivityComment>(),
                RawPeopleWhoLikeThisIds = new MergeableCollection<string>(),
                Updated = DateTime.Now,
            };
        }

        public ActivityPost UpdateStatus(string newStatus)
        {
            var statusMap = new METHOD_MAP
            {
                { "method", "stream.publish" },
                { "message", newStatus },
            };

            string result = Utility.FailableFunction(() => _SendRequest(statusMap));
            string postId = _serializer.DeserializeStreamPublishResponse(result);

            // Return a proxy that looks close to what we expect the updated status to look like.
            // We'll replace it with the real one the next time we sync.
            return new ActivityPost(_Service)
            {
                ActorUserId = _UserId,
                Attachment = null,
                CanComment = false,
                CanLike = false,
                CanRemoveComments = false,
                CommentCount = 0,
                Created = DateTime.Now,
                HasLiked = false,
                LikedCount = 0,
                LikeUrl = null,
                Message = newStatus,
                PostId = postId,
                RawComments = new MergeableCollection<ActivityComment>(),
                RawPeopleWhoLikeThisIds = new MergeableCollection<string>(),
                TargetUserId = null,
                Updated = DateTime.Now,
            };
        }

        // Consider: return a proxy post similar to posting a new status.
        public void PostLink(string comment, string uri)
        {
            Assert.IsNeitherNullNorWhitespace(comment);
            Assert.IsNeitherNullNorWhitespace(uri);

            var statusMap = new METHOD_MAP
            {
                { "method", "Links.Post" },
                { "url", uri },
                { "comment", comment },
            };

            Utility.FailableFunction(() => _SendRequest(statusMap));
        }

        public List<ActivityPost> GetStreamPosts(string userId)
        {
            string result = Utility.FailableFunction(() => _SendQuery(string.Format(_GetStreamPostsQueryString, userId)));
            return _serializer.DeserializePostDataList(result, true);
        }

        public void GetStream(string filterKey, int limit, DateTime getItemsSince, out List<ActivityPost> posts, out List<FacebookContact> users)
        {
            Assert.IsTrue(limit > 0);

            // Facebook changed the semantics of the default feed, so we need to explicitly 
            // request the newsfeed filter to keep things working as expected.
            // I think everyone should have a filter with this key, but this is unfortunately fragile.
            if (string.IsNullOrEmpty(filterKey))
            {
                filterKey = "nf";
            }

            long startTime = DataSerialization.GetUnixTimestampFromDateTime(getItemsSince);
            Assert.IsTrue(startTime >= 0);

            var streamMap = new METHOD_MAP
            {
                { "method", "stream.get" },
                { "viewer_id", _UserId },
                { "start_time", startTime.ToString("G") },
                { "limit", limit.ToString("G") },
                { "filter_key", filterKey },
                { "metadata", "[albums, profiles, photo_tags]" },
            };

            string result = Utility.FailableFunction(() => _SendRequest(streamMap));

            _serializer.DeserializeStreamData(result, out posts, out users);
        }

        public string AddComment(ActivityPost post, string comment)
        {
            var commentMap = new METHOD_MAP
                {
                    { "method", "stream.addComment" },
                    { "post_id", post.PostId },
                    { "comment", comment },
                };

            string result = Utility.FailableFunction(() => _SendRequest(commentMap));
            // retrieve the new comment Id.
            return _serializer.DeserializeAddCommentResponse(result);
        }

        public List<ActivityComment> GetComments(ActivityPost post)
        {
            Assert.IsNotNull(post);

            var commentMap = new METHOD_MAP
            {
                { "method", "stream.getComments" },
                { "post_id", post.PostId },
            };

            string response = Utility.FailableFunction(10, () => _SendRequest(commentMap));
            return _serializer.DeserializeCommentsDataList(post, response);
        }

        public void RemoveComment(string commentId)
        {
            if (string.IsNullOrEmpty(commentId))
            {
                // If we're removing a comment that we haven't yet posted we can't remove it.
                return;
            }
            var commentMap = new METHOD_MAP
            { 
                { "method", "stream.removeComment" },
                { "comment_id", commentId },
            };

            Utility.FailableFunction(() => _SendRequest(commentMap));
        }

        public void AddLike(string postId)
        {
            var likeMap = new METHOD_MAP
            {
                { "method", "stream.addLike" },
                { "post_id", postId },
            };

            Utility.FailableFunction(() => _SendRequest(likeMap));
        }

        public void RemoveLike(string postId)
        {
            var likeMap = new METHOD_MAP
            {
                { "method", "stream.removeLike" },
                { "post_id", postId },
            };

            Utility.FailableFunction(() => _SendRequest(likeMap));
        }

        public List<FacebookContact> GetFriends()
        {
            string multiqueryResult = Utility.FailableFunction(() =>
                _SendMultiQuery(
                    new[] { "friends", "profiles" },
                    new[] { string.Format(_GetFriendsQueryString, _UserId), string.Format(_GetProfilesMultiQueryString, "friends") }));

            XDocument xdoc = DataSerialization.SafeParseDocument(multiqueryResult);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            XElement friendsNode = (from descendant in xdoc.Descendants(ns + "name")
                                    where descendant.Value == "friends"
                                    select descendant)
                                   .First();
            friendsNode = (XElement)friendsNode.NextNode;

            XElement profilesNode = (from descendant in xdoc.Descendants(ns + "name")
                                     where descendant.Value == "profiles"
                                     select descendant)
                                   .First();
            profilesNode = (XElement)profilesNode.NextNode;
            
            List<FacebookContact> friendsList = _serializer.DeserializeUsersListWithProfiles(ns, friendsNode, profilesNode);

            return friendsList;
        }

        public Dictionary<string, OnlinePresence> GetFriendsOnlineStatus()
        {
            string result = Utility.FailableFunction(() => _SendQuery(string.Format(_GetFriendsOnlineStatusQueryString, _UserId)));
            return _serializer.DeserializeUserPresenceList(result);
        }

        public List<FacebookPhotoAlbum> GetFriendsPhotoAlbums()
        {
            string albumQueryResult = Utility.FailableFunction(() => _SendQuery(string.Format(_GetFriendsAlbumsQueryString, _UserId)));
            return _serializer.DeserializeGetAlbumsResponse(albumQueryResult);
        }

        public List<FacebookPhoto>[] GetPhotosWithTags(IEnumerable<string> albumIds)
        {
            var names = new List<string>();
            var queries = new List<string>();

            int albumCount = 0;
            foreach (string albumId in albumIds)
            {
                names.Add("get_photos" + albumCount);
                names.Add("get_tags" + albumCount);

                queries.Add(string.Format(_GetPhotosFromAlbumQueryString, albumId));
                queries.Add(string.Format(_GetPhotoTagsMultiQueryString, "get_photos" + albumCount));
                ++albumCount;
            }

            string photoMultiQueryResult = Utility.FailableFunction(() => _SendMultiQuery(names, queries));

            XDocument xdoc = DataSerialization.SafeParseDocument(photoMultiQueryResult);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            var photoCollections = new List<FacebookPhoto>[albumCount];

            albumCount = 0;
            foreach (string albumId in albumIds)
            {
                photoCollections[albumCount] = new List<FacebookPhoto>();
                XElement photosResponseNode = (from descendant in xdoc.Descendants(ns + "name") 
                                               where descendant.Value == ("get_photos" + albumCount)
                                               select descendant)
                                              .First();
                photosResponseNode = (XElement)photosResponseNode.NextNode;
                List<FacebookPhoto> photos = _serializer.DeserializePhotosGetResponse(photosResponseNode, ns);

                XElement tagsResponseNode = (from descendant in xdoc.Descendants(ns + "name")
                                             where descendant.Value == ("get_tags" + albumCount)
                                             select descendant)
                                            .First();
                tagsResponseNode = (XElement)tagsResponseNode.NextNode;
                List<FacebookPhotoTag> tags = _serializer.DeserializePhotoTagsList(tagsResponseNode, ns);

                foreach (var photo in photos)
                {
                    photo.RawTags.Merge(from tag in tags where tag.PhotoId == photo.PhotoId select tag, false);
                }

                photoCollections[albumCount] = photos;
                ++albumCount;
            }
            return photoCollections;
        }

        public List<ActivityComment> GetPhotoComments(string photoId)
        {
            var commentMap = new METHOD_MAP
            {
                { "method", "photos.getComments" },
                { "pid", photoId },
            };

            string response = Utility.FailableFunction(() => _SendRequest(commentMap));
            return _serializer.DeserializePhotoCommentsResponse(response);
        }

        public bool GetPhotoCanComment(string photoId)
        {
            var commentMap = new METHOD_MAP
            {
                { "method", "photos.canComment" },
                { "pid", photoId },
            };

            string response = Utility.FailableFunction(() => _SendRequest(commentMap));
            return _serializer.DeserializePhotoCanCommentResponse(response);
        }

        public string AddPhotoComment(string photoId, string comment)
        {
            var addMap = new METHOD_MAP
            {
                { "method", "photos.addComment" },
                { "pid", photoId },
                { "body", comment },
            };

            string response = Utility.FailableFunction(() => _SendRequest(addMap));
            return _serializer.DeserializePhotoAddCommentResponse(response);
        }

        public List<FacebookPhotoAlbum> GetUserAlbums(string userId)
        {
            Verify.IsNeitherNullNorEmpty(userId, "userId");

            string albumQueryResult = Utility.FailableFunction(() => _SendQuery(string.Format(_GetSingleUserAlbumsQueryString, userId)));
            return _serializer.DeserializeGetAlbumsResponse(albumQueryResult);
        }

        public void MarkNotificationsAsRead(params string[] notificationIds)
        {
            Verify.IsNotNull(notificationIds, "notificationIds");
            
            var sb = new StringBuilder();
            bool isFirst = true;
            foreach (string id in notificationIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    if (!isFirst)
                    {
                        sb.Append(",");
                    }
                    sb.Append(id);
                }
            }

            if (sb.Length == 0)
            {
                return;
            }
            
            var readMap = new METHOD_MAP
            {
                { "method", "Notifications.markRead" },
                { "notification_ids", sb.ToString() },
            };

            Utility.FailableFunction(() => _SendRequest(readMap));
        }

        public List<FacebookImage> GetProfilePictures(IEnumerable<FacebookContact> contacts)
        {
            var images = new List<FacebookImage>();
            foreach (var contact in contacts)
            {
                string response = Utility.FailableFunction(() => _SendQuery(string.Format(_GetSingleProfileInfoQueryString, contact.UserId)));
                List<FacebookContact> lite = _serializer.DeserializeProfileList(response);
                images.Add(lite[0].Image);
            }
            return images;
        }
    }
}
