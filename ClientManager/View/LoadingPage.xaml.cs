namespace ClientManager.View
{
    using System.Windows;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for LoadingPage.xaml
    /// </summary>
    public partial class LoadingPage
    {
        private class _Navigator : Navigator
        {
            public _Navigator(LoadingPage page, Dispatcher dispatcher)
                : base(page, "[Loading Page]", null)
            { }

            public override bool IncludeInJournal { get { return false; } }
        }

        private bool _signaled = false;

        public LoadingPage(Navigator next)
        {
            InitializeComponent();

            NextNavigator = next;
        }

        public Navigator NextNavigator { get; set; }

        public Navigator GetNavigator()
        {
            return new _Navigator(this, this.Dispatcher);
        }

        public void Signal()
        {
            if (!_signaled)
            {
                _signaled = true;

                if (this.IsLoaded && ServiceProvider.ViewManager != null)
                {
                    ServiceProvider.ViewManager.NavigateByCommand(NextNavigator);
                }
            }
        }

        private void ContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_signaled)
            {
                ServiceProvider.ViewManager.NavigateByCommand(NextNavigator);
                return;
            }

            this.StartLoading();
        }

        private void ContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.StopLoading();
        }

        private void StartLoading()
        {
            this.Spinner.IsRunning = true;
        }

        private void StopLoading()
        {
            this.Spinner.IsRunning = false;
        }
    }
}
