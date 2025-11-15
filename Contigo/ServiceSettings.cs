using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Standard;
using System.IO;
using System.Xml.Linq;

namespace Contigo
{
    internal class ServiceSettings
    {
        private const string _SettingsFileName = "ServiceSettings.xml";
        private readonly string _settingsPath;
        private readonly Dictionary<string, double> _userLookupInterestLevels = new Dictionary<string, double>();
        private readonly HashSet<string> _ignoredFriendRequests = new HashSet<string>();
        private readonly object _lock = new object();

        private string _sessionKey;
        private string _userId;

        public ServiceSettings(string folderPath)
        {
            Utility.EnsureDirectory(folderPath);

            _settingsPath = Path.Combine(folderPath, _SettingsFileName);
            try
            {
                if (File.Exists(_settingsPath))
                {
                    XDocument xdoc = XDocument.Load(_settingsPath);

                    if (1 != (int)xdoc.Root.Attribute("v"))
                    {
                        return; 
                    }

                    XElement contactsElement = xdoc.Root.Element("contacts");
                    if (contactsElement != null)
                    {
                        foreach (var interestInfo in
                            from contactNode in contactsElement.Elements("contact")
                            select new
                            {
                                UserId = (string)contactNode.Attribute("uid"),
                                InterestLevel = (double)contactNode.Attribute("interestLevel")
                            })
                        {
                            _userLookupInterestLevels.Add(interestInfo.UserId, interestInfo.InterestLevel);
                        }
                    }

                    XElement knownFriendRequestsElement = xdoc.Root.Element("knownFriendRequests");
                    if (knownFriendRequestsElement != null)
                    {
                        foreach (var requestInfo in
                            from contactNode in knownFriendRequestsElement.Elements("contact")
                            select (string)contactNode.Attribute("uid"))
                        {
                            _ignoredFriendRequests.Add(requestInfo);
                        }
                    }
                }
            }
            catch (Exception)
            {
                _userLookupInterestLevels.Clear();
                _sessionKey = null;
                _userId = null;
            }
        }

        public bool IsFriendRequestKnown(string uid)
        {
            lock (_lock)
            {
                return _ignoredFriendRequests.Contains(uid);
            }
        }

        public void MarkFriendRequestAsRead(string userId)
        {
            lock (_lock)
            {
                if (!_ignoredFriendRequests.Contains(userId))
                {
                    _ignoredFriendRequests.Add(userId);
                }
            }
        }

        // We don't want to keep a list of people who have requested friend status
        // but who have been either friended or really ignored from the website.
        // FacebookService should call this periodically to keep the list trimmed.
        public void RemoveKnownFriendRequestsExcept(List<string> uids)
        {
            lock (_lock)
            {
                _ignoredFriendRequests.RemoveWhere(uid => !uids.Contains(uid));
            }
        }

        public void AddInterestLevel(string userId, double value)
        {
            lock (_lock)
            {
                Assert.IsNeitherNullNorEmpty(userId);
                _userLookupInterestLevels[userId] = value;
            }
        }

        public double? GetInterestLevel(string userId)
        {
            lock (_lock)
            {
                double i;
                if (_userLookupInterestLevels.TryGetValue(userId, out i))
                {
                    return i;
                }
                return null;
            }
        }

        public bool GetSessionInfo(out string sessionKey, out string userId)
        {
            lock (_lock)
            {
                sessionKey = _sessionKey;
                userId = _userId;

                return _sessionKey != null;
            }
        }

        public void ClearSessionInfo()
        {
            SaveSessionInfo(null, null);
        }

        public void SaveSessionInfo(string sessionKey, string userId)
        {
            lock (_lock)
            {
                _sessionKey = sessionKey;
                _userId = userId;
            }
        }

        public void Save()
        {
            lock (_lock)
            {
                XElement xml = new XElement("settings",
                    new XAttribute("v", 1),
                    new XElement("sessionInfo",
                        new XElement("sessionKey", _sessionKey),
                        new XElement("userId", _userId)),
                    new XElement("contacts",
                        from pair in _userLookupInterestLevels
                        where pair.Value != FacebookContact.DefaultInterestLevel
                        select new XElement("contact",
                            new XAttribute("interestLevel", pair.Value),
                            new XAttribute("uid", pair.Key))),
                    new XElement("knownFriendRequests",
                        from uid in _ignoredFriendRequests
                        select new XElement("contact",
                            new XAttribute("uid", uid))));
                   
                xml.Save(_settingsPath);
            }
        }
    }
}
