using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace XamlUI.Tools
{
    /// <summary>
    /// Converts a decimal percent to and from a string, with passed in precision
    /// </summary>
    class PercentTostringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = null;

            if (value is decimal percent)
            {
                string format = "N" + (parameter is string precision ? precision : "1");
                result = (percent * 100).ToString(format) + "%";
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal result = 0;

            if (value is string str)
            {
                str = str.Trim();
                str = str.Trim(new char[] { '%' });
                if (decimal.TryParse(str, out result))
                {
                    if (parameter is string precisionStr && int.TryParse(precisionStr, out int precision))
                    {
                        result = Math.Round(result, precision);
                    }
                    result /= 100;
                }
            }

            return result;
        }
    }
}
