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
using BanaData.Logic.Dialogs.Reports;

namespace XamlUI.Dialogs.Reports
{
    /// <summary>
    /// Interaction logic for ShowCashFlowBetweenPersons.xaml
    /// </summary>
    public partial class ShowCashFlowBetweenPersons : Window
    {
        public ShowCashFlowBetweenPersons(ShowCashFlowBetweenPersonsLogic logic)
        {
            DataContext = logic;

            InitializeComponent();
        }
    }
}
