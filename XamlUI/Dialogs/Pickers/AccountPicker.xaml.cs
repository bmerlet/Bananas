using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BanaData.Logic.Dialogs.Pickers;

namespace XamlUI.Dialogs.Pickers
{
    /// <summary>
    /// Interaction logic for AccountPicker.xaml
    /// </summary>
    public partial class AccountPicker : Window
    {
        public AccountPicker(AccountPickerLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            // Tell the view model how to close this dialog
            logic.CloseView = result => DialogResult = result;

            InitializeComponent();
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is AccountPickerLogic logic)
            {
                logic.CommitCommand.Execute();
            }
        }
    }
}
