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
    /// Interaction logic for ShowYearlyCapGainsAndDividends.xaml
    /// </summary>
    public partial class ShowYearlyCapGainsAndDividends : Window
    {
        public ShowYearlyCapGainsAndDividends(ShowYearlyCapGainsAndDividendsLogic logic)
        {
            DataContext = logic;

            InitializeComponent();
        }

        #region Sorting

        private SortAdorner sortAdorner;

        protected void OnColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ShowYearlyCapGainsAndDividendsLogic brl)
            {
                // Get tag name from the source
                var sd = brl.Transactions.SortDescriptions;
                var column = sender as GridViewColumnHeader;
                var memberName = column.Tag.ToString();

                // Determine sorting direction
                var direction = ListSortDirection.Ascending;
                if (sd != null && sd.Count > 0 && sd[0].PropertyName == memberName)
                {
                    direction = sd[0].Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }

                // Do the sorting
                brl.Transactions.SortDescriptions.Clear();
                brl.Transactions.SortDescriptions.Add(new SortDescription(memberName, direction));

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
