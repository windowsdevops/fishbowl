
namespace FacebookClient.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using Contigo;

    /// <summary>
    /// Interaction logic for PhotoTaggerControl.xaml
    /// </summary>
    public partial class PhotoTaggerControl : UserControl
    {
        #region Fields

        private IEnumerable<FacebookContact> friends = new List<FacebookContact>();
        private ObservableCollection<FacebookContact> relevantFriends = new ObservableCollection<FacebookContact>();
        private ObservableCollection<FacebookContact> filteredFriends;
        public static Duration FADE_TIME = new Duration(new TimeSpan(0, 0, 0, 0, 3));
        public event TagsUpdatedEventHandler TagsUpdatedEvent;
        public event TagsCanceledEventHandler TagsCanceledEvent;

        private Storyboard fadeIn;
        private Storyboard fadeOut;

        #endregion // Fields

        public PhotoTaggerControl()
        {
            this.InitializeComponent();

            this.Visibility = Visibility.Collapsed;

            // Get a reference to appropriate animations
            this.fadeIn = (Storyboard)this.Resources["FadeInStoryboard"];
            this.fadeOut = (Storyboard)this.Resources["FadeOutStoryboard"];
            this.fadeOut.Completed += new EventHandler(FadeOutAnimation_Completed);

            // Update friends list
            this.UpdateLists();
        }

        public static readonly DependencyProperty PhotoAlbumProperty = DependencyProperty.Register(
            "PhotoAlbum",
            typeof(FacebookPhotoAlbum),
            typeof(PhotoTaggerControl),
            new FrameworkPropertyMetadata(null));

        public FacebookPhotoAlbum PhotoAlbum
        {
            get { return (FacebookPhotoAlbum)GetValue(PhotoAlbumProperty); }
            set { SetValue(PhotoAlbumProperty, value); }
        }

        /// <summary>
        /// Gets the transform point of control.
        /// </summary>
        public Point TransformPoint
        {
            get
            {
                TranslateTransform tf = (TranslateTransform)this.RenderTransform;
                return new Point(tf.X, tf.Y);
            }
            set
            {
                TranslateTransform tf = new TranslateTransform(value.X, value.Y);
                this.RenderTransform = tf;
            }
        }

        public ObservableCollection<FacebookContact> RelevantFriends
        {
            get { return this.relevantFriends; }
        }

        public ObservableCollection<FacebookContact> FilteredFriends
        {
            get { return this.filteredFriends; }
        }

        public IEnumerable<FacebookContact> Friends
        {
            get { return this.friends; }
            set
            {
                this.friends = value;

                // Validate for null list
                if (this.friends == null)
                {
                    this.friends = new Collection<FacebookContact>();
                }

                // Filter friends based on filter text
                this.FilterFriendsList();
            }
        }

        #region Public Methods

        /// <summary>
        /// Display the TagBin user control.
        /// </summary>
        public void Open()
        {
            if (this.Visibility != Visibility.Visible)
            {
                this.fadeIn.Begin();
            }

            // Give focus to filter text box
            this.NameFilterTextBox.Focus();
        }

        /// <summary>
        /// Close the TagBin user control.
        /// </summary>
        public void Close()
        {
            this.fadeOut.Begin();
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Event handler for changing text in filter text box.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.FilterFriendsList();
        }

        /// <summary>
        /// Event handler to clean up tag big after it's been closed.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private void FadeOutAnimation_Completed(object sender, EventArgs e)
        {
            // Clear filter textbox
            this.NameFilterTextBox.Text = String.Empty;

            // Refresh lists
            this.FilterFriendsList();
            this.relevantFriends = new ObservableCollection<FacebookContact>(this.relevantFriends);
            this.RelevantNames.GetBindingExpression(ListBox.ItemsSourceProperty).UpdateTarget();

            // Scroll to top of list
            if (this.FilteredNames.Items.Count > 1)
            {
                this.FilteredNames.ScrollIntoView(this.FilteredNames.Items[0]);
            }

            if (this.RelevantNames.Items.Count > 1)
            {
                this.RelevantNames.ScrollIntoView(this.RelevantNames.Items[0]);
            }
        }

        /// <summary>
        /// Filters friends list based on the filter text.
        /// </summary>
        private void FilterFriendsList()
        {
            IEnumerable<FacebookContact> list;

            // Filter list of friends based on filter text
            list = from friend in this.friends
                   where (friend.Name.IndexOf(this.NameFilterTextBox.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
                   select friend;

            this.filteredFriends = new ObservableCollection<FacebookContact>(list);
            this.FilteredNames.GetBindingExpression(ListBox.ItemsSourceProperty).UpdateTarget();
        }

        private void UpdateLists()
        {
            // Update friends list
            this.Friends = ClientManager.ServiceProvider.ViewManager.Friends as IEnumerable<FacebookContact>;

            if (PhotoAlbum != null)
            {
                // Add photo album owner to tags
                this.relevantFriends.Add(PhotoAlbum.Owner);
            }

            // Add yourself
            if (!this.relevantFriends.Contains(ClientManager.ServiceProvider.ViewManager.MeContact))
            {
                this.relevantFriends.Add(ClientManager.ServiceProvider.ViewManager.MeContact);
            }

            // Add all tagged people in current album
            if (PhotoAlbum != null)
            {
                foreach (FacebookPhoto photo in PhotoAlbum.Photos)
                {
                    FacebookPhotoTagCollection tags = photo.Tags;

                    foreach (FacebookPhotoTag tag in tags)
                    {
                        if (!this.relevantFriends.Contains(tag.Contact))
                        {
                            this.relevantFriends.Add(tag.Contact);
                        }
                    }
                }
            }

            // Update target
            this.FilteredNames.GetBindingExpression(ListBox.ItemsSourceProperty).UpdateTarget();
            this.RelevantNames.GetBindingExpression(ListBox.ItemsSourceProperty).UpdateTarget();
        }

        /// <summary>
        /// Event handler for canceling the tag operation.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            if (this.TagsCanceledEvent != null)
            {
                this.TagsCanceledEvent(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Event handler for when checkbox is toggled on or off.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Event args.</param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;

            if (cb != null)
            {
                // Find ListBoxItem parent of CheckBox
                ListBoxItem item = FindVisualParent(sender as CheckBox);
                FacebookContact selectedUser = (FacebookContact)item.Content;

                // TODO : make this an asyn call
                this.Close();

                if (this.TagsUpdatedEvent != null)
                {
                    this.TagsUpdatedEvent(this, new TagsUpdatedArgs(selectedUser));
                }
            }
        }

        /// <summary>
        /// Finds the first parent of type ListBoxItem for given UIElement.
        /// </summary>
        /// <param name="element">UIElement for which to find the parent.</param>
        /// <returns>First ListBoxItem parent.</returns>
        private static ListBoxItem FindVisualParent(UIElement element)
        {
            UIElement parent = element;
            while (parent != null)
            {
                ListBoxItem correctlyTyped = parent as ListBoxItem;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }

            return null;
        }

        #endregion Private Methods
    }

    /// <summary>
    /// Event handler for when the user is done tagging.
    /// </summary>
    /// <param name="sender">Event source.</param>
    /// <param name="e">Tag updated event.</param>
    public delegate void TagsUpdatedEventHandler(object sender, TagsUpdatedArgs e);

    /// <summary>
    /// Event handler for when the tagger control is closed.
    /// </summary>
    /// <param name="sender">Event source.</param>
    /// <param name="e">Event args.</param>
    public delegate void TagsCanceledEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Custom event args for updating the tags.
    /// </summary>
    public class TagsUpdatedArgs : EventArgs
    {
        public TagsUpdatedArgs()
        {
        }

        public TagsUpdatedArgs(FacebookContact contact)
        {
            this.SelectedContact = contact;
        }

        public FacebookContact SelectedContact
        {
            get;
            private set;
        }
    }
}
