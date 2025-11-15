//-----------------------------------------------------------------------
// <copyright file="FilmStripControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control that displays a film strip with all of the photos.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;
    using EffectLibrary;

    /// <summary>
    /// Control that displays a list of items as if they were in a film strip.  The background is specified separately from this control as that's the only way
    /// we can synchronize the movements of the filmstrip and the film strip items without clipping the film strip. 
    /// The currently selected film strip item is always animated to the center position.
    /// </summary>
    public class FilmStripControl : ListBox
    {
        public const int MaxNumberOfItemsDelta = 20;

        #region Fields
        /// <summary>
        /// Dependency property backing store for ItemHeight.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(FilmStripControl), new UIPropertyMetadata(0.0,
                new PropertyChangedCallback((sender, args) => 
                {
                    FilmStripControl filmStrip = (FilmStripControl)sender;
                    filmStrip.Dispatcher.BeginInvoke(new Action(() => filmStrip.BringCurrentItemIntoView()));
                })));

        /// <summary>
        /// True to enable shader effects while this control animates thumbnails.
        /// </summary>
        public static readonly DependencyProperty IsEffectEnabledProperty = 
            DependencyProperty.Register("IsEffectEnabled", typeof(bool), typeof(FilmStripControl), new UIPropertyMetadata(true));

        /// <summary>
        /// The duration of a "standard" slide; that is, the amount of time the animation should take, regardless of the number of items moved.
        /// </summary>
        /// <remarks>To synchronized the background and the items, this should match the values set in FilmStripPanel.</remarks>
        private static Duration standardSlideFilmStripDuration = new Duration(new TimeSpan(1700000));

        /// <summary>
        /// The time that should be added to the standard duration *per each item moved*; this prevents us warping from one side of the film 
        /// to the other if we move a large number of items.
        /// </summary>
        /// <remarks>To synchronized the background and the items, this should match the values set in FilmStripPanel.</remarks>
        private static int perItemSlideFilmStripTime = 300000;

        /// <summary>
        /// The index of the previous selected item. -1 means that the selection is empty just
        /// like ListBox.SelectedIndex does.
        /// </summary>
        private int previousSelectedIndex = -1;

        /// <summary>
        /// This is the element directly over visible area of the FilmStrip that the motion blur effect is applied. 
        /// Applying the effect on only the visible area improves performance.
        /// </summary>
        private UIElement filmStripBlurEffectArea;

        /// <summary>
        /// The animation for the filmStripBlurEffect when the film strip is scrolling
        /// more than one item at a time.
        /// </summary>
        private DoubleAnimation filmStripMultiItemBlurEffectAnimation = new DoubleAnimation();

        /// <summary>
        /// The animation for the filmStripBlurEffect when the film strip is scrolling
        /// one item at a time.
        /// </summary>
        private DoubleAnimation filmStripOneItemBlurEffectAnimation = new DoubleAnimation();

        /// <summary>
        /// The effect applied to the film strip when it is scrolling.
        /// </summary>
        private DirectionalBlurEffect filmStripBlurEffect;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the FilmStripControl class.
        /// </summary>
        public FilmStripControl()
        {
            this.Loaded += new RoutedEventHandler(this.OnLoad);
            this.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            this.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            this.BorderBrush = null;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the height of a photo thumbnail.
        /// </summary>
        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether shader effects are enable while
        /// this control animates thumbnails.
        /// </summary>
        public bool IsEffectsEnabled
        {
            get { return (bool)GetValue(IsEffectEnabledProperty); }
            set { SetValue(IsEffectEnabledProperty, value); }
        }
        #endregion

        #region Methods
        /// <summary>
        /// When the template is applied, locates the FilmStripBlurEffectArea
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.filmStripBlurEffectArea = this.FindName("FilmStripBlurEffectArea") as UIElement;
        }

        /// <summary>
        /// When the selected item changes, bring the current item into view.
        /// </summary>
        /// <param name="e">Event arguments describing the event.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            this.BringCurrentItemIntoView();
        }

        /// <summary>
        /// Brings the current item back into the center position when the size of the control changes so that
        /// it always stays centered over the photo.
        /// </summary>
        /// <param name="constraint">The size the control has for layout.</param>
        /// <returns>The size the control wants to layout.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            this.BringCurrentItemIntoView();
            return base.MeasureOverride(constraint);
        }

        /// <summary>
        /// Bring the currently selected item into view.
        /// </summary>
        private void BringCurrentItemIntoView()
        {
            ListBoxItem selectedListBoxItem;
            int numberOfItemsDelta = 0;
            int scrollDirection = -1; // -1 for moving to the left and 1 for right
            double blurAmount = 0.03; // the default DirectionalBlurEffect amount

            if (!this.IsLoaded)
            {
                return;
            }

            selectedListBoxItem = (ListBoxItem)this.ItemContainerGenerator.ContainerFromItem(this.SelectedItem);
            if (selectedListBoxItem != null)
            {
                selectedListBoxItem.BringIntoView();
            }

            if (this.SelectedIndex >= 0)
            {
                if (this.previousSelectedIndex > 0)
                {
                    numberOfItemsDelta = Math.Abs(this.SelectedIndex - this.previousSelectedIndex);
                }
                else
                {
                    // there was no previous selection
                    numberOfItemsDelta = this.SelectedIndex;
                }

                // determine if the new selected item is to the left or right of the 
                // previous item
                if (this.SelectedIndex > this.previousSelectedIndex)
                {
                    scrollDirection = -1;
                }
                else
                {
                    scrollDirection = 1;
                }

                this.previousSelectedIndex = this.SelectedIndex;
            }

            // Blur the entire control as it scrolls the item into view if the new item is at least 2 items away.
            if (this.IsEffectsEnabled && 
                this.filmStripBlurEffectArea != null && 
                FacebookClientApplication.IsShaderEffectSupported)
            {
                if (!(this.filmStripBlurEffect is DirectionalBlurEffect))
                {
                    this.filmStripBlurEffect = new DirectionalBlurEffect();
                    this.filmStripBlurEffect.Angle = 0; // horizontal blur
                }

                if (!(this.filmStripBlurEffectArea.Effect is DirectionalBlurEffect))
                {
                    this.filmStripBlurEffectArea.Effect = this.filmStripBlurEffect;
                }

                if (numberOfItemsDelta > 2)
                {
                    // The amount of blur is relative to the number of items being scrolled within the film strip
                    // with the maximum blur amount of blurAmount. The blur direction must also match direction of the
                    // FilmStripPanel.SetHorizontalOffset's horizontal animation to prevent jittering
                    blurAmount *= scrollDirection;
                    this.filmStripMultiItemBlurEffectAnimation.From = blurAmount * ((numberOfItemsDelta <= 0.0 ? 1.0 : numberOfItemsDelta) / (this.Items != null && this.Items.Count > 0.0 ? this.Items.Count - 1 : 1.0));
                    this.filmStripMultiItemBlurEffectAnimation.To = 0;
                    this.filmStripMultiItemBlurEffectAnimation.Duration = FilmStripControl.standardSlideFilmStripDuration + new Duration(new TimeSpan(Math.Min(MaxNumberOfItemsDelta, numberOfItemsDelta) * perItemSlideFilmStripTime));
                    this.filmStripMultiItemBlurEffectAnimation.AccelerationRatio = 0.4;
                    this.filmStripMultiItemBlurEffectAnimation.DecelerationRatio = 0.2;
                    this.filmStripBlurEffectArea.Effect.BeginAnimation(DirectionalBlurEffect.BlurAmountProperty, this.filmStripMultiItemBlurEffectAnimation, HandoffBehavior.SnapshotAndReplace);
                }
                else if (numberOfItemsDelta == 1)
                {
                    if (this.filmStripOneItemBlurEffectAnimation.From == null)
                    {
                        this.filmStripOneItemBlurEffectAnimation.From = 0;
                    }

                    // The amount of blur should slowly accumulate when moving between many items sequentially
                    // such as holding down the arrow key. The HandoffBehavior.SnapshotAndReplace behavior will ensure 
                    // the a smooth transition to the next animation.
                    this.filmStripOneItemBlurEffectAnimation.From += blurAmount * 0.01 * scrollDirection; // accumulate blur in small increments
                    this.filmStripOneItemBlurEffectAnimation.To = 0;
                    this.filmStripOneItemBlurEffectAnimation.Duration = FilmStripControl.standardSlideFilmStripDuration + new Duration(new TimeSpan(Math.Min(MaxNumberOfItemsDelta, numberOfItemsDelta) * perItemSlideFilmStripTime));
                    this.filmStripOneItemBlurEffectAnimation.AccelerationRatio = 0.4;
                    this.filmStripOneItemBlurEffectAnimation.DecelerationRatio = 0.2;
                    this.filmStripOneItemBlurEffectAnimation.Completed += new EventHandler(this.FilmStripOneItemBlurEffectAnimationCompleted);
                    this.filmStripBlurEffectArea.Effect.BeginAnimation(DirectionalBlurEffect.BlurAmountProperty, this.filmStripOneItemBlurEffectAnimation, HandoffBehavior.SnapshotAndReplace);
                }
            }
        }

        /// <summary>
        /// Reset the accumulated blurAmount from multiple sequential scrolling 
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private void FilmStripOneItemBlurEffectAnimationCompleted(object sender, EventArgs e)
        {
            this.filmStripOneItemBlurEffectAnimation.From = null;
        }

         /// <summary>
        /// Loaded event handler; brings the currently selected item into view.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments describing the event.</param>
        private void OnLoad(object sender, RoutedEventArgs e)
        {
            this.BringCurrentItemIntoView();
        }
        #endregion
    }
}
