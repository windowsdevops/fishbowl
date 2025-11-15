using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Documents;
using System.Windows.Data;

namespace FacebookClient
{
    [ContentProperty("Text")]
    public class BindableRun : Run
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(BindableRun),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnTextPropertyChanged)));

        new public string Text
        {
            get { return (string)this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        public BindableRun()
        {
            Binding b = new Binding(FrameworkContentElement.DataContextProperty.Name);
            b.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FrameworkElement), 1);
            this.SetBinding(FrameworkContentElement.DataContextProperty, b);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Run run = d as Run;
            run.Text = (string)e.NewValue;
        }
    }
}
