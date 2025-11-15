namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Collections;
    using System.Windows.Media;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;

    public class SearchContentControl : Control
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(IList), typeof(SearchContentControl));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(SearchContentControl));
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(SearchContentControl));
        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register("ItemWidth", typeof(double), typeof(SearchContentControl));
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register("ItemHeight", typeof(double), typeof(SearchContentControl));
        public static readonly DependencyProperty ListHeightProperty = DependencyProperty.Register("ListHeight", typeof(double), typeof(SearchContentControl));

        public static readonly RoutedCommand ScrollUpCommand = new RoutedCommand("ScrollUp", typeof(RowScrollingPanel));
        public static readonly RoutedCommand ScrollDownCommand = new RoutedCommand("ScrollDown", typeof(RowScrollingPanel));

        public IList Items
        {
            get { return (IList)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        public double ListHeight
        {
            get { return (double)GetValue(ListHeightProperty); }
            set { SetValue(ListHeightProperty, value); }
        }

        ListBox _listBox;
        RowScrollingPanel _panel;

        public SearchContentControl()
        {
            this.CommandBindings.Add(new CommandBinding(ScrollUpCommand, new ExecutedRoutedEventHandler(OnScrollUpCommand),
                new CanExecuteRoutedEventHandler(OnScrollUpCommandCanExecute)));
            this.CommandBindings.Add(new CommandBinding(ScrollDownCommand, new ExecutedRoutedEventHandler(OnScrollDownCommand),
                new CanExecuteRoutedEventHandler(OnScrollDownCommandCanExecute)));

            this.Loaded += new RoutedEventHandler(OnLoaded);

        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _listBox = this.Template.FindName("ListBox", this) as ListBox;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Render a transparent rectangle to make sure hit-testing works properly when our background is transparent.
            // (Needed for IsMouseOver triggers.)
            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), null, new Rect(0, 0, this.RenderSize.Width, this.RenderSize.Height));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _panel = FindType(_listBox, typeof(RowScrollingPanel)) as RowScrollingPanel;
        }

        private void OnScrollUpCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (_panel != null)
            {
                _panel.LineUp();
            }
        }

        private void OnScrollDownCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (_panel != null)
            {
                _panel.LineDown();
            }
        }

        private void OnScrollUpCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _panel != null && _panel.CanScrollUp;
        }

        private void OnScrollDownCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _panel != null && _panel.CanScrollDown;
        }

        private DependencyObject FindType(DependencyObject element, Type type)
        {
            int count = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(element, i);
                if (child.GetType() == type)
                {
                    return child;
                }

                DependencyObject result = FindType(child, type);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
