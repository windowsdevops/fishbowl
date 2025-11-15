//-----------------------------------------------------------------------
// <copyright file="RowScrollingPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The panel used to display items in the PhotoAlbumControl;
//     it virtualizes for performance, wraps for display, and
//     scrolls by row without displaying partial rows.
//     Essentially, a VirtualizingScrollByFullRowWrapPanel, or
//     RowScrollingPanel for short.
// </summary>
//-----------------------------------------------------------------------

namespace FacebookClient
{
    using System;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// The panel used to display items in the PhotoAlbumControl.
    /// </summary>
    public class RowScrollingPanel : VirtualizingPanel, IScrollInfo
    {
        #region Fields
        /// <summary>
        /// DependencyProperty backing store for ItemWidth.
        /// </summary>
        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register("ItemWidth", typeof(double), typeof(RowScrollingPanel),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// DependencyProperty backing store for ItemHeight.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(RowScrollingPanel),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The ScrollViewer displaying this panel.
        /// </summary>
        private ScrollViewer owner;

        /// <summary>
        /// A value indicating whether the content of this panel can scroll horizontally.
        /// </summary>
        private bool canHorizontallyScroll;

        /// <summary>
        /// A value indicating whether the content of this panel can scroll vertically.
        /// </summary>
        private bool canVerticallyScroll;

        /// <summary>
        /// The size of the entire RowScrollingPanel.
        /// </summary>
        private Size extent = new Size(0, 0);

        /// <summary>
        /// The size of the region currently in view.
        /// </summary>
        private Size viewport = new Size(0, 0);

        /// <summary>
        /// The viewport's offset from the top of the panel.
        /// </summary>
        private Point offset;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a value that specifies the width of all items that are contained within a RowScrollingPanel. This is a dependency property.
        /// </summary>
        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that specifies the height of all items that are contained within a RowScrollingPanel. This is a dependency property.
        /// </summary>
        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ScrollViewer displaying this panel.
        /// </summary>
        public ScrollViewer ScrollOwner
        {
            get { return this.owner; }
            set { this.owner = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content of this panel can scroll horizontally.
        /// </summary>
        public bool CanHorizontallyScroll
        {
            get { return this.canHorizontallyScroll; }
            set { this.canHorizontallyScroll = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content of this panel can scroll vertically.
        /// </summary>
        public bool CanVerticallyScroll
        {
            get { return this.canVerticallyScroll; }
            set { this.canVerticallyScroll = value; }
        }

        /// <summary>
        /// Gets the height of the entire RowScrollingPanel.
        /// </summary>
        public double ExtentHeight
        {
            get { return this.extent.Height; }
        }

        /// <summary>
        /// Gets the width of the entire RowScrollingPanel.
        /// </summary>
        public double ExtentWidth
        {
            get { return this.extent.Width; }
        }

        /// <summary>
        /// Gets the height of the region currently displayed.
        /// </summary>
        public double ViewportHeight
        {
            get { return this.viewport.Height; }
        }

        /// <summary>
        /// Gets the width of the region currently displayed.
        /// </summary>
        public double ViewportWidth
        {
            get { return this.viewport.Width; }
        }

        /// <summary>
        /// Gets the viewport's horizontal offset from the left of the panel.
        /// </summary>
        public double HorizontalOffset
        {
            get { return this.offset.X; }
        }

        /// <summary>
        /// Gets the viewport's vertical offset from the top of the panel.
        /// </summary>
        public double VerticalOffset
        {
            get { return this.offset.Y; }
        }

        public bool CanScrollUp
        {
            get
            {
                if (this.ItemHeight == 0.0 || this.ItemWidth == 0.0)
                {
                    return false;
                }

                int firstIndex, lastIndex;
                GetVisibleRange(out firstIndex, out lastIndex);
                return firstIndex > 0;
            }
        }

        public bool CanScrollDown
        {
            get
            {
                if (this.ItemHeight == 0.0 || this.ItemWidth == 0.0)
                {
                    return false;
                }

                int firstIndex, lastIndex;
                GetVisibleRange(out firstIndex, out lastIndex);

                ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
                int numItems = itemsControl.HasItems ? itemsControl.Items.Count : 0;

                return lastIndex < numItems - 1;
            }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the vertical position of the scrollbar.
        /// </summary>
        /// <param name="offset">The desired scrollbar position.</param>
        public void SetVerticalOffset(double offset)
        {
            this.SetVerticalOffset(offset, false);
        }

        /// <summary>
        /// Scrolls the display one row of items down, if possible.
        /// </summary>
        public void LineDown()
        {
            this.SetVerticalOffset(this.offset.Y + this.ItemHeight, true);
        }

        /// <summary>
        /// Scrolls the display one row of items up, if possible.
        /// </summary>
        public void LineUp()
        {
            this.SetVerticalOffset(this.offset.Y - this.ItemHeight, true);
        }

        /// <summary>
        /// Scrolls the display down with the mouse wheel, if possible.
        /// </summary>
        public void MouseWheelDown()
        {
            this.LineDown();
        }

        /// <summary>
        /// Scrolls the display up with the mouse wheel, if possible.
        /// </summary>
        public void MouseWheelUp()
        {
            this.LineUp();
        }

        /// <summary>
        /// Scrolls the display one page of items down, if possible.
        /// </summary>
        public void PageDown()
        {
            this.SetVerticalOffset(this.offset.Y + (this.ItemHeight * this.CalculateRowsInView(this.viewport)), true);
        }

        /// <summary>
        /// Scrolls the display one page of items up, if possible.
        /// </summary>
        public void PageUp()
        {
            this.SetVerticalOffset(this.offset.Y - (this.ItemHeight * this.CalculateRowsInView(this.viewport)), true);
        }

        /// <summary>
        /// Makes a specific item in the panel visible; not applicable to the current panel as the out-of-bounds items
        /// are virtualized and therefore don't exist.  Nevertheless, it is unused in FacebookClient.
        /// </summary>
        /// <param name="visual">The Visual to bring into view.</param>
        /// <param name="rectangle">A bounding rectangle that identifies the coordinate space to make visible.</param>
        /// <returns>The bounding rectangle that was brought into view.</returns>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }

        #region Horizontal Scrolling Members (Not Applicable), necessary for IScrollInfo
        /// <summary>
        /// Scrolls the display one line left.  Not Applicable -- this panel wraps instead of scrolling horizontally.
        /// </summary>
        public void LineLeft()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls the display one line right.  Not Applicable -- this panel wraps instead of scrolling horizontally.
        /// </summary>
        public void LineRight()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls the display left.  Not Applicable -- this panel wraps instead of scrolling horizontally.
        /// </summary>
        public void MouseWheelLeft()
        {
            this.LineLeft();
        }

        /// <summary>
        /// Scrolls the display right.  Not Applicable -- this panel wraps instead of scrolling horizontally.
        /// </summary>
        public void MouseWheelRight()
        {
            this.LineRight();
        }

        /// <summary>
        /// Scrolls the display one page left.   Not Applicable -- this panel wraps instead of scrolling horizontally.
        /// </summary>
        public void PageLeft()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls the display one page right.  Not Applicable -- this panel wraps instead of scrolling horizontally.
        /// </summary>
        public void PageRight()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Scrolls the display horizontally.  Not Applicable -- this panel wraps instead of scrolling horizontally.
        /// </summary>
        /// <param name="offset">The amount to scroll the display.</param>
        public void SetHorizontalOffset(double offset)
        {
            throw new NotImplementedException();
        }
        #endregion
        #endregion

        #region Protected Methods
        /// <summary>
        /// Overrides this control's Measure() function to virtualize the displayed items.
        /// </summary>
        /// <param name="availableSize">The amount of space this control has for layout.</param>
        /// <returns>The amount of space this control wants for layout.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            this.UpdateScrollInfo(availableSize);

            int firstVisibleItemIndex;
            int lastVisibleItemIndex;
            this.GetVisibleRange(out firstVisibleItemIndex, out lastVisibleItemIndex);

            // WORKAROUND: Calls to ItemContainerGenerator will fail unless this.InternalChildren is accessed prior to calling.
            UIElementCollection children = this.InternalChildren;
            GeneratorPosition startPosition = this.ItemContainerGenerator.GeneratorPositionFromIndex(firstVisibleItemIndex);

            // Inserts should happen after the current child, unless we're at the beginning:
            int childIndex = (startPosition.Offset == 0) ? startPosition.Index : startPosition.Index + 1;

            using (this.ItemContainerGenerator.StartAt(startPosition, GeneratorDirection.Forward, true))
            {
                for (int i = firstVisibleItemIndex; i <= lastVisibleItemIndex; i++, childIndex++)
                {
                    bool itemNewlyRealized;

                    UIElement item = this.ItemContainerGenerator.GenerateNext(out itemNewlyRealized) as UIElement;
                    if (itemNewlyRealized)
                    {
                        if (childIndex > children.Count)
                        {
                            this.AddInternalChild(item);
                        }
                        else
                        {
                            this.InsertInternalChild(childIndex, item);
                        }

                        this.ItemContainerGenerator.PrepareItemContainer(item);
                    }

                    item.Measure(new Size(this.ItemWidth, this.ItemHeight));
                }
            }

            this.VirtualizeUneededItems();

            // Report back the size we're actually using in case the panel's Horizontal/VerticalAlignment is set to something other than "stretch"
            availableSize.Width = this.CalculateItemsInRow(availableSize) * this.ItemWidth;
            availableSize.Height = this.CalculateRowsInView(availableSize) * this.ItemHeight;
            return availableSize;
        }

        /// <summary>
        /// Overrides this control's Arrange() method to display items in a wrapped grid.
        /// </summary>
        /// <param name="finalSize">The actual amount of space the control has for layout.</param>
        /// <returns>The size the control actually used for layout.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            this.UpdateScrollInfo(finalSize);

            for (int i = 0; i < this.Children.Count; i++)
            {
                this.ArrangeChild(i, this.Children[i], finalSize);
            }

            return finalSize;
        }

        /// <summary>
        /// Remove already visualized items when the bound collection changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event arguments describing the event.</param>
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Move || args.Action == NotifyCollectionChangedAction.Replace || args.Action == NotifyCollectionChangedAction.Remove)
            {
                this.RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
            }
        }

        /// <summary>
        /// Checks the scrolling position on Up/Down to see if more items need to be scrolled into view as a result of a key press.
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.Down || e.Key == Key.Up)
                {
                    this.CheckScrollingPosition(e);
                }
            }

            base.OnPreviewKeyDown(e);
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Virtualize items that were once displayed on screen but are not any longer.
        /// </summary>
        /// <returns>Always returns null.</returns>
        private object VirtualizeUneededItems()
        {
            int firstVisibleItemIndex;
            int lastVisibleItemIndex;
            this.GetVisibleRange(out firstVisibleItemIndex, out lastVisibleItemIndex);

            for (int i = this.InternalChildren.Count - 1; i >= 0; i--)
            {
                GeneratorPosition itemPosition = new GeneratorPosition(i, 0);
                int itemIndex = this.ItemContainerGenerator.IndexFromGeneratorPosition(itemPosition);
                if (itemIndex < firstVisibleItemIndex || itemIndex > lastVisibleItemIndex)
                {
                    this.ItemContainerGenerator.Remove(itemPosition, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the range of items that are visible by using the item size and viewport offset.
        /// </summary>
        /// <param name="firstVisibleItemIndex">The first item that is visible.</param>
        /// <param name="lastVisibleItemIndex">The last item that is visible.</param>
        private void GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
        {
            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
            int itemsAvailable = itemsControl.HasItems ? itemsControl.Items.Count : 0;
            int itemsInRow = this.CalculateItemsInRow(this.viewport);
            int rowsInView = this.CalculateRowsInView(this.viewport);

            firstVisibleItemIndex = Convert.ToInt32(Math.Floor(this.offset.Y / this.ItemHeight) * itemsInRow);
            lastVisibleItemIndex = (rowsInView * itemsInRow) + firstVisibleItemIndex;
            lastVisibleItemIndex = lastVisibleItemIndex > itemsAvailable ? itemsAvailable - 1 : lastVisibleItemIndex - 1;

            // Corner case: we can jam more items into the current view if the left-over space is enough for another row
            if (itemsInRow != 0)
            {
                if (((((itemsInRow - (lastVisibleItemIndex % itemsInRow) + lastVisibleItemIndex) - firstVisibleItemIndex) / itemsInRow) < rowsInView) && (firstVisibleItemIndex != 0))
                {
                    this.LineUp();
                    this.GetVisibleRange(out firstVisibleItemIndex, out lastVisibleItemIndex);
                }
            }
        }

        /// <summary>
        /// Updates the information needed to display the scrollbar.
        /// </summary>
        /// <param name="availableSize">The size available to the ScrollViewer.</param>
        private void UpdateScrollInfo(Size availableSize)
        {
            if (double.IsPositiveInfinity(availableSize.Height) || double.IsPositiveInfinity(availableSize.Width))
            {
                throw new ArgumentException("Cannot create RowScrollingPanel; ScrollViewer must set CanChildScroll to True.");
            }

            ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
            int itemsAvailable = itemsControl.HasItems ? itemsControl.Items.Count : 0;

            this.UpdateExtent(availableSize, itemsAvailable);

            if (availableSize != this.viewport)
            {
                this.viewport = availableSize;
                if (this.owner != null)
                {
                    this.owner.InvalidateScrollInfo();
                }
            }
        }

        /// <summary>
        /// Calculates the number of rows that will fit in a given area.
        /// </summary>
        /// <param name="availableSize">The amount of space to display the items.</param>
        /// <returns>The number of rows that will fit in the given space.</returns>
        private int CalculateRowsInView(Size availableSize)
        {
            if (this.ItemHeight == 0.0)
            {
                throw new ArgumentException("Cannot create RowScrollingPanel; Both ItemHeight and ItemWidth must be set.");
            }

            return Convert.ToInt32(Math.Floor(availableSize.Height / this.ItemHeight));
        }

        /// <summary>
        /// Calculates the number of items that will fit in a row.
        /// </summary>
        /// <param name="availableSize">The amount of space to display the row.</param>
        /// <returns>The number of items that will fit in a row.</returns>
        private int CalculateItemsInRow(Size availableSize)
        {
            if (this.ItemWidth == 0.0)
            {
                throw new ArgumentException("Cannot create RowScrollingPanel; Both ItemHeight and ItemWidth must be set.");
            }

            return Convert.ToInt32(Math.Floor(availableSize.Width / this.ItemWidth));
        }

        /// <summary>
        /// Updates the size of the view extent.
        /// </summary>
        /// <param name="availableSize">The available size for layout.</param>
        /// <param name="itemCount">The number of items to layout in that space.</param>
        private void UpdateExtent(Size availableSize, int itemCount)
        {
            // We need to figure out how many rows we have to display, how many rows we can display at once, 
            // and then set the extent based on that information.
            int itemsInRow = this.CalculateItemsInRow(availableSize);
            int rows = itemsInRow > 0 ? Convert.ToInt32(Math.Ceiling(itemCount / (double)itemsInRow)) : 1;
            Size measuredExtent = new Size(availableSize.Width, rows * this.ItemHeight);

            if (measuredExtent != this.extent)
            {
                this.extent = measuredExtent;
                if (this.owner != null)
                {
                    this.owner.InvalidateScrollInfo();
                }
            }
        }

        /// <summary>
        /// Positions a specific control in the layout area.
        /// </summary>
        /// <param name="index">The child's index position.</param>
        /// <param name="child">The child UIElement.</param>
        /// <param name="size">The amount of space in the layout area.</param>
        private void ArrangeChild(int index, UIElement child, Size size)
        {
            int itemsInRow = this.CalculateItemsInRow(size);

            // Calculate the item's row/column position
            int itemRow = itemsInRow > 0 ? index / itemsInRow : 0;
            int itemColumn = itemsInRow > 0 ? index % itemsInRow : 0;

            child.Arrange(new Rect(itemColumn * this.ItemWidth, itemRow * this.ItemHeight, this.ItemWidth, this.ItemHeight));
        }

        /// <summary>
        /// Sets the vertical position of the scrollbar to a specific line of items.
        /// </summary>
        /// <param name="offset">The amount of space between the top of the panel and the beginning of the displayed items.</param>
        /// <param name="scrollToLine">A value indicating whether the offset represents a line-based measurement or must be converted
        /// to a line-based measurement before setting the offset.</param>
        private void SetVerticalOffset(double offset, bool scrollToLine)
        {
            // General bounds checking:
            if (offset < 0 || this.viewport.Height >= this.extent.Height)
            {
                offset = 0;
            }
            else if (offset + this.viewport.Height > this.extent.Height)
            {
                if (scrollToLine)
                {
                    while (offset + this.viewport.Height > this.extent.Height + this.ItemHeight)
                    {
                        offset -= this.ItemHeight;
                    }
                }
                else
                {
                    offset = this.extent.Height - this.viewport.Height;
                }
            }

            double newOffset = 0.0;

            // If we're scrolling to a line, we have a line-based measurement already.
            // Otherwise, we have to create a line-based offset.
            if (scrollToLine)
            {
                newOffset = offset;
            }
            else
            {
                while (newOffset <= offset && offset % this.ItemHeight != 0.0)
                {
                    newOffset += this.ItemHeight;
                }
            }

            this.offset.Y = newOffset;

            if (this.owner != null)
            {
                this.owner.InvalidateScrollInfo();
            }

            this.InvalidateMeasure();
        }

        /// <summary>
        /// If navigation within this control is contained, it scrolls the view up and down to devirtualize items that are outside of the current view
        /// when the top or bottom edge is reached.
        /// </summary>
        /// <param name="e">Arguments describing the event.</param>
        private void CheckScrollingPosition(KeyEventArgs e)
        {
            ContentPresenter sourceElement = e.Source as ContentPresenter;
            KeyboardNavigationMode navigationMode = (KeyboardNavigationMode)this.GetValue(KeyboardNavigation.DirectionalNavigationProperty);

            if (sourceElement != null && navigationMode == KeyboardNavigationMode.Contained)
            {
                for (int i = 0; i < this.InternalChildren.Count; i++)
                {
                    ContentControl childElement = this.InternalChildren[i] as ContentControl;

                    if (childElement != null && childElement.Content == sourceElement.Content)
                    {
                        // Now that we've located the correct child, locate where it is rendered on screen
                        // and see if we should bring more items into view.
                        int childRow = i / this.CalculateItemsInRow(this.viewport);

                        if (childRow == 0 && e.Key == Key.Up)
                        {
                            this.LineUp();
                        }
                        else if ((childRow + 1) == this.CalculateRowsInView(this.viewport) && e.Key == Key.Down)
                        {
                            this.LineDown();
                        }

                        break;
                    }
                }
            }
        }
        #endregion
    }
}
