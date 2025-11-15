
namespace FacebookClient
{
    using System;
    using System.Deployment.Application;
    using System.Windows;
    using System.Windows.Threading;
    using Standard;

    internal class ApplicationUpdateFailedEventArgs : EventArgs
    {
        public bool WasUpdateDetected { get; set; }
        public Exception Exception { get; set; }
    }

    internal class DeploymentManager
    {
        private static TimeSpan _firstUpdateInterval = new TimeSpan(0, 0, 30);
        private static TimeSpan _steadyUpdateInterval = new TimeSpan(12, 0, 0);
        private static bool _isUpdating = false;
        private static bool _isFirstCheck = true;
        private static DispatcherTimer _updateTimer;

        public static event EventHandler ApplicationUpdated;
        public static event EventHandler<ApplicationUpdateFailedEventArgs> ApplicationUpdateFailed;

        public static void StartMonitor()
        {
// Only bother with the click-once update logic for retail binaries.
// ApplicationDeployment properties raise handled exceptions, which makes
//     debugging onerous.
#if !DEBUG
            // Only do this once.
            Assert.IsNull(_updateTimer);

            if (FacebookClientApplication.AreUpdatesEnabled && ApplicationDeployment.IsNetworkDeployed)
            {
                _updateTimer = new DispatcherTimer(_firstUpdateInterval, DispatcherPriority.ApplicationIdle, _TimerTick, Application.Current.Dispatcher);
                _updateTimer.Start();
            }
#endif
        }

        // NEVER crash the app because of a bad click-once deployment.
        // Kill the update timer if we see problems.  Raise an appropriate event.
        private static void _TimerTick(object sender, EventArgs e)
        {
            if (_isUpdating)
            {
                return;
            }

            _isUpdating = true;

            if (_isFirstCheck)
            {
                _updateTimer.Stop();
                _updateTimer = null;
                _isFirstCheck = false;
            }

            bool isUpdateAvailable = false;
            try
            {
                UpdateCheckInfo info = ApplicationDeployment.CurrentDeployment.CheckForDetailedUpdate();
                if (info != null)
                {
                    isUpdateAvailable = info.UpdateAvailable;
                }

                if (isUpdateAvailable)
                {
                    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                    ad.UpdateCompleted += (sender2, e2) => _NotifyUpdateSuccess();

                    ad.UpdateAsync();
                }
                else
                {
                    _isUpdating = false;
                }
            }
            catch (Exception ex)
            {
                _isUpdating = false;
                _NotifyUpdateFailure(new ApplicationUpdateFailedEventArgs { Exception = ex, WasUpdateDetected = isUpdateAvailable });
                return;
            }

            if (_updateTimer == null)
            {
                _updateTimer = new DispatcherTimer(_steadyUpdateInterval, DispatcherPriority.ApplicationIdle, _TimerTick, Application.Current.Dispatcher);
                _updateTimer.Start();
            }
        }

        private static void _NotifyUpdateFailure(ApplicationUpdateFailedEventArgs args)
        {
            // If we failed an update don't try again.  
            // Wait until the user restarts the application.
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer = null;
            }

            _isUpdating = false;

            var handler = ApplicationUpdateFailed;
            if (handler != null)
            {
                Application.Current.Dispatcher.BeginInvoke(handler, Application.Current, args);
            }
        }

        private static void _NotifyUpdateSuccess()
        {
            //update completed at this point. allow future updates to take place.  
            _isUpdating = false;

            var handler = ApplicationUpdated;
            if (handler != null)
            {
                Application.Current.Dispatcher.BeginInvoke(handler, Application.Current, EventArgs.Empty);
            }
        }
    }
}
