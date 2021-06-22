using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

using BanaData.Database;

namespace XamlUI.Tools
{
    sealed class TransactionStatusToBrushConverter : IValueConverter
    {
        // Convert a transaction status to a brush color
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var brush = Brushes.Black;

            if (value is ETransactionStatus status && status == ETransactionStatus.Reconciled)
            {
                brush = Brushes.Gray;
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
