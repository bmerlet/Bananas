using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BanaData.Logic;
using BanaData.Logic.Main;
using BanaData.Logic.Dialogs;
using Toolbox.UILogic;

namespace WinformsUI.Forms
{
    public partial class MainForm : Form, IGuiServices
    {
        // Logic for this form
        private readonly MainWindowLogic logic;

        public MainForm()
        {
            // Initialize all components
            InitializeComponent();

            // Create main window logic
            logic = new MainWindowLogic(this);

            // Init subcomponents
            accountGroup.Init(logic);

            // Set window to location specified by logic (if initialized)
            if (logic.Width > 40 && logic.Height > 40)
            {
                SetBounds(logic.LeftX, logic.TopY, logic.Width, logic.Height);
                //splitContainerMain.SplitterDistance = logic.SplitterX;
                accountGroup.UpdateSize();
            }

            // Initial state
            closedAccountsToolStripMenuItem.Checked = logic.MainMenuLogic.ShowClosedAccounts;

            // Subscribe to main window events
            logic.PropertyChanged += OnPropertyChanged;
        }

        #region Menu actions

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logic.MainMenuLogic.Open.Execute();
        }

        #endregion

        #region Changes from logic

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        #endregion

        #region Changes from user

        private void OnClosedAccountsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logic.MainMenuLogic.ShowClosedAccounts = closedAccountsToolStripMenuItem.Checked;
            accountGroup.UpdateSize();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            logic.SplitterX = splitContainerMain.SplitterDistance;
            accountGroup.UpdateSize();
        }

        private void SplitContainerMain_SplitterMoved(object sender, SplitterEventArgs e)
        {
            accountGroup.UpdateSize();
            logic.SplitterX = splitContainerMain.SplitterDistance;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            logic.LeftX = Left;
            logic.TopY = Top;
            logic.Width = Width;
            logic.Height = Height;
            logic.SaveUserSettings();
        }

        #endregion

        #region IGuiServices implementation

        public bool ShowDialog(LogicBase logic)
        {
            CommonDialog dialog = null;

            if (logic is OpenFileLogic openFileLogic)
            {
                dialog = new OpenFileDialog()
                {
                    FileName = openFileLogic.File,
                    InitialDirectory = openFileLogic.InitialDirectory,
                    RestoreDirectory = false,
                    Filter = openFileLogic.Filter,
                    FilterIndex = 1
                };
            }

            if (dialog == null)
            {
                throw new NotImplementedException();
            }

            return dialog.ShowDialog() == DialogResult.OK;
        }

        #endregion
    }
}
