namespace FacebookClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using ClientManager;
    using ClientManager.Controls;
    using Contigo;
    using Standard;

    /// <summary>
    /// Interaction logic for PhotoUploadWizard.xaml
    /// </summary>
    [TemplatePart(Name = "PART_AddPhotosPage", Type = typeof(Panel))]
    [TemplatePart(Name = "PART_AlbumPickerPage", Type = typeof(Panel))]
    [TemplatePart(Name = "PART_UploadProgressPage", Type = typeof(Panel))]
    [TemplatePart(Name = "PART_ZapScroller", Type = typeof(ZapScroller))]
    [TemplatePart(Name = "PART_AlbumName", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_AlbumLocation", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_AlbumDescription", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_UploadCountText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_NextPhotoImage", Type = typeof(Image))]
    [TemplatePart(Name = "PART_AlbumName2", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_UploadStatus", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_CloseCancelButton", Type = typeof(Button))]
    public partial class PhotoUploadWizard : UserControl
    {
        private static readonly string[] _ImageExtensions = new string[] { ".jpg", ".jpeg", ".bmp", ".png", ".gif" };

        public class UploadFile
        {
            public UploadFile(string path)
            {
                Path = path;
                Description = string.Empty;
            }

            public string Path { get; private set; }
            public string Description { get; set; }
        }

        private class NewPhotoAlbum
        {
            public string Title { get { return "New Album"; } }
            public string Location { get { return string.Empty; } }
            public string Description { get { return string.Empty; } }
            public FacebookPhoto CoverPic { get { return null; } }
        }

        public enum PhotoUploaderPage
        {
            AddPhotos,
            PickAlbum,
            Upload
        }

        public const int MaxPhotoDimension = 600;

        private Panel _addPhotosPage;
        private Panel _albumPickerPage;
        private Panel _uploadProgressPage;
        private ZapScroller _zapScroller;
        private ComboBox _albumsComboBox;
        private TextBox _albumName;
        private TextBox _albumLocation;
        private TextBox _albumDescription;
        private TextBlock _uploadCount;
        private Image _nextPhotoImage;
        private TextBlock _albumName2;
        private TextBlock _uploadStatus;
        private Button _closeCancelButton;

        private Thread _workerThread;
        private bool _uploading;

        public PhotoUploadWizard()
        {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(UploadCommand, new ExecutedRoutedEventHandler(OnUploadExecuted), new CanExecuteRoutedEventHandler(OnUploadCanExecute)));
            Files = new ObservableCollection<UploadFile>();

            ServiceProvider.ViewManager.MeContact.PhotoAlbums.CollectionChanged += new NotifyCollectionChangedEventHandler((sender, e) =>
            {
                UpdatePhotoAlbums();
            });
        }

        public static RoutedCommand SelectedAlbumChangedCommand = new RoutedCommand("SelectedAlbumChanged", typeof(PhotoUploadWizard));
        public static RoutedCommand UploadCommand = new RoutedCommand("Upload", typeof(PhotoUploadWizard));
        public static readonly DependencyProperty PageProperty = DependencyProperty.Register("Page", typeof(PhotoUploaderPage), typeof(PhotoUploadWizard),
            new FrameworkPropertyMetadata(OnPagePropertyChanged));

        public PhotoUploaderPage Page
        {
            get { return (PhotoUploaderPage)GetValue(PageProperty); }
            set { SetValue(PageProperty, value); }
        }

        public ObservableCollection<UploadFile> Files { get; private set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _addPhotosPage = Template.FindName("PART_AddPhotosPage", this) as Panel;
            _albumPickerPage = Template.FindName("PART_AlbumPickerPage", this) as Panel;
            _uploadProgressPage = Template.FindName("PART_UploadProgressPage", this) as Panel;
            _zapScroller = Template.FindName("PART_ZapScroller", this) as ZapScroller;
            _albumsComboBox = Template.FindName("PART_AlbumsComboBox", this) as ComboBox;
            _albumName = Template.FindName("PART_AlbumName", this) as TextBox;
            _albumLocation = Template.FindName("PART_AlbumLocation", this) as TextBox;
            _albumDescription = Template.FindName("PART_AlbumDescription", this) as TextBox;
            _uploadCount = Template.FindName("PART_UploadCount", this) as TextBlock;
            _nextPhotoImage = Template.FindName("PART_NextPhotoImage", this) as Image;
            _albumName2 = Template.FindName("PART_AlbumName2", this) as TextBlock;
            _uploadStatus = Template.FindName("PART_UploadStatus", this) as TextBlock;
            _closeCancelButton = Template.FindName("PART_CloseCancelButton", this) as Button;

            _albumName.TextChanged += new TextChangedEventHandler((sender, e) => CommandManager.InvalidateRequerySuggested());
            _albumLocation.TextChanged += new TextChangedEventHandler((sender, e) => CommandManager.InvalidateRequerySuggested());
            _albumDescription.TextChanged += new TextChangedEventHandler((sender, e) => CommandManager.InvalidateRequerySuggested());


            UpdatePhotoAlbums();
        }

        public void Show()
        {
            if (CheckWorkerThread())
            {
                if (ServiceProvider.ViewManager.Dialog == null)
                {
                    ServiceProvider.ViewManager.ShowDialog(this);
                }

                Page = PhotoUploaderPage.AddPhotos;
            }
        }

        public void Show(IEnumerable<string> fileList)
        {
            if (CheckWorkerThread())
            {
                Files.Clear();

                foreach (string fileName in fileList)
                {
                    Files.Add(new UploadFile(fileName));
                }

                if (ServiceProvider.ViewManager.Dialog == null)
                {
                    ServiceProvider.ViewManager.ShowDialog(this);
                }

                Page = PhotoUploaderPage.PickAlbum;
            }
        }

        public void Hide()
        {
            if (CheckWorkerThread())
            {
                ServiceProvider.ViewManager.EndDialog(this);
                Files.Clear();
            }
        }

        private bool CheckWorkerThread()
        {
            if (_workerThread != null)
            {
                if (!_workerThread.IsAlive)
                {
                    _workerThread = null;
                    return true;
                }

                if (_uploading)
                {
                    if (MessageBox.Show("Are you sure you want to cancel the current upload?", "Photo Upload", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    {
                        return false;
                    }
                }

                if (_workerThread != null)
                {
                    _workerThread.Abort();
                    _workerThread.Join();
                    _workerThread = null;
                }
            }

            return true;
        }

        private void UpdatePhotoAlbums()
        {
            if (_albumsComboBox != null)
            {
                _albumsComboBox.Items.Clear();
                _albumsComboBox.Items.Add(new NewPhotoAlbum());

                foreach (var album in ServiceProvider.ViewManager.MeContact.PhotoAlbums)
                {
                    _albumsComboBox.Items.Add(album);
                }

                _albumsComboBox.SelectedIndex = 0;
            }
        }

        private static void OnPagePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PhotoUploadWizard wizard = sender as PhotoUploadWizard;
    
            if ((PhotoUploaderPage)e.NewValue == PhotoUploaderPage.PickAlbum)
            {
                wizard.UpdatePhotoAlbums();
                CommandManager.InvalidateRequerySuggested();

            }
        }

        private void OnUploadExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Assert.IsNull(_workerThread);

            string albumName = _albumName.Text;
            string albumDescription = _albumDescription.Text;
            string albumLocation = _albumLocation.Text;

            FacebookPhotoAlbum album = _albumsComboBox.SelectedItem as FacebookPhotoAlbum;

            Page = PhotoUploaderPage.Upload;
            _albumName2.Text = album != null ? album.Title : albumName;
            _closeCancelButton.Content = "Cancel";
            _uploadCount.Text = "0";
            _uploadStatus.Text = "";

            _uploading = true;

            _workerThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    if (album == null)
                    {
                        album = ServiceProvider.ViewManager.CreatePhotoAlbum(albumName, albumDescription, albumLocation);
                    }

                    int count = 0;

                    foreach (var file in Files)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            BitmapImage image = new BitmapImage(new Uri(file.Path));
                            image.Freeze();
                            _nextPhotoImage.Source = image;
                        }));

                        string path = DragContainer.ConstrainImage(file.Path, MaxPhotoDimension);
                        ServiceProvider.ViewManager.AddPhotoToAlbum(album, file.Description, path);
                        count++;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _uploadCount.Text = count.ToString();
                        }));
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.Execute(album);
                        Hide();
                    }));
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _nextPhotoImage.Source = null;
                        _uploadStatus.Text = "Upload failed.";
                        _closeCancelButton.Content = "Close";
                    }));
                }

                _uploading = false;
            }));
            _workerThread.Start();
        }

        private void OnUploadCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_albumName == null || _albumName.Text.Length == 0)
            {
                e.CanExecute = false;
                return;
            }

            if (_albumsComboBox.SelectedItem is FacebookPhotoAlbum)
            {
                e.CanExecute = true;
                return;
            }

            foreach (var album in ServiceProvider.ViewManager.MeContact.PhotoAlbums) // this isn't quite right.. we might not have all the albums yet.
                                                                                     // this is a good quick check, though. we'll fail later if we don't catch it here.
            {
                if (album.Title == _albumName.Text)
                {
                    e.CanExecute = false;
                    return;
                }
            }

            e.CanExecute = true;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)));
        }

        private void RemovePhotoButtonClick(object sender, RoutedEventArgs e)
        {
            if (_zapScroller.CurrentItemIndex >= 0 && _zapScroller.CurrentItemIndex < Files.Count)
            {
                Files.RemoveAt(_zapScroller.CurrentItemIndex);
                if (Files.Count == 0)
                {
                    Hide();
                }
            }
        }

        private static bool _IsImageFile(string fileName)
        {
            Assert.IsNeitherNullNorEmpty(fileName);
            string ext = Path.GetExtension(fileName).ToLower();
            foreach (string imgExt in _ImageExtensions)
            {
                if (imgExt == ext)
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<string> _GetImageFiles(string[] fileNames, int maxFiles)
        {
            var directories = new List<string>();
            foreach (string file in fileNames)
            {
                if (File.Exists(file))
                {
                    if (_IsImageFile(file))
                    {
                        yield return file;
                        --maxFiles;
                        if (maxFiles == 0)
                        {
                            yield break;
                        }
                    }
                }
                else if (Directory.Exists(file))
                {
                    directories.Add(file);
                }
            }

            // We've processed all the top-level files.
            // If there are still files to be gotten then start walking the directories.
            foreach (string directory in directories)
            {
                DirectoryInfo di = new DirectoryInfo(directory);
                foreach (string imgExt in _ImageExtensions)
                {
                    foreach (FileInfo fi in FileWalker.GetFiles(di, "*" + imgExt, true))
                    {
                        yield return fi.FullName;
                        --maxFiles;
                        if (maxFiles == 0)
                        {
                            yield break;
                        }
                    }
                }
            }
        }

        public List<string> FindImageFiles(string[] fileNames)
        {
            return new List<string>(_GetImageFiles(fileNames, 50));
        }
    }
}
