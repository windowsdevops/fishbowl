
namespace FacebookClient
{
    using System;
    using System.Windows;
    using ClientManager.Controls;

    public class GalleryHomeControl : SizeTemplateControl
    {
        public static readonly DependencyProperty ShowSortBarProperty = DependencyProperty.Register(
            "ShowSortBar",
            typeof(bool), 
            typeof(GalleryHomeControl),
            new FrameworkPropertyMetadata(true));

        public bool ShowSortBar
        {
            get { return (bool)GetValue(ShowSortBarProperty); }
            set { SetValue(ShowSortBarProperty, value); }
        }

        public static readonly DependencyProperty ShowOwnerOverlayProperty = DependencyProperty.Register(
            "ShowOwnerOverlay",
            typeof(bool), 
            typeof(GalleryHomeControl),
            new FrameworkPropertyMetadata(true));

        public bool ShowOwnerOverlay
        {
            get { return (bool)GetValue(ShowOwnerOverlayProperty); }
            set { SetValue(ShowOwnerOverlayProperty, value); }
        }

        public GalleryHomeControl()
        { }
        
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            
            this.Focus();
        }
    }
}
