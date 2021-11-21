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
    /// Interaction logic for IncomeStatement.xaml
    /// </summary>
    public partial class IncomeStatement : Window
    {
        public IncomeStatement(IncomeStatementLogic logic)
        {
            DataContext = logic;
            InitializeComponent();
        }

        private void OnPrint(object sender, RoutedEventArgs e)
        {
            var logic = DataContext as IncomeStatementLogic;

            // Create the print helper
            var ph = new PrintHelper
            {
                Title = $"Income statement for {logic.SelectedMember} from {logic.DateRangeLogic.StartDate:MM/dd/yyyy} to {logic.DateRangeLogic.EndDate:MM/dd/yyyy}"
            };

            // Create the columns
            ph.AddColumn("Name", 400);
            ph.AddColumn("Amount", 120);

            // Create the data
            foreach (IncomeStatementLogic.IncomeStatementNode node in logic.NodesSource)
            {
                PrintNode(ph, node, "");
            }

            // Print
            ph.Print();
        }

        private void PrintNode(PrintHelper ph, IncomeStatementLogic.IncomeStatementNode node, string indent)
        {
            var nameCell = new PrintHelper.Cell(indent + node.Name)
            {
                FontWeight = node.Bold ? FontWeights.Bold : FontWeights.Normal
            };
            var valueCell = new PrintHelper.Cell(indent + $"{node.Value:N2}", TextAlignment.Right)
            {
                FontWeight = node.Bold ? FontWeights.Bold : FontWeights.Normal
            };
            ph.AddRow(new PrintHelper.Cell[] { nameCell, valueCell});

            foreach(var child in node.Children)
            {
                PrintNode(ph, child, indent + "   ");
            }
        }
    }
}
