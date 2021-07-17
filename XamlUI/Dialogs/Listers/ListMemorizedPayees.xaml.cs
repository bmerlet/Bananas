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
using BanaData.Logic.Dialogs.Listers;

namespace XamlUI.Dialogs.Listers
{
    /// <summary>
    /// Interaction logic for ListMemorizedPayees.xaml
    /// </summary>
    public partial class ListMemorizedPayees : Window
    {
        public ListMemorizedPayees(ListMemorizedPayeesLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            InitializeComponent();

            // Get norifications from logic
            logic.PropertyChanged += OnLogicPropertyChanged;
        }

        private void OnLogicPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DataContext is ListMemorizedPayeesLogic logic)
            {
                if (e.PropertyName == "MemorizedPayeeToScrollTo")
                {
                    listView.ScrollIntoView(logic.MemorizedPayeeToScrollTo);
                }
            }
        }

        private void OnListviewDoubleClick(object sender, EventArgs e)
        {
            if (DataContext is ListMemorizedPayeesLogic logic)
            {
                logic.EditCommand.Execute();
            }
        }
    }
}
