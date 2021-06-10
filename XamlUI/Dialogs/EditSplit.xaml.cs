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

using BanaData.Logic.Dialogs;

namespace XamlUI.Dialogs
{
    /// <summary>
    /// Interaction logic for EditSplit.xaml
    /// </summary>
    public partial class EditSplit : Window
    {
        public EditSplit(EditSplitLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            // Tell the view model how to close this dialog
            logic.CloseView = result => DialogResult = result;

            InitializeComponent();
        }
    }
}
