
namespace FacebookClient
{
    using System.Reflection;
    using System.Windows;
    using ClientManager.View;
    using Contigo;
    using Standard;

    internal class MainWindowCommands
    {
        public MainWindowCommands(ViewManager viewManager)
        {
            Assert.IsNotNull(viewManager);
            foreach (PropertyInfo publicInstanceProperty in typeof(MainWindowCommands).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                ConstructorInfo cons = publicInstanceProperty.PropertyType.GetConstructor(new[] { typeof(ViewManager) });
                Assert.IsNotNull(cons);
                object command = cons.Invoke(new object[] { viewManager });
                Assert.AreEqual(command.GetType().Name, publicInstanceProperty.Name);
                publicInstanceProperty.SetValue(this, command, null);
            }
        } 

        public SwitchThemeCommand SwitchThemeCommand { get; private set; }
        public InitiateRestartCommand InitiateRestartCommand { get; private set; }
        public SwitchToMiniModeCommand SwitchToMiniModeCommand { get; private set; }
        public ShowChatWindowCommand ShowChatWindowCommand { get; private set; }
        public ShowInboxCommand ShowInboxCommand { get; private set; }
    }

    /// <summary>
    /// Switches the application's theme.
    /// </summary>
    internal sealed class SwitchThemeCommand : ViewCommand
    {
        public SwitchThemeCommand(ViewManager viewManager)
            : base(viewManager) 
        {}

        protected override bool CanExecuteInternal(object parameter)
        {
            return parameter == null || parameter is string;
        }

        /// <summary>
        /// Execution logic for ViewCommand that can be overridden by derived classes.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for this command.
        /// </param>
        protected override void ExecuteInternal(object parameter)
        {
            (Application.Current as FacebookClientApplication).SwitchTheme(parameter as string);
        }
    }

    internal sealed class InitiateRestartCommand : ViewCommand
    {
        public InitiateRestartCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override void ExecuteInternal(object parameter)
        {
            // Turn off the single instance-ness of the app before initiating the restart.
            SingleInstance.Cleanup();

            FacebookLoginService.ClearCachedCredentials(ViewManager.FacebookAppId);
            System.Windows.Forms.Application.Restart();
            System.Windows.Application.Current.Shutdown();
        }
    }

    internal sealed class ShowInboxCommand : ViewCommand
    {
        public ShowInboxCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            return parameter is MainWindow;
        }

        protected override void ExecuteInternal(object parameter)
        {
            var w = parameter as MainWindow;
            if (w == null || !w.IsVisible)
            {
                return;
            }

            w.ShowInbox();
        }
    }

    internal sealed class SwitchToMiniModeCommand : ViewCommand
    {
        public SwitchToMiniModeCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override bool CanExecuteInternal(object parameter)
        {
            var w = parameter as Window;
            if (w == null)
            {
                return false;
            }

            return w.IsVisible;
        }

        protected override void ExecuteInternal(object parameter)
        {
            var w = parameter as Window;
            if (w == null || !w.IsVisible)
            {
                return;
            }

            ((FacebookClientApplication)Application.Current).SwitchToMiniMode();
        }
    }

    internal sealed class ShowChatWindowCommand : ViewCommand
    {
        public ShowChatWindowCommand(ViewManager viewManager)
            : base(viewManager)
        { }

        protected override void ExecuteInternal(object parameter)
        {
            ((FacebookClientApplication)Application.Current).ShowChatWindow();
        }
    }
}
