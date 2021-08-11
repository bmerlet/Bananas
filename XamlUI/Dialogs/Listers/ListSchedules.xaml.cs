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
    /// Interaction logic for ListSchedules.xaml
    /// </summary>
    public partial class ListSchedules : Window
    {
        public ListSchedules(ListSchedulesLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            InitializeComponent();

            // Get notifications from logic
            logic.PropertyChanged += OnLogicPropertyChanged;
        }

        private void OnLogicPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DataContext is ListSchedulesLogic logic)
            {
                if (e.PropertyName == "ScheduleToScrollTo")
                {
                    listView.ScrollIntoView(logic.ScheduleToScrollTo);
                }
            }
        }

        private void OnListviewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ListSchedulesLogic logic)
            {
                logic.EditCommand.Execute();
            }
        }
    }
}
