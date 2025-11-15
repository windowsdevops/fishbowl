
namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using Standard;
    
    internal sealed class AsyncWebGetter : IDisposable
    {
        /// <summary>
        /// An AsyncDataRequest that saves the context of the request.
        /// </summary>
        private sealed class _DataRequest
        {
            public object Sender { get; set; }
            public bool Canceled { get; set; }
            public SmallUri SmallUri { get; set; }
            public object UserState { get; set; }
            public GetImageSourceAsyncCallback Callback { get; set; }
        }

        private readonly string _cachePath;

        // The list of photos that are being requested.
        // It's a stack, assuming that the most recent request is the one that the UI
        // really wants to show first.
        private readonly Stack<_DataRequest> _activePhotoRequests = new Stack<_DataRequest>();
        private readonly Stack<_DataRequest> _activeWebRequests = new Stack<_DataRequest>();
        // The list of photos that the UI may want to show at some point.
        // If we don't have any active requests happening we can preemptively pull photos
        // from the web so we'll have them cached when they are requested.
        // TODO: We should also clean up old photos that are no longer part of the app.
        //    We can do this with rotating folders or just keeping track of which photos
        //    are being requested and after a certain amount of time start deleting the rest.
        private readonly Stack<SmallUri> _passiveWebRequests = new Stack<SmallUri>();
        private readonly object _localLock = new object();
        private readonly object _webLock = new object();
        private readonly Dispatcher _callbackDispatcher;
        private DispatcherPool _asyncPhotoPool;
        private DispatcherPool _asyncWebRequestPool;
        private bool _disposed = false;

        private static BitmapSource _GetBitmapSourceFromStream(Stream stream)
        {
            Verify.IsNotNull(stream, "stream");

            var imgstm = new MemoryStream((int)stream.Length);
            Utility.CopyStream(imgstm, stream);
            try
            {
                var bi = new BitmapImage();
                {
                    bi.BeginInit();
                    bi.StreamSource = imgstm;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                }

                return bi;
            }
            catch (NotSupportedException e)
            {
                throw new InvalidOperationException("The stream doesn't represent an image.", e);
            }
        }

        private void _Verify()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("this");
            }
        }

        private string _GenerateCachePath(SmallUri ssUri)
        {
            string key = ssUri.GetString();
            if (key.Length > 100)
            {
                key = Utility.GetHashString(key) + key.Substring(key.Length - 100);
            }
            string filePart = key
                .Replace('\\', '_')
                .Replace('/', '_')
                .Replace(':', '_')
                .Replace('*', '_')
                .Replace('?', '_')
                .Replace('\"', '_')
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('|', '_');

            return Path.Combine(_cachePath, filePart);
        }

        public AsyncWebGetter(Dispatcher dispatcher, string settingsPath)
        {
            Verify.IsNeitherNullNorEmpty(settingsPath, "settingsPath");
            Assert.IsTrue(Path.IsPathRooted(settingsPath));
            Verify.IsNotNull(dispatcher, "dispatcher");

            _callbackDispatcher = dispatcher;
            _cachePath = Path.Combine(settingsPath, "ImageCache") + "\\";
            Utility.EnsureDirectory(_cachePath);

            _asyncPhotoPool = new DispatcherPool("Photo Fetching Thread", 1);
            _asyncWebRequestPool = new DispatcherPool("Web Photo Fetching Thread", 2, () => new WebClient { CachePolicy = HttpWebRequest.DefaultCachePolicy });
        }

        public void QueueImageRequest(SmallUri ssUri)
        {
            if (_disposed)
            {
                return;
            }

            if (ssUri == default(SmallUri))
            {
                return;
            }

            //Verify.UriIsAbsolute(uri, "uri");

            if (IsImageCached(ssUri))
            {
                return;
            }

            bool needToQueue = false;
            lock (_localLock)
            {
                needToQueue = !_asyncWebRequestPool.HasPendingRequests; 
                _passiveWebRequests.Push(ssUri);
            }

            if (needToQueue)
            {
                // When we queue web requests, do it twice to effectively use the pool.
                // Since _ProcessNextWebRequest operates in a tight loop flushing our list it's not aware
                // that the work can be shared.
                _asyncWebRequestPool.QueueRequest(DispatcherPriority.Background, _ProcessNextWebRequest, null);
                _asyncWebRequestPool.QueueRequest(DispatcherPriority.Background, _ProcessNextWebRequest, null);
            }
        }

        public void GetImageSourceAsync(object sender, object userState, SmallUri ssUri, GetImageSourceAsyncCallback callback)
        {
            //Verify.UriIsAbsolute(uri, "uri");
            Verify.IsNotNull(sender, "sender");
            // UserState may be null.
            // Verify.IsNotNull(userState, "userState");
            Verify.IsNotNull(callback, "callback");

            if (_disposed)
            {
                callback(this, new GetImageSourceCompletedEventArgs(new ObjectDisposedException("this"), false, userState));
                return;
            }

            if (default(SmallUri) == ssUri)
            {
                callback(this, new GetImageSourceCompletedEventArgs(new ArgumentException("The requested image doesn't exist.", "ssUri"), false, userState));
                return;
            }

            // Make asynchronous request to get ImageSource object from the data feed.
            var imageRequest = new _DataRequest
            {
                Sender = sender,
                UserState = userState,
                SmallUri = ssUri,
                Callback = callback,
            };

            bool needToCache = _GetCacheLocation(ssUri) == null;
            if (needToCache)
            {
                bool needToQueue = false;
                lock (_webLock)
                {
                    needToQueue = !_asyncWebRequestPool.HasPendingRequests;
                    _activeWebRequests.Push(imageRequest);
                }

                if (needToQueue)
                {
                    _asyncWebRequestPool.QueueRequest(_ProcessNextWebRequest, null);
                    _asyncWebRequestPool.QueueRequest(_ProcessNextWebRequest, null);
                }
            }
            else
            {
                bool needToQueue = false;
                lock (_localLock)
                {
                    needToQueue = !_asyncPhotoPool.HasPendingRequests;
                    _activePhotoRequests.Push(imageRequest);
                }

                if (needToQueue)
                {
                    _asyncPhotoPool.QueueRequest(_ProcessNextLocalRequest, null);
                }
            }
        }

        private string _GetCacheLocation(SmallUri ssUri)
        {
            string path = _GenerateCachePath(ssUri);
            if (File.Exists(path))
            {
                return path;
            }
            return null;
        }

        public bool IsImageCached(SmallUri ssUri)
        {
            _Verify();
            return _GetCacheLocation(ssUri) != null;
        }

        public string GetImageFile(SmallUri ssUri)
        {
            _Verify();
            return _GetCacheLocation(ssUri);
        }

        private void _ProcessNextLocalRequest(object unused)
        {
            while (_asyncPhotoPool != null)
            {
                // Retrieve the next data request for processing.
                GetImageSourceAsyncCallback callback = null;
                object userState = null;
                object sender = null;
                SmallUri ssUri = default(SmallUri);

                lock (_localLock)
                {
                    while (_activePhotoRequests.Count > 0)
                    {
                        _DataRequest nextDataRequest = _activePhotoRequests.Pop();
                        if (!nextDataRequest.Canceled)
                        {
                            sender = nextDataRequest.Sender;
                            callback = nextDataRequest.Callback;
                            userState = nextDataRequest.UserState;
                            ssUri = nextDataRequest.SmallUri;
                            break;
                        }
                    }

                    if (ssUri == default(SmallUri))
                    {
                        Assert.AreEqual(0, _activePhotoRequests.Count);
                        return;
                    }
                }

                Assert.IsNotNull(callback);
                GetImageSourceCompletedEventArgs callbackArgs = null;
                string localCachePath = null;
                try
                {
                    localCachePath = _GetCacheLocation(ssUri);
                    if (localCachePath == null)
                    {
                        callbackArgs = new GetImageSourceCompletedEventArgs(new FileNotFoundException(), false, userState);
                    }
                    else
                    {
                        using (Stream imageStream = File.Open(localCachePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            callbackArgs = new GetImageSourceCompletedEventArgs(_GetBitmapSourceFromStream(imageStream), userState);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!string.IsNullOrEmpty(localCachePath))
                    {
                        // If the cache is bad try removing the file so we can fetch it correctly next time.
                        try
                        {
                            File.Delete(localCachePath);
                        }
                        catch { }
                    }
                    callbackArgs = new GetImageSourceCompletedEventArgs(e, false, userState);
                }

                _callbackDispatcher.BeginInvoke(callback, sender, callbackArgs);
            }
        }

        private void _ProcessNextWebRequest(object unused)
        {
            if (_asyncWebRequestPool == null)
            {
                return;
            }

            var webClient = _asyncWebRequestPool.Tag as WebClient;
            Assert.IsNotNull(webClient);

            while (_asyncWebRequestPool != null)
            {
                // Retrieve the next data request for processing.
                _DataRequest activeRequest = null;
                SmallUri ssUri = default(SmallUri);
                bool isPassive = false;

                lock (_webLock)
                {
                    while (_activeWebRequests.Count > 0)
                    {
                        activeRequest = _activeWebRequests.Pop();
                        if (!activeRequest.Canceled)
                        {
                            ssUri = activeRequest.SmallUri;
                            break;
                        }
                    }

                    if (ssUri == default(SmallUri) && _passiveWebRequests.Count > 0)
                    {
                        ssUri = _passiveWebRequests.Pop();
                        isPassive = true;
                    }

                    if (ssUri == default(SmallUri))
                    {
                        Assert.AreEqual(0, _activeWebRequests.Count);
                        Assert.AreEqual(0, _passiveWebRequests.Count);
                        return;
                    }
                }

                try
                {
                    string localCachePath = _GenerateCachePath(ssUri);
                    if (!File.Exists(localCachePath))
                    {
                        // There's a potential race here with other attempts to write the same file.
                        // We don't really care because there's not much we can do about it when
                        // it happens from multiple processes.
                        string tempFile = Path.GetTempFileName();
                        Uri address = ssUri.GetUri();
                        try
                        {
                            webClient.DownloadFile(address, tempFile);
                        }
                        catch (WebException)
                        {
                            // Fail once, just try again.  Servers are flakey.
                            // Fails again let it throw.  Caller is expected to catch.
                            webClient.DownloadFile(address, tempFile);
                        }

                        // Should really block multiple web requests for the same file, which causes this...
                        if (!File.Exists(localCachePath))
                        {
                            File.Move(tempFile, localCachePath);
                        }
                    }

                    if (!isPassive)
                    {
                        bool needToQueue = false;
                        lock (_localLock)
                        {
                            needToQueue = !_asyncPhotoPool.HasPendingRequests;
                            _activePhotoRequests.Push(activeRequest);
                        }

                        if (needToQueue)
                        {
                            _asyncPhotoPool.QueueRequest(_ProcessNextLocalRequest, null);
                        }
                    }
                }
                catch
                { }
            }
        }

        internal void Shutdown(Action<string> deleteCallback)
        {
            _disposed = true;
            Utility.SafeDispose(ref _asyncPhotoPool);
            Utility.SafeDispose(ref _asyncWebRequestPool);

            if (deleteCallback != null)
            {
                deleteCallback(_cachePath);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Shutdown(null);
        }

        #endregion
    }
}
