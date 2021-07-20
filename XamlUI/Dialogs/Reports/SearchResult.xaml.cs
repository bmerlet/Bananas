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
using XamlUI.Tools;

namespace XamlUI.Dialogs.Reports
{
    /// <summary>
    /// Interaction logic for SearchResult.xaml
    /// </summary>
    public partial class SearchResult : Window
    {
        public SearchResult(SearchResultLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            InitializeComponent();
        }

        private void OnListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SearchResultLogic logic && sender is ListView listView && listView.SelectedItem is SearchResultLogic.FoundItem item)
            {
                DialogResult = true;
                logic.GoTo(item);
            }
        }

        private void OnPrintClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is SearchResultLogic logic)
            {
                DoPrint(logic);
            }
        }

        private void DoPrint(SearchResultLogic logic)
        {
            // Create the print helper
            var ph = new PrintHelper
            {
                Title = $"Search for: '{logic.SearchText}'"
            };

            // Create the columns
            ph.AddColumn("Account", 145);
            ph.AddColumn("Date",  66);
            ph.AddColumn("Payee", 145);
            ph.AddColumn("Memo", 145);
            ph.AddColumn("Category", 145);
            ph.AddColumn("Amount", 60);

            // Create the data
            foreach (SearchResultLogic.FoundItem item in logic.FoundItemsSource)
            {
                ph.AddRow(new PrintHelper.Cell[] {
                    new PrintHelper.Cell(item.Account),
                    new PrintHelper.Cell(item.Date.ToString("MM/dd/yyyy")),
                    new PrintHelper.Cell(item.Payee),
                    new PrintHelper.Cell(item.Memo),
                    new PrintHelper.Cell(item.Category),
                    new PrintHelper.Cell(item.Amount.ToString("N2"), TextAlignment.Right)
                });
            }

            // Print
            ph.Print();
        }
    }
}
