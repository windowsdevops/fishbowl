namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using ClientManager;
    using System.Windows.Media.Imaging;
    using ClientManager.Controls;
    using Standard;
    using System.Threading;

    /// <summary>
    /// Interaction logic for UpdateStatusControl.xaml
    /// </summary>
    [TemplatePart(Name = "PART_ShareTextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_LinkTextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_PhotoImage", Type = typeof(Image))]
    public partial class UpdateStatusControl : UserControl
    {
        private TextBox shareTextBox;
        private TextBox linkTextBox;
        private Image photoImage;
        private string imageFile;

        public enum StatusDisplayMode
        {
            Base,
            Link,
            Photo
        }

        public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register("DisplayMode", typeof(StatusDisplayMode), typeof(UpdateStatusControl),
            new FrameworkPropertyMetadata(StatusDisplayMode.Base));
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(UpdateStatusControl));

        public static RoutedCommand UpdateStatusCommand { get; private set; }


        public StatusDisplayMode DisplayMode
        {
            get { return (StatusDisplayMode)GetValue(DisplayModeProperty); }
            set { SetValue(DisplayModeProperty, value); }
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        static UpdateStatusControl()
        {
            UpdateStatusCommand = new RoutedCommand("UpdateStatus", typeof(UpdateStatusControl));
        }

        public UpdateStatusControl()
        {
            InitializeComponent();

            this.CommandBindings.Add(new CommandBinding(UpdateStatusCommand, new ExecutedRoutedEventHandler(OnUpdateStatusCommand)));

            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.PreviewMouseDown += new MouseButtonEventHandler((sender, e) => this.IsActive = this.IsMouseOver);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.shareTextBox = this.Template.FindName("PART_ShareTextBox", this) as TextBox;
            this.linkTextBox = this.Template.FindName("PART_LinkTextBox", this) as TextBox;
            this.photoImage = this.Template.FindName("PART_PhotoImage", this) as Image;
        }

        private void OnUpdateStatusCommand(object sender, ExecutedRoutedEventArgs e)
        {
            string newStatus = this.shareTextBox.Text;
            string newlink = this.linkTextBox.Text;

            switch (this.DisplayMode)
            {
                case StatusDisplayMode.Link:
                    if (string.IsNullOrEmpty(newlink))
                    {
                        return;
                    }

                    ServiceProvider.ViewManager.UpdateStatus(newStatus, newlink);
                    break;
                case StatusDisplayMode.Photo:
                    if (string.IsNullOrEmpty(this.imageFile))
                    {
                        return;
                    }

                    string fileName = this.imageFile;

                    new Thread(new ThreadStart(() =>
                    {
                        Utility.FailableFunction(() => ServiceProvider.ViewManager.AddPhotoToApplicationAlbum(newStatus, fileName));
                    })).Start();

                    break;
                case StatusDisplayMode.Base:
                    if (string.IsNullOrEmpty(newStatus))
                    {
                        return;
                    }

                    ServiceProvider.ViewManager.UpdateStatus(newStatus);
                    break;
            }

            this.shareTextBox.Text = string.Empty;
            this.linkTextBox.Text = string.Empty;
            this.photoImage.Source = null;

            SwitchDisplayMode(StatusDisplayMode.Base);
            this.IsActive = false;
        }

        private void OnLinkButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.DisplayMode == StatusDisplayMode.Base)
            {
                SwitchDisplayMode(StatusDisplayMode.Link);
            }
            else
            {
                SwitchDisplayMode(StatusDisplayMode.Base);
            }
        }

        private void OnPhotoButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.DisplayMode == StatusDisplayMode.Base)
            {
                System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
                fileDialog.Filter = "Image Files (*.jpg;*.jpeg;*.gif;*.png)|*.jpg;*.jpeg;*.gif;*.png";

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.imageFile = DragContainer.ConstrainImage(fileDialog.FileName, PhotoUploadWizard.MaxPhotoDimension);

                    BitmapImage image = new BitmapImage(new Uri(this.imageFile));
                    image.Freeze();
                    this.photoImage.Source = image;

                    this.imageFile = fileDialog.FileName;
                    SwitchDisplayMode(StatusDisplayMode.Photo);
                }
            }
            else
            {
                this.photoImage.Source = null;
                SwitchDisplayMode(StatusDisplayMode.Base);
            }
        }

        private void SwitchDisplayMode(StatusDisplayMode mode)
        {
            if (mode == StatusDisplayMode.Base)
            {
                this.DisplayMode = StatusDisplayMode.Base;
                this.shareTextBox.Focus();
                this.shareTextBox.Select(this.shareTextBox.Text.Length, 0);
            }
            else if (mode == StatusDisplayMode.Link)
            {
                this.DisplayMode = StatusDisplayMode.Link;
                this.linkTextBox.Text = "";
                this.linkTextBox.Focus();
                this.linkTextBox.Select(this.linkTextBox.Text.Length, 0);
            }
            else if (mode == StatusDisplayMode.Photo)
            {
                this.DisplayMode = StatusDisplayMode.Photo;
            }
        }
    }
}
