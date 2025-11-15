//-----------------------------------------------------------------------
// <copyright file="FriendViewerControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control used to display a full photo.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;
    using ClientManager;
    using ClientManager.Controls;
    using Contigo;
    using Standard;

    public class FriendViewerControl : SizeTemplateControl
    {
        private FriendBarControl _friendBar;
        private ScaleScrollViewer _wallScrollViewer;

        public static RoutedCommand DisplayInfoCommand { get; private set; }
        public static RoutedCommand DisplayWallCommand { get; private set; }
        public static RoutedCommand DisplayPhotosCommand { get; private set; }

        public static readonly DependencyProperty FacebookContactProperty = DependencyProperty.Register(
            "FacebookContact",
            typeof(FacebookContact),
            typeof(FriendViewerControl), 
            new FrameworkPropertyMetadata(
                null,
                (d, e) => ((FriendViewerControl)d)._OnFacebookContactChanged(e)));

        public FacebookContact FacebookContact
        {
            get { return (FacebookContact)this.GetValue(FacebookContactProperty); }
            set { this.SetValue(FacebookContactProperty, value); }
        }

        private static readonly DependencyPropertyKey IsMeContactPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsMeContact",
            typeof(bool),
            typeof(FriendViewerControl),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsMeContactProperty = IsMeContactPropertyKey.DependencyProperty;

        public bool IsMeContact
        {
            get { return (bool)GetValue(IsMeContactProperty); }
            private set { SetValue(IsMeContactPropertyKey, value); }
        }

        /// <summary>
        /// WallDisplayIndex Dependency Property
        /// </summary>
        public static readonly DependencyProperty WallDisplayIndexProperty = DependencyProperty.Register(
            "WallDisplayIndex", 
            typeof(int), 
            typeof(FriendViewerControl),
            new PropertyMetadata(0, new PropertyChangedCallback((sender, e) => s_wallDisplayIndex = (int)e.NewValue)));

        public int WallDisplayIndex
        {
            get { return (int)GetValue(WallDisplayIndexProperty); }
            set { SetValue(WallDisplayIndexProperty, value); }
        }

        public static readonly DependencyProperty InfoDisplayIndexProperty = DependencyProperty.Register(
            "InfoDisplayIndex",
            typeof(int),
            typeof(FriendViewerControl),
            new PropertyMetadata(-1, new PropertyChangedCallback((sender, e) => s_infoDisplayIndex = (int)e.NewValue)));

        public int InfoDisplayIndex
        {
            get { return (int)GetValue(InfoDisplayIndexProperty); }
            set { SetValue(InfoDisplayIndexProperty, value); }
        }

        public static readonly DependencyProperty PhotosDisplayIndexProperty = DependencyProperty.Register(
            "PhotosDisplayIndex",
            typeof(int),
            typeof(FriendViewerControl),
            new PropertyMetadata(-1, new PropertyChangedCallback((sender, e) => s_photosDisplayIndex = (int)e.NewValue)));

        public int PhotosDisplayIndex
        {
            get { return (int)GetValue(PhotosDisplayIndexProperty); }
            set { SetValue(PhotosDisplayIndexProperty, value); }
        }

        private static int s_wallDisplayIndex = 0;
        private static int s_infoDisplayIndex = -1;
        private static int s_photosDisplayIndex = -1;

        static FriendViewerControl()
        {
            DisplayInfoCommand = new RoutedCommand("DisplayInfo", typeof(FriendViewerControl));
            DisplayWallCommand = new RoutedCommand("DisplayWall", typeof(FriendViewerControl));
            DisplayPhotosCommand = new RoutedCommand("DisplayPhotos", typeof(FriendViewerControl));
        }

        public FriendViewerControl()
        {
            CommandBindings.Add(new CommandBinding(DisplayInfoCommand,   (d, e) => ((FriendViewerControl)d)._OnDisplayInfoCommand(e)));
            CommandBindings.Add(new CommandBinding(DisplayWallCommand,   (d, e) => ((FriendViewerControl)d)._OnDisplayWallCommand(e)));
            CommandBindings.Add(new CommandBinding(DisplayPhotosCommand, (d, e) => ((FriendViewerControl)d)._OnDisplayPhotosCommand(e)));

            WallDisplayIndex = s_wallDisplayIndex;
            InfoDisplayIndex = s_infoDisplayIndex;
            PhotosDisplayIndex = s_photosDisplayIndex;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _friendBar = (FriendBarControl)Template.FindName("PART_FriendBar", this);
            Assert.IsNotNull(_friendBar);
            _friendBar.SetActiveItem(FacebookContact);

            _wallScrollViewer = (ScaleScrollViewer)Template.FindName("PART_WallScrollViewer", this);
            Assert.IsNotNull(_wallScrollViewer);

            // Focus the control so that we can grab keyboard events.
            this.Focus();
        }

        private void _OnDisplayInfoCommand(ExecutedRoutedEventArgs e)
        {
            PhotosDisplayIndex = -1;
            WallDisplayIndex = -1;
            InfoDisplayIndex = 0;
        }

        private void _OnDisplayWallCommand(ExecutedRoutedEventArgs e)
        {
            PhotosDisplayIndex = -1;
            WallDisplayIndex = 0;
            InfoDisplayIndex = -1;
        }

        private void _OnDisplayPhotosCommand(ExecutedRoutedEventArgs e)
        {
            PhotosDisplayIndex = 0;
            WallDisplayIndex = -1;
            InfoDisplayIndex = -1;
        }

        private void _OnFacebookContactChanged(DependencyPropertyChangedEventArgs e)
        {
            IsMeContact = (FacebookContact == ServiceProvider.ViewManager.MeContact);

            if (_friendBar != null)
            {
                _friendBar.SetActiveItem(e.NewValue);
            }

            if (_wallScrollViewer != null)
            {
                _wallScrollViewer.FinalVerticalOffset = 0;
            }
        }
    }
}
