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
    /// Interaction logic for ShowCashFlowBetweenPersons.xaml
    /// </summary>
    public partial class ShowCashFlowBetweenPersons : Window
    {
        public ShowCashFlowBetweenPersons(ShowCashFlowBetweenPersonsLogic logic)
        {
            DataContext = logic;

            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildGrid();
        }

        private void BuildGrid()
        {
            var logic = DataContext as ShowCashFlowBetweenPersonsLogic;

            //
            // Define columns
            //
            // Date
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });
            // Description
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(210) });
            // Amount/Balance
            for (int i = 0; i < logic.Members.Length; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });
            }

            //
            // First line
            //
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            // Date
            var txt = new TextBlock() { Text = "Date", FontWeight = FontWeights.Bold };
            grid.Children.Add(txt);
            // Description
            txt = new TextBlock() { Text = "Description", FontWeight = FontWeights.Bold };
            Grid.SetColumn(txt, 1);
            grid.Children.Add(txt);
            // Member names
            for (int i = 0; i < logic.Members.Length; i++)
            {
                txt = new TextBlock() { Text = logic.Members[i].Name, FontWeight = FontWeights.Bold };
                Grid.SetColumn(txt, 2 + i * 2);
                Grid.SetColumnSpan(txt, 2);
                grid.Children.Add(txt);
            }

            //
            // Second line
            //
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            for (int i = 0; i < logic.Members.Length; i++)
            {
                txt = new TextBlock() { Text = "Amount", FontWeight = FontWeights.Bold };
                Grid.SetColumn(txt, 2 + i * 2);
                Grid.SetRow(txt, 1);
                grid.Children.Add(txt);
                txt = new TextBlock() { Text = "Balance", FontWeight = FontWeights.Bold };
                Grid.SetColumn(txt, 2 + i * 2 + 1);
                Grid.SetRow(txt, 1);
                grid.Children.Add(txt);
            }

            //
            // First cash flow item
            //
            int row = 2;
            BuildGridLine(logic.CashFlowFirstItem, row++);

            //
            // Other lines
            //
            foreach(var item in logic.CashFlowItems)
            {
                BuildGridLine(item, row++);
            }

            //
            // Last cash flow item
            //
            BuildGridLine(logic.CashFlowLastItem, row++);
        }

        private void BuildGridLine(ShowCashFlowBetweenPersonsLogic.CashFlowItem item, int row)
        {
            // Add row definition
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            // Add date
            var txt = new TextBlock() { Text = $"{item.Date:MM/dd/yyyy}", TextAlignment=TextAlignment.Right };
            Grid.SetRow(txt, row);
            grid.Children.Add(txt);

            // Add description
            txt = new TextBlock() { Text = $"{item.Description}" };
            Grid.SetRow(txt, row);
            Grid.SetColumn(txt, 1);
            grid.Children.Add(txt);

            // Add all members
            int col = 2;
            foreach(var member in item.MemberItems)
            {
                if (member.ShowAmount)
                {
                    txt = new TextBlock() { Text = $"{member.Amount:N2}", TextAlignment = TextAlignment.Right };
                    Grid.SetRow(txt, row);
                    Grid.SetColumn(txt, col);
                    grid.Children.Add(txt);
                }

                col++;

                if (member.ShowBalance)
                {
                    txt = new TextBlock() { Text = $"{member.Balance:N2}", TextAlignment=TextAlignment.Right };
                    Grid.SetRow(txt, row);
                    Grid.SetColumn(txt, col);
                    grid.Children.Add(txt);
                }

                col++;
            }
        }
    }
}
