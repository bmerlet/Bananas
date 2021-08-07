using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace XamlUI.Tools
{
    class PrintHelper
    {
        private readonly List<Tuple<string, double>> columns = new List<Tuple<string, double>>();
        private readonly List<Cell[]> rows = new List<Cell[]>();

        public PrintHelper()
        {
        }

        public bool Landscape { get; set; } = false;

        public string Title { get; set; }
        public double TitleFontSize { get; set; } = 14;
        public FontWeight TitleFontWeight { get; set; } = FontWeights.Bold;

        public double ColumnHeaderFontSize { get; set; } = 11;
        public FontWeight ColumnHeaderFontWeight { get; set; } = FontWeights.Bold;
        public void AddColumn(string name, double width)
        {
            columns.Add(new Tuple<string, double>(name, width));
        }

        public double RowFontSize { get; set; } = 11;
        public void AddRow(IEnumerable<Cell> cells)
        {
            rows.Add(cells.ToArray());
        }

        public class Cell
        {
            public Cell(string text, TextAlignment textAlignment = TextAlignment.Left) => (Text, TextAlignemnt) = (text, textAlignment);
            public string Text { get; }
            public TextAlignment TextAlignemnt { get; }
            public Color Color { get; set; } = Colors.Black;
            public FontWeight FontWeight { get; set; } = FontWeights.Normal; 
        }

        public void Print()
        {
            // Create the title row
            var titleRow = new TableRow
            {
                FontWeight = TitleFontWeight,
                FontSize = TitleFontSize
            };
            titleRow.Cells.Add(new TableCell(new Paragraph(new Run(Title))));
            titleRow.Cells[0].ColumnSpan = columns.Count;
            var titleRowGroup = new TableRowGroup();
            titleRowGroup.Rows.Add(titleRow);

            // Create the column headers
            var headerRow = new TableRow
            {
                FontWeight = ColumnHeaderFontWeight,
                FontSize = ColumnHeaderFontSize
            };
            foreach(var col in columns)
            {
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(col.Item1))));
            }
            var headerRowGroup = new TableRowGroup();
            headerRowGroup.Rows.Add(headerRow);

            // Create the data
            var mainRowGroup = new TableRowGroup();
            foreach (Cell[] row in rows)
            {
                var tableRow = new TableRow();
                foreach (var cell in row)
                {
                    var tableCell = new TableCell(new Paragraph(new Run(cell.Text)) {FontWeight=cell.FontWeight })
                    {
                        TextAlignment = cell.TextAlignemnt
                    };
                    tableRow.Cells.Add(tableCell);
                }
                mainRowGroup.Rows.Add(tableRow);
            }

            // Create the table
            var table = new Table();

            // Create the columns
            foreach (var col in columns)
            {
                table.Columns.Add(new TableColumn() { Width = new GridLength(col.Item2) });
            }
            double totalWidth = columns.Sum(t => t.Item2);

            // Add the row groups
            table.RowGroups.Add(titleRowGroup);
            table.RowGroups.Add(headerRowGroup);
            table.RowGroups.Add(mainRowGroup);

            // Create flow document and add the table
            var flowDocument = new FlowDocument
            {
                FontSize = RowFontSize
            };
            flowDocument.Blocks.Add(table);

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                if (Landscape || totalWidth > printDialog.PrintableAreaWidth - 100)
                {
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                }
                flowDocument.PageHeight = printDialog.PrintableAreaHeight;
                flowDocument.PageWidth = printDialog.PrintableAreaWidth;
                flowDocument.PagePadding = new Thickness(50);
                flowDocument.ColumnGap = 0;
                flowDocument.ColumnWidth = printDialog.PrintableAreaWidth;
                IDocumentPaginatorSource dps = flowDocument;
                try
                {
                    printDialog.PrintDocument(dps.DocumentPaginator, Title);
                }
                catch(Exception e)
                {
                    MessageBox.Show("Error: " + e.Message);
                }
            }
        }
    }
}
