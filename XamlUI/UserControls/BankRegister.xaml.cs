using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using BanaData.Logic.Main;

namespace XamlUI.UserControls
{
    /// <summary>
    /// Interaction logic for BankRegister.xaml
    /// </summary>
    public partial class BankRegister : UserControl
    {
        public BankRegister()
        {
            InitializeComponent();

            // Hook to initialize new transaction
            dataGrid.InitializingNewItem += OnDataGridInitializingNewItem;

            // Programatically start edit
            // dataGrid.BeginEdit()
            // Events whe editing
            // dataGrid.BeginningEdit += ...
            // dataGrid.PreparingCellForEdit += ...
            // dataGrid.CellEditEnding +=
            // dataGrid.RowEditEnding +=

            // Listen to sorting on columns
            dataGrid.Sorting += OnDataGridSorting;

            dataGrid.PreparingCellForEdit += OnDataGridPreparingCellForEdit;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Listen to logic changes when data context is set 
            if (e.Property.Name == "DataContext" && e.NewValue is BankRegisterLogic brl)
            {
                brl.PropertyChanged += OnDataContextPropertyChanged;
            }
        }

        private void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext is BankRegisterLogic brl)
            {
                // Show/hide type column depending on whether this is a bank account or a credit card
                if (e.PropertyName == "IsBank")
                {
                    dataGrid.Columns[1].Visibility = brl.IsBank ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (e.PropertyName == "TransactionToScrollTo")
                {
                    // Scroll to where we are supposed to
                    dataGrid.ScrollIntoView(brl.TransactionToScrollTo);

                    // ZZZ
                    SetRowToEditMode(brl.TransactionToScrollTo);
                }
            }
        }

        // Recompute balances after sorting
        private void OnDataGridSorting(object sender, DataGridSortingEventArgs e)
        {
            //Console.WriteLine(string.Format("sorting grid by '{0}' column in {1} order", e.Column.SortMemberPath, e.Column.SortDirection));

            if (DataContext is BankRegisterLogic brl)
            {
                Dispatcher.BeginInvoke((Action)delegate ()
                {
                    // Hopefully runs after the sorting is done (??)
                    brl.RecomputeBalances();
                }, null);
            }
        }

        private void OnDataGridInitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            Console.WriteLine("OnDataGridInitializingNewItem");
        }

        private void OnDataGridPreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            Console.WriteLine("OnDataGridPreparingCellForEdit");
        }

        private void SetRowToEditMode(BankingTransactionLogic btl)
        {
            // Get row
            DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(btl);
            if (row == null)
            {
                dataGrid.UpdateLayout();
                row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromItem(btl);
            }


            var presenter = FindVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null)
            {
                return;
            }

            // try to get the cells but it may possibly be virtualized
            for (int column = 0; column < dataGrid.Columns.Count; column++)
            {
                var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    // now try to bring into view and retreive the cell
                    //ScrollIntoView(rowContainer, Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }

                cell.IsEditing = true;
            }
        }

        // ZZZZ Somewhere else
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj)
               where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj)
            where childItem : DependencyObject
        {
            foreach (childItem child in FindVisualChildren<childItem>(obj))
            {
                return child;
            }

            return null;
        }
        // ZZZZ End
    }
}
