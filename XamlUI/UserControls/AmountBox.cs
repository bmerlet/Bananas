using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XamlUI.UserControls
{
    class AmountBox : TextBox
    {
        private bool internalAmountUpdate;

        #region Dependency Properties

        //
        // Amount dependency property
        //
        public decimal Amount
        {
            get { return (decimal)GetValue(AmountProperty); }
            set { SetValue(AmountProperty, value); }
        }

        public static readonly DependencyProperty AmountProperty =
            DependencyProperty.Register("Amount", typeof(decimal), typeof(AmountBox),
                new UIPropertyMetadata((decimal)0, OnAmountChanged));

        // Amount change dispatcher
        private static void OnAmountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AmountBox ab)
            {
                ab.OnAmountChanged((decimal)e.NewValue);
            }
        }

        // Amount listener: Assign value to textbox text
        protected void OnAmountChanged(decimal amount)
        {
            if (!internalAmountUpdate)
            {
                Text = amount.ToString("N");
            }
        }

        // ZZZ Precision

        #endregion

        #region Overrides

        // Filter out input that makes the text not parsable
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            // Build text resulting from this input
            string futureText = Text;
            if (SelectionLength > 0)
            {
                // Replace selection
                futureText = futureText.Replace(Text.Substring(SelectionStart, SelectionLength), e.Text);
            }
            else
            {
                // Insert at caret
                futureText = futureText.Insert(CaretIndex, e.Text);
            }

            if (!decimal.TryParse(futureText, out _))
            {
                // Can't parse - don't accept
                e.Handled = true;
            }
        }

        // Update amount property when text changes
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            // Update amount property
            internalAmountUpdate = true;
            Amount = decimal.Parse(Text);
            internalAmountUpdate = false;
        }

        // Reformat amount and update the amount property when moving away
        protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnPreviewLostKeyboardFocus(e);

            if (!string.IsNullOrEmpty(Text))
            {
                internalAmountUpdate = true;
                Amount = decimal.Parse(Text);
                Text = Amount.ToString("N");
                internalAmountUpdate = false;
            }
        }

        #endregion
    }
}
