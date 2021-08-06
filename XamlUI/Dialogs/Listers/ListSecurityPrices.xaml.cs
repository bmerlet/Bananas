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

            logic.Quotes.CollectionChanged += (s, e) => BuildGraph();
            logic.ReinvestedDividends.CollectionChanged += (s, e) => BuildGraph();
            logic.Trades.CollectionChanged += (s, e) => BuildGraph();
            logic.PropertyChanged += OnPropertyChanged;
            Loaded += (s, e) => BuildGraph();
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowReinvDivs" || e.PropertyName == "ShowTrades")
            {
                BuildGraph();
            }
        }

        private void BuildGraph()
        {

            var logic = DataContext as ListSecurityPricesLogic;
            var points = logic.Quotes;

            canvas.Children.Clear();

            if (points.Count == 0)
            {
                return;
            }

            decimal minPrice = points.Min(p => p.Price);
            decimal maxPrice = points.Max(p => p.Price);

            TimeSpan span = logic.EndDate - logic.StartDate;
            double spanInDays = span.TotalDays;

            double marginX = 20;
            double marginY = 20;
            double canvasWidth = canvas.ActualWidth - 2 * marginX;
            double canvasHeight = canvas.ActualHeight - 2 * marginY;

            double lastX = double.MinValue;
            double lastY = double.MinValue;
            var strokeDash = new DoubleCollection(new double[] { 5, 18 });

            // Draw high and low prices
            canvas.Children.Add(new Line {
                X1 = marginX / 2, X2 = canvasWidth + marginX / 2,
                Y1 = marginY + canvasHeight, Y2= marginY + canvasHeight,
                Stroke = Brushes.DarkGray, StrokeThickness = 1, StrokeDashArray = strokeDash
            });
            canvas.Children.Add(new Line {
                X1 = marginX / 2, X2 = canvasWidth + marginX / 2,
                Y1 = marginY, Y2 = marginY, 
                Stroke = Brushes.DarkGray, StrokeThickness = 1, StrokeDashArray = strokeDash });

            var highPriceTextBlock = new TextBlock { Text = $"Max:{maxPrice:N2}", FontSize = 9 };
            canvas.Children.Add(highPriceTextBlock);
            Canvas.SetLeft(highPriceTextBlock, 0);
            Canvas.SetTop(highPriceTextBlock, 0);
            var lowPriceTextBlock = new TextBlock { Text = $"Min:{minPrice:N2}", FontSize = 9 };
            canvas.Children.Add(lowPriceTextBlock);
            Canvas.SetLeft(lowPriceTextBlock, 0);
            Canvas.SetTop(lowPriceTextBlock, canvasHeight + marginY);

            // Draw quotes graph
            foreach (var p in points)
            {
                double x = (p.Date - logic.StartDate).TotalDays / spanInDays * canvasWidth;
                x += marginX;
                double y = (double)((p.Price - minPrice) / (maxPrice - minPrice + 1)) * canvasHeight;
                y = marginY + canvasHeight - y;

                if (lastX != double.MinValue)
                {
                    var line = new Line
                    {
                        Stroke = Brushes.SteelBlue,
                        StrokeThickness = 1,
                        X1 = lastX,
                        X2 = x,
                        Y1 = lastY,
                        Y2 = y,
                        ToolTip = p.Tip
                    };

                    canvas.Children.Add(line);
                }

                lastX = x;
                lastY = y;
            }

            // Draw reinvestments
            const double radius = 3;
            if (logic.ShowReinvDivs == true)
            {
                foreach(var p in logic.ReinvestedDividends)
                {
                    double x = (p.Date - logic.StartDate).TotalDays / spanInDays * canvasWidth;
                    x += marginX;
                    double y = (double)((p.Price - minPrice) / (maxPrice - minPrice + 1)) * canvasHeight;
                    y = marginY + canvasHeight - y;

                    var ellipse = new Ellipse() { Fill = Brushes.Blue, Width=radius*2, Height=radius*2, ToolTip=p.Tip };
                    canvas.Children.Add(ellipse);
                    Canvas.SetLeft(ellipse, x - radius);
                    Canvas.SetTop(ellipse, y - radius);
                }
            }

            if (logic.ShowTrades == true)
            {
                foreach (var p in logic.Trades)
                {
                    double x = (p.Date - logic.StartDate).TotalDays / spanInDays * canvasWidth;
                    x += marginX;
                    double y = (double)((p.Price - minPrice) / (maxPrice - minPrice + 1)) * canvasHeight;
                    y = marginY + canvasHeight - y;

                    var ellipse = new Ellipse() { Fill = Brushes.Pink, Width = radius * 2, Height = radius * 2, ToolTip = p.Tip };
                    canvas.Children.Add(ellipse);
                    Canvas.SetLeft(ellipse, x - radius);
                    Canvas.SetTop(ellipse, y - radius);
                }
            }
        }
    }
}
