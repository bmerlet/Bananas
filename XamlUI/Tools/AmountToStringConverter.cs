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
    /// Converts an amount to a string, and show the zero value as an empty string
    /// </summary>
    class AmountToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = null;

            if (value is decimal amount)
            {
                if (amount == 0)
                {
                    result = "";
                }
                else if (parameter is string format)
                {
                    result = amount.ToString(format);
                }
                else
                {
                    result = amount.ToString("C2");
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class AmountToStringNoNegativeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = null;

            if (value is decimal amount)
            {
                if (amount <= 0)
                {
                    result = "";
                }
                else if (parameter is string format)
                {
                    result = amount.ToString(format);
                }
                else
                {
                    result = amount.ToString("C2");
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
