namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using Contigo;
    using System.Text.RegularExpressions;
    using System.Windows.Documents;
    using System.Windows.Navigation;
    using System.Diagnostics;
    using System.Text;
    using System.ComponentModel;
    using System.Windows.Media;
    using System.Windows.Data;
    using System.Collections.Generic;
    using System.Linq;
    using Standard;

    /// <remarks>
    /// This control assumes it has a SearchViewControl as an ancestor.
    /// </remarks>
    public class SearchTextBlock : TextBlock // todo: merge this class (or setup some sort of inheritance structure) with HyperlinkTextContent if possible.
    {
        private bool _isInContentChange;

        public static readonly DependencyProperty InputTextProperty = DependencyProperty.Register("InputText", typeof(string), typeof(SearchTextBlock),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnTextChanged)));
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText", typeof(string), typeof(SearchTextBlock),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnTextChanged)));

        public string InputText
        {
            get { return (string)GetValue(InputTextProperty); }
            set { SetValue(InputTextProperty, value); }
        }

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public SearchTextBlock()
        {
            Binding binding = new Binding("DataContext.SearchText");
            binding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(SearchViewControl), 1);
            SetBinding(SearchTextProperty, binding);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SearchTextBlock tb = d as SearchTextBlock;
            tb.HandleTextChanged();
        }

        private void HandleTextChanged()
        {
            if (!_isInContentChange)
            {
                _isInContentChange = true;
                Inlines.Clear();
                GenerateInlines();
                _isInContentChange = false;
            }
        }

        private void GenerateInlines()
        {
            string[] searchWords = SearchIndex.StemWords(SearchIndex.GetWords(this.SearchText));
            if (searchWords.Length == 0)
            {
                return;
            }

            string[] inputWords = SearchIndex.GetWords(this.InputText);
            if (inputWords.Length == 0)
            {
                return;
            }

            List<string> highlightWords = new List<string>();
            Stemmer stemmer = new Stemmer();

            foreach (string word in inputWords)
            {
                if (Enumerable.Contains<string>(searchWords, stemmer.Stem(word)))
                {
                    highlightWords.Add(word);
                }
            }

            string text = this.InputText;
            Regex regex = GetRegexFromWordList(highlightWords.ToArray());
            int index = 0;

            if (regex != null)
            {
                MatchCollection matches = regex.Matches(text);

                foreach (Match match in matches)
                {
                    if (match.Index > index)
                    {
                        this.Inlines.Add(new Run(text.Substring(index, match.Index - index)));
                    }

                    string searchWord = text.Substring(match.Index, match.Length);
                    this.Inlines.Add(new Bold(new Run(searchWord)));

                    index = match.Index + match.Length;
                }
            }

            if (index < text.Length)
            {
                this.Inlines.Add(new Run(text.Substring(index, text.Length - index)));
            }

            Assert.IsTrue(this.Inlines.Count != 0);
        }

        private Regex GetRegexFromWordList(string[] words)
        {
            if (words.Length == 0)
            {
                return null;
            }

            StringBuilder regexString = new StringBuilder();
            foreach (string word in words)
            {
                if (regexString.Length != 0)
                {
                    regexString.Append("|");
                }

                regexString.Append(@"\b");
                regexString.Append(word);
                regexString.Append(@"\b");
            }

            return new Regex(regexString.ToString(), RegexOptions.IgnoreCase);
        }
    }
}
