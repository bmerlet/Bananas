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
    sealed class TransactionStateToBrushConverter : IValueConverter
    {
        static SolidColorBrush MyDarkGray = new SolidColorBrush(Color.FromRgb(100, 100, 100));

        // Convert a transaction status to a brush color
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var brush = Brushes.Black;

            if (value is ETransactionState state)
            {
                if (state.HasFlag(ETransactionState.Reconciled))
                {
                    brush = state.HasFlag(ETransactionState.TransferFillIn) ? Brushes.DarkSlateBlue : MyDarkGray;
                }
                else if (state.HasFlag(ETransactionState.TransferFillIn))
                {
                    brush = Brushes.DarkBlue;
                }
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
