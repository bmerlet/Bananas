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

using XamlUI.Widgets;

namespace XamlUI.UserControls
{
    /// <summary>
    /// Interaction logic for InvestmentRegister.xaml
    /// </summary>
    public partial class InvestmentRegister : ListviewWithOverlay
    {
        public InvestmentRegister()
        {
            InitializeComponent();

            base.SetListViewAndOverlay(listView, overlay);
        }
    }
}
