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
using System.Windows.Threading;

using BanaData.Logic.Main;
using XamlUI.Tools;

namespace XamlUI.UserControls
{
    /// <summary>
    /// Interaction logic for BankRegisterLV.xaml
    /// </summary>
    public partial class BankRegisterLV : UserControl
    {
        #region Construction

        public BankRegisterLV()
        {
            InitializeComponent();
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

        #endregion

        #region Process events from logic

        private GridViewColumn maroonedColumn;

        private void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext is BankRegisterLogic brl)
            {
                //
                // IsBank: Show/hide medium column depending on whether this is a bank account or a credit card
                //
                if (e.PropertyName == "IsBank")
                {
                    GridView gridview = listView.View as GridView;
                    if (brl.IsBank)
                    {
                        if (maroonedColumn != null && !gridview.Columns.Contains(maroonedColumn))
                        {
                            gridview.Columns.Insert(1, maroonedColumn);
                            maroonedColumn = null;
                        }
                    }
                    else
                    {
                        if (maroonedColumn == null)
                        {
                            maroonedColumn = gridview.Columns[1];
                            gridview.Columns.RemoveAt(1);
                        }
                    }
                }
                //
                // TransactionToScrollTo: Ensure a specific transaction is visible
                //
                else if (e.PropertyName == "TransactionToScrollTo")
                {
                    listView.UpdateLayout();
                    listView.ScrollIntoView(brl.TransactionToScrollTo);
                }
                //
                // Scroll to bottom: Go to the bottom of the listview
                //
                else if (e.PropertyName == "ScrollToBottom")
                {
                    if (VisualTreeHelper.GetChildrenCount(listView) > 0)
                    {
                        var elt = (FrameworkElement)VisualTreeHelper.GetChild(listView, 0);
                        ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(elt, 0);
                        scrollViewer.ScrollToBottom();
                    }
                }
                //
                // Update overlay position
                //
                else if (e.PropertyName == "UpdateOverlayPosition")
                {
                    SetOverlayPosition();
                }
            }
        }

        #endregion

        #region Selection and overlay

        private void SetOverlayPosition()
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                if (listView.SelectedItem is BankingTransactionLogic btl)
                {
                    listView.UpdateLayout();
                    ListViewItem lvi = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(btl);
                    if (lvi != null)
                    {
                        var pos = lvi.TranslatePoint(new Point(0, 0), listView);
                        overlay.Visibility = Visibility.Visible;
                        overlay.Margin = new Thickness(3, pos.Y, 0, 0);
                    }
                }
            }, DispatcherPriority.ContextIdle, null);
        }

        private void OnListViewScrollChanged(object source, ScrollChangedEventArgs e)
        {
            SetOverlayPosition();
        }

        #endregion

        #region Keyboard input

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is BankRegisterLogic && listView.SelectedItem is BankingTransactionLogic btl)
            {
                if (e.Key == Key.Escape)
                {
                    btl.CancelEdit();
                    btl.BeginEdit();
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    btl.EndEdit();
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region Sorting

        private SortAdorner sortAdorner;

        private void OnColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is BankRegisterLogic brl)
            {
                // Get tag name from the source
                var sd = brl.Transactions.SortDescriptions;
                var column = sender as GridViewColumnHeader;
                var memberName = column.Tag.ToString();

                // Determine sorting direction
                var direction = ListSortDirection.Ascending;
                if (sd != null && sd.Count > 0 && sd[0].PropertyName == memberName)
                {
                    direction = sd[0].Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }

                // Do the sorting
                brl.Transactions.SortDescriptions.Clear();
                brl.Transactions.SortDescriptions.Add(new SortDescription(memberName, direction));

                // Un-adorn existing adornment
                if (sortAdorner != null)
                {
                    AdornerLayer.GetAdornerLayer(sortAdorner.AdornedElement).Remove(sortAdorner);
                }
                sortAdorner = new SortAdorner(column, direction);
                AdornerLayer.GetAdornerLayer(column).Add(sortAdorner);

                // Recompute balances after sorting
                Dispatcher.BeginInvoke((Action)delegate ()
                {
                    // Hopefully runs after the sorting is done (??)
                    brl.RecomputeBalances();
                }, null);
            }
        }

        #endregion
    }
}
