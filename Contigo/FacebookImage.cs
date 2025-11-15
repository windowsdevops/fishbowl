
namespace Contigo
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using Standard;
    
    public enum FacebookImageDimensions
    {
        /// <summary>Photo with a max width of 130px and max height of 130px.</summary>
        Normal,
        /// <summary>Photo with a max width of 604px and max height of 604px.</summary>
        Big,
        /// <summary>Photo with a max width of 75px and max height of 225px.</summary>
        Small,
        /// <summary>Square photo with a width and height of 50px.  Usually available for user images.</summary>
        Square,
    }

    public class FacebookImage : IFacebookObject
    {
        private class _ImageCallbackState
        {
            public GetImageSourceAsyncCallback Callback { get; set; }
            public FacebookImageDimensions RequestedSize { get; set; }
        }

        private static readonly Dictionary<FacebookImageDimensions, Size> _DimensionLookup = new Dictionary<FacebookImageDimensions, Size>
        {
            { FacebookImageDimensions.Normal, new Size(130, 130) },
            { FacebookImageDimensions.Big,    new Size(604, 604) },
            { FacebookImageDimensions.Small,  new Size(75,  225) },
            { FacebookImageDimensions.Square, new Size(50,   50) },
        };

        private Thickness? _margin;
        private SmallUri? _normal;
        private SmallUri? _big;
        private SmallUri? _small;
        private SmallUri? _square;
        private WeakReference _normalWR;
        private WeakReference _bigWR;
        private WeakReference _smallWR;
        private WeakReference _squareWR;

        internal FacebookImage(FacebookService service, Uri uri, Thickness marginPercent)
            : this(service, uri, (Thickness?)marginPercent)
        {}

        internal FacebookImage(FacebookService service, Uri uri)
            : this(service, uri, null)
        {}

        private FacebookImage(FacebookService service)
        {
            Assert.IsNotNull(service);
            SourceService = service;
        }

        private FacebookImage(FacebookService service, Uri uri, Thickness? marginPercent)
        {
            Assert.IsNotNull(service);
            SourceService = service;

            if (uri != null)
            {
                _normal = new SmallUri(uri.OriginalString);
                SourceService.WebGetter.QueueImageRequest(_normal.Value);
                _margin = marginPercent;
            }
        }

        internal FacebookImage(FacebookService service, Uri normal, Uri big, Uri small, Uri square)
        {
            Assert.IsNotNull(service);
            SourceService = service;

            if (normal != null)
            {
                _normal = new SmallUri(normal.OriginalString);
                SourceService.WebGetter.QueueImageRequest(_normal.Value);
            }
            if (small != null)
            {
                _small = new SmallUri(small.OriginalString);
                SourceService.WebGetter.QueueImageRequest(_small.Value);
            }
            if (big != null)
            {
                _big = new SmallUri(big.OriginalString);
                SourceService.WebGetter.QueueImageRequest(_big.Value);
            }
            if (square != null)
            {
                _square = new SmallUri(square.OriginalString);
                SourceService.WebGetter.QueueImageRequest(_square.Value);
            }
        }

        public void GetImageAsync(FacebookImageDimensions requestedSize, GetImageSourceAsyncCallback callback)
        { 
            Verify.IsNotNull(callback, "callback");

            Assert.IsNotNull(SourceService);
            Assert.IsNotNull(SourceService.WebGetter);

            SmallUri ss = _GetSmallUriFromRequestedSize(requestedSize);
            if (ss == default(SmallUri))
            {
                callback(this, new GetImageSourceCompletedEventArgs(new InvalidOperationException("The requested image doesn't exist"), false, this));
            }

            BitmapSource img;
            if (_TryGetFromWeakCache(requestedSize, out img))
            {
                callback(this, new GetImageSourceCompletedEventArgs(img, this));
            }

            var userState = new _ImageCallbackState { Callback = callback, RequestedSize = requestedSize };
            SourceService.WebGetter.GetImageSourceAsync(this, userState, ss, _AddWeakCacheCallback);
        }

        private bool _TryGetFromWeakCache(FacebookImageDimensions requestedSize, out BitmapSource img)
        {
            img = null;
            switch (requestedSize)
            {
                case FacebookImageDimensions.Big:
                    if (_bigWR != null)
                    {
                        img = _bigWR.Target as BitmapSource;
                    }
                    break;
                case FacebookImageDimensions.Normal:
                    if (_normalWR != null)
                    {
                        img = _normalWR.Target as BitmapSource;
                    }
                    break;
                case FacebookImageDimensions.Small:
                    if (_smallWR != null)
                    {
                        img = _smallWR.Target as BitmapSource;
                    }
                    break;
                case FacebookImageDimensions.Square:
                    if (_squareWR != null)
                    {
                        img = _squareWR.Target as BitmapSource;
                    }
                    break;
            }

            return img != null;
        }

        private void _AddWeakCacheCallback(object sender, GetImageSourceCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                return;
            }

            var bs = (BitmapSource)e.ImageSource;
            if (_margin.HasValue)
            {
                Thickness margin = _margin.Value;
                bs = new CroppedBitmap(bs, new Int32Rect((int)(margin.Left * bs.Width), (int)(margin.Top * bs.Height), (int)(bs.Width - (margin.Left + margin.Right) * bs.Width), (int)(bs.Height - (margin.Top + margin.Bottom) * bs.Height)));
            }

            var userState = (_ImageCallbackState)e.UserState;
            switch (userState.RequestedSize)
            {
                case FacebookImageDimensions.Big:
                    _bigWR = new WeakReference(bs);
                    break;
                case FacebookImageDimensions.Normal:
                    _normalWR = new WeakReference(bs);
                    break;
                case FacebookImageDimensions.Small:
                    _smallWR = new WeakReference(bs);
                    break;
                case FacebookImageDimensions.Square:
                    _squareWR = new WeakReference(bs);
                    break;
            }

            if (userState.Callback != null)
            {
                userState.Callback(this, new GetImageSourceCompletedEventArgs(bs, this));
            }
        }

        /// <remarks>
        /// This is a synchronous operation that actively fetches this image.
        /// </remarks>
        public string GetCachePath(FacebookImageDimensions requestedSize)
        {
            SmallUri sizedString = _GetSmallUriFromRequestedSize(requestedSize);
            //Assert.IsNotDefault(sizedString);
            return SourceService.WebGetter.GetImageFile(sizedString);
        }

        /// <remarks>
        /// This is a synchronous operation.
        /// </remarks>
        public void SaveToFile(FacebookImageDimensions requestedSize, string path)
        {
            string cachePath = GetCachePath(requestedSize);
            if (cachePath == null)
            {
                return;
            }

            File.Copy(cachePath, path);
        }

        public bool IsCached(FacebookImageDimensions requestedSize)
        {
            SmallUri sizedString = _GetSmallUriFromRequestedSize(requestedSize);
            //Assert.IsNotDefault(sizedString);
            return SourceService.WebGetter.IsImageCached(sizedString);
        }

        private SmallUri _GetSmallUriFromRequestedSize(FacebookImageDimensions requestedSize)
        {
            // If one url type isn't available, try to fallback on other provided values.

            SmallUri? str = null;
            switch (requestedSize)
            {
                case FacebookImageDimensions.Big:    str = _big ?? _normal ?? _small ?? _square; break;
                case FacebookImageDimensions.Small:  str = _small ?? _normal ?? _big ?? _square; break;
                case FacebookImageDimensions.Square: str = _square ?? _small ?? _normal ?? _big; break;
                case FacebookImageDimensions.Normal: str = _normal ?? _big ?? _small ?? _square; break;
            }

            return str ?? default(SmallUri);
        }

        #region IFacebookObject Members

        FacebookService IFacebookObject.SourceService { get; set; }

        private FacebookService SourceService
        {
            get { return ((IFacebookObject)this).SourceService; }
            set { ((IFacebookObject)this).SourceService = value; }
        }

        #endregion

        public override bool Equals(object obj)
        {
            return this.Equals(obj as FacebookImage);
        }

        public override int GetHashCode()
        {
            return _big.GetHashCode() ^ _normal.GetHashCode() ^ _small.GetHashCode() << 8 ^ _square.GetHashCode() >> 8;
        }

        #region IEquatable<FacebookImage> Members

        public bool Equals(FacebookImage other)
        {
            if (other == null)
            {
                return false;
            }

            return other._big == _big
                && other._normal == _normal
                && other._small == _small
                && other._square == _square;
        }

        #endregion

        internal bool SafeMerge(FacebookImage other)
        {
            bool modified = false;

            if (other == null)
            {
                return false;
            }

            if (_normal == null && other._normal != null)
            {
                _normal = other._normal;
                modified = true;
            }

            if (_small == null && other._small != null)
            {
                _small = other._small;
                modified = true;
            }

            if (_square == null && other._square != null)
            {
                _square = other._square;
                modified = true;
            }

            if (_big == null && other._big != null)
            {
                _big = other._big;
                modified = true;
            }

            return modified;
        }

        public FacebookImage Clone()
        {
            var img = new FacebookImage(SourceService);
            img._big = _big;
            img._bigWR = _bigWR;
            img._margin = _margin;
            img._normal = _normal;
            img._normalWR = _normalWR;
            img._small = _small;
            img._smallWR = _smallWR;
            img._square = _square;
            img._squareWR = _squareWR;

            return img;
        }

        public bool IsEmpty
        {
            get
            {
                return _GetSmallUriFromRequestedSize(FacebookImageDimensions.Normal) == default(SmallUri);
            }
        }

        internal bool IsMostlyEmpty
        {
            get
            {
                return _GetSmallUriFromRequestedSize(FacebookImageDimensions.Big) == _GetSmallUriFromRequestedSize(FacebookImageDimensions.Square)
                    && _square != null;
            }
        }
    }
}
