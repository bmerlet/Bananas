using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;
using BanaData.Logic.Dialogs;

namespace XamlUI.Dialogs
{
    /// <summary>
    /// Interaction logic for EditSplitLV.xaml
    /// </summary>
    public partial class EditSplitLV : Window
    {
        #region Constructor

        public EditSplitLV(EditSplitLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            // Tell the view model how to close this dialog
            logic.CloseView = result => DialogResult = result;

            InitializeComponent();

            // Listen to logic changes
            logic.PropertyChanged += OnDataContextPropertyChanged;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditSplitLogic logic)
            {
                logic.OnLoaded();
            }
        }

        #endregion

        #region Process events from logic

        private void OnDataContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext is EditSplitLogic)
            {
                //
                // Scroll to bottom: Go to the bottom of the listview
                //
                if (e.PropertyName == "ScrollToBottom")
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
                if (listView.SelectedItem is EditSplitLogic.GridViewLineItem gvli)
                {
                    listView.UpdateLayout();
                    ListViewItem lvi = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(gvli);
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

            if (DataContext is EditSplitLogic && listView.SelectedItem is EditSplitLogic.GridViewLineItem gvli)
            {
                if (e.Key == Key.Escape)
                {
                    gvli.CancelEdit();
                    gvli.BeginEdit();
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    gvli.EndEdit();
                    e.Handled = true;
                }
            }
        }

        #endregion

    }
}
