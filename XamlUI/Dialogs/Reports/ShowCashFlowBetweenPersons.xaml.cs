using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Interaction logic for ShowCashFlowBetweenPersons.xaml
    /// </summary>
    public partial class ShowCashFlowBetweenPersons : Window
    {
        public ShowCashFlowBetweenPersons(ShowCashFlowBetweenPersonsLogic logic)
        {
            DataContext = logic;

            InitializeComponent();
        }
    }

    /// <summary>
    /// Converts a set of column descriptors to a gridview hosting those columns
    /// </summary>
    public class PersonsToDynamicGridViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<ShowCashFlowBetweenPersonsLogic.ColumnDescription> columns)
            {
                var gridView = new GridView();
                var amountToString = new AmountToStringConverter();
                var amountToColor = new AmountToColorConverter();

                foreach(var column in columns)
                {
                    var binding = new Binding(column.ValueName);
                    if (column.Format != null)
                    {
                        binding.StringFormat = column.Format;
                    }

                    GridViewColumn gridViewColumn;
                    if (column.IsAmount)
                    {
                        binding.Converter = amountToString;

                        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock)) { Name = "AmountTextBoxFactory" };
                        textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
                        textBlockFactory.SetBinding(TextBlock.TextProperty, binding);

                        var foregroundBinding = new Binding(column.ValueName) { Converter = amountToColor };
                        textBlockFactory.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);

                        var cellTemplate = new DataTemplate() { VisualTree = textBlockFactory };

                        gridViewColumn = new GridViewColumn { Header = column.ColumnName, CellTemplate = cellTemplate };
                    }
                    else
                    {
                        // Simple case, just a column name and a DisplayMemberBinding
                        gridViewColumn = new GridViewColumn { Header = column.ColumnName, DisplayMemberBinding = binding };
                    }

                    if (column.Width != 0)
                    {
                        gridViewColumn.Width = column.Width;
                    }
                    gridView.Columns.Add(gridViewColumn);
                }

                return gridView;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
