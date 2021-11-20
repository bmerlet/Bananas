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
using BanaData.Logic.Dialogs.Reports.Accounting;
using XamlUI.Tools;

namespace XamlUI.Dialogs.Reports.Accounting
{
    /// <summary>
    /// Interaction logic for BalanceSheet.xaml
    /// </summary>
    public partial class BalanceSheet : Window
    {
        public BalanceSheet(BalanceSheetLogic logic)
        {
            DataContext = logic;

            InitializeComponent();
        }

        private void OnPrint(object sender, RoutedEventArgs e)
        {
            var logic = DataContext as BalanceSheetLogic;

            // Create the print helper
            var ph = new PrintHelper
            {
                Title = $"Balance sheet on '{logic.Date:MM/dd/yyyy}'"
            };

            // Create the columns
            ph.AddColumn("Asset name", 240);
            ph.AddColumn("Amount", 80);
            ph.AddColumn("Liability name", 240);
            ph.AddColumn("Amount", 80);

            // Create the data
            var rows = new List<List<PrintHelper.Cell>>();

            // Assets
            foreach (BalanceSheetLogic.BalanceSheetItem item in logic.AssetsSource)
            {
                var row = new List<PrintHelper.Cell>();
                rows.Add(row);

                var cellName = new PrintHelper.Cell(item.Name)
                {
                    FontWeight = item.Bold ? FontWeights.Bold : FontWeights.Normal
                };
                var cellValue = new PrintHelper.Cell(item.ShowValue ? $"{item.Value:N2}" : "", TextAlignment.Right)
                {
                    FontWeight = item.Bold ? FontWeights.Bold : FontWeights.Normal
                };

                row.Add(cellName);
                row.Add(cellValue);
            }

            // Liabilities
            int curRow = 0;
            foreach (BalanceSheetLogic.BalanceSheetItem item in logic.LiabilitiesSource)
            {
                List<PrintHelper.Cell> row;
                if (curRow < rows.Count)
                {
                    row = rows[curRow++];
                }
                else
                {
                    row = new List<PrintHelper.Cell>();
                    row.Add(new PrintHelper.Cell(""));
                    row.Add(new PrintHelper.Cell(""));
                    rows.Add(row);
                }

                var cellName = new PrintHelper.Cell(item.Name)
                {
                    FontWeight = item.Bold ? FontWeights.Bold : FontWeights.Normal
                };
                var cellValue = new PrintHelper.Cell(item.ShowValue ? $"{item.Value:N2}" : "", TextAlignment.Right)
                {
                    FontWeight = item.Bold ? FontWeights.Bold : FontWeights.Normal
                };

                row.Add(cellName);
                row.Add(cellValue);
            }

            foreach(var row in rows)
            {
                ph.AddRow(row);
            }

            // Print
            ph.Print();
        }
    }
}
