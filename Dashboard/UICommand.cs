using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dashboard
{
    /// <summary>
    /// Basic implementation of xaml ICommand
    /// </summary>
    public class UICommand : ICommand
    {
        #region Private memebers

        private bool canExecute;
        private Action action;

        #endregion

        #region Actions from logic

        public UICommand(Action action, bool canExecute = true)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public void SetCanExecute(bool value)
        {
            if (canExecute != value)
            {
                canExecute = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region ICommand implementation

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return canExecute;
        }

        public void Execute(object parameter)
        {
            action();
        }

        #endregion
    }
}
