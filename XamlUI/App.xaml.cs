using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace XamlUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Register a class handler for TextBox's GotFocus event  
            EventManager.RegisterClassHandler(
                typeof(TextBox),
                TextBox.GotFocusEvent,
                new RoutedEventHandler(OnTextBoxGotFocus));

            base.OnStartup(e);
        }

        // Select all text in textbox when getting focus
        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }
    }
}
