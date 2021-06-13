using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace XamlUI.Tools
{
    // Binding proxy for stuff not in the visual tree
    // The magic is in the "Freezable": It makes it so that the data context is inherited despite not
    // being part of the visual tree.
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        // Anonymous dependency property, bound in XAML to the data context:
        // <local:BindingProxy x:Key="..." Data={Binding}/>
        public object Data
        {
            get => (object)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}
