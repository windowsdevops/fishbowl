
namespace FacebookClient
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Navigation;
    using ClientManager;
    using Contigo;
    using Standard;

    public class PeopleWhoLikeThisSpan : Span
    {
        public PeopleWhoLikeThisSpan()
        {
            Loaded += PeopleWhoLikeThisSpan_Loaded;
            Unloaded += PeopleWhoLikeThisSpan_Unloaded;
        }

        void PeopleWhoLikeThisSpan_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ActivityPost != null)
            {
                ActivityPost.PropertyChanged -= _ActivityPostPropertyChanged;
            }
        }

        void PeopleWhoLikeThisSpan_Loaded(object sender, RoutedEventArgs e)
        {
            if (ActivityPost != null)
            {
                ActivityPost.PropertyChanged += _ActivityPostPropertyChanged;
                _GenerateInlines();
            }
        }

        public static readonly DependencyProperty ActivityPostProperty = DependencyProperty.Register(
            "ActivityPost",
            typeof(ActivityPost),
            typeof(PeopleWhoLikeThisSpan),
            new PropertyMetadata(
                (d, e) => ((PeopleWhoLikeThisSpan)d)._OnActivityPostChanged(e)));

        private void _OnActivityPostChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var oldPost = e.OldValue as ActivityPost;
            var newPost = e.NewValue as ActivityPost;

            if (oldPost != null)
            {
                oldPost.PropertyChanged -= _ActivityPostPropertyChanged;                
            }

            if (newPost != null)
            {
                newPost.PropertyChanged += _ActivityPostPropertyChanged;
            }
            _GenerateInlines();
        }

        private void _ActivityPostPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PeopleWhoLikeThis":
                case "LikedCount":
                case "HasLiked":
                    Dispatcher.BeginInvoke((Action)_GenerateInlines, null);
                    break;
                default:
                    break;
            }
        }

        public ActivityPost ActivityPost
        {
            get { return (ActivityPost)GetValue(ActivityPostProperty); }
            set { SetValue(ActivityPostProperty, value); }
        }

        private void _GenerateInlines()
        {
            // This all could probably be simplified...

            Inlines.Clear();
            if (ActivityPost == null)
            {
                return;
            }

            if (ActivityPost.LikedCount == 0)
            {
               // Inlines.Add(new Run("No one has liked this."));  
               // We shouldn't add this text.  It looks like an undigg...
                return;
            }

            if (ActivityPost.PeopleWhoLikeThis.Count == 0)
            {
                if (ActivityPost.LikedCount == 1)
                {
                    if (ActivityPost.HasLiked)
                    {
                        Inlines.Add(new Run("You like this."));
                    }
                    else
                    {
                        Inlines.Add(new Run("1 person likes this."));
                    }
                }
                else
                {
                    if (ActivityPost.HasLiked)
                    {
                        if (ActivityPost.LikedCount == 2)
                        {
                            Inlines.Add(new Run("You and 1 other person like this"));
                        }
                        else
                        {
                            Inlines.Add(new Run(string.Format("You and {0} other people like this", ActivityPost.LikedCount-1)));
                        }
                    }
                    else
                    {
                        Inlines.Add(new Run(string.Format("{0} people like this.", ActivityPost.LikedCount)));
                    }
                }
                return;
            }

            // Scenarios:
            // A likes this.
            // A and 3 others like this.
            // A and B like this
            // A, B, and 1 other person like this.
            // A, B, and 3 others like this.
            // A and you like this.
            // A, B, and C like this.

            int peopleLeft = ActivityPost.LikedCount - ActivityPost.PeopleWhoLikeThis.Count;
            Assert.IsTrue(peopleLeft >= 0);

            int namedCount = ActivityPost.PeopleWhoLikeThis.Count;
            int youIndex = -1;

            if (ActivityPost.HasLiked)
            {
                youIndex = namedCount;
                ++namedCount;
                --peopleLeft;
            }
            else if (namedCount == 1 && peopleLeft == 0)
            {
                Inlines.Add(new Run(string.Format("{0} likes this.", ActivityPost.PeopleWhoLikeThis[0])));
                return;
            }

            for (int i = 0; i < namedCount; ++i)
            {
                if (i == namedCount-1 && peopleLeft == 0)
                {
                    if (i != 1)
                    {
                        Inlines.Add(new Run(", and "));
                    }
                    else
                    {
                        Inlines.Add(new Run(" and "));
                    }
                }
                else if (i != 0)
                {
                    Inlines.Add(new Run(", "));
                }

                if (i == youIndex)
                {
                    Inlines.Add("you");
                }
                else
                {
                    FacebookContact contact = ActivityPost.PeopleWhoLikeThis[i];
                    var hyperlink = new Hyperlink
                    {
                        NavigateUri = new Uri("http://facebook.com/profile.php?id=" + contact.UserId)
                    };
                    hyperlink.RequestNavigate += _OnRequestNavigate;

                    if (string.IsNullOrEmpty(contact.Name))
                    {
                        hyperlink.Inlines.Add("Someone");
                    }
                    else
                    {
                        hyperlink.Inlines.Add(contact.Name);
                    }
                    Inlines.Add(hyperlink);
                }
            }

            if (namedCount != 1 && peopleLeft != 0)
            {
                Inlines.Add(",");
            }

            if (peopleLeft == 1)
            {
                Inlines.Add(" and 1 other person like this.");
            }
            else if (peopleLeft != 0)
            {
                Inlines.Add(string.Format(" and {0} others like this.", peopleLeft));
            }
            else
            {
                Inlines.Add(" like this.");
            }
        }

        private static void _OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;
            ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.Execute(hyperlink.NavigateUri);
            e.Handled = true;
        }
    }
}
