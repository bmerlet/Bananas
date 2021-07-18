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
    /// Interaction logic for TransactionReport.xaml
    /// </summary>
    public partial class TransactionReport : Window
    {
        public TransactionReport(TransactionReportLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            InitializeComponent();

            BuildListView(logic);
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

                var fontweightBinding = new Binding("IsSubtotal")
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
    }
}
