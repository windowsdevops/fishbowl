
namespace ClientManager.View
{
    using System;
    using System.Windows.Input;
    using System.Windows.Threading;
    using System.Windows;

    /// <summary>
    /// Abstract base class for all commands that exist on ViewManager. These commands are initialized with ViewManager in the ctor.
    /// </summary>
    public abstract class ViewCommand : ICommand
    {
        public virtual event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// CanExecute logic.
        /// </summary>
        /// <param name="parameter">Command parameter.</param>
        /// <returns>True if command can execute.</returns>
        public bool CanExecute(object parameter)
        {
            return this.CanExecuteInternal(parameter);
        }

        /// <summary>
        /// Command execution logic.
        /// </summary>
        /// <param name="parameter">Command parameter.</param>
        public void Execute(object parameter)
        {
            this.ExecuteInternal(parameter);
        }

        /// <summary>Initializes a new instance of the ViewCommand class.</summary>
        /// <param name="viewManager">ViewManager associated with this command.</param>
        protected ViewCommand(ViewManager viewManager)
        {
            ViewManager = viewManager;
        }

        /// <summary>
        /// Gets the ViewManager associated with this command. ViewCommands must have access to a ViewManager since they may call APIs or
        /// require state information from it.
        /// </summary>
        protected ViewManager ViewManager { get; private set; }

        /// <summary>
        /// Execution logic for ViewCommand that can be overridden by derived classes.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for this command.
        /// </param>
        protected virtual void ExecuteInternal(object parameter) { }

        /// <summary>
        /// CanExecute logic for ViewCommand that can be overridden by derived classes.
        /// </summary>
        /// <param name="parameter">
        /// Execution parameter for this command.
        /// </param>
        /// <returns>Always returns true.</returns>
        protected virtual bool CanExecuteInternal(object parameter)
        {
            return true;
        }
    }
}
