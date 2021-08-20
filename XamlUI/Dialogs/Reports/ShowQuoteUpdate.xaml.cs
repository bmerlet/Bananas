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
    /// Interaction logic for ShowQuoteUpdate.xaml
    /// </summary>
    public partial class ShowQuoteUpdate : Window
    {
        public ShowQuoteUpdate(ShowQuoteUpdateLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            InitializeComponent();
        }
    }
}
