namespace FacebookClient
{
    using System;
    using System.Collections;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using Contigo;
    using Standard;

    public class FriendInfoDescriptionViewer : FlowDocumentPageViewer
    {
        #region Block Builders

        private delegate Block SingleBlockBuilder(string title, object value);
        private delegate TableRow SingleRowBuilder(string title, object value);

        private Block _BlockBuildSingleString(string title, object objValue)
        {
            Assert.IsNeitherNullNorEmpty(title);
            Assert.Implies(objValue != null, () => objValue is string);

            var value = objValue as string;

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            value = value.Trim();

            Paragraph header = _CreateHeaderParagraph(title + ":  ");
            header.Inlines.Add(_CreateDetailsSpan(value));
            return header;
        }

        private TableRow _RowBuildSingleString(string title, object objValue)
        {
            Assert.IsNeitherNullNorEmpty(title);
            Assert.Implies(objValue != null, () => objValue is string);

            var value = objValue as string;

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            value = value.Trim();
            
            var titleCell = new TableCell(_CreateHeaderParagraph(title + ":")) { TextAlignment = TextAlignment.Right };
            var contentCell = new TableCell(new Paragraph(_CreateDetailsSpan(value))) { TextAlignment = TextAlignment.Left };

            var retRow = new TableRow();
            retRow.Cells.Add(titleCell);
            retRow.Cells.Add(contentCell);

            return retRow;
        }

        private TableRow _RowBuildSingleLocation(string title, object objValue)
        {
            Assert.IsNeitherNullNorEmpty(title);
            Assert.Implies(objValue != null, () => objValue is Location);

            var value = objValue as Location;

            if (value == null || value.IsEmpty)
            {
                return null;
            }

            var titleCell = new TableCell(_CreateHeaderParagraph(title + ":")) { TextAlignment = TextAlignment.Right };

            bool includeComma = !string.IsNullOrEmpty(value.State) && !string.IsNullOrEmpty(value.City);
            var contentParagraph = new Paragraph();
            if (!string.IsNullOrEmpty(value.State) || !string.IsNullOrEmpty(value.City))
            {
                contentParagraph.Inlines.Add(_CreateDetailsSpan(string.Format("{0}{1}{2} ",
                    value.City,
                    includeComma ? ", " : "",
                    value.State)));
                if (value.ZipCode != null)
                {
                    contentParagraph.Inlines.Add(new LineBreak());
                    contentParagraph.Inlines.Add(_CreateDetailsSpan(value.ZipCode.Value.ToString()));
                }
                if (!string.IsNullOrEmpty(value.Country))
                {
                    contentParagraph.Inlines.Add(new LineBreak());
                    contentParagraph.Inlines.Add(_CreateDetailsSpan(value.Country));
                }
            }
            else if (!string.IsNullOrEmpty(value.Country))
            {
                contentParagraph.Inlines.Add(_CreateDetailsSpan(value.Country));
            }

            var contentCell = new TableCell(contentParagraph) { TextAlignment = TextAlignment.Left };

            var retRow = new TableRow();
            retRow.Cells.Add(titleCell);
            retRow.Cells.Add(contentCell);

            return retRow;
        }


        private Block _BlockBuildSingleUri(string title, object objValue)
        {
            Assert.IsNeitherNullNorEmpty(title);
            Assert.Implies(objValue != null, () => objValue is Uri);

            var value = objValue as Uri;

            if (value == null)
            {
                return null;
            }
            
            Paragraph header = _CreateHeaderParagraph(title + ":  ");
            header.Inlines.Add(new Hyperlink(new Run(value.OriginalString)));
            return header;
        }

        private Block _BlockBuildSingleLocation(string title, object objValue)
        {
            Assert.IsNeitherNullNorEmpty(title);
            Assert.Implies(objValue != null, () => objValue is Location);

            var value = objValue as Location;

            if (value == null || value.IsEmpty)
            {
                return null;
            }

            Paragraph header = _CreateHeaderParagraph(title + ":  ");

            bool includeComma = !string.IsNullOrEmpty(value.State) && !string.IsNullOrEmpty(value.City);
            header.Inlines.Add(_CreateDetailsSpan(string.Format("{0}{1}{2} ",
                value.City,
                includeComma ? ", " : "",
                value.State)));
            if (!string.IsNullOrEmpty(value.Country))
            {
                header.Inlines.Add(_CreateDetailsSpan(value.Country));
            }

            return header;
        }

        private struct BlockMap
        {
            public BlockMap(string title, object value, SingleBlockBuilder func)
            {
                Title = title;
                Value = value;
                Func = func;
            }
            public readonly string Title;
            public readonly object Value;
            public readonly SingleBlockBuilder Func;
        }

        private struct RowMap
        {
            public RowMap(string title, object value, SingleRowBuilder func)
            {
                Title = title;
                Value = value;
                Func = func;
            }
            public readonly string Title;
            public readonly object Value;
            public readonly SingleRowBuilder Func;
        }

        #endregion

        private Style _headerStyle;
        private Style _detailsStyle;

        static FriendInfoDescriptionViewer()
        {
            ZoomProperty.OverrideMetadata(typeof(FriendInfoDescriptionViewer), new FrameworkPropertyMetadata(_OnZoomChanged));
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Zoom = Properties.Settings.Default.FriendInfoDescriptionZoom;
        }

        // Ensure that mouse wheel over this control doesn't get propagated to the outer application.
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            e.Handled = true;
        }

        private static void _OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FriendInfoDescriptionViewer)d)._OnZoomChanged();
        }

        private void _OnZoomChanged()
        {
            Properties.Settings.Default.FriendInfoDescriptionZoom = this.Zoom;
        }

        public static readonly DependencyProperty FacebookContactProperty = DependencyProperty.Register(
            "FacebookContact",
            typeof(FacebookContact), 
            typeof(FriendInfoDescriptionViewer),
            new FrameworkPropertyMetadata(
                null,
                (d, e) => ((FriendInfoDescriptionViewer)d)._OnFacebookContactChanged()));

        public FacebookContact FacebookContact
        {
            get { return (FacebookContact)GetValue(FacebookContactProperty); }
            set { SetValue(FacebookContactProperty, value); }
        }

        private void _OnFacebookContactChanged()
        {
            _headerStyle = (Style)Application.Current.Resources["BlockTextBlockStyle"];
            _detailsStyle = (Style)Application.Current.Resources["BlockDetailTextBlockStyle"];

            this.FontStretch = FontStretches.Condensed;
            var doc = new FlowDocument
            {
                ColumnGap = 25,
                ColumnRuleBrush = Brushes.LightBlue,
                ColumnRuleWidth = 2,
                ColumnWidth = 500,
                IsHyphenationEnabled = true,
            };

            if (FacebookContact != null)
            {
                doc.Blocks.AddRange(_GetContactDisplayInfo(FacebookContact));
            }

            this.Document = doc;
        }

        private Paragraph _CreateHeaderParagraph(string text)
        {
            Assert.IsNeitherNullNorEmpty(text);
            return new Paragraph(new Run(text)) { Style = _headerStyle };
        }

        private HyperlinkTextContent _CreateDetailsSpan(string text)
        {
            Assert.IsNeitherNullNorEmpty(text);
            return new HyperlinkTextContent { Text = text, Style = _detailsStyle };
        }

        private IEnumerable _GetContactDisplayInfo(FacebookContact contact)
        {
            RowMap[] _rowTable = new[]
            {
                //new BlockMap("Facebook Page",       contact.ProfileUri,         _BlockBuildSingleUri),
                new RowMap("Birthday",            contact.Birthday,           _RowBuildSingleString),
                new RowMap("Current Location",    contact.CurrentLocation,    _RowBuildSingleLocation),
                new RowMap("Hometown",            contact.Hometown,           _RowBuildSingleLocation),
                // Most people already know the sex of their friends.  No reason to display it.
                //new RowMap("Sex",                 contact.Sex,                _RowBuildSingleString),
                new RowMap("Relationship Status", contact.RelationshipStatus, _RowBuildSingleString),
                new RowMap("Religion",            contact.Religion,           _RowBuildSingleString),

                new RowMap("Website",             contact.Website,            _RowBuildSingleString),
                new RowMap("Activities",          contact.Activities,         _RowBuildSingleString),
                new RowMap("Interests",           contact.Interests,          _RowBuildSingleString),
                new RowMap("Favorite Music",      contact.Music,              _RowBuildSingleString),
                new RowMap("Favorite TV Shows",   contact.TV,                 _RowBuildSingleString),
                new RowMap("Favorite Movies",     contact.Movies,             _RowBuildSingleString),
                new RowMap("Favorite Books",      contact.Books,              _RowBuildSingleString),
                new RowMap("Favorite Quotes",     contact.Quotes,             _RowBuildSingleString),
                new RowMap("About Me",            contact.AboutMe,            _RowBuildSingleString),
                // Need to add Education, Highschool, Work History.
            };

            var table = new Table { CellSpacing = 5, Padding = new Thickness(15) };
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn { Width = new GridLength(0, GridUnitType.Auto) });
            
            TableRowGroup rowGroup = new TableRowGroup();

            foreach (var rowMap in _rowTable)
            {
                TableRow retRow = rowMap.Func(rowMap.Title, rowMap.Value);
                if (retRow != null)
                {
                    rowGroup.Rows.Add(retRow);
                }
            }

            table.RowGroups.Add(rowGroup);

            yield return table;
        }
    }
}
