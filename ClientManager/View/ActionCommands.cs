namespace ClientManager.View
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Contigo;
    using Standard;

    public sealed class ActionCommands
    {
        public ActionCommands(ViewManager viewManager)
        {
            Assert.IsNotNull(viewManager);
            foreach (PropertyInfo publicInstanceProperty in typeof(ActionCommands).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                ConstructorInfo cons = publicInstanceProperty.PropertyType.GetConstructor(new[] { typeof(ViewManager) });
                Assert.IsNotNull(cons);
                object command = cons.Invoke(new object[] { viewManager });
                Assert.AreEqual(command.GetType().Name, publicInstanceProperty.Name);
                publicInstanceProperty.SetValue(this, command, null);
            }
        }

        public AddCommentCommand AddCommentCommand { get; private set; }
        public AddCommentToPhotoCommand AddCommentToPhotoCommand { get; private set; }
        public AddLikeCommand AddLikeCommand { get; private set; }
        public CopyItemCommand CopyItemCommand { get; private set; }
        public GetMoreCommentsCommand GetMoreCommentsCommand { get; private set; }
        public MarkAsReadCommand MarkAsReadCommand { get; private set; }
        public MarkAllAsReadCommand MarkAllAsReadCommand { get; private set; }
        public RemoveCommentCommand RemoveCommentCommand { get; private set; }
        public RemoveLikeCommand RemoveLikeCommand { get; private set; }
        public SetNewsFeedFilterCommand SetNewsFeedFilterCommand { get; private set; }
        public SetSortOrderCommand SetSortOrderCommand { get; private set; }
        public StartSyncCommand StartSyncCommand { get; private set; }
        public WriteOnWallCommand WriteOnWallCommand { get; private set; }
    }

    public abstract class ActionCommand : ViewCommand
    {
        protected ActionCommand(ViewManager viewManager)
            : base(viewManager)
        {}

        protected sealed override void ExecuteInternal(object parameter)
        {
            PerformAction(parameter);
        }

        protected abstract void PerformAction(object parameter);
    }

    public sealed class AddLikeCommand : ActionCommand
    {
        public AddLikeCommand(ViewManager viewManager)
            : base(viewManager)
        {}

        protected override bool CanExecuteInternal(object parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            ActivityPost activityPost = parameter as ActivityPost;
            return activityPost.CanLike && !activityPost.HasLiked;
        }

        protected override void PerformAction(object parameter)
        {
            ActivityPost activityPost = parameter as ActivityPost;
            ServiceProvider.FacebookService.AddLike(activityPost);
        }
    }

    public sealed class RemoveLikeCommand : ActionCommand
    {
        public RemoveLikeCommand(ViewManager viewManager)
            : base(viewManager)
        {
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            ActivityPost activityPost = parameter as ActivityPost;
            return activityPost.HasLiked;
        }

        protected override void PerformAction(object parameter)
        {
            ActivityPost activityPost = parameter as ActivityPost;
            ServiceProvider.FacebookService.RemoveLike(activityPost);
        }
    }

    public sealed class GetMoreCommentsCommand : ActionCommand
    {
        public GetMoreCommentsCommand(ViewManager viewManager)
            : base(viewManager)
        {
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            ActivityPost activityPost = parameter as ActivityPost;
            return activityPost.HasMoreComments;
        }

        protected override void PerformAction(object parameter)
        {
            ActivityPost activityPost = parameter as ActivityPost;
            ServiceProvider.FacebookService.GetMoreComments(activityPost);
        }
    }

    public sealed class AddCommentCommand : ActionCommand
    {
        public AddCommentCommand(ViewManager viewManager)
            : base(viewManager)
        {
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            object[] parameterList = parameter as object[];
            ActivityPost activityPost = parameterList[0] as ActivityPost;

            if (activityPost == null)
            {
                return false;
            }

            return activityPost.CanComment;
        }

        protected override void PerformAction(object parameter)
        {
            object[] parameterList = parameter as object[];
            ActivityPost activityPost = parameterList[0] as ActivityPost;
            string comment = parameterList[1] as string;
            ServiceProvider.FacebookService.AddComment(activityPost, comment);
        }
    }

    public sealed class RemoveCommentCommand : ActionCommand
    {
        public RemoveCommentCommand(ViewManager viewManager)
            : base(viewManager)
        {
        }

        protected override bool CanExecuteInternal(object parameter)
        {
            ActivityComment comment = parameter as ActivityComment;

            if (comment == null)
            {
                return false;
            }

            return comment.IsMine;
        }

        protected override void PerformAction(object parameter)
        {
            ActivityComment comment = parameter as ActivityComment;
            ServiceProvider.FacebookService.RemoveComment(comment);
        }
    }

    public sealed class WriteOnWallCommand : ActionCommand
    {
        public WriteOnWallCommand(ViewManager viewManager)
            : base(viewManager)
        {}

        protected override bool CanExecuteInternal(object parameter)
        {
            object[] parameterList = parameter as object[];
            return parameterList != null && parameterList.Length == 2 &&
                parameterList[0] is FacebookContact && parameterList[1] is string;
        }

        protected override void PerformAction(object parameter)
        {
            object[] parameterList = parameter as object[];
            FacebookContact contact = parameterList[0] as FacebookContact;
            string comment = parameterList[1] as string;

            ServiceProvider.FacebookService.WriteOnWall(contact, comment);
        }
    }

    public sealed class CopyItemCommand : ActionCommand
    {
        public CopyItemCommand(ViewManager viewManager)
            : base(viewManager)
        {}

        protected override bool CanExecuteInternal(object parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            ActivityPost activityPost = parameter as ActivityPost;
            return true;
        }

        protected override void PerformAction(object parameter)
        {
            ActivityPost activityPost = parameter as ActivityPost;

            String fullitemstring = activityPost.Actor + ": " + activityPost.Message + " " + activityPost.Updated.ToString();

            Clipboard.SetData(DataFormats.Text, fullitemstring);
        }
    }

    public sealed class StartSyncCommand : ViewCommand
    {
        public StartSyncCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override void ExecuteInternal(object parameter)
        {
            ServiceProvider.FacebookService.Refresh();
        }
    }

    public sealed class MarkAsReadCommand : ViewCommand
    {
        public MarkAsReadCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            var notification = parameter as Notification;
            return notification != null && notification.IsUnread;
        }

        protected override void ExecuteInternal(object parameter)
        {
            var notification = parameter as Notification;
            if (notification != null)
            {
                ServiceProvider.FacebookService.ReadNotification(notification);
            }
        }
    }

    public sealed class MarkAllAsReadCommand : ViewCommand
    {
        public MarkAllAsReadCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            return parameter is IEnumerable;
        }

        protected override void ExecuteInternal(object parameter)
        {
            var enumerable = parameter as IEnumerable;
            if (enumerable == null)
            {
                return;
            }

            foreach (var notification in enumerable.OfType<Notification>())
            {
                ServiceProvider.FacebookService.ReadNotification(notification);
            }
        }
    }

    public sealed class SetNewsFeedFilterCommand : ViewCommand
    {
        public SetNewsFeedFilterCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            return parameter == null || parameter is ActivityFilter;
        }

        protected override void ExecuteInternal(object parameter)
        {
            var filter = parameter as ActivityFilter;
            ServiceProvider.FacebookService.NewsFeedFilter = filter;
        }
    }

    public sealed class SetSortOrderCommand : ViewCommand
    {
        public SetSortOrderCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            return parameter is PhotoAlbumSortOrder || parameter is ContactSortOrder;
        }

        protected override void ExecuteInternal(object parameter)
        {
            if (parameter is PhotoAlbumSortOrder)
            {
                var sortOrder = (PhotoAlbumSortOrder)parameter;
                ServiceProvider.FacebookService.PhotoAlbumSortOrder = sortOrder;
            }
            else if (parameter is ContactSortOrder)
            {
                var sortOrder = (ContactSortOrder)parameter;
                ServiceProvider.FacebookService.ContactSortOrder = sortOrder;
            }
        }
    }

    public sealed class AddCommentToPhotoCommand : ViewCommand
    {
        public AddCommentToPhotoCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            object[] parameters = parameter as object[];
            if (parameters == null || parameters.Length != 2)
            {
                return false;
            }

            FacebookPhoto photo = parameters[0] as FacebookPhoto;
            string comment = parameters[1] as string;
            return photo != null && photo.CanComment && !string.IsNullOrEmpty(comment);
        }

        protected override void ExecuteInternal(object parameter)
        {
            object[] parameters = parameter as object[];
            FacebookPhoto photo = parameters[0] as FacebookPhoto;
            string comment = parameters[1] as string;

            if (photo != null && photo.CanComment && !string.IsNullOrEmpty(comment))
            {
                ServiceProvider.FacebookService.AddComment(photo, comment);
            }
        }
    }
}
