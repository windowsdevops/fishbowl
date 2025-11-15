
namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using Standard;
    using System.Windows;

    internal class DataSerialization
    {
        // Just in case Facebook messes up and gives us bad data for an id that's supposed to be unique.  Don't let it crash the app.
        private static int _badFacebookCounter = 1;

        /// <summary>The start time for Unix based clocks.  Facebook usually returns their timestamps based on ticks from this value.</summary>
        private static readonly DateTime _UnixEpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts a timestamp from the facebook server into a UTC DateTime.
        /// </summary>
        /// <param name="ticks">The Unix epoch based tick count</param>
        /// <returns>The parameter as a DateTime.</returns>
        internal static DateTime GetDateTimeFromUnixTimestamp(long ticks)
        {
            return _UnixEpochTime.AddSeconds(ticks).ToLocalTime();
        }

        internal static long GetUnixTimestampFromDateTime(DateTime date)
        {
            date = date.ToUniversalTime();
            if (date > _UnixEpochTime)
            {
                return (long)(date - _UnixEpochTime).TotalSeconds;
            }
            return 0;
        }

        private readonly FacebookService _service;

        public DataSerialization(FacebookService service)
        {
            _service = service; 
        }

        private static string _SafeGetElementValue(XElement elt, params XName[] names)
        {
            if (elt == null)
            {
                return "";
            }

            foreach (var name in names)
            {
                elt = elt.Element(name);
                if (elt == null)
                {
                    return "";
                }
            }
            return elt.Value;
        }

        private static DateTime? _SafeGetElementDateTime(XElement elt, params XName[] names)
        {
            long? ticks = _SafeGetElementInt64(elt, names);
            if (ticks != null)
            {
                return GetDateTimeFromUnixTimestamp(ticks.Value);
            }
            return null;
        }

        private static string _SafeGetElementXml(XElement elt, params XName[] names)
        {
            if (elt == null)
            {
                return "";
            }

            foreach (var name in names)
            {
                elt = elt.Element(name);
                if (elt == null)
                {
                    return "";
                }
            }
            return elt.ToString();
        }

        private static float? _SafeGetElementSingle(XElement elt, params XName[] names)
        {
            if (elt == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                elt = elt.Element(name);
                if (elt == null)
                {
                    return null;
                }
            }
            float ret;
            if (float.TryParse(elt.Value, out ret))
            {
                return ret;
            }
            return null;
        }

        private static int? _SafeGetElementInt32(XElement elt, params XName[] names)
        {
            if (elt == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                elt = elt.Element(name);
                if (elt == null)
                {
                    return null;
                }
            }
            int ret;
            if (int.TryParse(elt.Value, out ret))
            {
                return ret;
            }
            return null;
        }

        private static long? _SafeGetElementInt64(XElement elt, params XName[] names)
        {
            if (elt == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                elt = elt.Element(name);
                if (elt == null)
                {
                    return null;
                }
            }
            long ret;
            if (long.TryParse(elt.Value, out ret))
            {
                return ret;
            }
            return null;
        }

        private static Uri _SafeGetElementUri(XElement elt, params XName[] names)
        {
            if (elt == null)
            {
                return null;
            }

            foreach (var name in names)
            {
                elt = elt.Element(name);
                if (elt == null)
                {
                    return null;
                }
            }
            Uri ret;
            Uri.TryCreate(elt.Value, UriKind.Absolute, out ret);
            if (ret != null)
            {
                Assert.IsTrue(ret.IsAbsoluteUri);
            }
            return ret;
        }

        private static string _SafeGetUniqueId()
        {
            return "FacebookGotItWrongCount_" + _badFacebookCounter++;
        }

        private static bool _IsValidXmlCharacter(char c)
        {
            // Obtained from section 2.2 of the XML 1.0 specification.
            // http://www.w3.org/TR/REC-xml/#NT-Char
            return c == 0x9 // '\t'
                || c == 0xA // '\n'
                || c == 0xD // '\r'
                || (c >= 0x20 && c <= 0xD7FF)
                || (c >= 0xE000 && c <= 0xFFFD);
                // These are called out in the spec, but they're outside the range of System.Char
                //|| (c >= unchecked(((char)0x10000)) && c <= unchecked((char)0x10FFFF));
        }

        // Facebook lets people put invalid characters into some of their fields.
        // They should validate their data before sending us XML files, but be that as it may
        // we should try to not crash.
        // Call this as a second retry when we get XML that we can't open...
        private static string _SterilizeBadXml(string xml)
        {
            // Epic fail.
            StringBuilder sb = new StringBuilder(xml.Length);
            bool containedBadData = false;
            foreach (char c in xml)
            {
                if (_IsValidXmlCharacter(c))
                {
                    sb.Append(c);
                }
                else 
                {
                    containedBadData = true;
                }
            }

            if (containedBadData)
            {
                return sb.ToString();
            }
            return null;
        }

        public static XDocument SafeParseDocument(string xml)
        {
            try
            {
                return XDocument.Parse(xml);
            }
            catch (XmlException e)
            {
                // Awesome.  Facebook sometimes returns us HTML in response to a REST API call.
                // Just detect this and bail.
                if (xml.StartsWith("<!DOCTYPE html"))
                {
                    // BADBADBADFACEBOOK.
                    throw new FacebookException("Received HTML from Facebook where XML was expected.", e);
                }
                xml = _SterilizeBadXml(xml);
                if (xml == null)
                {
                    throw;
                }
                return XDocument.Parse(xml);
            }
        }

        private ActivityComment _DeserializeCommentData(XNamespace ns, XElement elt)
        {
            return new ActivityComment(_service)
            {
                CommentType = ActivityComment.Type.ActivityPost,
                FromUserId = _SafeGetElementValue(elt, ns + "fromid"),
                Time = _SafeGetElementDateTime(elt, ns + "time") ?? _UnixEpochTime,
                Text = _SafeGetElementValue(elt, ns + "text"),
                CommentId = _SafeGetElementValue(elt, ns + "id"),
            };
        }

        private FacebookContact _DeserializeUser(XNamespace ns, XElement elt)
        {
            Uri sourceUri = _SafeGetElementUri(elt, ns + "pic");
            Uri sourceBigUri = _SafeGetElementUri(elt, ns + "pic_big");
            Uri sourceSmallUri = _SafeGetElementUri(elt, ns + "pic_small");
            Uri sourceSquareUri = _SafeGetElementUri(elt, ns + "pic_square");

            Location currentLocation = null;
            Location hometownLocation = null;
            HighSchoolInfo hsInfo = null;
            List<EducationInfo> educationHistory = null;
            List<WorkInfo> workHistory = null;
            
            XElement clElement = elt.Element(ns + "current_location");
            if (clElement != null)
            {
                currentLocation = _DeserializeLocation(ns, clElement);
            }

            XElement htElement = elt.Element(ns + "hometown_location");
            if (htElement != null)
            {
                hometownLocation = _DeserializeLocation(ns,  htElement);
            }

            XElement hsElement = elt.Element(ns + "hs_info");
            if (hsElement != null)
            {
                hsInfo = new HighSchoolInfo
                {
                    GraduationYear = _SafeGetElementInt32(hsElement, ns + "grad_year"),
                    //Id = _SafeGetElementValue(hsElement, ns + "hs1_id") ?? "0",
                    //Id2 = _SafeGetElementValue(hsElement, ns + "hs2_id") ?? "0",
                    Name = _SafeGetElementValue(hsElement, ns + "hs1_name"),
                    Name2 = _SafeGetElementValue(hsElement, ns + "hs2_name"),
                };
            }

            XElement ehElement = elt.Element(ns + "education_history");
            if (ehElement != null)
            {
                educationHistory = new List<EducationInfo>(
                    from infoNode in ehElement.Elements(ns + "education_info")
                    select _DeserializeEducationInfo(ns, infoNode));
            }
            else
            {
                educationHistory = new List<EducationInfo>();
            }

            XElement whElement = elt.Element(ns + "work_history");
            if (whElement != null)
            {
                workHistory = new List<WorkInfo>(
                    from wiNode in whElement.Elements(ns + "work_info")
                    select _DeserializeWorkInfo(ns, wiNode));
            }

            var contact = new FacebookContact(_service)
            {
                Name = _SafeGetElementValue(elt, ns + "name"),
                FirstName = _SafeGetElementValue(elt, ns + "first_name"),
                LastName = _SafeGetElementValue(elt, ns + "last_name"),

                AboutMe = _SafeGetElementValue(elt, ns + "about_me"),
                Activities = _SafeGetElementValue(elt, ns + "activities"),
                // Affilitions =
                // AllowedRestrictions = 
                Birthday = _SafeGetElementValue(elt, ns + "birthday"),
                MachineSafeBirthday = _SafeGetElementValue(elt, ns + "birthday_date"),
                Books = _SafeGetElementValue(elt, ns + "books"),
                CurrentLocation = currentLocation,
                EducationHistory = educationHistory.AsReadOnly(),
                Hometown = hometownLocation,
                HighSchoolInfo = hsInfo,
                Interests = _SafeGetElementValue(elt, ns + "interests"),
                Image = new FacebookImage(_service, sourceUri, sourceBigUri, sourceSmallUri, sourceSquareUri),
                Movies = _SafeGetElementValue(elt, ns + "movies"),
                Music = _SafeGetElementValue(elt, ns + "music"),
                Quotes = _SafeGetElementValue(elt, ns + "quotes"),
                RelationshipStatus = _SafeGetElementValue(elt, ns + "relationship_status"),
                Religion = _SafeGetElementValue(elt, ns + "religion"),
                Sex = _SafeGetElementValue(elt, ns + "sex"),
                TV = _SafeGetElementValue(elt, ns + "tv"),
                Website = _SafeGetElementValue(elt, "website"),
                ProfileUri = _SafeGetElementUri(elt, ns + "profile_url"),
                UserId = _SafeGetElementValue(elt, ns + "uid"),
                ProfileUpdateTime = _SafeGetElementDateTime(elt, ns + "profile_update_time") ?? _UnixEpochTime,
                OnlinePresence = _DeserializePresenceNode(ns, elt),
            };

            if (!string.IsNullOrEmpty(_SafeGetElementValue(elt, ns + "status", ns + "message")))
            {
                contact.StatusMessage = new ActivityPost(_service)
                {
                    PostId = "status_" + contact.UserId,
                    ActorUserId = _SafeGetElementValue(elt, ns + "uid"),
                    Created = _SafeGetElementDateTime(elt, ns + "status", ns + "time") ?? _UnixEpochTime,
                    Updated = _SafeGetElementDateTime(elt, ns + "status", ns + "time") ?? _UnixEpochTime,
                    Message = _SafeGetElementValue(elt, ns + "status", ns + "message"),
                    TargetUserId = "",
                    CanLike = false,
                    HasLiked = false,
                    LikedCount = 0,
                    CanComment = false,
                    CanRemoveComments = false,
                    CommentCount = 0,
                };
            }

            return contact;
        }

        private static OnlinePresence _DeserializePresenceNode(XNamespace ns, XElement elt)
        {
            string presence = _SafeGetElementValue(elt, ns + "online_presence");
            switch (presence)
            {
                case "active":  return OnlinePresence.Active;
                case "idle":    return OnlinePresence.Idle;
                case "offline": return OnlinePresence.Offline;
                case "error":   return OnlinePresence.Unknown;
            }
            return OnlinePresence.Unknown;
        }

        private WorkInfo _DeserializeWorkInfo(XNamespace ns, XElement elt)
        {
            Assert.IsNotNull(elt);
            Assert.IsNotNull(ns);

            return new WorkInfo
            {
                CompanyName = _SafeGetElementValue(elt, ns + "company_name"),
                Description = _SafeGetElementValue(elt, ns + "description"),
                EndDate = _SafeGetElementValue(elt, ns + "end_date"),
                StartDate = _SafeGetElementValue(elt, ns + "start_date"),
                Location = _DeserializeLocation(ns, elt.Element(ns + "location")),
            };
        }

        private static Location _DeserializeLocation(XNamespace ns, XElement elt)
        {
            if (elt == null)
            {
                return null;
            }

            return new Location
            {
                // current_location: city, state, country (well defined), zip (may be zero)
                City = _SafeGetElementValue(elt, ns + "city"),
                Country = _SafeGetElementValue(elt, ns + "country"),
                State = _SafeGetElementValue(elt, ns + "state"),
                ZipCode = _SafeGetElementInt32(elt, ns + "zip")
            };
        }

        private EducationInfo _DeserializeEducationInfo(XNamespace ns, XElement infoNode)
        {
            int? maybeYear = _SafeGetElementInt32(infoNode, ns + "year") ?? 0;
            if (maybeYear == 0)
            {
                maybeYear = null;
            }

            var concentrationBuilder = new StringBuilder();
            bool first = true;
            foreach (string conString in from c in infoNode.Elements(ns + "concentrations") select c.Value)
            {
                if (!first)
                {
                    concentrationBuilder.Append(", ");
                }
                else
                {
                    first = false;
                }

                concentrationBuilder.Append(conString);
            }

            return new EducationInfo
            {
                Concentrations = concentrationBuilder.ToString(),
                Degree = _SafeGetElementValue(infoNode, ns + "degree"),
                Name = _SafeGetElementValue(infoNode, ns + "name"),
                Year = maybeYear,
            };
        }

        private FacebookPhotoTag _DeserializePhotoTag(XNamespace ns, XElement elt)
        {
            float xcoord = (_SafeGetElementSingle(elt, ns + "xcoord") ?? 0) / 100;
            float ycoord = (_SafeGetElementSingle(elt, ns + "ycoord") ?? 0) / 100;

            xcoord = Math.Max(Math.Min(1, xcoord), 0);
            ycoord = Math.Max(Math.Min(1, ycoord), 0);

            var tag = new FacebookPhotoTag(_service) 
            {
                PhotoId = _SafeGetElementValue(elt, ns + "pid"),
                ContactId = _SafeGetElementValue(elt, ns + "subject"),
                Text = _SafeGetElementValue(elt, ns + "text"),
                Offset = new System.Windows.Point(xcoord, ycoord),
            };

            return tag;
        }

        private ActivityPost _DeserializePostData(XNamespace ns, XElement elt)
        {
            var post = new ActivityPost(_service);

            var attachmentElement = elt.Element(ns + "attachment");
            if (attachmentElement != null)
            {
                string postType = _SafeGetElementValue(elt, ns + "attachment", ns + "media", ns + "stream_media", ns + "type");

                switch (postType)
                {
                    case "photo":
                        post.Attachment = _DeserializePhotoPostAttachmentData(post, ns, attachmentElement);
                        break;
                    case "link":
                        post.Attachment = _DeserializeLinkPostAttachmentData(post, ns, attachmentElement);
                        break;
                    case "video":
                        post.Attachment = _DeserializeVideoPostAttachmentData(post, ns, attachmentElement);
                        break;

                    // We're not currently supporting music or flash.  Just treat it like a normal post...
                    case "music":
                    case "swf":

                    case "":
                    case null:
                        if (attachmentElement.Elements().Count() != 0)
                        {
                            // We have attachment information but no rich stream-media associated with it.
                            post.Attachment = _DeserializeGenericPostAttachmentData(post, ns, attachmentElement);
                            post.Attachment.Type = ActivityPostAttachmentType.Simple;
                        }
                        break;
                    default:
                        Assert.Fail("Unknown type:" + postType);
                        break;
                }
            }

            XElement commentsElement = elt.Element(ns + "comments");

            post.PostId = _SafeGetElementValue(elt, ns + "post_id");
            if (string.IsNullOrEmpty(post.PostId))
            {
                // Massive Facebook failure.
                Assert.Fail();
                post.PostId = _SafeGetUniqueId();
            }
            post.ActorUserId = _SafeGetElementValue(elt, ns + "actor_id");
            post.Created = _SafeGetElementDateTime(elt, ns + "created_time") ?? _UnixEpochTime;
            post.Message = _SafeGetElementValue(elt, ns + "message");
            post.TargetUserId = _SafeGetElementValue(elt, ns + "target_id");
            post.Updated = _SafeGetElementDateTime(elt, ns + "updated_time") ?? _UnixEpochTime;

            XElement likesElement = elt.Element(ns + "likes");
            if (likesElement != null)
            {
                post.CanLike = _SafeGetElementValue(likesElement, ns + "can_like") == "1";
                post.HasLiked = _SafeGetElementValue(likesElement, ns + "user_likes") == "1";
                post.LikedCount = _SafeGetElementInt32(likesElement, ns + "count") ?? 0;
                post.LikeUrl = _SafeGetElementUri(likesElement, ns + "likes", ns + "href");
                XElement friendsElement = likesElement.Element(ns + "friends");
                XElement sampleElement = likesElement.Element(ns + "sample");
                post.RawPeopleWhoLikeThisIds = new MergeableCollection<string>(
                    Enumerable.Union(
                        sampleElement == null
                            ? new string[0]
                            : from uidElement in sampleElement.Elements(ns + "uid") select uidElement.Value,
                        friendsElement == null
                            ? new string[0]
                            : from uidElement in friendsElement.Elements(ns + "uid") select uidElement.Value));
            }

            post.CanComment = _SafeGetElementValue(commentsElement, ns + "can_post") == "1";
            post.CanRemoveComments = _SafeGetElementValue(commentsElement, ns + "can_remove") == "1";
            post.CommentCount = _SafeGetElementInt32(commentsElement, ns + "count") ?? 0;

            if (commentsElement != null && post.CommentCount != 0)
            {
                XElement commentListElement = commentsElement.Element(ns + "comment_list");
                if (commentListElement != null)
                {
                    var commentNodes = from XElement celt in commentListElement.Elements(ns + "comment")
                                       let comment = _DeserializeCommentData(ns, celt)
                                       where (comment.Post = post) != null
                                       select comment;

                    post.RawComments = new MergeableCollection<ActivityComment>(commentNodes);
                }
            }

            if (post.RawComments == null)
            {
                post.RawComments = new MergeableCollection<ActivityComment>();
            }

            // post.Comments = null;

            return post;
        }

        private ActivityPostAttachment _DeserializePhotoPostAttachmentData(ActivityPost post, XNamespace ns, XElement elt)
        {
            if (elt == null)
            {
                return null;
            }

            ActivityPostAttachment attachment = _DeserializeGenericPostAttachmentData(post, ns, elt);
            attachment.Type = ActivityPostAttachmentType.Photos;

            var photosEnum = from smElement in elt.Element(ns + "media").Elements(ns + "stream_media")
                             let photoElement = smElement.Element(ns + "photo")
                             where photoElement != null
                             let link = _SafeGetElementUri(smElement, ns + "href")
                             select new FacebookPhoto(
                                 _service,
                                 _SafeGetElementValue(photoElement, ns + "aid"),
                                 _SafeGetElementValue(photoElement, ns + "pid"),
                                 _SafeGetElementUri(smElement, ns + "src"))
                                 {
                                     Link = link,
                                     OwnerId = _SafeGetElementValue(photoElement, ns + "owner"),
                                 };
            attachment.Photos = FacebookPhotoCollection.CreateStaticCollection(photosEnum);

            return attachment;
        }

        private ActivityPostAttachment _DeserializeVideoPostAttachmentData(ActivityPost post, XNamespace ns, XElement elt)
        {
            if (elt == null)
            {
                return null;
            }

            ActivityPostAttachment attachment = _DeserializeGenericPostAttachmentData(post, ns, elt);
            Uri previewImage = _SafeGetElementUri(elt, ns + "media", ns + "preview_img");

            attachment.Type = ActivityPostAttachmentType.Video;
            XElement mediaElement = elt.Element(ns + "media");
            if (mediaElement != null)
            {
                XElement streamElement = mediaElement.Element(ns + "stream_media");
                if (streamElement != null)
                {
                    Uri previewImageUri = _SafeGetElementUri(streamElement, ns + "src");

                    attachment.VideoPreviewImage = new FacebookImage(_service, previewImageUri);
                    attachment.VideoSource = _SafeGetElementUri(streamElement, ns + "href");
                    // Not using this one because of a bug in Adobe's player when loading in an external browser...
                    //XElement videoElement = streamElement.Element(ns + "video");
                    //if (videoElement != null)
                    //{
                    //    attachment.VideoSource = _SafeGetElementUri(videoElement, ns + "source_url");
                    //}
                }
            }

            return attachment;
        }

        private ActivityPostAttachment _DeserializeLinkPostAttachmentData(ActivityPost post, XNamespace ns, XElement elt)
        {
            if (elt == null)
            {
                return null;
            }

            ActivityPostAttachment attachment = _DeserializeGenericPostAttachmentData(post, ns, elt);
            attachment.Type = ActivityPostAttachmentType.Links;

            var linksEnum = from smElement in elt.Element(ns + "media").Elements(ns + "stream_media")
                            let srcUri = _SafeGetElementUri(smElement, ns + "src")
                            let hrefUri = _SafeGetElementUri(smElement, ns + "href")
                            where srcUri != null && hrefUri != null
                            select new FacebookImageLink()
                            {
                                Image = new FacebookImage(_service, srcUri),
                                Link = hrefUri,
                            };
            attachment.Links = new FacebookCollection<FacebookImageLink>(linksEnum);
            return attachment;
        }

        private ActivityPostAttachment _DeserializeGenericPostAttachmentData(ActivityPost post, XNamespace ns, XElement elt)
        {
            Assert.IsNotNull(post);
            Uri iconUri = _SafeGetElementUri(elt, ns + "icon");

            return new ActivityPostAttachment(post)
            {
                Caption = _SafeGetElementValue(elt, ns + "caption"),
                Link = _SafeGetElementUri(elt, ns + "href"),
                Name = _SafeGetElementValue(elt, ns + "name"),
                Description = _SafeGetElementValue(elt, ns + "description"),
                Properties = _SafeGetElementXml(elt, ns + "properties"),
                Icon = new FacebookImage(_service, iconUri),
            };
        }

        public List<FacebookContact> DeserializeUsersList(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            var userNodes = from XElement elt in ((XElement)xdoc.FirstNode).Elements(ns + "user")
                            select _DeserializeUser(ns, elt);
            return new List<FacebookContact>(userNodes);
        }

        public List<FacebookContact> DeserializeUsersListWithProfiles(XNamespace ns, XElement usersElement, XElement profilesElement)
        {
            var contacts = usersElement.Elements(ns + "user").Zip(
                profilesElement.Elements(ns + "profile"),
                (userElt, profileElt) =>
                {
                    var c = _DeserializeUser(ns, userElt);
                    c.MergeImage(_DeserializeProfile(ns, profileElt).Image);
                    return c;
                });
            return new List<FacebookContact>(contacts);
        }

        public Dictionary<string, OnlinePresence> DeserializeUserPresenceList(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            var items = from userNode in xdoc.Root.Elements(ns + "user")
                        let uid = _SafeGetElementValue(userNode, ns + "uid")
                        let presence = _DeserializePresenceNode(ns, userNode)
                        select new { UserId = uid, Presence = presence };
            return items.ToDictionary(item => item.UserId, item => item.Presence);
        }
        
        public List<FacebookContact> DeserializeProfileList(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            return DeserializeProfileList(ns, xdoc.Root); 
        }

        public List<FacebookContact> DeserializeProfileList(XNamespace ns, XElement elt)
        {
            var profileNodes = from XElement elt2 in elt.Elements(ns + "profile")
                               select _DeserializeProfile(ns, elt2);
            return new List<FacebookContact>(profileNodes);
        }

        private FacebookContact _DeserializeProfile(XNamespace ns, XElement elt)
        {
            Uri sourceUri = _SafeGetElementUri(elt, ns + "pic");
            Uri sourceBigUri = _SafeGetElementUri(elt, ns + "pic_big");
            Uri sourceSmallUri = _SafeGetElementUri(elt, ns + "pic_small");
            Uri sourceSquareUri = _SafeGetElementUri(elt, ns + "pic_square");

            var profile = new FacebookContact(_service)
            {
                UserId = _SafeGetElementValue(elt, ns + "id"),
                Name = _SafeGetElementValue(elt, ns + "name"),
                Image = new FacebookImage(_service, sourceUri, sourceBigUri, sourceSmallUri, sourceSquareUri),
                ProfileUri = _SafeGetElementUri(elt, ns + "url"),
                // ContactType = "type" => "user" | "page"
            };

            return profile;
        }

        public List<FacebookPhotoTag> DeserializePhotoTagsList(XElement root, XNamespace ns)
        {
            var tagList = new List<FacebookPhotoTag>();

            var tagNodes = from XElement elt in root.Elements(ns + "photo_tag")
                           let tag = _DeserializePhotoTag(ns, elt)
                           where tag != null
                           select tag;
            tagList.AddRange(tagNodes);
            return tagList;
        }

        public List<FacebookPhotoTag> DeserializePhotoTagsList(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            return DeserializePhotoTagsList((XElement)xdoc.FirstNode, ns);
        }

        public List<ActivityFilter> DeserializeFilterList(string xml)
        {
            var filterList = new List<ActivityFilter>();

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            var filterNodes = from XElement elt in ((XElement)xdoc.FirstNode).Elements(ns + "stream_filter")
                              select _DeserializeFilter(ns, elt);
            filterList.AddRange(filterNodes);
            return filterList;
        }

        private ActivityFilter _DeserializeFilter(XNamespace ns, XElement elt)
        {
            Uri iconUri = _SafeGetElementUri(elt, ns + "icon_url");

            var filter = new ActivityFilter(_service)
            {
                // "uid" maps to the current user's UID.
                // "value" is a sometimes nil integer value.  Not sure what it's for.
                Key = _SafeGetElementValue(elt, ns + "filter_key"),
                Name = _SafeGetElementValue(elt, ns + "name"),
                Rank = _SafeGetElementInt32(elt, ns + "rank") ?? Int32.MaxValue,
                // Facebook gives us an image map of both selected and not versions of the icon.
                // The right half is the selected state, so just return that as the image.
                Icon = new FacebookImage(_service, iconUri, new Thickness(.5, 0, 0, 0)),
                IsVisible = (_SafeGetElementInt32(elt, ns + "is_visible") ?? 0) != 0,
                FilterType = _SafeGetElementValue(elt, ns + "type"),
            };

            return filter;
        }

        public void DeserializeStreamData(string xml, out List<ActivityPost> posts, out List<FacebookContact> userData)
        {
            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            posts = _DeserializePostDataList(ns, ((XElement)xdoc.FirstNode).Element(ns + "posts"));
            userData = DeserializeProfileList(ns, ((XElement)xdoc.FirstNode).Element(ns + "profiles"));
        }

        public List<ActivityPost> DeserializePostDataList(string xml, bool isFQL)
        {

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            XContainer rootList = null;
            if (isFQL)
            {
                rootList = ((XElement)xdoc.FirstNode);
            }
            else
            {
                rootList = ((XElement)xdoc.FirstNode).Element(ns + "posts");
            }

            return _DeserializePostDataList(ns, rootList);
        }

        private List<ActivityPost> _DeserializePostDataList(XNamespace ns, XContainer root)
        {
            var postsList = new List<ActivityPost>();
            var postNodes = from XElement elt in root.Elements(ns + "stream_post")
                            select _DeserializePostData(ns, elt);
            postsList.AddRange(postNodes);
            return postsList;
        }

        public List<ActivityComment> DeserializeCommentsDataList(ActivityPost post, string xml)
        {
            var commentList = new List<ActivityComment>();

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            var commentNodes = from XElement elt in ((XElement)xdoc.FirstNode).Elements(ns + "comment")
                               let comment = _DeserializeCommentData(ns, elt)
                               where (comment.Post = post) != null
                               select comment;
            commentList.AddRange(commentNodes);
            return commentList;
        }

        public static void DeserializeSessionInfo(string xml, out string sessionKey, out string userId)
        {
            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            sessionKey = _SafeGetElementValue(xdoc.Root, ns + "session_key");
            userId = _SafeGetElementValue(xdoc.Root, ns + "uid");
        }

        public List<FacebookPhoto> DeserializePhotosGetResponse(string xml)
        {
            var photoList = new List<FacebookPhoto>();

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            return DeserializePhotosGetResponse((XElement)xdoc.FirstNode, ns);
        }

        public List<FacebookPhoto> DeserializePhotosGetResponse(XElement root, XNamespace ns)
        {
            var photoList = new List<FacebookPhoto>();

            var photoNodes = from XElement elt in root.Elements(ns + "photo")
                             select _DeserializePhotoData(ns, elt);
            photoList.AddRange(photoNodes);
            return photoList;
        }

        private FacebookPhoto _DeserializePhotoData(XNamespace ns, XElement elt)
        {
            Uri linkUri = _SafeGetElementUri(elt, ns + "link");
            Uri sourceUri = _SafeGetElementUri(elt, ns + "src");
            Uri smallUri = _SafeGetElementUri(elt, ns + "src_small");
            Uri bigUri = _SafeGetElementUri(elt, ns + "src_big");

            var photo = new FacebookPhoto(_service)
            {
                PhotoId = _SafeGetElementValue(elt, ns + "pid"),
                AlbumId = _SafeGetElementValue(elt, ns + "aid"),
                OwnerId = _SafeGetElementValue(elt, ns + "owner"),
                Caption = _SafeGetElementValue(elt, ns + "caption"),
                Created = _SafeGetElementDateTime(elt, ns + "created") ?? _UnixEpochTime,
                Image = new FacebookImage(_service, sourceUri, bigUri, smallUri, null),
                Link = linkUri,
            };

            return photo;
        }

        public FacebookPhoto DeserializePhotoUploadResponse(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            // photos.Upload returns photo data embedded in the root node.
            return _DeserializePhotoData(ns, xdoc.Root);
        }

        public FacebookPhotoAlbum DeserializeUploadAlbumResponse(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            // photos.Upload returns photo data embedded in the root node.
            FacebookPhotoAlbum album = _DeserializeAlbumData(ns, xdoc.Root);
            return album;
        }

        public List<FacebookPhotoAlbum> DeserializeGetAlbumsResponse(string xml)
        {
            var albumList = new List<FacebookPhotoAlbum>();

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            var albumNodes = from XElement elt in ((XElement)xdoc.FirstNode).Elements(ns + "album")
                             select _DeserializeAlbumData(ns, elt);
            albumList.AddRange(albumNodes);
            return albumList;
        }

        private FacebookPhotoAlbum _DeserializeAlbumData(XNamespace ns, XElement elt)
        {
            Uri linkUri = _SafeGetElementUri(elt, ns + "link");

            var album = new FacebookPhotoAlbum(_service)
            {
                AlbumId = _SafeGetElementValue(elt, ns + "aid"),
                CoverPicPid = _SafeGetElementValue(elt, ns + "cover_pid"),
                OwnerId = _SafeGetElementValue(elt, ns + "owner"),
                Title = _SafeGetElementValue(elt, ns + "name"),
                Created = _SafeGetElementDateTime(elt, ns + "created") ?? _UnixEpochTime,
                LastModified = _SafeGetElementDateTime(elt, ns + "modified") ?? _UnixEpochTime,
                Description = _SafeGetElementValue(elt, ns + "description"),
                Location = _SafeGetElementValue(elt, ns + "location"),
                Link = linkUri,
                // Size = _SafeGetElementInt32(elt, ns + "size"),
                // Visible = _SafeGetElementValue(elt, ns + "visible"),
            };

            return album;
        }

        public static Exception DeserializeFacebookException(string xml, string requestXml)
        {
            // Do a sanity check on the opening XML tags to see if it looks like an exception.
            if (xml.Substring(0, Math.Min(xml.Length, 200)).Contains("<error_response"))
            {
                XDocument xdoc = SafeParseDocument(xml);
                XNamespace ns = xdoc.Root.GetDefaultNamespace();
                if (xdoc.Root.Name == ns + "error_response")
                {
                    return new FacebookException(
                        xml,
                        _SafeGetElementInt32(xdoc.Root, ns + "error_code") ?? 0,
                        _SafeGetElementValue(xdoc.Root, ns + "error_msg"),
                        requestXml);
                }
            }

            return null;
        }

        public void DeserializeNotificationsGetResponse(string xml, out List<Notification> friendRequests, out int unreadMessageCount)
        {
            var notificationList = new List<Notification>();

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            // Get friend requests
            notificationList.AddRange(
                from XElement elt in ((XElement)xdoc.FirstNode).Element(ns + "friend_requests").Elements(ns + "uid")
                let uid = _SafeGetElementValue(elt)
                where !string.IsNullOrEmpty(uid)
                select (Notification)new FriendRequestNotification(_service, uid));

            unreadMessageCount = _SafeGetElementInt32((XElement)xdoc.FirstNode, ns + "messages", ns + "unread") ?? 0;
            friendRequests = notificationList;
        }

        public List<Notification> DeserializeNotificationsListResponse(string xml)
        {
            var notificationList = new List<Notification>();

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            // I should also be able to use the "apps" list to get the icons to display next to the notification.
            var notificationNodes = from XElement elt in ((XElement)xdoc.FirstNode).Element(ns + "notifications").Elements(ns + "notification")
                                    select _DeserializeNotificationData(ns, elt);
            notificationList.AddRange(notificationNodes);
            return notificationList;
        }

        private Notification _DeserializeNotificationData(XNamespace ns, XElement elt)
        {
            // To make these consistent with the rest of Facebook's HTML, enclose these in div tags if they're present.
            string bodyHtml = _SafeGetElementValue(elt, ns + "body_html");
            if (!string.IsNullOrEmpty(bodyHtml))
            {
                bodyHtml = "<div>" + bodyHtml + "</div>";
            }

            string titleHtml = _SafeGetElementValue(elt, ns + "title_html");
            if (!string.IsNullOrEmpty(titleHtml))
            {
                titleHtml = "<div>" + titleHtml + "</div>";
            }

            var notification = new Notification(_service)
            {
                Created = _SafeGetElementDateTime(elt, ns + "created_time") ?? _UnixEpochTime,
                Description = bodyHtml,
                DescriptionText = _SafeGetElementValue(elt, ns + "body_text"),
                IsHidden = _SafeGetElementValue(elt, ns + "is_hidden") == "1",
                IsUnread = _SafeGetElementValue(elt, ns + "is_unread") == "1",
                Link = _SafeGetElementUri(elt, ns + "href"),
                NotificationId = _SafeGetElementValue(elt, ns + "notification_id"),
                RecipientId = _SafeGetElementValue(elt, ns + "recipient_id"),
                SenderId = _SafeGetElementValue(elt, ns + "sender_id"),
                Title = titleHtml,
                TitleText = _SafeGetElementValue(elt, ns + "title_text"),
                Updated = _SafeGetElementDateTime(elt, ns + "updated_time") ?? _UnixEpochTime,
            };

            return notification;
        }

        public string DeserializeAddCommentResponse(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            return xdoc.Root.Value;
        }

        public List<ActivityComment> DeserializePhotoCommentsResponse(string xml)
        {
            var commentList = new List<ActivityComment>();

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            var commentNodes = from XElement elt in ((XElement)xdoc.FirstNode).Elements(ns + "photo_comment")
                               select _DeserializePhotoCommentData(ns, elt);
            commentList.AddRange(commentNodes);
            return commentList;
        }

        private ActivityComment _DeserializePhotoCommentData(XNamespace ns, XElement elt)
        {
            var comment = new ActivityComment(_service)
            {
                CommentType = ActivityComment.Type.Photo,
                CommentId = _SafeGetElementValue(elt, ns + "pcid"),
                FromUserId = _SafeGetElementValue(elt, ns + "from"),
                Time = _SafeGetElementDateTime(elt, ns + "time") ?? _UnixEpochTime,
                Text = _SafeGetElementValue(elt, ns + "body"),
            };
            return comment;
        }

        public bool DeserializePhotoCanCommentResponse(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            return xdoc.Root.Value == "1";
        }

        public string DeserializePhotoAddCommentResponse(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            return xdoc.Root.Value;
        }

        public string DeserializeStreamPublishResponse(string xml)
        {
            XDocument xdoc = SafeParseDocument(xml);
            return xdoc.Root.Value;
        }

        public List<MessageNotification> DeserializeMessageQueryResponse(string xml)
        {
            var messageList = new List<MessageNotification>();

            XDocument xdoc = SafeParseDocument(xml);
            XNamespace ns = xdoc.Root.GetDefaultNamespace();

            var messageNodes = from XElement elt in ((XElement)xdoc.FirstNode).Elements(ns + "thread")
                               select _DeserializeMessageNotificationData(ns, elt);
            messageList.AddRange(messageNodes);
            return messageList;
        }

        private MessageNotification _DeserializeMessageNotificationData(XNamespace ns, XElement elt)
        {
            var message = new MessageNotification(_service)
            {
                Created = _SafeGetElementDateTime(elt, ns + "updated_time") ?? _UnixEpochTime,
                IsUnread = _SafeGetElementValue(elt, ns + "unread") == "1",
                DescriptionText = _SafeGetElementValue(elt, ns + "snippet"),
                IsHidden = false,
                NotificationId = _SafeGetElementValue(elt, ns + "thread_id"),
                // TODO: This is actually a list of recipients.
                RecipientId = _SafeGetElementValue(elt, ns + "recipients", ns + "uid"),
                SenderId = _SafeGetElementValue(elt, ns + "snippet_author"),
                Title = _SafeGetElementValue(elt, ns + "subject"),
                Updated = _SafeGetElementDateTime(elt, ns + "updated_time") ?? DateTime.Now,
            };

            if (!string.IsNullOrEmpty(message.NotificationId))
            {
                message.Link = new Uri(string.Format("http://www.facebook.com/inbox/#/inbox/?folder=[fb]messages&page=1&tid={0}", message.NotificationId));
            }
            else
            {
                Assert.Fail();
                message.NotificationId = _SafeGetUniqueId();
                message.Link = new Uri("http://www.facebook.com/inbox");
            }

            return message;
        }
    }
}
