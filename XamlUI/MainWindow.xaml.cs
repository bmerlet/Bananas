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

using BanaData.Logic;
using BanaData.Logic.Dialogs;
using BanaData.Logic.Main;
using Microsoft.Win32;
using Toolbox.UILogic;

namespace XamlUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IGuiServices
    {
        // Logic for this window
        private readonly MainWindowLogic logic;

        public MainWindow()
        {
            // Initialize all components
            InitializeComponent();

            // Create main window logic
            logic = new MainWindowLogic(this);

            // Init subcomponents
            //accountGroup.Init(logic);

            // Set window to location specified by logic (if initialized)
            if (logic.Width > 40 && logic.Height > 40)
            {
                Left = logic.LeftX;
                Top = logic.TopY;
                Width = logic.Width;
                Height = logic.Height;
                //ZZZZ splitContainerMain.SplitterDistance = logic.SplitterX;
                //ZZZZ accountGroup.UpdateSize();
            }

            // Logic is the data context
            DataContext = logic;

            // Memorize size on resize
            SizeChanged += (o, e) =>
            {
                logic.Width = (int)Width;
                logic.Height = (int)Height;
            };

            // Memorize change of location
            LocationChanged += (o, e) =>
            {
                logic.LeftX = (int)Left;
                logic.TopY = (int)Top;
            };

            // Save settings on close
            Closed += (o, e) => logic.SaveUserSettings();
        }

        public bool ShowDialog(LogicBase logic)
        {
            // Windows-provided dialogs
            if (logic is OpenFileLogic openFileLogic)
            {
                return ShowOpenFileDialog(openFileLogic);
            }

            throw new NotImplementedException();
        }

        // Show open file dialog
        private bool ShowOpenFileDialog(OpenFileLogic logic)
        {
            string dir = logic.InitialDirectory;
            if (!System.IO.Directory.Exists(dir))
            {
                dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            var ofd = new OpenFileDialog()
            {
                Filter = logic.Filter,
                FilterIndex = 1,
                InitialDirectory = dir,
                FileName = logic.File,
                Multiselect = false
            };

            bool result = ofd.ShowDialog() == true;
            logic.File = result ? ofd.FileName : null;

            return result;
        }

    }
}
