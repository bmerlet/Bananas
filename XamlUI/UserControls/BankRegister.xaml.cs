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
                }
            }
        }
    }
}
