using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace XamlUI.Widgets
{
    class WatermarkTextBox : TextBox
    {
        #region Constructors

        // Change the metadata type of this class to WatermarkTextBox, so that WPF loads
        // the proper default style (from Themes\Generic.xaml)
        static WatermarkTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WatermarkTextBox), new FrameworkPropertyMetadata(typeof(WatermarkTextBox)));
        }

        #endregion

        #region Dependency Properties

        //
        // Watermark property: String that appears faintly through the (transparent) textbox when it is empty
        //
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register("Watermark", typeof(string), typeof(WatermarkTextBox), new FrameworkPropertyMetadata(string.Empty));

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        //
        // Watermark template (how to display the watermark)
        //
        public static readonly DependencyProperty WatermarkTemplateProperty =
            DependencyProperty.Register("WatermarkTemplate", typeof(DataTemplate), typeof(WatermarkTextBox), new UIPropertyMetadata(null));

        public DataTemplate WatermarkTemplate
        {
            get => (DataTemplate)GetValue(WatermarkTemplateProperty);
            set => SetValue(WatermarkTemplateProperty, value);
        }

        #endregion
    }
}
