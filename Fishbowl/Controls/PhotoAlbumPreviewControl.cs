
namespace FacebookClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Controls;
    using System.Windows;

    public class PhotoAlbumPreviewControl : Control
    {
        public static readonly DependencyProperty ShowOwnerProperty = DependencyProperty.Register(
            "ShowOwner",
            typeof(bool),
            typeof(GalleryHomeControl),
            new PropertyMetadata(true));

        public bool ShowOwner
        {
            get { return (bool)GetValue(ShowOwnerProperty); }
            set { SetValue(ShowOwnerProperty, value); }
        }


    }
}
