using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XamlUI.UserControls
{
    /// <summary>
    /// Interaction logic for DateTextBox.xaml
    /// </summary>
    public partial class DateTextBox : UserControl
    {
        #region Private members

        private bool internalDateUpdate;

        #endregion

        #region Date Dependency Property

        public DateTime Date
        {
            get { return (DateTime)GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }

        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register("Date", typeof(DateTime), typeof(DateTextBox),
                new UIPropertyMetadata(DateTime.Now, OndateChanged));

        // Date change dispatcher
        private static void OndateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateTextBox dtb)
            {
                dtb.OndateChanged((DateTime)e.NewValue);
            }
        }

        // Date listener: Assign value to textbox text
        protected void OndateChanged(DateTime date)
        {
            if (!internalDateUpdate)
            {
                textBox.Text = date.ToShortDateString();
            }
        }

        #endregion

        #region Constructor

        public DateTextBox()
        {
            InitializeComponent();
            textBox.GotFocus += OnTextBoxGotFocus;
            textBox.PreviewLostKeyboardFocus += OnTextBoxPreviewLostKeyboardFocus;
            textBox.PreviewTextInput += OnTextBoxPreviewTextInput;
            textBox.TextChanged += OnTextBoxTextChanged;
        }

        #endregion

        #region Actions

        // Select first field of date when getting focus
        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (textBox.Text != "")
            {
                textBox.CaretIndex = 0;
                textBox.SelectionStart = 0;
                textBox.SelectionLength = textBox.Text.IndexOf("/");
            }
        }

        // Reformat date and update the date property when moving away
        private void OnTextBoxPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                internalDateUpdate = true;
                Date = DateTime.Parse(textBox.Text);
                textBox.Text = Date.ToShortDateString();
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

        // Filter input
        private void OnTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "/")
            {
                // Find next "/" (circular)
                int firstSlashIndex = textBox.Text.IndexOf('/');
                int secondSlashIndex = textBox.Text.IndexOf('/', firstSlashIndex + 1);
                if (firstSlashIndex >= 0 && secondSlashIndex >= 0)
                {
                    if (textBox.CaretIndex <= firstSlashIndex)
                    {
                        // Select 2nd field, put caret at beginning of 2nd field
                        textBox.CaretIndex = firstSlashIndex + 1;
                        textBox.SelectionStart = firstSlashIndex + 1;
                        textBox.SelectionLength = secondSlashIndex - firstSlashIndex - 1;
                    }
                    else if (textBox.CaretIndex <= secondSlashIndex)
                    {
                        // Select 3rd field, put caret at beginning of 3rd field
                        textBox.CaretIndex = secondSlashIndex + 1;
                        textBox.SelectionStart = secondSlashIndex + 1;
                        textBox.SelectionLength = textBox.Text.Length - secondSlashIndex - 1;
                    }
                    else
                    {
                        // Select first field
                        textBox.CaretIndex = 0;
                        textBox.SelectionStart = 0;
                        textBox.SelectionLength = firstSlashIndex;
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

        // Update the date dependency property on text change
        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                internalDateUpdate = true;
                Date = DateTime.Parse(textBox.Text);
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
