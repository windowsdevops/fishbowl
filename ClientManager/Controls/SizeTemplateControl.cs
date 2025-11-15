//-----------------------------------------------------------------------
// <copyright file="SizeTemplateControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Selects a ControlTemplate for use by window/panel size.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;
    using Standard;

    /// <summary>
    /// Control that allows a collection of ControlTemplates to be associated with it, to be applied based on size, and selects an
    /// appropriate template based on its layout size.
    /// </summary>
    public class SizeTemplateControl : Control
    {
        #region Fields
        /// <summary>
        /// DependencyProperty for <see cref="Templates" /> property.
        /// </summary>
        public static readonly DependencyProperty TemplatesProperty =
                DependencyProperty.Register(
                        "Templates",
                        typeof(SizeControlTemplateCollection),
                        typeof(SizeTemplateControl),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Default height to be used for constraint size if size passed at measure is infinity.
        /// </summary>
        private const double DefaultConstraintHeight = 600;

        /// <summary>
        /// Default width  to be used for constraint size if size passed at measure is infinity.
        /// </summary>
        private const double DefaultConstraintWidth = 480;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the collection of SizeControlTemplate objects that provide ControlTemplate selectable for a given Control’s size.
        /// </summary>
        public SizeControlTemplateCollection Templates
        {
            get { return (SizeControlTemplateCollection)GetValue(TemplatesProperty); }
            set { SetValue(TemplatesProperty, value); }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Update current control's tempate based on the size and control’s properties.
        /// </summary>
        /// <param name="size">The size of the current template.</param>
        protected virtual void UpdateCurrentTemplate(Size size)
        {
            ControlTemplate newTemplate = this.SelectTemplate(this.Templates, size);
            if (newTemplate != this.Template)
            {
                this.Template = newTemplate;
            }
        }

        /// <summary>
        /// Select appropriate ControlTemplate for a given size.
        /// </summary>
        /// <param name="templates">The collection of SizeControlTemplate objects.</param>
        /// <param name="size">The size used to determine the current control's template.</param>
        /// <returns>Selected ControlTemplate.</returns>
        protected virtual ControlTemplate SelectTemplate(SizeControlTemplateCollection templates, Size size)
        {
            ControlTemplate template = null;
            if (templates != null && templates.Count > 0)
            {
                for (int i = 0; i < templates.Count; i++)
                {
                    if (templates[i].IsSelectable(size))
                    {
                        template = templates[i].Template;
                        break;
                    }
                }
            }

            return template;
        }

        /// <summary>
        /// Override for logic that determines size required by this object.
        /// </summary>
        /// <param name="availableSize">Available size.</param>
        /// <returns>Desired size for this object.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // If measure constraint is infinity, snap it to default size
            Size newConstraint = this.GetValidConstraint(availableSize);

            Size desiredSize = base.MeasureOverride(newConstraint);
            return desiredSize;
        }

        /// <summary>
        /// Infinity is not a valid size for SizeTemplateControl.
        /// </summary>
        /// <param name="constraint">The possibly invalid size.</param>
        /// <returns>A Size that is less than infinity.</returns>
        protected virtual Size GetValidConstraint(Size constraint)
        {
            Size size = Size.Empty;
            if (constraint != Size.Empty)
            {
                double height = Double.IsInfinity(constraint.Height) ? DefaultConstraintHeight : constraint.Height;
                double width = Double.IsInfinity(constraint.Width) ? DefaultConstraintWidth : constraint.Width;
                size = new Size(width, height);
            }

            return size;
        }

        /// <summary>
        /// Positions child elements and determines a size for this object. 
        /// <see cref="FrameworkElement.ArrangeOverride"/>
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            this.UpdateCurrentTemplate(finalSize);
            return base.ArrangeOverride(finalSize);
        }

        #endregion
    }

    /// <summary>
    /// A class that allows to specify a ControlTemplate which is dependent of the Control’s size.
    /// Basically Min/Max Width/Height values specify an area for which the ControlTemplate is applicable.
    /// </summary>
    public class SizeControlTemplate
    {
        #region Fields
        /// <summary>
        /// The ControlTemplate applicable for area defined by Min/Max Width/Height values.
        /// </summary>
        private ControlTemplate controlTemplate;

        /// <summary>
        /// The minimum width for which ControlTemplate is selectable.
        /// </summary>
        private double minWidth;

        /// <summary>
        /// The maximum width for which ControlTemplate is selectable.
        /// </summary>
        private double maxWidth;

        /// <summary>
        /// The minimum height for which ControlTemplate is selectable.
        /// </summary>
        private double minHeight;

        /// <summary>
        /// The maximum height for which ControlTemplate is selectable.
        /// </summary>
        private double maxHeight;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the SizeControlTemplate class.
        /// Initially sets area to maximum available range.
        /// </summary>
        public SizeControlTemplate()
        {
            this.maxWidth = Double.PositiveInfinity;
            this.maxHeight = Double.PositiveInfinity;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the ControlTemplate applicable for area defined by Min/Max Width/Height values.
        /// </summary>
        public ControlTemplate Template
        {
            get { return this.controlTemplate; }
            set { this.controlTemplate = value; }
        }

        /// <summary>
        /// Gets or sets the minimum width for which ControlTemplate is selectable.
        /// </summary>
        public double MinWidth
        {
            get { return this.minWidth; }
            set { this.minWidth = value; }
        }

        /// <summary>
        /// Gets or sets the maximum width for which ControlTemplate is selectable.
        /// </summary>
        public double MaxWidth
        {
            get { return this.maxWidth; }
            set { this.maxWidth = value; }
        }

        /// <summary>
        /// Gets or sets the minimum height for which ControlTemplate is selectable.
        /// </summary>
        public double MinHeight
        {
            get { return this.minHeight; }
            set { this.minHeight = value; }
        }

        /// <summary>
        /// Gets or sets the maximum height for which ControlTemplate is selectable.
        /// </summary>
        public double MaxHeight
        {
            get { return this.maxHeight; }
            set { this.maxHeight = value; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Return whether the ControlTemplate can be selected for the specified size.
        /// </summary>
        /// <param name="size">The size the SizeControlTemplate is tested against.</param>
        /// <returns>Whether the ControlTemplate can be selected for the specified size.</returns>
        public bool IsSelectable(Size size)
        {
            return DoubleUtilities.LessThanOrClose(this.minWidth, size.Width) &&
                   DoubleUtilities.GreaterThanOrClose(this.maxWidth, size.Width) &&
                   DoubleUtilities.LessThanOrClose(this.minHeight, size.Height) &&
                   DoubleUtilities.GreaterThanOrClose(this.maxHeight, size.Height);
        }
        #endregion
    }

    /// <summary>
    /// Collection of SizeControlTemplate objects.
    /// </summary>
    public class SizeControlTemplateCollection : Collection<SizeControlTemplate>
    {
    }
}