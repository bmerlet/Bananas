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
    //    -> RESOLVED: SelectedItem is then null 
    // - Button at bottom of list
    //
    public class AutoCompleteTextBox : Control
    {
        #region Constructors

        // Change the metadata type of this class to AutoCompleteTextBox, so that WPF loads
        // the proper default style (from Themes\Generic.xaml)
        static AutoCompleteTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(typeof(AutoCompleteTextBox)));
        }

        #endregion

        #region Dependency properties

        //
        // Text (bound to the textbox' property of the same name)
        //
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        //
        // ItemsSource (bound to the listbox' property of the same name)
        //
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(Enumerable.Empty<object>()));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        //
        // Selected Item (bound to the listbox' property of the same name)
        //
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(null));

        public string SelectedItem
        {
            get => (string)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        //
        // Selection command. Activated when an item is chosen
        //
        public static readonly DependencyProperty ItemSelectedCommandProperty =
            DependencyProperty.Register("ItemSelectedCommand", typeof(ICommand), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(null));

        public ICommand ItemSelectedCommand
        {
            get => (ICommand)GetValue(ItemSelectedCommandProperty);
            set => SetValue(ItemSelectedCommandProperty, value);
        }

        //
        // Item template property (bound to the listbox' property of the same name)
        //
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(null));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        //
        // Item template selector property (bound to the listbox' property of the same name)
        //
        public static readonly DependencyProperty ItemTemplateSelectorProperty =
            DependencyProperty.Register("ItemTemplateSelector", typeof(DataTemplateSelector), typeof(AutoCompleteTextBox));

        public DataTemplateSelector ItemTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty);
            set => SetValue(ItemTemplateSelectorProperty, value);
        }

        //
        // Listbox width (bound to the listbox' width property)
        //
        public static readonly DependencyProperty ListBoxWidthProperty =
            DependencyProperty.Register("ListBoxWidth", typeof(double), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(double.NaN));

        public double ListBoxWidth
        {
            get => (double)GetValue(ListBoxWidthProperty);
            set => SetValue(ListBoxWidthProperty, value);
        }


        //
        // Listbox height (bound to the listox' Height property)
        //
        public static readonly DependencyProperty ListBoxHeightProperty =
            DependencyProperty.Register("ListBoxHeight", typeof(double), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(double.NaN));

        public double ListBoxHeight
        {
            get => (double)GetValue(ListBoxHeightProperty);
            set => SetValue(ListBoxHeightProperty, value);
        }

        //
        // Watermark (bound to the WatermarkTextBox property of the same name)
        //
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register("Watermark", typeof(string), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(string.Empty));

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        //
        // Display Member: Name of the property of the listbox items which content is copied to the editor on selection.
        //
        public static readonly DependencyProperty DisplayMemberProperty =
            DependencyProperty.Register("DisplayMember", typeof(string), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(string.Empty));

        public string DisplayMember
        {
            get => (string)GetValue(DisplayMemberProperty);
            set => SetValue(DisplayMemberProperty, value);
        }

        //
        // IsTextFromItemsSourceOnly: Only allow text that is in the items source (like a combo box)
        //
        public static readonly DependencyProperty IsTextFromItemsSourceOnlyProperty =
            DependencyProperty.Register("IsTextFromItemsSourceOnly", typeof(bool), typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(false));

        public bool IsTextFromItemsSourceOnly
        {
            get => (bool)GetValue(IsTextFromItemsSourceOnlyProperty);
            set => SetValue(IsTextFromItemsSourceOnlyProperty, value);
        }

        #endregion

        #region Private fields

        //
        // Name of the controls in the control template
        //
        private const string editorPartName = "PART_Editor";
        private const string popupPartName = "PART_Popup";
        private const string selectorPartName = "PART_Selector";

        //
        // The components of the control we keep track of
        //
        private TextBox editor;
        private Popup popup;
        private ListBox selector;

        #endregion

        #region Popup control

        public bool IsPopupOpen
        {
            get => popup != null && popup.IsOpen;
            set
            {
                if (popup != null)
                {
                    // Written this way so that one can set a breakpoint to know who is closing/opening
                    if (value)
                    {
                        popup.IsOpen = true;
                    }
                    else
                    {
                        popup.IsOpen = false;
                    }
                }
            }
        }

        #endregion

        #region Initialization

        //
        // Action when the control template is applied
        //
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Find the parts defined in the control template
            editor = Template.FindName(editorPartName, this) as TextBox;
            popup = Template.FindName(popupPartName, this) as Popup;
            selector = Template.FindName(selectorPartName, this) as ListBox;

            GotFocus += OnAutoCompleteTextBoxGotFocus;

            if (editor != null)
            {
                editor.TextChanged += OnEditorTextChanged;
                editor.LostFocus += OnEditorLostFocus;
                editor.PreviewKeyDown += OnEditorPreviewKeyDown;
            }

            if (popup != null)
            {
                //popup.StaysOpen = false;
                popup.StaysOpen = true;
            }

            if (selector != null)
            {
                selector.PreviewMouseDown += OnSelectorPreviewMouseDown;
            }
        }

        #endregion

        #region Editor handlers

        //
        // Got focus on this control: Focus on the editor and select the text
        //
        private void OnAutoCompleteTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (editor != null)
            {
                // Set the filter on the collection to the filter for THIS autocomplete text box.
                // The selector's item collection may be shared by multiple autocomplete text box,
                // the one with the focus needs to impose its own filter
                selector.Items.Filter = Filter;

                editor.Focus();
                RefreshSelector();
            }
        }

        //
        // Lost focus: close popup if really lost focus
        private void OnEditorLostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                IsPopupOpen = false;
                selector.Items.Filter = null;
            }
        }

        //
        // Text has changed in the editor: Update the list of choices
        //
        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            // Update autocomplete list
           RefreshSelector();
        }

        //
        // Manage special keystrokes
        //
        private void OnEditorPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    // On escape key, close popup
                    if (IsPopupOpen)
                    {
                        IsPopupOpen = false;
                        //editor.Focus(); ZZZZ Probably not needed

                        // Set handled ONLY if we closed the popup,
                        // so that the transaction can be cancelled via another Esc
                        e.Handled = true;
                    }
                    break;

                case Key.Enter:
                case Key.Tab:
                    // Autocomplete text if possible
                    if (selector.Items.Count == 1)
                    {
                        // Only one choice left
                        selector.SelectedIndex = 0;
                    }
                    else if (IsTextFromItemsSourceOnly && selector.SelectedItem == null)
                    {
                        // Gotta select something
                        if (selector.Items.Count > 0)
                        {
                            selector.SelectedIndex = 0;
                        }
                        else
                        {
                            // Nothing matching, consume the tab event so that the user
                            // stays on this box
                            e.Handled = true;
                            return;
                        }
                    }

                    // Publish selection
                    PublishSelection();

                    // ZZZZ Doesn't work
                    //if (e.Key == Key.Tab)
                    //{
                    //    editor.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    //}
                    break;

                case Key.Down:
                    if (IsPopupOpen && selector.Items.Count > 0)
                    {
                        // Move selection down in the listbox, or select top one if none selected
                        if (selector.SelectedIndex < 0)
                        {
                            selector.SelectedIndex = 0;
                            selector.ScrollIntoView(selector.SelectedItem);
                        }
                        else if (selector.SelectedIndex < (selector.Items.Count - 1))
                        {
                            selector.SelectedIndex += 1;
                            selector.ScrollIntoView(selector.SelectedItem);
                        }
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    if (IsPopupOpen && selector.Items.Count > 0)
                    {
                        // Move selection up in the listbox, or select bottom one if none selected
                        if (selector.SelectedIndex < 0)
                        {
                            selector.SelectedIndex = selector.Items.Count - 1;
                            selector.ScrollIntoView(selector.SelectedItem);
                        }
                        else if (selector.SelectedIndex > 0)
                        {
                            selector.SelectedIndex -= 1;
                            selector.ScrollIntoView(selector.SelectedItem);
                        }
                        e.Handled = true;
                    }
                    break;
            }
        }

        //
        // Evaluate text based on listbox selected item and display member
        //
        private void PublishSelection()
        {
            var item = selector.SelectedItem;
            if (item == null)
            {
                Text = editor.Text;
            }
            else
            {
                if (string.IsNullOrEmpty(DisplayMember))
                {
                    Text = item.ToString();
                }
                else
                {
                    Text = BindingEvaluator.GetValue(item, DisplayMember);
                }

                if (ItemSelectedCommand != null)
                {
                    ItemSelectedCommand.Execute(item);
                }
            }
        }

        #endregion

        #region Selector handlers

        //
        // Filter applied to listbox' items
        //
        private bool Filter(object o)
        {
            var curTxt = editor.Text;

            // Show everything if nothing to filter on
            if (curTxt == null || curTxt == "")
            {
                return true;
            }

            // Get string to filter on from input object 
            string itemString;
            if (string.IsNullOrEmpty(DisplayMember))
            {
                itemString = o.ToString();
            }
            else
            {
                itemString = BindingEvaluator.GetValue(o, DisplayMember);
            }

            return itemString.IndexOf(curTxt, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        //
        // Re-apply filter to listbox item
        //
        private void RefreshSelector()
        {
            if (selector?.ItemsSource != null && popup != null && IsKeyboardFocusWithin)
            {
                // Apply filter to the listbox' items
                CollectionViewSource.GetDefaultView(selector.ItemsSource).Refresh();
                IsPopupOpen = selector.Items.Count > 0;
            }
        }

        //
        // Mouse up on the selector: Publish selection and close up
        //
        private void OnSelectorPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Loop through the item containers
            if (selector.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                foreach (var item in selector.Items)
                {
                    if (selector.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container && container.IsMouseOver)
                    {
                        // This item has the mouse over it, use it.
                        container.IsSelected = true;
                        PublishSelection();
                        IsPopupOpen = false;
                        e.Handled = true;
                        break;
                    }
                }
            }
        }

        #endregion

        #region Binding evaluator

        //
        // Class to apply a binding to an arbitrary object and read the value pointed to by the binding
        //
        class BindingEvaluator : FrameworkElement
        {
            // Dependency property on which the binding is applied
            static public DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(string), typeof(BindingEvaluator), new FrameworkPropertyMetadata(string.Empty));

            static public string GetValue(object item, string bindingPath)
            {
                // Create a dummy framework element, with its data context pointing to the item
                FrameworkElement element = new FrameworkElement
                {
                    DataContext = item
                };

                // Bind the value property using the supplied binding path
                element.SetBinding(ValueProperty, new Binding(bindingPath));

                // Read the value property's value
                var result = element.GetValue(ValueProperty) as string;

                return result;
            }
        }
        #endregion
    }
}
