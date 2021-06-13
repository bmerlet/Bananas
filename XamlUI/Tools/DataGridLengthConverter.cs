using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace XamlUI.Tools
{
    // Converter DataGridLength <-> double
    [ValueConversion(typeof(double), typeof(DataGridLength))]
    public class DataGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return new DataGridLength(d);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataGridLength dgl)
            {
                return dgl.DisplayValue;
            }
            return null;
        }
    }
}
