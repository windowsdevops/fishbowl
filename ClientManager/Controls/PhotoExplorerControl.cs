//-----------------------------------------------------------------------
// <copyright file="PhotoExplorerControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Control that displays PhotoExplorerBaseNodes in a graph and allows users to navigate between them.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Diagnostics;
    using Contigo;

    /// <summary>
    /// Control for rendering search results in an 'explorer' view.
    /// </summary>
    public class PhotoExplorerControl : FrameworkElement
    {
        #region Fields
        #region Dependency Properties
        /// <summary>
        /// DependencyProperty backing store for CenterNode.
        /// </summary>
        public static readonly DependencyProperty CenterNodeProperty =
            DependencyProperty.Register("CenterNode", typeof(PhotoExplorerBaseNode), typeof(PhotoExplorerControl), GetCenterNodePropertyMetadata());

        /// <summary>
        /// DependencyProperty backing store for CoefficientOfDamping.
        /// </summary>
        public static readonly DependencyProperty CoefficientOfDampeningProperty =
            DependencyProperty.Register("CoefficientOfDampening", typeof(double), typeof(PhotoExplorerControl), new FrameworkPropertyMetadata(.9, null, new CoerceValueCallback(CoerceCoefficientOfDampeningPropertyCallback)));

        /// <summary>
        /// DependencyProperty backing store for FrameRate.
        /// </summary>
        public static readonly DependencyProperty FrameRateProperty =
            DependencyProperty.Register("FrameRate", typeof(double), typeof(PhotoExplorerControl), new FrameworkPropertyMetadata(.4, null, new CoerceValueCallback(CoerceFrameRatePropertyCallback)));

        /// <summary>
        /// DependencyProperty backing store for LinePen.
        /// </summary>
        public static readonly DependencyProperty LinePenProperty =
            DependencyProperty.Register("LinePen", typeof(Pen), typeof(PhotoExplorerControl), new PropertyMetadata(new Pen(Brushes.Gray, 1)));
        #endregion

        /// <summary>
        /// Maximum number of photos to display at any one time.
        /// </summary>
        public const short MaximumDisplayedPhotos = 20;

        /// <summary>
        /// Maximum node velocity.  Nodes reaching this velocity are capped at this value.
        /// </summary>
        private const double TerminalVelocity = 150;

        /// <summary>
        /// Minimum node velocity.  Nodes reaching this velocity are no longer updated (stopped).
        /// </summary>
        private const double MinVelocity = .1;

        /// <summary>
        /// The minimum value the CoefficientOfDampening property can be set to.
        /// </summary>
        private const double MinimumCoefficientOfDampening = .001;

        /// <summary>
        /// The maximum value the CoefficientOfDampening property can be set to.
        /// </summary>
        private const double MaximumCoefficientOfDampening = .999;

        /// <summary>
        /// Vector pointing along the Y axis.
        /// </summary>
        private static readonly Vector VerticalVector = new Vector(0, 1);

        /// <summary>
        /// Vector pointing along the X axis.
        /// </summary>
        private static readonly Vector HorizontalVector = new Vector(1, 0);

        /// <summary>
        /// The length of time it takes to fade a node from display.
        /// </summary>
        private static readonly Duration NodeHideAnimationDuration = new Duration(new TimeSpan(0, 0, 1));

        /// <summary>
        /// The length of time it takes to fade a node into display.
        /// </summary>
        private static readonly Duration ShowDuration = new Duration(new TimeSpan(0, 0, 0, 0, 500));

        /// <summary>
        /// The maximum amount of time nodes have to settle into their positions.
        /// </summary>
        private static readonly TimeSpan MaxSettleTime = new TimeSpan(0, 0, 8);

        /// <summary>
        /// Rectangle with no size.
        /// </summary>
        private static readonly Rect EmptyRect = new Rect();

        /// <summary>
        /// Random number generator.
        /// </summary>
        private static readonly Random Rnd = new Random();

        /// <summary>
        /// Collection of node presenters used to display the other nodes.
        /// </summary>
        private readonly List<NodePresenter> nodePresenters;

        /// <summary>
        /// Collection of node presenters that are fading from view.
        /// </summary>
        private readonly List<NodePresenter> fadingNodeList = new List<NodePresenter>();

        /// <summary>
        /// Event Handler for the CompositionTarget.Rendering event.
        /// </summary>
        private readonly EventHandler compositionTargetRenderingHandler;

        /// <summary>
        /// RoutedCommand to switch the center node.
        /// </summary>
        private static RoutedCommand switchCenterNodeCommand = new RoutedCommand("SwitchCenterNode", typeof(PhotoExplorerControl));

        /// <summary>
        /// The NodePresenter used to display the center node.
        /// </summary>
        private NodePresenter centerNodePresenter;

        /// <summary>
        /// Indicates whether the current control measurements have been invalidated.
        /// </summary>
        private bool measureInvalidated;

        /// <summary>
        /// Indicates whether nodes are still being shuffled around.
        /// </summary>
        private bool stillMoving;

        /// <summary>
        /// Matrix of vectors representing the forces between two nodes.
        /// </summary>
        private Vector[,] springForces;

        /// <summary>
        /// The center of the PhotoExplorerControl.
        /// </summary>
        private Point controlCenter;

        /// <summary>
        /// The last time the animating nodes were updated.
        /// </summary>
        private int ticksOfLastMeasureUpdate = int.MinValue;

        /// <summary>
        /// Indicates whether this control is connected to the CompositionTarget.Rendering event.
        /// </summary>
        private bool connectedToCompositionTargetRendering;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes static members of the PhotoExplorerControl class.
        /// </summary>
        static PhotoExplorerControl()
        {
            ClipToBoundsProperty.OverrideMetadata(typeof(PhotoExplorerControl), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the PhotoExplorerControl class.
        /// </summary>
        public PhotoExplorerControl()
        {
            this.CommandBindings.Add(new CommandBinding(switchCenterNodeCommand, new ExecutedRoutedEventHandler(OnSwitchCenterNodeCommand)));
            this.Unloaded += new RoutedEventHandler(this.OnPhotoExplorerControlUnloaded);
            this.compositionTargetRenderingHandler = new EventHandler(this.OnCompositionTargetRendering);

            this.nodePresenters = new List<NodePresenter>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the command to switch the center node.
        /// </summary>
        public static RoutedCommand SwitchCenterNodeCommand
        {
            get { return PhotoExplorerControl.switchCenterNodeCommand; }
        }

        /// <summary>
        /// Gets or sets the node at the center of the photo explorer control.
        /// </summary>
        public PhotoExplorerBaseNode CenterNode
        {
            get { return (PhotoExplorerBaseNode)GetValue(CenterNodeProperty); }
            set { SetValue(CenterNodeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the coefficient of dampening used when animating nodes.
        /// </summary>
        public double CoefficientOfDampening
        {
            get { return (double)GetValue(CoefficientOfDampeningProperty); }
            set { SetValue(CoefficientOfDampeningProperty, value); }
        }

        /// <summary>
        /// Gets or sets the frame rate used when animating nodes.
        /// </summary>
        public double FrameRate
        {
            get { return (double)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Windows.Media.Pen">Pen</see> used to connect the nodes.
        /// </summary>
        public Pen LinePen
        {
            get { return (Pen)GetValue(LinePenProperty); }
            set { SetValue(LinePenProperty, value); }
        }

        /// <summary>
        /// Gets the number of visual children in this control.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                int visualChildren = 0;

                if (this.centerNodePresenter != null)
                {
                    visualChildren++;
                }

                if (this.nodePresenters != null)
                {
                    visualChildren += this.nodePresenters.Count;
                }

                if (this.fadingNodeList != null)
                {
                    visualChildren += this.fadingNodeList.Count;
                }

                return visualChildren;
            }
        }
        #endregion

        /// <summary>
        /// Measures all of this control's elements for display.
        /// </summary>
        /// <param name="availableSize">The space available to the control for layout.</param>
        /// <returns>The control's desired amount of space.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            this.measureInvalidated = true;
            this.ConnectToCompositionTargetRendering();

            if (this.centerNodePresenter != null)
            {
                this.centerNodePresenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            foreach (NodePresenter nodePresenter in this.nodePresenters)
            {
                nodePresenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            return new Size();
        }

        /// <summary>
        /// Arranges all of this control's elements for display.
        /// </summary>
        /// <param name="finalSize">The actual space available to the control for layout.</param>
        /// <returns>The size the control actually used for layout.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            this.controlCenter.X = finalSize.Width / 2;
            this.controlCenter.Y = finalSize.Height / 2;

            if (this.centerNodePresenter != null)
            {
                this.centerNodePresenter.Arrange(EmptyRect);
            }

            foreach (NodePresenter nodePresenter in this.nodePresenters)
            {
                nodePresenter.Arrange(EmptyRect);
            }

            return finalSize;
        }

        /// <summary>
        /// When rendering the control, draw the lines to connect the nodes.
        /// </summary>
        /// <param name="drawingContext">The current rendering thread's drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.LinePen != null && this.nodePresenters != null && this.centerNodePresenter != null)
            {
                foreach (NodePresenter nodePresenter in this.nodePresenters)
                {
                    drawingContext.DrawLine(this.LinePen, this.centerNodePresenter.ActualLocation, nodePresenter.ActualLocation);
                }
            }
        }

        /// <summary>
        /// Gets the visual child at the provided index.
        /// </summary>
        /// <param name="index">The index of the desired child.</param>
        /// <returns>The desired child element.</returns>
        protected override Visual GetVisualChild(int index)
        {
            if (index < this.fadingNodeList.Count)
            {
                return this.fadingNodeList[index];
            }
            else
            {
                // Remove the fading nodes from consideration
                index -= this.fadingNodeList.Count;
            }

            if (this.nodePresenters != null)
            {
                if (index < this.nodePresenters.Count)
                {
                    return this.nodePresenters[index];
                }
                else
                {
                    // Remove the related nodes from consideration
                    index -= this.nodePresenters.Count;
                }
            }

            // It's either the center node, or doesn't exist.
            if (index == 0)
            {
                return this.centerNodePresenter;
            }
            else
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }

        /// <summary>
        /// Generates the property metadata for the center node dependency property.
        /// </summary>
        /// <returns>The PropertyMetadata object for CenterNode.</returns>
        private static PropertyMetadata GetCenterNodePropertyMetadata()
        {
            FrameworkPropertyMetadata fpm = new FrameworkPropertyMetadata();
            fpm.AffectsMeasure = true;
            fpm.PropertyChangedCallback = new PropertyChangedCallback(CenterNodePropertyChanged);
            return fpm;
        }

        /// <summary>
        /// Removes the collection changed handler from the old center node's related nodes collection when the center node changes.
        /// </summary>
        /// <param name="element">The DependencyObject that changed.</param>
        /// <param name="args">Arguments describing the change event.</param>
        private static void CenterNodePropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            ((PhotoExplorerControl)element).CenterNodePropertyChanged(args);
        }

        /// <summary>
        /// Ensures that the coefficient of dampening is always between 0 and 1, exclusive.
        /// </summary>
        /// <param name="element">The DependencyObject that was set.</param>
        /// <param name="baseValue">The value that was originally set.</param>
        /// <returns>A value in the range (0,1).</returns>
        private static object CoerceCoefficientOfDampeningPropertyCallback(DependencyObject element, object baseValue)
        {
            return ConformValueToDampeningRange((double)baseValue);
        }

        /// <summary>
        /// Ensures that the frame rate is always between 0 and 1, exclusive.
        /// </summary>
        /// <param name="element">The DependencyObject that was set.</param>
        /// <param name="baseValue">The value that was originally set.</param>
        /// <returns>A value in the range (0,1).</returns>
        private static object CoerceFrameRatePropertyCallback(DependencyObject element, object baseValue)
        {
            return ConformValueToDampeningRange((double)baseValue);
        }

        /// <summary>
        /// Ensures that the coefficient of dampening is always between 0 and 1, exclusive.
        /// </summary>
        /// <param name="baseValue">The value that was originally set.</param>
        /// <returns>A value in the range (0,1).</returns>
        private static double ConformValueToDampeningRange(double baseValue)
        {
            if (baseValue <= MinimumCoefficientOfDampening)
            {
                return MinimumCoefficientOfDampening;
            }
            else if (baseValue >= MaximumCoefficientOfDampening)
            {
                return MaximumCoefficientOfDampening;
            }
            else
            {
                return baseValue;
            }
        }

        /// <summary>
        /// Switches the current center node for the one passed in as a parameter.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the routed event.</param>
        private static void OnSwitchCenterNodeCommand(object sender, ExecutedRoutedEventArgs e)
        {
            PhotoExplorerControl photoExplorer = sender as PhotoExplorerControl;
            PhotoExplorerBaseNode nextNode = e.Parameter as PhotoExplorerBaseNode;

            var pNode = nextNode as PhotoExplorerPhotoNode;
            PhotoExplorerBaseNode overrideNode = null;
            if (pNode != null)
            {
                if (pNode.Photo != null)// && pNode.Photo.PhotoUrlBig.Contains("profile"))
                {
                    if (pNode.RelatedNodes.Count == 1)
                    {
                        overrideNode = pNode.RelatedNodes[0];
                    }
                }
            }

            if (photoExplorer != null && nextNode != null)
            {
                if (nextNode == photoExplorer.CenterNode)
                {
                    if (photoExplorer.CenterNode.Content != null)
                    {
                        ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.Execute(photoExplorer.CenterNode.Content);
                    }
                }
                else
                {
                    photoExplorer.CenterNode = overrideNode ?? nextNode;
                }
            }
        }

        /// <summary>
        /// Updates the position of the node within the control.
        /// </summary>
        /// <param name="nodePresenter">The node to update.</param>
        /// <param name="forceVector">The force on the node.</param>
        /// <param name="coefficientOfDampening">The system dampening force.</param>
        /// <param name="frameRate">The system frame rate.</param>
        /// <param name="parentCenter">The center of the parent control.</param>
        /// <returns>A value indicating whether any updates took place.</returns>
        private static bool UpdateNodePositions(NodePresenter nodePresenter, Vector forceVector, double coefficientOfDampening, double frameRate, Point parentCenter)
        {
            bool parentCenterChanged = (nodePresenter.ParentCenter != parentCenter);
            if (parentCenterChanged)
            {
                nodePresenter.ParentCenter = parentCenter;
            }

            // add system drag & force
            nodePresenter.Velocity *= 1 - (coefficientOfDampening * frameRate);
            nodePresenter.Velocity += (forceVector * frameRate);

            // apply terminalVelocity
            if (nodePresenter.Velocity.Length > TerminalVelocity)
            {
                nodePresenter.Velocity *= (TerminalVelocity / nodePresenter.Velocity.Length);
            }

            if (nodePresenter.Velocity.Length > MinVelocity && forceVector.Length > MinVelocity)
            {
                nodePresenter.Location += (nodePresenter.Velocity * frameRate);
                return true;
            }
            else
            {
                nodePresenter.Velocity = new Vector();
                return parentCenterChanged;
            }
        }

        /// <summary>
        /// Creates a square matrix of size count.
        /// </summary>
        /// <param name="count">The size of the square matrix.</param>
        /// <returns>A new square matrix of size count.</returns>
        private static Vector[,] SetupForceVectors(int count)
        {
            return new Vector[count, count];
        }

        /// <summary>
        /// Creates a new animation to hide the provided node.
        /// </summary>
        /// <param name="node">The node to hide.</param>
        /// <param name="owner">The PhotoExplorerControl containing the node.</param>
        /// <returns>A new DoubleAnimation that hides the provided node.</returns>
        private static DoubleAnimation GetNewHideAnimation(NodePresenter node, PhotoExplorerControl owner)
        {
            DoubleAnimation hideAnimation = new DoubleAnimation(0, NodeHideAnimationDuration);
            hideAnimation.FillBehavior = FillBehavior.Stop;
            HideAnimationManager hideAnimationManager = new HideAnimationManager(owner, node);
            hideAnimation.Completed += new EventHandler(hideAnimationManager.CompletedHandler);
            hideAnimation.Freeze();
            return hideAnimation;
        }

        /// <summary>
        /// Gets the sum of an array of vectors.
        /// </summary>
        /// <param name="itemIndex">The index of the vector to exlude when summing.</param>
        /// <param name="itemCount">The number of vectors in the array.</param>
        /// <param name="vectors">The vector array to sum.</param>
        /// <returns>The sum of the provided vectors.</returns>
        private static Vector GetVectorSum(int itemIndex, int itemCount, Vector[,] vectors)
        {
            Vector vector = new Vector();
            if (itemIndex >= 0 && itemIndex < itemCount)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    if (i != itemIndex)
                    {
                        vector += GetVector(itemIndex, i, vectors);
                    }
                }
            }

            return vector;
        }

        /// <summary>
        /// Gets the vector at position (a,b) if the a is less than b, otherwise, returns the inverse of the vector at (b,a).
        /// </summary>
        /// <param name="a">The first array dimension.</param>
        /// <param name="b">The second array dimension.</param>
        /// <param name="vectors">A multi-dimensional array of vectors.</param>
        /// <returns>The vector at position (a,b) if the a is less than b, otherwise, returns the inverse of the vector at (b,a).</returns>
        private static Vector GetVector(int a, int b, Vector[,] vectors)
        {
            if (a < b)
            {
                return vectors[a, b];
            }
            else
            {
                return -vectors[b, a];
            }
        }

        /// <summary>
        /// Gets the spring force for a vector.
        /// </summary>
        /// <param name="x">The vector to compute the spring for for.</param>
        /// <returns>The sum of the attraction and repulsion forces on the provided vector.</returns>
        private static Vector GetSpringForce(Vector x)
        {
            Vector force = new Vector();
            force += GetAttractionForce(x);
            force += GetRepulsiveForce(x);

            return force;
        }

        /// <summary>
        /// Gets the attraction force on the provided vector, based on its direction and length.
        /// </summary>
        /// <param name="x">The vector to compute attraction force for.</param>
        /// <returns>The attraction force on the provided vector.</returns>
        private static Vector GetAttractionForce(Vector x)
        {
            Vector force = -.2 * Normalize(x) * x.Length;
            return force;
        }

        /// <summary>
        /// Gets the repulsion force on the provided vector, based on its direction and length.
        /// </summary>
        /// <param name="x">The vector to compute repulsion force for.</param>
        /// <returns>The repulsion force on the provided vector.</returns>
        private static Vector GetRepulsiveForce(Vector x)
        {
            Vector force = .5 * Normalize(x) / Math.Pow(x.Length / 1000, 2);
            return force;
        }

        /// <summary>
        /// Normalizes the vector for use in calculations.
        /// </summary>
        /// <param name="v">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        private static Vector Normalize(Vector v)
        {
            v.Normalize();
            return v;
        }

        /// <summary>
        /// Gets the force of the wall on the photos; this force serves to keep the nodes away from the edges and inside the control.
        /// </summary>
        /// <param name="area">The size of the control.</param>
        /// <param name="location">The location to compute the edge forces for.</param>
        /// <returns>A vector representing the wall forces.</returns>
        private static Vector GetWallForce(Size area, Point location)
        {
            Vector force = new Vector();
            force += VerticalVector * GetForce(-location.Y - (area.Height / 2));
            force += -VerticalVector * GetForce(location.Y - (area.Height / 2));

            force += HorizontalVector * GetForce(-location.X - (area.Width / 2));
            force += -HorizontalVector * GetForce(location.X - (area.Width / 2));

            force *= 1000;
            return force;
        }

        /// <summary>
        /// Gets the force on a specific point as a function of an S-curve distribution.
        /// </summary>
        /// <param name="x">The point to determine the force for.</param>
        /// <returns>The force on the point.</returns>
        private static double GetForce(double x)
        {
            return GetSCurve((x + 100) / 200);
        }

        /// <summary>
        /// Gets the value for a specific point on an 's' shaped curve.
        /// </summary>
        /// <param name="x">The point on the curve.</param>
        /// <returns>The value at the provided point.</returns>
        private static double GetSCurve(double x)
        {
            return 0.5 + (Math.Sin(Math.Abs(x * (Math.PI / 2)) - Math.Abs((x * (Math.PI / 2)) - (Math.PI / 2))) / 2);
        }

        /// <summary>
        /// Ensurese that the returned vector is non-zero by generating a random vector if the provided vector has magnitude 0.
        /// </summary>
        /// <param name="vector">The vector to ensure is non-zero.</param>
        /// <returns>A vector of non-zero length.</returns>
        private static Vector EnsureNonzeroVector(Vector vector)
        {
            if (vector.Length > 0)
            {
                return vector;
            }
            else
            {
                return new Vector(Rnd.NextDouble() - .5, Rnd.NextDouble() - .5);
            }
        }

        /// <summary>
        /// Removes the collection changed handler from the old center node's related nodes collection when the center node changes.
        /// </summary>
        /// <param name="args">Arguments describing the change event.</param>
        private void CenterNodePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            PhotoExplorerBaseNode oldNode = args.OldValue as PhotoExplorerBaseNode;

            if (oldNode != null)
            {
                oldNode.RelatedNodes.CollectionChanged -= new NotifyCollectionChangedEventHandler(this.OnRelatedNodesCollectionChanged);
            }

            PhotoExplorerBaseNode newNode = args.NewValue as PhotoExplorerBaseNode;

            if (newNode != null)
            {
                newNode.RelatedNodes.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnRelatedNodesCollectionChanged);
            }

            this.UpdateDisplayedNodes();
        }

        /// <summary>
        /// Updates the collection of nodes in use when the source collection changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the collection changed event.</param>
        private void OnRelatedNodesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateDisplayedNodes();
        }

        /// <summary>
        /// Connects this control to the CompositionTarget.Rendering event so that it can animate the nodes into position using the physics engine.
        /// </summary>
        private void ConnectToCompositionTargetRendering()
        {
            if (!this.connectedToCompositionTargetRendering)
            {
                CompositionTarget.Rendering += this.compositionTargetRenderingHandler;
                this.connectedToCompositionTargetRendering = true;
            }
        }

        /// <summary>
        /// Disconnects this control from the CompositionTarget.Rendering event when the control unloads or all nodes are in their final positions.
        /// </summary>
        private void DisconnectFromCompositionTargetRendering()
        {
            if (this.connectedToCompositionTargetRendering)
            {
                CompositionTarget.Rendering -= this.compositionTargetRenderingHandler;
                this.connectedToCompositionTargetRendering = false;
            }
        }

        /// <summary>
        /// When the photo explorer control unloads, disconnects the control from the CompositionTarget.Rendering event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Arguments describing the routed event.</param>
        private void OnPhotoExplorerControlUnloaded(object sender, RoutedEventArgs e)
        {
            this.DisconnectFromCompositionTargetRendering();
        }

        /// <summary>
        /// When the framework renders a visual frame, use a physics engine to update the positions of the nodes on screen.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Arguments describing the event.</param>
        private void OnCompositionTargetRendering(object sender, EventArgs args)
        {
            if (this.nodePresenters != null && this.centerNodePresenter != null)
            {
                if (this.springForces == null)
                {
                    this.springForces = SetupForceVectors(this.nodePresenters.Count);
                }
                else if (this.springForces.GetLowerBound(0) != this.nodePresenters.Count)
                {
                    this.springForces = SetupForceVectors(this.nodePresenters.Count);
                }

                bool somethingInvalid = false;

                if (this.measureInvalidated || this.stillMoving)
                {
                    if (this.measureInvalidated)
                    {
                        this.ticksOfLastMeasureUpdate = Environment.TickCount;
                    }

                    // Update the center node
                    if (this.centerNodePresenter != null)
                    {
                        if (this.centerNodePresenter.NewNode)
                        {
                            this.centerNodePresenter.ParentCenter = this.controlCenter;
                            this.centerNodePresenter.NewNode = false;
                            somethingInvalid = true;
                        }
                        else
                        {
                            Vector forceVector = GetAttractionForce(EnsureNonzeroVector((Vector)this.centerNodePresenter.Location));

                            if (UpdateNodePositions(this.centerNodePresenter, forceVector, this.CoefficientOfDampening, this.FrameRate, this.controlCenter))
                            {
                                somethingInvalid = true;
                            }
                        }
                    }

                    // Update the other nodes
                    for (int i = 0; i < this.nodePresenters.Count; i++)
                    {
                        NodePresenter nodePresenter = this.nodePresenters[i];

                        if (nodePresenter.NewNode)
                        {
                            nodePresenter.NewNode = false;
                            somethingInvalid = true;
                        }

                        for (int j = (i + 1); j < this.nodePresenters.Count; j++)
                        {
                            Vector distance = EnsureNonzeroVector(nodePresenter.Location - this.nodePresenters[j].Location);

                            Vector repulsiveForce = GetRepulsiveForce(distance);
                            this.springForces[i, j] = repulsiveForce;
                        }
                    }

                    for (int i = 0; i < this.nodePresenters.Count; i++)
                    {
                        Vector forceVector = new Vector();
                        forceVector += GetVectorSum(i, this.nodePresenters.Count, this.springForces);
                        forceVector += GetSpringForce(EnsureNonzeroVector(this.nodePresenters[i].Location - this.centerNodePresenter.Location));
                        forceVector += GetWallForce(this.RenderSize, this.nodePresenters[i].Location);

                        if (UpdateNodePositions(this.nodePresenters[i], forceVector, this.CoefficientOfDampening, this.FrameRate, this.controlCenter))
                        {
                            somethingInvalid = true;
                        }
                    }

                    // Animate away the fading nodes
                    for (int i = 0; i < this.fadingNodeList.Count; i++)
                    {
                        if (!this.fadingNodeList[i].WasCenter)
                        {
                            Vector centerDiff = EnsureNonzeroVector(this.fadingNodeList[i].Location - this.centerNodePresenter.Location);
                            centerDiff.Normalize();
                            centerDiff *= 20;
                            if (UpdateNodePositions(this.fadingNodeList[i], centerDiff, this.CoefficientOfDampening, this.FrameRate, this.controlCenter))
                            {
                                somethingInvalid = true;
                            }
                        }
                    }

                    // If we've moved nodes this round (and we've still got time to shuffle more nodes), invalidate the visual so that we'll 
                    // get a chance to update again in the near future.
                    if (somethingInvalid && this.BelowMaxSettleTime())
                    {
                        this.stillMoving = true;
                        InvalidateVisual();
                    }
                    else
                    {
                        this.stillMoving = false;
                        this.DisconnectFromCompositionTargetRendering();
                    }

                    this.measureInvalidated = false;
                }
            }
        }

        /// <summary>
        /// Determine whether or not we've run out of time to move nodes.
        /// </summary>
        /// <returns>A value indicating whether nodes can still be shuffled around.</returns>
        private bool BelowMaxSettleTime()
        {
            return MaxSettleTime > TimeSpan.FromMilliseconds(Environment.TickCount - this.ticksOfLastMeasureUpdate);
        }

        /// <summary>
        /// Removes a node from the photo explorer control.
        /// </summary>
        /// <param name="nodePresenter">The node to remove.</param>
        /// <param name="nodeIsCenterNode">Whether or not this node used to be the center node.</param>
        private void RemoveNode(NodePresenter nodePresenter, bool nodeIsCenterNode)
        {
            this.InvalidateVisual();
            this.fadingNodeList.Add(nodePresenter);
            nodePresenter.IsHitTestVisible = false;

            if (nodeIsCenterNode)
            {
                nodePresenter.WasCenter = true;
            }

            DoubleAnimation nodeAnimation = GetNewHideAnimation(nodePresenter, this);
            nodePresenter.ScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, nodeAnimation);
            nodePresenter.ScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, nodeAnimation);
            nodePresenter.BeginAnimation(OpacityProperty, nodeAnimation);
        }

        /// <summary>
        /// Disposes of the node and its presenter when the hide animation completes.
        /// </summary>
        /// <param name="nodePresenter">The node to clean up.</param>
        private void CleanUpNode(NodePresenter nodePresenter)
        {
            this.RemoveVisualChild(nodePresenter);
            this.fadingNodeList.Remove(nodePresenter);
        }

        /// <summary>
        /// Updates the nodes being displayed when the center node or the nodes related to it change.
        /// </summary>
        private void UpdateDisplayedNodes()
        {
            // Clean up all of the old nodes; if the center node was on screen previously, we can reuse it instead.
            NodePresenter newCenterPresenter = null;
            foreach (NodePresenter nodePresenter in this.nodePresenters)
            {
                if (nodePresenter.Content == this.CenterNode)
                {
                    newCenterPresenter = nodePresenter;
                }
                else
                {
                    this.RemoveNode(nodePresenter, false);
                }
            }

            this.nodePresenters.Clear();

            if (this.centerNodePresenter != null)
            {
                this.RemoveNode(this.centerNodePresenter, true);
            }

            // If we weren't able to reuse the center node presenter, generate a new one.
            if (newCenterPresenter == null)
            {
                newCenterPresenter = new NodePresenter(this.CenterNode, true);
                this.AddVisualChild(newCenterPresenter);
            }

            this.centerNodePresenter = newCenterPresenter;

            // Add presenters for all of the new nodes.
            foreach (PhotoExplorerBaseNode node in this.CenterNode.RelatedNodes)
            {
                NodePresenter nodePresenter = new NodePresenter(node, false);
                this.AddVisualChild(nodePresenter);
                this.nodePresenters.Add(nodePresenter);
            }
        }

        #region HideAnimationManager Helper Class
        /// <summary>
        /// Manages the cleanup of a node being removed from the photo explorer control.
        /// </summary>
        private class HideAnimationManager
        {
            /// <summary>
            /// The owning PhotoExplorerControl.
            /// </summary>
            private PhotoExplorerControl owner;

            /// <summary>
            /// The key for the node being hidden.
            /// </summary>
            private NodePresenter nodePresenter;

            /// <summary>
            /// Initializes a new instance of the HideAnimationManager class.
            /// </summary>
            /// <param name="owner">The owning PhotoExplorerControl.</param>
            /// <param name="nodePresenter">The NodePresenter being hidden.</param>
            public HideAnimationManager(PhotoExplorerControl owner, NodePresenter nodePresenter)
            {
                this.owner = owner;
                this.nodePresenter = nodePresenter;
            }

            /// <summary>
            /// Cleans up the node structures once the node has been completely hidden.
            /// </summary>
            /// <param name="sender">The source of the event.</param>
            /// <param name="args">Arguments describing the event.</param>
            public void CompletedHandler(object sender, EventArgs args)
            {
                this.owner.CleanUpNode(this.nodePresenter);
            }
        }
        #endregion

        /// <summary>
        /// Class representing a node, with the transforms needed to animate it as part of the photo explorer control.
        /// </summary>
        private class NodePresenter : ContentPresenter
        {
            #region Fields
            /// <summary>
            /// Indicates whether this node was newly created.
            /// </summary>
            private bool newNode = true;

            /// <summary>
            /// Indicates whether this node was formerly the center node.
            /// </summary>
            private bool wasCenter;

            /// <summary>
            /// The node's velocity.
            /// </summary>
            private Vector velocity;

            /// <summary>
            /// The node's location on screen.
            /// </summary>
            private Point location;

            /// <summary>
            /// A vector pointing to this node's center on screen.
            /// </summary>
            private Vector centerVector;

            /// <summary>
            /// The node's parent's center on screen.
            /// </summary>
            private Point parentCenter;

            /// <summary>
            /// The size the node wants for layout.
            /// </summary>
            private Size actualDesiredSize;

            /// <summary>
            /// The size the node actually rendered in.
            /// </summary>
            private Size actualRenderSize;

            /// <summary>
            /// The transform used to position this node.
            /// </summary>
            private TranslateTransform translateTransform;

            /// <summary>
            /// The transform used to size this node.
            /// </summary>
            private ScaleTransform scaleTransform = new ScaleTransform();
            #endregion

            #region Constructor
            /// <summary>
            /// Initializes a new instance of the NodePresenter class.
            /// </summary>
            /// <param name="content">The node content.</param>
            /// <param name="nodeIsCenterNode">Whether or not this node the center node.</param>
            public NodePresenter(object content, bool nodeIsCenterNode)
                : base()
            {
                this.Content = content;

                if (!nodeIsCenterNode)
                {
                    this.translateTransform = new TranslateTransform(Rnd.NextDouble() - .5, Rnd.NextDouble() - .5);
                }
                else
                {
                    this.translateTransform = new TranslateTransform();
                }

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(this.ScaleTransform);
                transformGroup.Children.Add(this.translateTransform);

                this.RenderTransform = transformGroup;

                DoubleAnimation nodeAnimation = new DoubleAnimation(.5, 1, PhotoExplorerControl.ShowDuration);
                this.BeginAnimation(OpacityProperty, nodeAnimation);
                this.ScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, nodeAnimation);
                this.ScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, nodeAnimation);
            }
            #endregion

            #region Properties
            /// <summary>
            /// Gets or sets a value indicating whether the node is new.
            /// </summary>
            public bool NewNode
            {
                get { return this.newNode; }
                set { this.newNode = value; }
            }

            /// <summary>
            /// Gets or sets the node's velocity.
            /// </summary>
            public Vector Velocity
            {
                get { return this.velocity; }
                set { this.velocity = value; }
            }

            /// <summary>
            /// Gets or sets a value indicating whether this node was formerly in the center.
            /// </summary>
            public bool WasCenter
            {
                get { return this.wasCenter; }
                set { this.wasCenter = value; }
            }

            /// <summary>
            /// Gets the ScaleTransform used to size this node.
            /// </summary>
            public ScaleTransform ScaleTransform
            {
                get { return this.scaleTransform; }
            }

            /// <summary>
            /// Gets or sets the location of the node.
            /// </summary>
            public Point Location
            {
                get
                {
                    return this.location;
                }

                set
                {
                    if (this.location != value)
                    {
                        this.location = value;
                        this.UpdateTransform();
                    }
                }
            }

            /// <summary>
            /// Gets or sets the location of this node's parent.
            /// </summary>
            public Point ParentCenter
            {
                get
                {
                    return this.parentCenter;
                }

                set
                {
                    if (this.parentCenter != value)
                    {
                        this.parentCenter = value;
                        this.UpdateTransform();
                    }
                }
            }

            /// <summary>
            /// Gets this node's actual location.
            /// </summary>
            public Point ActualLocation
            {
                get
                {
                    return new Point(this.location.X + this.parentCenter.X, this.location.Y + this.parentCenter.Y);
                }
            }
            #endregion

            #region Methods
            /// <summary>
            /// Overrides the measure pass so that nodes have unconstrained layout.
            /// </summary>
            /// <param name="constraint">The size the framework thinks the node has for layout. Not used.</param>
            /// <returns>An empty Size.</returns>
            protected override Size MeasureOverride(Size constraint)
            {
                this.actualDesiredSize = base.MeasureOverride(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return new Size();
            }

            /// <summary>
            /// Overrides the arrange pass so that the node's size and transforms are set accurately.
            /// </summary>
            /// <param name="arrangeSize">The size the framework thinks the node has for layout. Not used.</param>
            /// <returns>An empty Size.</returns>
            protected override Size ArrangeOverride(Size arrangeSize)
            {
                this.actualRenderSize = base.ArrangeOverride(this.actualDesiredSize);

                this.ScaleTransform.CenterX = this.actualRenderSize.Width / 2;
                this.ScaleTransform.CenterY = this.actualRenderSize.Height / 2;

                this.centerVector.X = -this.actualRenderSize.Width / 2;
                this.centerVector.Y = -this.actualRenderSize.Height / 2;

                this.UpdateTransform();

                return new Size();
            }

            /// <summary>
            /// Updates this node's position.
            /// </summary>
            private void UpdateTransform()
            {
                this.translateTransform.X = this.centerVector.X + this.location.X + this.parentCenter.X;
                this.translateTransform.Y = this.centerVector.Y + this.location.Y + this.parentCenter.Y;
            }
            #endregion
        }
    }
}
