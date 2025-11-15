//-----------------------------------------------------------------------
// <copyright file="SearchControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Interaction logic for Search control in main application chrome.
// </summary>
//-----------------------------------------------------------------------

#if ENABLE_SEARCH
namespace FacebookClient
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using ClientManager;
    using ClientManager.View;

    /// <summary>
    /// Interaction logic for Search control in main application chrome.
    /// </summary>
    [TemplatePart(Name = "PART_SearchTextBox", Type = typeof(TextBox))]
    public class SearchControl : Control
    {
        /// <summary>
        /// Search text box that should be specified in this control's template.
        /// </summary>
        private TextBox searchTextBox;

        /// <summary>
        /// Gets a value indicating whether search text box has focus
        /// </summary>
        public bool IsSearchAreaFocused
        {
            get
            {
                if (this.searchTextBox != null)
                {
                    return this.searchTextBox.IsKeyboardFocused || this.searchTextBox.IsKeyboardFocusWithin;
                }

                return false;
            }
        }

        /// <summary>
        /// OnApplyTemplate override searches for TextBox in control's template.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (this.Template != null)
            {
                this.searchTextBox = this.Template.FindName("PART_SearchTextBox", this) as TextBox;
            }
        }

        /// <summary>
        /// Moves focus to the search TextBox.
        /// </summary>
        /// <returns>
        /// True if the search text box could be successfully focused.
        /// </returns>
        public bool MoveFocusToSearch()
        {
            if (this.searchTextBox != null)
            {
                this.searchTextBox.Focus();
                this.searchTextBox.SelectAll();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Override for KeyDown event.
        /// </summary>
        /// <param name="e">EventArgs describing the event.</param>
        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                {
                    switch (e.Key)
                    {
                        case Key.Down:
                            if (this.searchTextBox != null && e.OriginalSource == this.searchTextBox)
                            {
                                SearchViewControl searchViewControl = ServiceProvider.ViewManager.CurrentVisual as SearchViewControl;
                                if (searchViewControl != null)
                                {
                                    searchViewControl.MoveFocusToSearchResults();
                                    e.Handled = true;
                                }
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            if (!e.Handled)
            {
                base.OnPreviewKeyDown(e);
            }
        }
    }
}
#endif