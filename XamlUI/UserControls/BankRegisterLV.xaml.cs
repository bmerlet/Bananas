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
    public partial class BankRegisterLV : ListviewWithOverlay
    {
        #region Construction

        public BankRegisterLV()
        {
            InitializeComponent();

            base.SetListViewAndOverlay(listView, overlay);
        }

        // Hook to listen to logic notifications
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Listen to logic changes when data context is set 
            if (e.Property.Name == "DataContext" && e.NewValue is AbstractRegisterLogic arl)
            {
                arl.PropertyChanged += OnDataContextPropertyChanged;
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
            }
        }

        #endregion
    }
}
