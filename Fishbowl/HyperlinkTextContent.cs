namespace FacebookClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Navigation;
    using System.Xml;
    using System.Xml.Linq;
    using ClientManager;
    using Standard;

    public class HyperlinkTextContent : Span
    {
        // Don't try to be too clever here.  Match anything that looks like it might be an internet URL.
        // False positives are better UX than false negatives.
        private static readonly Regex _SimpleUrlRegex = new Regex(@"(http://|www\.[a-zA-Z0-9])[^ \t\r\n\v\f\(\)!,]+");

        // These are characters we should change before trying to parse with XML.
        // Do not check in changes to this file if these characters look funny.
        // If you see strange characters at the beginning of this file then the editor is not detecting the BOM.
        private static readonly Dictionary<string, string> _EarlyDiacriticLookup = new Dictionary<string, string>
        {
            { "&euro;",   "€" },
            { "&nbsp",    new string((char)160, 1) },
            { "&iexcl;",  "¡" },
            { "&iquest;", "¿" },
            { "&Aacute;", "Á" },
            { "&Eacute;", "É" },
            { "&Iacute;", "Í" },
            { "&Ntilde;", "Ñ" },
            { "&Oacute;", "Ó" },
            { "&Uacute;", "Ú" },
            { "&Uuml;",   "Ü" },
            { "&aacute;", "á" },
            { "&eacute;", "é" },
            { "&iacute;", "í" },
            { "&ntilde;", "ñ" },
            { "&oacute;", "ó" },
            { "&uacute;", "ú" },
            { "&uuml;",   "ü" },
        };

        // These are characters we should change later.  We don't want these strings removed from within URIs.
        private static readonly Dictionary<string, string> _LateDiacriticLookup = new Dictionary<string, string>
        {
            { "&quot;",    "\"" },
            { "&amp;",     "&" },
        };

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(HyperlinkTextContent),
            new FrameworkPropertyMetadata(
                (d, e) => ((HyperlinkTextContent)d)._GenerateInlines()));

        public string Text
        {
            get { return (string)this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty DefaultUriProperty = DependencyProperty.Register(
            "DefaultUri", 
            typeof(Uri), 
            typeof(HyperlinkTextContent),
            new PropertyMetadata(null));

        public Uri DefaultUri
        {
            get { return (Uri)GetValue(DefaultUriProperty); }
            set { SetValue(DefaultUriProperty, value); }
        }

        public HyperlinkTextContent()
        {
            Binding b = new Binding(FrameworkContentElement.DataContextProperty.Name);
            b.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FrameworkElement), 1);
            this.SetBinding(FrameworkContentElement.DataContextProperty, b);
        }

        private static XDocument _SafeParseXDocument(string text)
        {
            if (text.Contains('&'))
            {
                foreach (var diacriticPair in _EarlyDiacriticLookup)
                {
                     text = text.Replace(diacriticPair.Key, diacriticPair.Value);
                }
            }
            return XDocument.Parse("<div>" + text + "</div>", LoadOptions.PreserveWhitespace);
        }

        private void _GenerateInlines()
        {
            Inlines.Clear();
            DefaultUri = null;

            if (!string.IsNullOrEmpty(this.Text))
            {
                string text = this.Text;
                bool formatted = false;
                if (text.StartsWith("<div", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        IEnumerable<Inline> inlines = _BuildInlinesFromHtml(text);
                        foreach (var inline in inlines)
                        {
                            this.Inlines.Add(inline);
                        }
                        formatted = true;
                    }
                    catch
                    {
                        // If we're unable to parse the DIV tags then just display the HTML with the tags stripped out.
                        // This will only work if the HTML is mostly correctly formatted, e.g. start and end tags match up.
                        // TODO: Anecdotally, it seems like the most common reason for this is a '&' character in the content.
                        //    We should be able to handle that...
                        bool include = true;
                        text = text.Split('<', '>').Aggregate(new StringBuilder(), (sb, token) => { sb.Append(include ? token : ""); include = !include; return sb; }).ToString();
                    }
                }

                if (!formatted)
                {
                    DefaultUri = null;
                    this.Inlines.AddRange(_BuildInlinesFromRawText(text, _OnRequestNavigate));
                }
            }
        }

        private IEnumerable<Inline> _BuildInlinesFromHtml(string text)
        {
            Assert.IsNeitherNullNorEmpty(text);
            // Enclose these in a redundant <div> tag pair because we're seeing multiple
            // top level elements in the text.
            XDocument xdoc = _SafeParseXDocument(text);
            bool hasData = false;
            foreach (var node in xdoc.Root.Nodes())
            {
                IEnumerable<Inline> inlines = _BuildInlinesFromFragment(node);
                foreach (var inline in inlines)
                {
                    if (inline is LineBreak && !hasData)
                    {
                        continue;
                    }
                    hasData = true;
                    yield return inline;
                }
            }
        }

        private IEnumerable<Inline> _BuildInlinesFromFragment(XNode fragment)
        {
            if (fragment.NodeType == XmlNodeType.Text)
            {
                string text = fragment.ToString();
                if (string.IsNullOrEmpty(text))
                {
                    text = " ";
                }

                if (text.Contains('&'))
                {
                    foreach (var diacriticPair in _LateDiacriticLookup)
                    {
                        text = text.Replace(diacriticPair.Key, diacriticPair.Value);
                    }
                }

                yield return new Run(text);
                yield break;
            }

            if (fragment.NodeType == XmlNodeType.Element)
            {
                XElement element = (XElement)fragment;
                Span span = null;
                switch (element.Name.LocalName.ToUpper())
                {
                    case "I":
                    case "EM":
                        span = new Italic();
                        break;
                    case "B":
                    case "STRONG":
                        span = new Bold();
                        break;
                    case "U":
                        span = new Underline();
                        break;
                    case "DIV":
                        yield return new LineBreak();

                        // Facebook makes some textual distinctions based on div class names.
                        XAttribute classAttr = element.Attribute("class");
                        if (classAttr != null)
                        {
                            switch (classAttr.Value)
                            {
                                case "UIStoryAttachment_Title":
                                    span = new Bold();
                                    break;
                                default:
                                    span = new Span();
                                    break;
                            }
                        }
                        else
                        {
                            span = new Span();
                        }
                        break;
                    case "A":
                        // hyperlinks are special because Facebook apps tend to be sloppy and nest them.
                        // Just detect the simple version of this and block it.
                        if (element.Attribute("href") != null
                            && !(from subnode in element.Nodes()
                                 let subelt = subnode as XElement
                                 where subelt != null
                                 where subelt.Name.LocalName.ToUpper() == "A"
                                 select subnode).Any())
                        {
                            var hyperlink = new Hyperlink();
                            Uri navigateUri;
                            // Attribute(string) is case sensitive... 
                            if (Uri.TryCreate(element.Attribute("href").Value, UriKind.RelativeOrAbsolute, out navigateUri))
                            {
                                hyperlink.NavigateUri = navigateUri;
                                hyperlink.RequestNavigate += _OnRequestNavigate;

                                if (DefaultUri == null)
                                {
                                    DefaultUri = navigateUri;
                                }
                            }

                            span = hyperlink;
                        }
                        else
                        {
                            // Treat this like a normal span.
                            span = new Span();
                        }
                        break;
                    default:
                        // Treat anything unknown as a linebreak?
                        yield return new LineBreak();
                        yield break;
                }

                bool hasContent = false;
                foreach (var subnode in element.Nodes())
                {
                    hasContent = true;
                    span.Inlines.AddRange(_BuildInlinesFromFragment(subnode));
                }
                if (hasContent)
                {
                    yield return span;
                }
                else
                {
                    yield return new Run(" ");
                }
            }
            yield break;
        }

        private static IEnumerable<Inline> _BuildInlinesFromRawText(string text, RequestNavigateEventHandler navigateCallback)
        {
            Assert.IsNotNull(navigateCallback);
            MatchCollection matches = _SimpleUrlRegex.Matches(text);

            int index = 0;
            foreach (Match match in matches)
            {
                if (match.Index > index)
                {
                    yield return new Run(text.Substring(index, match.Index - index));
                }

                string url = text.Substring(match.Index, match.Length);
                Hyperlink hyperlink = null;
                try
                {
                    hyperlink = new Hyperlink(new Run(url));
                    string protocoledUrl = url;
                    if (url.StartsWith("www", StringComparison.OrdinalIgnoreCase))
                    {
                        protocoledUrl = "http://" + url;
                    }
                    hyperlink.NavigateUri = new Uri(protocoledUrl);
                    hyperlink.RequestNavigate += navigateCallback;
                }
                catch (UriFormatException)
                {
                }

                if (hyperlink != null)
                {
                    yield return hyperlink;
                }
                else
                {
                    yield return new Run(url);
                }

                index = match.Index + match.Length;
            }

            if (index < text.Length)
            {
                yield return new Run(text.Substring(index, text.Length - index));
            }
        }

        private void _OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var handler = RequestNavigate;
            if (handler != null)
            {
                handler(sender, e);
                return;
            }

            // If there's no handler then send it as a navigation command.
            // Not at a point in the product where it makes sense to remove the default and update all callers to do this.
            Hyperlink hyperlink = sender as Hyperlink;
            ServiceProvider.ViewManager.NavigationCommands.NavigateToContentCommand.Execute(hyperlink.NavigateUri);
            e.Handled = true;
        }

        public event RequestNavigateEventHandler RequestNavigate;
    }
}
