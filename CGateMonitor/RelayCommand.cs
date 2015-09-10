using System;
using System.Windows.Input;


namespace Mercatum.CGateMonitor
{
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> _executeMethod;
        private readonly Predicate<object> _canExecuteMethod;


        public RelayCommand(Action<object> executeMethod,
                            Predicate<object> canExecuteMethod = null)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }


        public bool CanExecute(object parameter)
        {
            return _canExecuteMethod == null || _canExecuteMethod(parameter);
        }


        public void Execute(object parameter)
        {
            if( _executeMethod != null )
                _executeMethod(parameter);
        }


        public event EventHandler CanExecuteChanged;
    }
}
