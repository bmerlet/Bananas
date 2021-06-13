using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace XamlUI.Tools
{
    /// <summary>
    /// Converts bool to fontweight (false: Normal; true: bold)
    /// </summary>
    public class FontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bold = (bool)value;
            FontWeight fontWeight = FontWeight.FromOpenTypeWeight(bold ? 700 : 400);
            return fontWeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
