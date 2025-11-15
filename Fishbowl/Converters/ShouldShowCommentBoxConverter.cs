
namespace FacebookClient
{
    using Contigo;
    using System;
    using System.Windows.Data;
    using System.Globalization;

    public class ShouldShowCommentBoxConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var activityPost = value as ActivityPost;
            if (value == null)
            {
                return false;
            }

            if (!activityPost.CanComment)
            {
                return false;
            }

            if (activityPost.CommentCount != 0)
            {
                return true;
            }

            //if (activityPost.LikedCount != 0)
            //{
            //    return true;
            //}

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
