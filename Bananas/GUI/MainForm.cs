//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BanaData.Database;
using BanaData.Serializations;
using Bananas.GUI.Events;
using Bananas.Properties;

namespace Bananas.GUI
{
    public partial class MainForm : Form
    {
        private readonly Household household;

        public MainForm()
        {
            this.household = new Household();
            InitializeComponent();
        }

        public event EventHandler HouseholdChanged;
        public event AccountClickedEventHandler AccountClicked;

        internal Household Household
        {
            get { return household; }
        }

        // Application start
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set rememebered size
            Size = Settings.Default.MainFormSize;
            splitContainerMain.SplitterDistance = Settings.Default.AccountListUCWidth;

            // Action on account click
            accountListUC.AccountClicked += accountListUC_AccountClicked;

        }

        // Application exit
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save settings
            Settings.Default.MainFormSize = Size;
            Settings.Default.Save();
        }

        // Action when an account is clicked
        public void accountListUC_AccountClicked(object sender, AccountClickedEventArgs e)
        {
            // Propagate the event to local subscribers
            AccountClicked?.Invoke(sender, e);
        }

        #region File menu

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void lastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Converter.ConvertFromQIF("\\users\\bmerlet\\documents\\lab\\projects\\c#\\bananas\\sgbjm.qif", household);

            if (HouseholdChanged != null)
            {
                HouseholdChanged.Invoke(this, EventArgs.Empty);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion
    }
}
