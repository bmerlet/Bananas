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
            // Create the title row
            var titleRow = new TableRow
            {
                FontWeight = System.Windows.FontWeights.Bold
            };
            titleRow.Cells.Add(new TableCell(new Paragraph(new Run($"Search for: '{logic.SearchText}'"))));
            titleRow.Cells[0].ColumnSpan = 6;
            var titleRowGroup = new TableRowGroup();
            titleRowGroup.Rows.Add(titleRow);

            // Create the column headers
            var headerRow = new TableRow();
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Account"))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Date"))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Payee"))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Memo"))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Category"))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Amount"))));
            var headerRowGroup = new TableRowGroup();
            headerRowGroup.Rows.Add(headerRow);

            // Create the data
            var mainRowGroup = new TableRowGroup();
            foreach (SearchResultLogic.FoundItem item in logic.FoundItemsSource)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Account))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Date.ToString("MM/dd/yyyy")))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Payee))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Memo))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Category))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Amount.ToString("N2")))));
                row.Cells[5].TextAlignment = TextAlignment.Right;
                mainRowGroup.Rows.Add(row);
            }

            // Create the table
            var table = new Table();

            // Create the columns
            table.Columns.Add(new TableColumn() { Width = new GridLength(200) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(200) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(200) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(70) });

            // Add the row groups
            table.RowGroups.Add(titleRowGroup);
            table.RowGroups.Add(headerRowGroup);
            table.RowGroups.Add(mainRowGroup);

            // Create flow document and add the table
            var flowDocument = new FlowDocument
            {
                FontSize = 11
            };
            flowDocument.Blocks.Add(table);

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                flowDocument.PageHeight = printDialog.PrintableAreaHeight;
                flowDocument.PageWidth = printDialog.PrintableAreaWidth;
                flowDocument.PagePadding = new Thickness(50);
                flowDocument.ColumnGap = 0;
                flowDocument.ColumnWidth = printDialog.PrintableAreaWidth;
                IDocumentPaginatorSource dps = flowDocument;
                printDialog.PrintDocument(dps.DocumentPaginator, "Search result");
            }
        }

    }
}
