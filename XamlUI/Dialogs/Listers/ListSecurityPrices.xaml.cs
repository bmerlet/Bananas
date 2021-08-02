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
using BanaData.Logic.Dialogs.Listers;

namespace XamlUI.Dialogs.Listers
{
    /// <summary>
    /// Interaction logic for ListSecurityPrices.xaml
    /// </summary>
    public partial class ListSecurityPrices : Window
    {
        public ListSecurityPrices(ListSecurityPricesLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            // Tell the view model how to close this dialog
            logic.CloseView = result => DialogResult = result;

            InitializeComponent();

            logic.GraphPoints.CollectionChanged += (s, e) => BuildGraph();
            Loaded += (s, e) => BuildGraph();
        }

        private void BuildGraph()
        {
            var logic = DataContext as ListSecurityPricesLogic;
            var points = logic.GraphPoints;

            canvas.Children.Clear();

            if (points.Count == 0)
            {
                return;
            }

            decimal minPrice = points.Min(p => p.Price);
            decimal maxPrice = points.Max(p => p.Price);

            TimeSpan span = logic.EndDate - logic.StartDate;
            double spanInDays = span.TotalDays;

            double canvasWidth = canvas.ActualWidth /*- canvas.Margin.Left - canvas.Margin.Right */;
            double canvasHeight = canvas.ActualHeight /*- canvas.Margin.Top - canvas.Margin.Bottom */;

            double lastX = double.MinValue;
            double lastY = double.MinValue;

            foreach (var p in points)
            {
                double x = (p.Date - logic.StartDate).TotalDays / spanInDays * canvasWidth;
                double y = (double)((p.Price - minPrice) / (maxPrice - minPrice + 1)) * canvasHeight;
                y = canvasHeight - y;

                if (lastX != double.MinValue)
                {
                    var line = new Line()
                    {
                        Stroke = System.Windows.Media.Brushes.LightSteelBlue,
                        StrokeThickness = 1,
                        X1 = lastX,
                        X2 = x,
                        Y1 = lastY,
                        Y2 = y,
                    };

                    canvas.Children.Add(line);
                }

                lastX = x;
                lastY = y;
            }
        }
    }
}
