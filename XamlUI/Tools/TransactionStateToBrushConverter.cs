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
        static SolidColorBrush MyDarkRed = new SolidColorBrush(Color.FromRgb(220, 0, 0));

        // Convert a transaction status to a brush color
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var brush = Brushes.Black;

            if (value is ETransactionState state)
            {
                switch(state)
                {
                    case ETransactionState.Idle:
                        break;
                    case ETransactionState.Reconciled:
                        brush = MyDarkGray;
                        break;
                    case ETransactionState.TransferFillIn:
                        brush = Brushes.DarkBlue;
                        break;
                    case ETransactionState.Reconciled | ETransactionState.TransferFillIn:
                        brush = Brushes.DarkSlateBlue;
                        break;
                    case ETransactionState.NegativeAmount:
                    case ETransactionState.NegativeAmount | ETransactionState.TransferFillIn:
                        brush = Brushes.Red;
                        break;
                    case ETransactionState.NegativeAmount | ETransactionState.Reconciled:
                    case ETransactionState.NegativeAmount | ETransactionState.Reconciled | ETransactionState.TransferFillIn:
                        brush = MyDarkRed;
                        break;
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
