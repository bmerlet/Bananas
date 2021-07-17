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
using BanaData.Logic.Dialogs.Editors;

namespace XamlUI.Dialogs.Editors
{
    /// <summary>
    /// Interaction logic for ReconcileInfo.xaml
    /// </summary>
    public partial class ReconcileInfo : Window
    {
        public ReconcileInfo(ReconcileInfoLogic logic)
        {
            // Use the view model as data context
            DataContext = logic;

            // Tell the view model how to close this dialog
            logic.CloseView = result => DialogResult = result;

            InitializeComponent();
        }
    }
}
