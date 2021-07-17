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
using BanaData.Logic.Dialogs.Basics;

namespace XamlUI.Dialogs.Basics
{
    /// <summary>
    /// Interaction logic for PasswordPrompt.xaml
    /// </summary>
    public partial class PasswordPrompt : Window
    {
        public PasswordPrompt(PasswordPromptLogic logic)
        {
            // Use the view model as data context
            this.DataContext = logic;

            // Tell the view model how to close this dialog
            logic.CloseView = result =>
            {
                logic.Password = passwordBox.Password;
                DialogResult = result;
            };

            InitializeComponent();

            passwordBox.Password = logic.Password;
            passwordBox.Focus();
        }
    }
}
