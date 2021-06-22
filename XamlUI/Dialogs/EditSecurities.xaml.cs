using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using XamlUI.Tools;

namespace XamlUI.Dialogs
{
    /// <summary>
    /// Interaction logic for EditSecurities.xaml
    /// </summary>
    public partial class EditSecurities : Window
    {
        public EditSecurities(EditSecuritiesLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            InitializeComponent();

            // Get norifications from logic
            logic.PropertyChanged += OnLogicPropertyChanged;
        }

        private void OnLogicPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (DataContext is EditSecuritiesLogic logic)
            {
                if (e.PropertyName == "SecurityToScrollTo")
                {
                    listView.ScrollIntoView(logic.SecurityToScrollTo);
                }
            }
        }

        private void OnListviewDoubleClick(object sender, EventArgs e)
        {
            if (DataContext is EditSecuritiesLogic logic)
            {
                logic.EditCommand.Execute();
            }
        }

        #region Sorting

        private SortAdorner sortAdorner;

        private void OnColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditSecuritiesLogic esl)
            {
                // Get tag name from the source
                var sd = esl.SecuritiesSource.SortDescriptions;
                var column = sender as GridViewColumnHeader;
                var memberName = column.Tag.ToString();

                // Determine sorting direction
                var direction = ListSortDirection.Ascending;
                if (sd != null && sd.Count > 0 && sd[0].PropertyName == memberName)
                {
                    direction = sd[0].Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }

                // Do the sorting
                esl.SecuritiesSource.SortDescriptions.Clear();
                esl.SecuritiesSource.SortDescriptions.Add(new SortDescription(memberName, direction));

                // Un-adorn existing adornment
                if (sortAdorner != null)
                {
                    AdornerLayer.GetAdornerLayer(sortAdorner.AdornedElement).Remove(sortAdorner);
                }
                sortAdorner = new SortAdorner(column, direction);
                AdornerLayer.GetAdornerLayer(column).Add(sortAdorner);
            }
        }

        #endregion
    }
}

