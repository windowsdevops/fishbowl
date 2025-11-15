    //-----------------------------------------------------------------------
// <copyright file="FilmStripPanel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     The panel used to display items in the FilmStripControl;
//     it animates the items it displays into the center position.
// </summary>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using ClientManager.Controls;
    using Standard;

    /// <summary>
    /// The panel used to display items in the FilmStripControl.
    /// </summary>
    /// <remarks>
    /// This derives from Panel and not VirtualizingPanel because MakeVisible() is not called on items that are virtualized, 
    /// which prevents us from centering the panel on those items.
    /// </remarks>
    public class FilmStripPanel : Panel, IScrollInfo
    {
        #region Fields
        /// <summary>
        /// DependencyProperty backing store for ItemHeight.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(FilmStripPanel), new UIPropertyMetadata(0.0));

        /// <summary>
        /// The duration of a "standard" slide; that is, the amount of time the animation should take, regardless of the number of items moved.
        /// </summary>
        /// <remarks>To synchronized the background and the items, this should match the values set in FilmStripControl.</remarks>
        private static Duration standardSlideFilmStripDuration = new Duration(new TimeSpan(1700000));

        /// <summary>
        /// The time that should be added to the standard duration *per each item moved*; this prevents us warping from one side of the film 
        /// to the other if we move a large number of items.
        /// </summary>
        /// <remarks>To synchronized the background and the items, this should match the values set in FilmStripControl.</remarks>
        private static int perItemSlideFilmStripTime = 300000;

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
        private Size _extent = new Size(0, 0);

        /// <summary>
        /// The size of the region currently in view.
        /// </summary>
        private Size _viewport = new Size(0, 0);

        /// <summary>
        /// The _viewport's absolute _offset from the origin of its parent.
        /// </summary>
        private Point _offset;

        /// <summary>
        /// The _transform used to animate the panel back and forth.
        /// </summary>
        private TranslateTransform _transform = new TranslateTransform();

        /// <summary>
        /// The animation used to animate position changes.
        /// </summary>
        private DoubleAnimation _transformAnimation = new DoubleAnimation();

        private double[] _itemOffsets, _itemWidths;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the FilmStripPanel class.
        /// </summary>
        public FilmStripPanel()
        {
            this.RenderTransform = this._transform;
            this.Background = Brushes.Transparent;
        } 
        #endregion

        #region Properties
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
            set 
            { 
                this.owner = value;
                if (this.owner != null)
                {
                    this.owner.PreviewMouseWheel += new System.Windows.Input.MouseWheelEventHandler(owner_PreviewMouseWheel);
                }
            }
        }

        private void owner_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            PhotoViewerControl.HandleScrollViewerMouseWheel((ScrollViewer)sender, false, e);
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
            get { return this._extent.Height; }
        }

        /// <summary>
        /// Gets the width of the entire RowScrollingPanel.
        /// </summary>
        public double ExtentWidth
        {
            get { return this._extent.Width; }
        }

        /// <summary>
        /// Gets the height of the region currently displayed.
        /// </summary>
        public double ViewportHeight
        {
            get { return this._viewport.Height; }
        }

        /// <summary>
        /// Gets the width of the region currently displayed.
        /// </summary>
        public double ViewportWidth
        {
            get { return this._viewport.Width; }
        }

        /// <summary>
        /// Gets the _viewport's horizontal _offset from the left of the panel.
        /// </summary>
        public double HorizontalOffset
        {
            get { return this._offset.X; }
        }

        /// <summary>
        /// Gets the _viewport's vertical _offset from the top of the panel.
        /// </summary>
        public double VerticalOffset
        {
            get { return this._offset.Y; }
        } 
        #endregion

        #region Public Methods
        /// <summary>
        /// Make a specific item of the panel visible.  In this case, the FilmStripControl calls BringIntoView()
        /// on the selected item, which then ends up here.  
        /// </summary>
        /// <param name="visual">The visual that needs to be made visible.</param>
        /// <param name="rectangle">The amount of the visual that should be made visible (not used -- the whole item is centered).</param>
        /// <returns>The area made visible (not used -- the whole item is made visible, so it is the same as the rectangle parameter).</returns>
        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                if ((Visual)this.InternalChildren[i] == visual)
                {
                    Assert.IsNotNull(_itemOffsets);
                    this.SetHorizontalOffset(_itemOffsets[i]);
                    break;
                }
            }

            return rectangle;
        }

        /// <summary>
        /// Animates the panel from its current position to the provided horizontal _offset.
        /// We then set the horizontal _offset so that the desiredOffsetX is in the center of the panel's
        /// ViewPortWidth.
        /// </summary>
        /// <param name="offset">The desired horizontal _offset of the current item.</param>
        public void SetHorizontalOffset(double offset)
        {
           double centerRelativeOffset;
            int numberOfItemsDelta;

            // animate horizontally so that current item is positioned at the center of the _viewport
            //centerRelativeOffset = (this.ViewportWidth - this.ItemWidth) / 2;
            centerRelativeOffset = this.ViewportWidth / 2;
            this._transformAnimation.From = centerRelativeOffset - this._offset.X;
            this._transformAnimation.To = centerRelativeOffset - offset;

            // this duration must match FilmStripControl.BringCurrentItemIntoView's Effect animation duration
            numberOfItemsDelta = Math.Min(GetChildIndexFromOffset(Math.Abs((double)this._transformAnimation.From - (double)this._transformAnimation.To)),
                FilmStripControl.MaxNumberOfItemsDelta);
            this._transformAnimation.Duration = standardSlideFilmStripDuration + new Duration(new TimeSpan(numberOfItemsDelta*perItemSlideFilmStripTime));

            this._transformAnimation.AccelerationRatio = 0.4;
            this._transformAnimation.DecelerationRatio = 0.2;
            this._transform.BeginAnimation(TranslateTransform.XProperty, this._transformAnimation, HandoffBehavior.SnapshotAndReplace);

            // save the current horizontal position
            this._offset.X = offset;

            if (this.owner != null)
            {
                this.owner.InvalidateScrollInfo();
            }

            // Invalidate the measure so that new items come into view if required
            this.InvalidateMeasure();
        }

        public void SetHorizontalOffsetDecelerate(double velocity)
        {
            double centerRelativeOffset;
            double offset;

            if ( _offset.X < 0 )
            {
                offset = 0;
            }
            else if (_offset.X > ExtentWidth )
            {
                offset = ExtentWidth;
            }
            else
                offset = _offset.X - velocity*200;

            offset = Math.Max(Math.Min(offset, ExtentWidth), 0.0);

            // animate horizontally so that current item is positioned at the center of the _viewport
            //centerRelativeOffset = (this.ViewportWidth - this.ItemWidth) / 2;
            centerRelativeOffset = this.ViewportWidth / 2;
            this._transformAnimation.From = centerRelativeOffset - this._offset.X;
            this._transformAnimation.To = centerRelativeOffset - offset;

            // this duration must match FilmStripControl.BringCurrentItemIntoView's Effect animation duration
            this._transformAnimation.Duration = standardSlideFilmStripDuration + new Duration(new TimeSpan(200));

            this._transformAnimation.AccelerationRatio = 0.0;
            this._transformAnimation.DecelerationRatio = 1.0;
            this._transform.BeginAnimation(TranslateTransform.XProperty, this._transformAnimation, HandoffBehavior.SnapshotAndReplace);

            // save the current horizontal position
            this._offset.X = offset;

            if (this.owner != null)
            {
                this.owner.InvalidateScrollInfo();
            }

            // Invalidate the measure so that new items come into view if required
            this.InvalidateMeasure();
        } 

        #region Unused IScrollInfo Members

        /// <summary>
        /// Shifts the panel one line down.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void LineDown()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Shifts the panel one line left.
        /// </summary>
        public void LineLeft()
        {
            int index = Math.Max(0, GetChildIndexFromOffset(this._offset.X) - 1);
            double offset = _itemOffsets[index];
            this.SetHorizontalOffset(offset);
        }

        /// <summary>
        /// Shifts the panel one line right.
        /// </summary>
        public void LineRight()
        {
            int index = Math.Min(this.InternalChildren.Count - 1, GetChildIndexFromOffset(this._offset.X) + 1);
            double offset = _itemOffsets[index];
            this.SetHorizontalOffset(offset);
        }

        /// <summary>
        /// Shifts the panel one line up.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void LineUp()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Shifts the panel on mouse wheel input.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void MouseWheelDown()
        {
           // Ignore mouse wheel commands, don't throw an error.
        }

        /// <summary>
        /// Shifts the panel on mouse wheel input.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void MouseWheelLeft()
        {
            // Ignore mouse wheel commands, don't throw an error.
        }

        /// <summary>
        /// Shifts the panel on mouse wheel input.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void MouseWheelRight()
        {
            // Ignore mouse wheel commands, don't throw an error.
        }

        /// <summary>
        /// Shifts the panel on mouse wheel input.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void MouseWheelUp()
        {
            // Ignore mouse wheel commands, don't throw an error.
        }

        /// <summary>
        /// Shifts the panel one page down.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void PageDown()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Shifts the panel one page down.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void PageLeft()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Shifts the panel one page right.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void PageRight()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Shifts the panel one page up.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        public void PageUp()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the panel's vertical _offset.  Not implemented -- this panel only shifts horizontally, and only through calls to MakeVisible().
        /// </summary>
        /// <param name="offset">The desired vertical _offset.</param>
        public void SetVerticalOffset(double offset)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion

        #region Protected Methods

        /// <summary>
        /// Overrides measure so that the scrolling position is updated.
        /// </summary>
        /// <param name="availableSize">The amount of space the panel has for layout.</param>
        /// <returns>The amount of space the panel wants for layout.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            _itemOffsets = new double[this.InternalChildren.Count];
            _itemWidths = new double[this.InternalChildren.Count];

            Size itemSize = new Size(double.PositiveInfinity, this.ItemHeight);
            double previousWidth = 0.0;

            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                this.InternalChildren[i].Measure(itemSize);

                if (double.IsPositiveInfinity(this.InternalChildren[i].DesiredSize.Width))
                {
                    throw new ArgumentException("Children of FilmStripPanel must request finite width.");
                }

                _itemOffsets[i] = (i > 0) ? _itemOffsets[i - 1] + previousWidth : 0.0;
                previousWidth = this.InternalChildren[i].DesiredSize.Width;
                _itemWidths[i] = previousWidth;
            }

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

            double verticalOffset = 0;
            if (this.ItemHeight < this.ActualHeight)
            {
                verticalOffset = (this.ActualHeight - this.ItemHeight) / 2;
            }

            for (int i = 0; i < this.InternalChildren.Count; i++)
            {
                Rect finalRect = new Rect(_itemOffsets[i], verticalOffset, this.InternalChildren[i].DesiredSize.Width, this.ItemHeight);
                this.InternalChildren[i].Arrange(finalRect);
            }

            return finalSize;
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Gets the index of an item by horizontal offset.
        /// </summary>
        private int GetChildIndexFromOffset(double offset)
        {
            Assert.IsNotNull(_itemOffsets);

            for (int i = 0; i < _itemOffsets.Length - 1; i++)
            {
                if (_itemOffsets[i + 1] > offset)
                {
                    return i;
                }
            }

            return _itemOffsets.Length - 1;
        }

        /// <summary>
        /// Updates the scrolling position for the panel.
        /// </summary>
        /// <param name="availableSize">The amount of space the panel has for layout.</param>
        private void UpdateScrollInfo(Size availableSize)
        {
            if (double.IsPositiveInfinity(availableSize.Height) || double.IsPositiveInfinity(availableSize.Width))
            {
                throw new ArgumentException("Cannot create FilmStripPanel; ScrollViewer must set CanChildScroll to True and/or restrict the size of the film strip.");
            }

            this.UpdateExtent(availableSize, this.InternalChildren.Count);

            if (availableSize != this._viewport)
            {
                this._viewport = availableSize;
                if (this.owner != null)
                {
                    this.owner.InvalidateScrollInfo();
                }
            }
        }

        /// <summary>
        /// Updates the total size ('_extent') of the panel, without scrolling.
        /// </summary>
        /// <param name="availableSize">The amount of space the panel has for layout.</param>
        /// <param name="itemsAvailable">The number of items it needs to display.</param>
        private void UpdateExtent(Size availableSize, int itemsAvailable)
        {
            Assert.IsNotNull(_itemOffsets);
            Size measuredExtent = new Size(_itemOffsets.Length > 0 ? _itemOffsets[itemsAvailable - 1] + _itemWidths[itemsAvailable - 1] : 0, availableSize.Height);

            if (measuredExtent != this._extent)
            {
                this._extent = measuredExtent;
                if (this.owner != null)
                {
                    this.owner.InvalidateScrollInfo();
                }
            }
        }

        #endregion

        private bool _testForDrag;
        private bool _inDrag;
        private Point _initialDragPosition;
        private UIElement _initialElement;
        private Point _initialMouseDown;
        private Point _lastMousePosition;
        private List<KeyValuePair<int, double>> _mousePositions;
        private bool _inPromotion;

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if ( _inPromotion)
            {
                base.OnMouseDown(e);
            }
            else
            {
                _mousePositions = new List<KeyValuePair<int, double>>();
                _initialMouseDown = e.GetPosition((IInputElement)Parent);
                _initialDragPosition = e.GetPosition(Application.Current.MainWindow.Content as UIElement);
                _lastMousePosition = _initialMouseDown;
                _mousePositions.Add(new KeyValuePair<int, double>(e.Timestamp, _lastMousePosition.X));
                _testForDrag = true;
                _inDrag = false;
                _initialElement = e.MouseDevice.DirectlyOver as UIElement;
                CaptureMouse();
                e.Handled = true;         
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (_inPromotion)
            {
                base.OnPreviewMouseMove(e);
            }

            if (_mousePositions != null )
            {
                var currentPosition = e.GetPosition((IInputElement)Parent);
                double dx = currentPosition.X - _lastMousePosition.X;
                _lastMousePosition = currentPosition;

                if (_testForDrag )
                {
                    int idx = GetChildIndexFromOffset(_initialMouseDown.X);
                    if (Math.Abs(currentPosition.X - _initialMouseDown.X) > _itemWidths[idx] / 2)
                    {
                        _testForDrag = false;
                    }
                    else if (Math.Abs(currentPosition.Y - _initialMouseDown.Y) > ItemHeight / 2)
                    {
                        var pos = e.GetPosition(Application.Current.MainWindow.Content as UIElement);

                        _inDrag = true;
                        _testForDrag = false;
                        _mousePositions = null;
                        _inDrag = false;
                        ReleaseMouseCapture();

                        _initialElement.RaiseEvent(new DragContainerEventArgs(DragContainer.DragStartedEvent, _initialDragPosition, _initialDragPosition));
                        _initialElement.RaiseEvent(new DragContainerEventArgs(DragContainer.DragDeltaEvent, _initialDragPosition, pos));

                        _initialElement = null;
                        return;
                    }
                }

                if (Math.Abs(dx) > 0.9)
                {
                    SetHorizontalOffset(HorizontalOffset - dx);
                }

                var lastPos = _mousePositions[_mousePositions.Count - 1];
                if (lastPos.Key != e.Timestamp && lastPos.Value != _lastMousePosition.X)
                {
                    _mousePositions.Add(new KeyValuePair<int, double>(e.Timestamp, _lastMousePosition.X));
                    while (_mousePositions.Count > 5)
                    {
                        _mousePositions.RemoveAt(0);
                    }
                }

                e.Handled = true;
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            if (_inPromotion)
            {
                base.OnPreviewMouseUp(e);
                _inPromotion = false;
            }
            else
            {
                ReleaseMouseCapture();

                if (_inDrag)
                {
                    Point pos = e.GetPosition(Application.Current.MainWindow.Content as UIElement);
                    _initialElement.RaiseEvent(
                        new DragContainerEventArgs(DragContainer.DragCompletedEvent, _initialDragPosition, pos));
                }
                else
                {
                    var pos = e.GetPosition(this);

                    if (_mousePositions == null || _mousePositions.Count < 2)
                    {
                        var item = GetChildIndexFromOffset(pos.X);

                        if (item >= 0 && item < InternalChildren.Count)
                        {
                            var listbox = FindElement<ListBox>(this);

                            if (listbox != null)
                            {
                                Button button = FindElement<Button>((UIElement) e.MouseDevice.DirectlyOver);
                                //listbox.SelectedIndex = item;

                                if (button != null)
                                {
                                    ButtonAutomationPeer peer =
                                        new ButtonAutomationPeer(button);

                                    IInvokeProvider invokeProv =
                                        peer.GetPattern(PatternInterface.Invoke)
                                        as IInvokeProvider;

                                    invokeProv.Invoke();
                                }
                            }
                        }
                    }
                    else if (_mousePositions != null && _mousePositions.Count >= 2)
                    {
                        var first = _mousePositions[_mousePositions.Count - 2];
                        var last = _mousePositions[_mousePositions.Count - 1];

                        double velocity = (double) (last.Value - first.Value)/(double) (last.Key - first.Key);
                        SetHorizontalOffsetDecelerate(velocity);
                    }
                }

                e.Handled = true;
                _mousePositions = null;
                _initialElement = null;
                _inDrag = false;
            }
        }

        private static T FindElement<T>(DependencyObject element) where T : UIElement
        {
            while (element != null && !(element is T))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return element as T;
        }
    }
}
