
namespace ClientManager.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using Contigo;

    public class ActivityPostControl : Control
    {
        public static readonly DependencyProperty ActivityPostProperty = DependencyProperty.Register(
            "ActivityPost",
            typeof(ActivityPost),
            typeof(ActivityPostControl),
            new PropertyMetadata(null,
                (d, e) => ((ActivityPostControl)d)._OnActivityPostChanged(e)));

        private void _OnActivityPostChanged(DependencyPropertyChangedEventArgs e)
        {
            ShowCommentTextBox = _ShouldShowCommentTextBox();
        }

        public ActivityPost ActivityPost
        {
            get { return (ActivityPost)GetValue(ActivityPostProperty); }
            set { SetValue(ActivityPostProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        private static readonly DependencyPropertyKey ShowCommentTextBoxPropertyKey = DependencyProperty.RegisterReadOnly(
            "ShowCommentTextBox",
            typeof(bool),
            typeof(ActivityPostControl),
            new PropertyMetadata(false));

        public static readonly DependencyProperty ShowCommentTextBoxProperty = ShowCommentTextBoxPropertyKey.DependencyProperty;

        public bool ShowCommentTextBox
        {
            get { return (bool)GetValue(ShowCommentTextBoxProperty); }
            private set { SetValue(ShowCommentTextBoxPropertyKey, value); }
        }

        private bool _ShouldShowCommentTextBox()
        {
            if (ActivityPost == null)
            {
                return false;
            }

            if (!ActivityPost.CanComment)
            {
                return false;
            }

            if (ActivityPost.CommentCount != 0)
            {
                return true;
            }

            if (ActivityPost.LikedCount != 0)
            {
                return true;
            }

            return false;
        }
    }
}
