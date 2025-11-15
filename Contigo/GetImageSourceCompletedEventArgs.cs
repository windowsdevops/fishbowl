
namespace Contigo
{
    using System;
    using System.ComponentModel;
    using System.Windows.Media;
    using Standard;
using System.Windows;
    using System.Windows.Media.Imaging;

    public delegate void GetImageSourceAsyncCallback(object sender, GetImageSourceCompletedEventArgs e);

    /// <summary>
    /// Event arguments accompanying delegate for GetImageSource event.
    /// </summary>
    public class GetImageSourceCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>The ImageSource representing data from the Internet resource.</summary>
        private readonly ImageSource _imageSource;

        private readonly Size? _naturalSize;

        /// <summary>
        /// Initializes a new instance of the GetImageSourceCompletedEventArgs class for successful completion.
        /// </summary>
        /// <param name="imageSource">The ImageSource representing data from the Internet resource.</param>
        /// <param name="userState">The user-supplied state object.</param>
        internal GetImageSourceCompletedEventArgs(BitmapSource bitmapSource, object userState)
            : base(null, false, userState)
        {
            Verify.IsNotNull(bitmapSource, "bitmapSource");
            _imageSource = bitmapSource;
            _naturalSize = new Size(bitmapSource.Width, bitmapSource.Height);
        }

        /// <summary>
        /// Initializes a new instance of the GetImageSourceCompletedEventArgs class for an error or a cancellation.
        /// </summary>
        /// <param name="error">Any error that occurred during the asynchronous operation.</param>
        /// <param name="cancelled">A value indicating whether the asynchronous operation was canceled.</param>
        /// <param name="userState">The user-supplied state object.</param>
        internal GetImageSourceCompletedEventArgs(Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
        }

        /// <summary>
        /// Gets the ImageSource representing data from the Internet resource.
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _imageSource;
            }
        }

        public Size? NaturalSize
        {
            get
            {
                RaiseExceptionIfNecessary();
                return _naturalSize;
            }
        }
    }
}
