using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace XamlUI.Tools
{
    //
    // Class to apply a binding to an arbitrary object and read the value pointed to by the binding
    //
    class BindingEvaluator : FrameworkElement
    {
        // Dependency property on which the binding is applied
        static public DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(BindingEvaluator), new FrameworkPropertyMetadata(string.Empty));

        static public object GetValue(object item, string bindingPath)
        {
            // Create a dummy framework element, with its data context pointing to the item
            FrameworkElement element = new FrameworkElement
            {
                DataContext = item
            };

            // Bind the value property using the supplied binding path
            element.SetBinding(ValueProperty, new Binding(bindingPath));

            // Read the value property's value
            var result = element.GetValue(ValueProperty);

            return result;
        }
    }
}
