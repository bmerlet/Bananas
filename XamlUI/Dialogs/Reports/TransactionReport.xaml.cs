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
    /// Interaction logic for TransactionReport.xaml
    /// </summary>
    public partial class TransactionReport : Window
    {
        public TransactionReport(TransactionReportLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            // Tell the view model how to close this dialog
            logic.CloseView = result => DialogResult = result;

            InitializeComponent();

            BuildListView(logic);
            if (logic.IsPieChartVisible)
            {
                BuildPieChart(logic);
            }
        }

        private void BuildListView(TransactionReportLogic logic)
        {
            GridView gridView = new GridView
            {
                AllowsColumnReorder = true
            };

            foreach (var col in logic.Columns)
            {
                var textBinding = new Binding(col.PropertyPath)
                {
                    StringFormat = col.Format,
                };

                var fontweightBinding = new Binding("IsBold")
                {
                    Converter = new Tools.FontWeightConverter()
                };

                FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock))
                {
                    Name = "myTextBlockFactory"
                };

                textBlockFactory.SetBinding(TextBlock.TextProperty, textBinding);
                textBlockFactory.SetBinding(TextBlock.FontWeightProperty, fontweightBinding);
                if (col.RightAligned)
                {
                    textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
                }

                var dataTemplate = new DataTemplate
                {
                    VisualTree = textBlockFactory
                };

                var gcv = new GridViewColumn
                {
                    Width = col.Width,
                    Header = col.Header,
                    CellTemplate = dataTemplate
                };

                gridView.Columns.Add(gcv);
            }

            listView.View = gridView;
        }

        private void OnListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is TransactionReportLogic logic && sender is ListView listView && listView.SelectedItem is TransactionReportLogic.TransactionItem item)
            {
                logic.GoTo(item);
            }
        }

        private void OnPrint(object sender, RoutedEventArgs e)
        {
            var logic = DataContext as TransactionReportLogic;

            // Create the print helper
            var ph = new PrintHelper
            {
                Title = $"Transaction report: '{logic.Title}'"
            };

            // Create the columns
            foreach (var col in logic.Columns)
            {
                var listviewColumn = ((GridView)listView.View).Columns.First(c => (string)c.Header == col.Header);
                ph.AddColumn(col.Header, listviewColumn.ActualWidth * 1);
            }

            // Create the data
            foreach (TransactionReportLogic.TransactionItem item in logic.TransactionsSource)
            {
                var cells = new List<PrintHelper.Cell>();
                foreach(var col in logic.Columns)
                {
                    var text = BindingEvaluator.GetValue(item, col.PropertyPath) as string;
                    var cell = new PrintHelper.Cell(text, col.RightAligned ? TextAlignment.Right : TextAlignment.Left)
                    {
                        FontWeight = item.IsBold ? FontWeights.Bold : FontWeights.Normal
                    };
                    cells.Add(cell);
                }
                ph.AddRow(cells);
            }

            // Print
            ph.Print();
        }

        private void BuildPieChart(TransactionReportLogic logic)
        {
            // Get total sum in slices
            decimal total = logic.PieSlices.Sum(ps => Math.Abs(ps.Amount));
            if (total == 0)
            {
                return;
            }

            // Drawing constants
            double canvasWidth = pieChartCanvas.Width;
            double canvasHeight = pieChartCanvas.Height;
            double centerX = canvasWidth / 2.0;
            double centerY = canvasHeight / 2.0;
            double pieMargin = 10;
            double radius = Math.Min(canvasWidth, canvasHeight) / 2.0 - pieMargin;

            // Create all pie slices
            double angle = 0;
            byte r = 100, g = 170, b = 250;
            foreach(var pieSlice in logic.PieSlices)
            {
                double startArcX = radius * Math.Sin(angle);
                double startArcY = -radius * Math.Cos(angle);
                angle += 2.0 * Math.PI * (double)(Math.Abs(pieSlice.Amount) / total);
                double endArcX = radius * Math.Sin(angle);
                double endArcY = -radius * Math.Cos(angle);

                var pathFigure = new PathFigure() { IsClosed = true, StartPoint = new Point(startArcX, startArcY) };
                pathFigure.Segments.Add(new LineSegment() { Point = new Point(0, 0) });
                pathFigure.Segments.Add(new LineSegment() { Point = new Point(endArcX, endArcY) });
                pathFigure.Segments.Add(new ArcSegment() { SweepDirection = SweepDirection.Counterclockwise, Size = new Size(radius, radius), Point = new Point(startArcX, startArcY) });

                var pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                var path = new Path
                {
                    Fill = new SolidColorBrush(new Color() { R = r, G = g, B = b, A = 255 }),
                    Stroke = Brushes.Black,
                    Data = pathGeometry,
                    ToolTip = pieSlice.Tip
                };

                pieChartCanvas.Children.Add(path);
                Canvas.SetLeft(path, centerX);
                Canvas.SetTop(path, centerY);

                byte cst = 110;
                r = (byte)(r + cst);
                g = (byte)(g + cst);
                b = (byte)(b + cst);
            }
        }
    }
}
