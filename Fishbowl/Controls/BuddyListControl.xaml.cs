namespace FacebookClient
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Navigation;

    public partial class BuddyListControl : UserControl
    {
        public static readonly DependencyProperty IsDisplayedProperty = DependencyProperty.Register(
            "IsDisplayed",
            typeof(bool),
            typeof(BuddyListControl));

        public bool IsDisplayed
        {
            get { return (bool)GetValue(IsDisplayedProperty); }
            set { SetValue(IsDisplayedProperty, value); }
        }

        public static readonly RoutedCommand CloseCommand = new RoutedCommand("Close", typeof(BuddyListControl));

        public BuddyListControl()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(CloseCommand, new ExecutedRoutedEventHandler((sender, e) => IsDisplayed = false)));
        }

        private void _OnMessageRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var handler = RequestNavigate;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        public event RequestNavigateEventHandler RequestNavigate;

        private void StartChatButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ApplicationCommands.ShowChatWindowCommand.Execute(Application.Current.MainWindow);
        }
    }
}
