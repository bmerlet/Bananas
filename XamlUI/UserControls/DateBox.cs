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
    /// <summary>
    /// Textbox repurposed for date input
    /// </summary>
    class DateBox : TextBox
    {
        private bool internalDateUpdate;

        #region Date Dependency Property

        public DateTime Date
        {
            get { return (DateTime)GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }

        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register("Date", typeof(DateTime), typeof(DateBox),
                new UIPropertyMetadata(DateTime.Now, OndateChanged));

        // Date change dispatcher
        private static void OndateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateBox dtb)
            {
                dtb.OndateChanged((DateTime)e.NewValue);
            }
        }

        // Date listener: Assign value to textbox text
        protected void OndateChanged(DateTime date)
        {
            if (!internalDateUpdate)
            {
                Text = date.ToShortDateString();
            }
        }

        #endregion

        #region Overrides

        // Select the first date field
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            if (!string.IsNullOrEmpty(Text))
            {
                CaretIndex = 0;
                SelectionStart = 0;
                SelectionLength = Text.IndexOf("/");
            }
        }

        // Ignore mouse events when we don't yet have focus
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            if (!IsKeyboardFocusWithin)
            {
                // If the text box is not yet focussed, give it the focus and
                // stop further processing of this click event. This prevents the framework
                // from swallowing up the selection we make when we get focus
                Focus();
                e.Handled = true;
            }
        }

        // Reformat date and update the date property when moving away
        protected override void OnPreviewLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnPreviewLostKeyboardFocus(e);

            try
            {
                internalDateUpdate = true;
                Date = DateTime.Parse(Text);
                Text = Date.ToShortDateString();
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter a valid date", "Date", MessageBoxButton.OK);
                e.Handled = true;
            }
            finally
            {
                internalDateUpdate = false;
            }
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            if (e.Text == "/")
            {
                // Find next "/" (circular)
                int firstSlashIndex = Text.IndexOf('/');
                int secondSlashIndex = Text.IndexOf('/', firstSlashIndex + 1);
                if (firstSlashIndex >= 0 && secondSlashIndex >= 0)
                {
                    if (CaretIndex <= firstSlashIndex)
                    {
                        // Select 2nd field, put caret at beginning of 2nd field
                        CaretIndex = firstSlashIndex + 1;
                        SelectionStart = firstSlashIndex + 1;
                        SelectionLength = secondSlashIndex - firstSlashIndex - 1;
                    }
                    else if (CaretIndex <= secondSlashIndex)
                    {
                        // Select 3rd field, put caret at beginning of 3rd field
                        CaretIndex = secondSlashIndex + 1;
                        SelectionStart = secondSlashIndex + 1;
                        SelectionLength = Text.Length - secondSlashIndex - 1;
                    }
                    else
                    {
                        // Select first field
                        CaretIndex = 0;
                        SelectionStart = 0;
                        SelectionLength = firstSlashIndex;
                    }
                    e.Handled = true;
                }
            }
            else if ("0123456789".Contains(e.Text))
            {
                // Default behavior for numbers
            }
            else
            {
                // Ignore other input
                e.Handled = true;
            }
        }

        // Update the date property on text change
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            try
            {
                internalDateUpdate = true;
                Date = DateTime.Parse(Text);
            }
            catch (FormatException)
            {

            }
            finally
            {
                internalDateUpdate = false;
            }

        }
        #endregion
    }
}
