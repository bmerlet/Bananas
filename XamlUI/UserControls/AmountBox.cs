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
    class AmountBox : WatermarkTextBox
    {
        private bool internalAmountUpdate;
        private string formatString = "N";

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
                new UIPropertyMetadata(decimal.MinValue, OnAmountChanged));

        // Amount change dispatcher
        private static void OnAmountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AmountBox ab)
            {
                ab.OnAmountChanged((decimal)e.NewValue);
            }
        }

        // Amount listener: Assign value to textbox text
        private void OnAmountChanged(decimal amount)
        {
            if (!internalAmountUpdate)
            {
                if (amount == 0 && !ShowZeroAmount)
                {
                    Text = "";
                }
                else
                {
                    Text = amount.ToString(formatString);
                }
            }
        }

        //
        // Show 0 as 0.00 instead of blank
        //
        public bool ShowZeroAmount
        {
            get { return (bool)GetValue(ShowZeroAmountProperty); }
            set { SetValue(ShowZeroAmountProperty, value); }
        }

        public static readonly DependencyProperty ShowZeroAmountProperty =
            DependencyProperty.Register("ShowZeroAmount", typeof(bool), typeof(AmountBox), new UIPropertyMetadata(false, OnShowZeroAmountChanged));

        private static void OnShowZeroAmountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AmountBox ab)
            {
                ab.OnShowZeroAmountChanged((bool)e.NewValue);
            }
        }

        private void OnShowZeroAmountChanged(bool showZeroAmount)
        {
            if (Amount == 0)
            {
                Text = showZeroAmount ? Amount.ToString(formatString) : "";
            }
        }

        //
        // Precision
        //
        public int Precision
        {
            get { return (int)GetValue(PrecisionProperty); }
            set { SetValue(PrecisionProperty, value); }
        }

        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register("Precision", typeof(int), typeof(AmountBox), new UIPropertyMetadata(2, OnPrecisionChanged));

        private static void OnPrecisionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AmountBox ab)
            {
                ab.OnPrecisionChanged((int)e.NewValue);
            }
        }

        private void OnPrecisionChanged(int precision)
        {
            formatString = "N" + precision.ToString();

            if (ShowZeroAmount || Amount != 0)
            {
                Text = Amount.ToString(formatString);
            }
        }

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

            if (!string.IsNullOrWhiteSpace(futureText) && !decimal.TryParse(futureText, out _))
            {
                // Can't parse - don't accept
                e.Handled = true;
            }
        }

        // Filter out input that makes the text not parsable (del and backspace)
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            string futureText = null;
            if (e.Key == Key.Delete)
            {
                if (SelectionLength > 0)
                {
                    // Delete selection
                    futureText = Text.Replace(Text.Substring(SelectionStart, SelectionLength), "");
                }
                else if (CaretIndex < Text.Length)
                {
                    // Delete at caret position
                    futureText = Text.Remove(CaretIndex, 1);
                }
            }

            if (e.Key == Key.Back)
            {
                if (SelectionLength > 0)
                {
                    // Delete selection
                    futureText = Text.Replace(Text.Substring(SelectionStart, SelectionLength), "");
                }
                else if (CaretIndex > 0)
                {
                    // Delete at caret position
                    futureText = Text.Remove(CaretIndex - 1, 1);
                }
            }

            if (!string.IsNullOrWhiteSpace(futureText))
            {
                if (!decimal.TryParse(futureText, out _))
                {
                    // Can't parse - don't accept
                    e.Handled = true;
                }
            }
        }

        // Update amount property when text changes
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            // Update amount property
            internalAmountUpdate = true;
            if (string.IsNullOrWhiteSpace(Text))
            {
                Amount = 0;
            }
            else
            {
                Amount = decimal.Parse(Text);
            }
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
                Text = Amount.ToString(formatString);
                internalAmountUpdate = false;
            }
        }

        #endregion
    }
}
