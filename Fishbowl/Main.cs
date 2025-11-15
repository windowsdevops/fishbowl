namespace FacebookClient
{
    using System;
    using System.Windows;
    using System.Windows.Threading;

    public static class FishBowl
    {
        [STAThread]
        public static void Main()
        {
            if (SingleInstance.InitializeAsFirstInstance("Fishbowl"))
            {
                var splash = new SplashScreen("Resources/Images/Splash.png");
                
                // Don't show this with the fade-out.  It pops the main window and doesn't look good.
                // Fixed in .Net with the TopMost property...
                splash.Show(false);
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() => splash.Close(TimeSpan.Zero)));

                var application = new FacebookClientApplication();

                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance.Cleanup();
            }
        }
    }
}