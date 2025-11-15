//#define USE_STANDARD_DRAGDROP

namespace ClientManager.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.IO;
    using System.Collections.Generic;
    using Contigo;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using System.Windows.Media;
    using System.Runtime.InteropServices;


    public class DragContainerEventArgs : RoutedEventArgs
    {
        public DragContainerEventArgs(RoutedEvent routedEvent, Point initialPoint, Point currentPoint)
            : base(routedEvent)
        {
            InitialPosition = initialPoint;
            CurrentPosition = currentPoint;
        }

        public Point InitialPosition { get; set; }
        public Point CurrentPosition { get; set; }
    }

    public abstract class DragContainer : Decorator
    {
        protected DragContainer()
        {
            DragStarted += new EventHandler<DragContainerEventArgs>(HandleDragStarted);
            DragDelta += new EventHandler<DragContainerEventArgs>(HandleDragDelta);
            DragCompleted += new EventHandler<DragContainerEventArgs>(HandleDragCompleted);
        }

        public static readonly DependencyProperty IsDragEnabledProperty = DependencyProperty.Register("IsDragEnabled", typeof(bool), typeof(DragContainer),
            new FrameworkPropertyMetadata(true));

        public bool IsDragEnabled
        {
            get { return (bool)GetValue(IsDragEnabledProperty); }
            set { SetValue(IsDragEnabledProperty, value); }
        }


        public static readonly RoutedEvent DragStartedEvent = EventManager.RegisterRoutedEvent("DragStarted",
                                                                                               RoutingStrategy.Bubble,
                                                                                               typeof (EventHandler<DragContainerEventArgs>),
                                                                                               typeof (DragContainer));

        public static readonly RoutedEvent DragDeltaEvent = EventManager.RegisterRoutedEvent("DragDelta",
                                                                                               RoutingStrategy.Bubble,
                                                                                               typeof(EventHandler<DragContainerEventArgs>),
                                                                                               typeof(DragContainer));

        public static readonly RoutedEvent DragCompletedEvent = EventManager.RegisterRoutedEvent("DragCompleted",
                                                                                               RoutingStrategy.Bubble,
                                                                                               typeof(EventHandler<DragContainerEventArgs>),
                                                                                               typeof(DragContainer));

        public event EventHandler<DragContainerEventArgs> DragStarted
        {
            add { AddHandler(DragStartedEvent, value);}
            remove { RemoveHandler(DragStartedEvent, value);}
        }

        public event EventHandler<DragContainerEventArgs> DragDelta
        {
            add { AddHandler(DragDeltaEvent, value); }
            remove { RemoveHandler(DragDeltaEvent, value); }
        }

        public event EventHandler<DragContainerEventArgs> DragCompleted
        {
            add { AddHandler(DragCompletedEvent, value); }
            remove { RemoveHandler(DragCompletedEvent, value); }
        }



        /// <summary>
        /// PreviewMouseLeftButtonDown event handler.
        /// </summary>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.IsDragEnabled)
            {
                Point pos = e.GetPosition(Application.Current.MainWindow.Content as UIElement);
                RaiseEvent(new DragContainerEventArgs(DragStartedEvent, pos, pos));
            }

            base.OnPreviewMouseLeftButtonDown(e);
        }

        /// <summary>
        /// PreviewMouseLeftButtonUp event handler.
        /// </summary>
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (this.IsDragEnabled)
            {
                var pos = e.GetPosition(Application.Current.MainWindow.Content as UIElement);
                RaiseEvent(new DragContainerEventArgs(DragCompletedEvent, this.dragStartPoint, pos));
            }

            base.OnPreviewMouseLeftButtonUp(e);
        }

        /// <summary>
        /// PreviewMouseMove event handler.
        /// </summary>
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (this.IsDragEnabled && this.isMouseDown && !IsInDrag) //!this.IsMouseCaptured)
            {
                var pos = e.GetPosition(Application.Current.MainWindow.Content as UIElement);
                RaiseEvent(new DragContainerEventArgs(DragDeltaEvent, this.dragStartPoint, pos));
            }

            base.OnPreviewMouseMove(e);
        }



        private void HandleDragStarted(object sender, DragContainerEventArgs e)
        {
            this.dragStartPoint = e.InitialPosition;
            this.isMouseDown = true;
        }


        private void HandleDragDelta(object sender, DragContainerEventArgs e)
        {
            // Don't actually start the drag unless the mouse has moved for enough away
            // from the initial MouseDown point.
            Point currentPoint = e.CurrentPosition;

            if (Math.Abs(currentPoint.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(currentPoint.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                this.isMouseDown = false;

                // Since we have to have the data before the drag starts, we don't allow a drag for data that
                // is not cached to avoid blocking the UI while the data is fetched before the drag starts.
                if (IsDataAvailable())
                {
                    IsInDrag = true;

                    int width = (int)this.RenderSize.Width;
                    int height = (int)this.RenderSize.Height;

                    RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32); // TODO: use actual dpi.
                    rtb.Render(this);

                    ConstrainSize(ref width, ref height, 200);
                    BitmapSource ghostImage = ResizeImage(rtb, width, height);

                    this.CaptureMouse();
                    Point mousePoint = new Point(0.5 * width, 0.5 * height);

#if USE_STANDARD_DRAGDROP
                    DataObject dataObject = new DataObject();
                    var items = GetData();
                    foreach (var item in items)
                    {
                        dataObject.SetData(item.Key, item.Value);
                    }

                    DragDrop.DoDragDrop(this, dataObject, DragDropEffects.Copy);
#else
                    try
                    {
                        DragSourceHelper.DoDragDrop(this, ghostImage, mousePoint, DragDropEffects.Copy, GetData());
                    }
                    catch (COMException)
                    {
                        // DragDropLib is buggy. Fail silently if this happens.
                    }
#endif

                    this.ReleaseMouseCapture();

                    IsInDrag = false;
                }
            }
        }

        private void HandleDragCompleted(object sender, DragContainerEventArgs e)
        {
            this.isMouseDown = false;
        }


        protected virtual bool IsDataAvailable()
        {
            return false;
        }

        protected virtual KeyValuePair<string, object>[] GetData()
        {
            return null;
        }

        public static BitmapSource ResizeImage(BitmapSource source, int width, int height)
        {
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawImage(source, new Rect(0.0, 0.0, width, height));
            }

            RenderTargetBitmap dest = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32); // TODO: use actual dpi.
            dest.Render(visual);
            dest.Freeze();

            return dest;
        }

        /// <remarks>
        /// This retains aspect ratio.
        /// </remarks>
        public static void ConstrainSize(ref int width, ref int height, int max)
        {
            if (width > height)
            {
                if (width > max)
                {
                    height = height * max / width;
                    width = max;
                }
            }
            else
            {
                if (height > max)
                {
                    width = width * max / height;
                    height = max;
                }
            }
        }

        public static string ConstrainImage(string path, int max)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();

            int width = image.PixelWidth;
            int height = image.PixelHeight;

            if (width <= max && height <= max)
            {
             //   return path;
            }

            ConstrainSize(ref width, ref height, max);
            BitmapSource resizedImage = ResizeImage(image, width, height);

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 90;
            encoder.Frames.Add(BitmapFrame.Create(resizedImage));
            string resizedFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".jpg");

            using (FileStream outStream = new FileStream(resizedFile, FileMode.Create))
            {
                encoder.Save(outStream);
            }

            return resizedFile;
        }

        public static bool IsInDrag { get; private set; }

        /// <summary>
        /// If the left button is currently down and the drag has not yet started.
        /// </summary>
        private bool isMouseDown;

        /// <summary>
        /// The point where the drag was started.
        /// </summary>
        private Point dragStartPoint;
    }

    public class FacebookPhotoDragContainer : DragContainer
    {
        public static readonly DependencyProperty FacebookPhotoProperty = DependencyProperty.Register("FacebookPhoto", 
            typeof(FacebookPhoto), typeof(FacebookPhotoDragContainer));

        public FacebookPhoto FacebookPhoto
        {
            get { return (FacebookPhoto)GetValue(FacebookPhotoProperty); }
            set { SetValue(FacebookPhotoProperty, value); }
        }

        protected override bool IsDataAvailable()
        {
            bool isCached = this.FacebookPhoto.Image.IsCached(FacebookImageDimensions.Big);
            if (!isCached)
            {
                // Add this to the asynchronous download queue.
                this.FacebookPhoto.Image.GetImageAsync(FacebookImageDimensions.Big, new GetImageSourceAsyncCallback(OnGetImageSourceCompleted));
            }

            return isCached;
        }

        protected override KeyValuePair<string, object>[] GetData()
        {
            string path = this.FacebookPhoto.Image.GetCachePath(FacebookImageDimensions.Big);
            KeyValuePair<string, object> data = new KeyValuePair<string, object>(DataFormats.FileDrop, new string[] { path });
            return new KeyValuePair<string, object>[] { data };
        }

        private void OnGetImageSourceCompleted(object sender, GetImageSourceCompletedEventArgs e)
        {
        }
    }

    public class FacebookImageDragContainer : DragContainer
    {
        public static readonly DependencyProperty FacebookImageProperty = DependencyProperty.Register("FacebookImage", 
            typeof(FacebookImage), typeof(FacebookImageDragContainer));

        public FacebookImage FacebookImage
        {
            get { return (FacebookImage)GetValue(FacebookImageProperty); }
            set { SetValue(FacebookImageProperty, value); }
        }

        protected override bool IsDataAvailable()
        {
            bool isCached = this.FacebookImage.IsCached(FacebookImageDimensions.Big);
            if (!isCached)
            {
                // Add this to the asynchronous download queue.
                this.FacebookImage.GetImageAsync(FacebookImageDimensions.Big, new GetImageSourceAsyncCallback(OnGetImageSourceCompleted));
            }

            return isCached;
        }

        protected override KeyValuePair<string, object>[] GetData()
        {
            string path = this.FacebookImage.GetCachePath(FacebookImageDimensions.Big);
            KeyValuePair<string, object> data = new KeyValuePair<string, object>(DataFormats.FileDrop, new string[] { path });
            return new KeyValuePair<string, object>[] { data };
        }

        private void OnGetImageSourceCompleted(object sender, GetImageSourceCompletedEventArgs e)
        {
        }
    }

    public class FacebookPhotoAlbumDragContainer : DragContainer
    {
        public static readonly DependencyProperty FacebookPhotoAlbumProperty = DependencyProperty.Register("FacebookPhotoAlbum", 
            typeof(FacebookPhotoAlbum), typeof(FacebookPhotoAlbumDragContainer));

        public FacebookPhotoAlbum FacebookPhotoAlbum
        {
            get { return (FacebookPhotoAlbum)GetValue(FacebookPhotoAlbumProperty); }
            set { SetValue(FacebookPhotoAlbumProperty, value); }
        }

        protected override bool IsDataAvailable()
        {
            bool isCached = true;

            foreach (FacebookPhoto photo in this.FacebookPhotoAlbum.Photos)
            {
                if (!photo.Image.IsCached(FacebookImageDimensions.Big))
                {
                    // Add this to the asynchronous download queue.
                    photo.Image.GetImageAsync(FacebookImageDimensions.Big, new GetImageSourceAsyncCallback(OnGetImageSourceCompleted));
                    isCached = false;
                }
            }

            return isCached;
        }

        protected override KeyValuePair<string, object>[] GetData()
        {
            string path = Path.Combine(Path.GetTempPath(), @"Faboolous");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, this.FacebookPhotoAlbum.AlbumId);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            this.FacebookPhotoAlbum.SaveToFolder(path);
            KeyValuePair<string, object> data = new KeyValuePair<string, object>(DataFormats.FileDrop, new string[] { path });
            return new KeyValuePair<string, object>[] { data };
        }

        private void OnGetImageSourceCompleted(object sender, GetImageSourceCompletedEventArgs e)
        {
        }
    }

    public class FacebookContactDragContainer : DragContainer
    {
        public static readonly DependencyProperty FacebookContactProperty = DependencyProperty.Register("FacebookContact", 
            typeof(FacebookContact), typeof(FacebookContactDragContainer));

        public FacebookContact FacebookContact
        {
            get { return (FacebookContact)GetValue(FacebookContactProperty); }
            set { SetValue(FacebookContactProperty, value); }
        }

        protected override bool IsDataAvailable()
        {
            return true;
        }

        protected override KeyValuePair<string, object>[] GetData()
        {
            KeyValuePair<string, object> data = new KeyValuePair<string, object>(DataFormats.StringFormat, this.FacebookContact.UserId); // todo: we need a custom application format.
            return new KeyValuePair<string, object>[] { data };
        }
    }
}
