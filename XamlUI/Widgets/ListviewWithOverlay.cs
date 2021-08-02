using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BanaData.Logic.Main;
using XamlUI.Tools;

namespace XamlUI.Widgets
{
    public class ListviewWithOverlay : UserControl
    {
        #region Members

        // The listview, set by the derived class
        private ListView _listView;
        private Grid _overlay;

        protected void SetListViewAndOverlay(ListView listView, Grid overlay) =>
            (_listView, _overlay) = (listView, overlay);

        #endregion

        #region Process events from logic

        // Hook to listen to logic notifications
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Listen to logic changes when data context is set 
            if (e.Property.Name == "DataContext" && e.NewValue is BaseRegisterLogic brl)
            {
                brl.PropertyChanged += OnDataContextPropertyChanged;
            }
        }

        private void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext is BaseRegisterLogic brl)
            {
                //
                // TransactionToScrollTo: Ensure a specific transaction is visible
                //
                if (e.PropertyName == "TransactionToScrollTo")
                {
                    _listView.UpdateLayout();
                    _listView.ScrollToCenterOfView(brl.TransactionToScrollTo);
                }
                //
                // Scroll to bottom: Go to the bottom of the listview
                //
                else if (e.PropertyName == "ScrollToBottom")
                {
                    if (VisualTreeHelper.GetChildrenCount(_listView) > 0)
                    {
                        var elt = (FrameworkElement)VisualTreeHelper.GetChild(_listView, 0);
                        ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(elt, 0);
                        scrollViewer.ScrollToBottom();
                    }
                }
                //
                // Update overlay position
                //
                else if (e.PropertyName == "UpdateOverlayPosition")
                {
                    SetOverlayPosition(brl.UpdateOverlayPosition);
                }
            }
        }

        #endregion

        #region Overlay

        private void SetOverlayPosition(Action actionAfterOverlayPositionIsUpdated)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                if (_listView.SelectedItem != null)
                {
                    _listView.UpdateLayout();
                    ListViewItem lvi = (ListViewItem)_listView.ItemContainerGenerator.ContainerFromItem(_listView.SelectedItem);
                    if (lvi != null)
                    {
                        var pos = lvi.TranslatePoint(new Point(0, 0), _listView);
                        _overlay.Visibility = Visibility.Visible;
                        _overlay.Margin = new Thickness(3 + pos.X, pos.Y, 0, 0);
                        if (actionAfterOverlayPositionIsUpdated != null)
                        {
                            Dispatcher.BeginInvoke(actionAfterOverlayPositionIsUpdated, DispatcherPriority.ContextIdle, null);
                        }
                    }
                }
            }, DispatcherPriority.ContextIdle, null);
        }

        protected void OnListViewScrollChanged(object source, ScrollChangedEventArgs e)
        {
            SetOverlayPosition(null);
        }

        #endregion

        #region Keyboard input

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is BaseRegisterLogic && _listView.SelectedItem is IEditableObject editableObject)
            {
                if (e.Key == Key.Escape)
                {
                    editableObject.CancelEdit();
                    editableObject.BeginEdit();
                    e.Handled = true;
                }
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (DataContext is BaseRegisterLogic brl)
            {
                AutoCompleteTextBox AutoCompleteTextBoxWithPopupOpen = null;
                if (e.OriginalSource is WatermarkTextBox wtb &&
                    wtb.TemplatedParent is AutoCompleteTextBox actb &&
                    actb.IsPopupOpen)
                {
                    AutoCompleteTextBoxWithPopupOpen = actb;
                }

                if (e.Key == Key.Up && AutoCompleteTextBoxWithPopupOpen == null)
                {
                    brl.MoveUp();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Down && AutoCompleteTextBoxWithPopupOpen == null)
                {
                    brl.MoveDown();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    if (AutoCompleteTextBoxWithPopupOpen != null)
                    {
                        AutoCompleteTextBoxWithPopupOpen.IsPopupOpen = false;
                        AutoCompleteTextBoxWithPopupOpen.ProcessEnterOrTab();
                    }
                    brl.ProcessEnter();
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region Sorting

        private SortAdorner sortAdorner;

        protected void OnColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is BaseRegisterLogic brl)
            {
                // Get tag name from the source
                var sd = brl.RegisterItems.SortDescriptions;
                var column = sender as GridViewColumnHeader;
                var memberName = column.Tag.ToString();

                // Determine sorting direction
                var direction = ListSortDirection.Ascending;
                if (sd != null && sd.Count > 0 && sd[0].PropertyName == memberName)
                {
                    direction = sd[0].Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                }

                // Do the sorting
                brl.RegisterItems.SortDescriptions.Clear();
                brl.RegisterItems.SortDescriptions.Add(new SortDescription(memberName, direction));

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
