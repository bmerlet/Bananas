using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace XamlUI.UserControls
{
    //
    // TODO:
    // - Custom action when leaving with something that does not exist in the list
    // - Button at bottom of list
    // - Test if custom colors are necessary
    //
    public class AutoCompleteTextBox : TextBox
    {
        #region Constructors

        // Static constructor
        static AutoCompleteTextBox()
        {
            // Change the metadata type of this class to AutoCompleteTextBox, so that WPF loads
            // the proper default style (from Themes\Generic.xaml)
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(typeof(AutoCompleteTextBox)));
        }

        #endregion

        #region Dependency properties

        //
        // Dependency properties are exported to the markup and can be used for styling, binding, ...
        //

        #region Listbox ItemsSource Dependency Property

        //
        // ListBoxItemsSource represents the source of choices for autocompletion
        //

        public IEnumerable ListBoxItemsSource
        {
            get { return (IEnumerable)GetValue(ListBoxItemsSourceProperty); }
            set { SetValue(ListBoxItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ListBoxItemsSourceProperty =
            DependencyProperty.Register("ListBoxItemsSource", typeof(IEnumerable), typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(Enumerable.Empty<object>(), OnListBoxItemsSourceChanged));

        // Listbox item source change dispatcher
        private static void OnListBoxItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteTextBox actb)
            {
                actb.OnListBoxItemsSourceChanged(e.NewValue as IEnumerable);
            }
        }

        // Item source listener: Assign value to the list box' item source
        protected void OnListBoxItemsSourceChanged(IEnumerable listBoxItemsSource)
        {
            if (listBox != null)
            {
                listBox.ItemsSource = listBoxItemsSource;
            }
        }

        #endregion

        #region Listbox Height Dependency Property

        public double ListBoxHeight
        {
            get { return (double)GetValue(ListBoxHeightProperty); }
            set { SetValue(ListBoxHeightProperty, value); }
        }

        public static readonly DependencyProperty ListBoxHeightProperty =
            DependencyProperty.Register("ListBoxHeight", typeof(double), typeof(AutoCompleteTextBox), new UIPropertyMetadata(double.NaN, OnListBoxHeightChanged));

        // List box height change dispatcher
        private static void OnListBoxHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteTextBox actb)
            {
                actb.OnListBoxHeightChanged((double)e.NewValue);
            }
        }

        private void OnListBoxHeightChanged(double newValue)
        {
            if (listBox != null)
            {
                listBox.Height = newValue;
            }
        }

        #endregion

        #region Listbox Width Dependency Property

        public double ListBoxWidth
        {
            get { return (double)GetValue(ListBoxWidthProperty); }
            set { SetValue(ListBoxWidthProperty, value); }
        }

        public static readonly DependencyProperty ListBoxWidthProperty =
            DependencyProperty.Register("ListBoxWidth", typeof(double), typeof(AutoCompleteTextBox), new UIPropertyMetadata(double.NaN, OnListBoxWidthChanged));

        // List box height change dispatcher
        private static void OnListBoxWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteTextBox actb)
            {
                actb.OnListBoxWidthChanged((double)e.NewValue);
            }
        }

        private void OnListBoxWidthChanged(double newValue)
        {
            if (listBox != null)
            {
                listBox.Width = newValue;
            }
        }

        #endregion

        #region Listbox ItemTemplate Dependency Property

        //
        // Property to access the listbox' ItemTemplate property
        //
        public DataTemplate ListBoxItemTemplate
        {
            get { return (DataTemplate)GetValue(ListBoxItemTemplateProperty); }
            set { SetValue(ListBoxItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ListBoxItemTemplateProperty =
            ItemsControl.ItemTemplateProperty.AddOwner(
                typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(null, OnListBoxItemTemplateChanged));

        private static void OnListBoxItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteTextBox actb)
            {
                actb.OnListBoxItemTemplateChanged(e.NewValue as DataTemplate);
            }
        }

        private void OnListBoxItemTemplateChanged(DataTemplate dataTemplate)
        {
            if (listBox != null)
            {
                listBox.ItemTemplate = dataTemplate;
            }
        }

        #endregion

        #endregion

        #region Private members

        // The popup to show the listbox
        private Popup popup;

        // The list box in the popup
        private ListBox listBox;

        #endregion

        #region Overrides

        // Retrieve the popup and listbox from control template when it is applied
        // And start listening to changes
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            popup = Template.FindName("PART_Popup", this) as Popup;
            listBox = Template.FindName("PART_ListBox", this) as ListBox;
            if (listBox != null)
            {
                // Filter the listbox content based on the textbox content
                listBox.Items.Filter = Filter;

                // Listen to listbox keystrokes and mouse clicks
                listBox.KeyDown += OnListBoxKeyDown;
                listBox.MouseUp += OnListBoxMouseUp;

                // Apply listbox properties hosted by this control 
                OnListBoxItemsSourceChanged(ListBoxItemsSource);
                OnListBoxHeightChanged(ListBoxHeight);
                OnListBoxWidthChanged(ListBoxWidth);
                OnListBoxItemTemplateChanged(ListBoxItemTemplate);

                // ZZZ RFU
                //OnItemContainerStyleChanged(ItemContainerStyle);
                //OnItemTemplateSelectorChanged(ItemTemplateSelector);
            }
        }

        // Filter applied to listbox' items
        private bool Filter(object o)
        {
            if (o == null)
            {
                return false;
            }

            // Get current text from textbox
            var curTxt = Text;

            // Show everything if nothing to filter on
            if (curTxt == null || curTxt == "")
            {
                return true;
            }

            // Filter on whatever is returned by o.ToString()
            return o.ToString().IndexOf(curTxt, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // React to getting focus
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            RefreshListBox();
        }

        // React to text change
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            RefreshListBox();
        }

        private void RefreshListBox()
        {
            if (listBox?.ItemsSource != null && popup != null)
            {
                // Apply filter to listBox.Items.Filter
                CollectionViewSource.GetDefaultView(listBox.ItemsSource).Refresh();
                popup.IsOpen = listBox.Items.Count > 0;
            }
        }

        // Close popup on focus loss, except if the new focus is the listbox
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (popup != null)
            {
                var focusScope = FocusManager.GetFocusScope(this);
                var focusedElement = FocusManager.GetFocusedElement(focusScope);
                bool listBoxFocus =
                    focusedElement == listBox ||
                    (focusedElement is ListBoxItem item && listBox.Items.Contains(item.Content));

                if (!listBoxFocus)
                {
                    popup.IsOpen = false;
                }
            }
        }

        // React to special keystrokes on textbox
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            var fs = FocusManager.GetFocusScope(this);
            var focusedElement = FocusManager.GetFocusedElement(fs);

            if (e.Key == Key.Escape)
            {
                // On escape key, close popup and re-focus on textbox
                if (popup != null)
                {
                    popup.IsOpen = false;
                }
                Focus();
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                // On down key, slide the focus to the listbox
                if (listBox != null && focusedElement == this)
                {
                    listBox.Focus();
                }
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Tab)
            {
                // Autocomplete text if only one choice left
                if (listBox.Items.Count == 1)
                {
                    Text = listBox.Items[0] as string;
                }
            }
        }

        // React to special keystrokes on listbox
        private void OnListBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Tab)
            {
                e.Handled = true;

                // Set text to selected list box item
                Text = listBox.SelectedItem as string;

                if (e.Key == Key.Tab)
                {
                    // For the tab key, move to the next field
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
                else
                {
                    // For enter, re-focus to textbox
                    Focus();
                }

                // Force-close the popup
                popup.IsOpen = false;
            }
        }

        private void OnListBoxMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Set text to selected list box item
            Text = listBox.SelectedItem as string;

            // Re-focus to textbox
            Focus();

            // Force-close the popup
            popup.IsOpen = false;
        }

        #endregion

    }
}
