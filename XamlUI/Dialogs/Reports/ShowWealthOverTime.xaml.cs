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
    /// Interaction logic for ShowWealthOverTime.xaml
    /// </summary>
    public partial class ShowWealthOverTime : Window
    {
        public ShowWealthOverTime(ShowWealthOverTimeLogic logic)
        {
            DataContext = logic;

            InitializeComponent();

            logic.PropertyChanged += OnPropertyChanged;
            Loaded += (s, e) => BuildGraph();
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UpdateGraphSignal")
            {
                BuildGraph();
            }
        }
        private void BuildGraph()
        {

            var logic = DataContext as ShowWealthOverTimeLogic;
            var points = logic.DateValues;

            canvas.Children.Clear();

            if (points.Count == 0)
            {
                return;
            }

            decimal minPrice = points.Min(p => p.Value);
            decimal maxPrice = points.Max(p => p.Value);

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
            canvas.Children.Add(new Line
            {
                X1 = marginX / 2,
                X2 = canvasWidth + marginX / 2,
                Y1 = marginY + canvasHeight,
                Y2 = marginY + canvasHeight,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 1,
                StrokeDashArray = strokeDash
            });
            canvas.Children.Add(new Line
            {
                X1 = marginX / 2,
                X2 = canvasWidth + marginX / 2,
                Y1 = marginY,
                Y2 = marginY,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 1,
                StrokeDashArray = strokeDash
            });

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
                double y = (double)((p.Value - minPrice) / (maxPrice - minPrice + 0.01M)) * canvasHeight;
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
        }
    }
}
