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
        // Graphical constants
        private const int rowHeight = 20;
        private const int scrollBarWidth = 23;

        // Logic for this form
        private readonly MainWindow logic;

        public MainForm()
        {
            // Initialize all components
            InitializeComponent();

            // Create main window logic
            logic = new MainWindow(this);

            // Subscribe to main window events
            logic.PropertyChanged += OnPropertyChanged;
            logic.BankAccountGroup.PropertyChanged += OnBankAccountGroupPropertyChanged;
            logic.InvestmentAccountGroup.PropertyChanged += OnInvestmentAccountGroupPropertyChanged;
            logic.AssetAccountGroup.PropertyChanged += OnAssetAccountGroupPropertyChanged;
        }

        #region Menu actions

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logic.MainMenu.Open.Execute();
        }

        #endregion

        #region Changes from logic

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "NetWorth":
                    labelNetWorthValue.Text = logic.NetWorth;
                    UpdateAccountGroupSize(tableLayoutPanelNetWorth);
                    break;
            }
        }

        private void OnBankAccountGroupPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateAccountGroup(tableLayoutPanelBankAccounts, logic.BankAccountGroup);
        }

        private void OnInvestmentAccountGroupPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateAccountGroup(tableLayoutPanelInvestmentAccounts, logic.InvestmentAccountGroup);
        }

        private void OnAssetAccountGroupPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateAccountGroup(tableLayoutPanelAssetAccounts, logic.AssetAccountGroup);
        }

        // Go over the account table and (re)create what is missing
        private void UpdateAccountGroup(TableLayoutPanel tbl, AccountGroup grp)
        {
            UpdateAccountGroupLabel(tbl, 0, 0, grp.Header, false, 2);

            int row = 1;
            foreach (var acctAndBal in grp.AccountsAndBalances)
            {
                bool recreate = false;

                var existingAcct = tbl.GetControlFromPosition(0, row);
                if (existingAcct == null)
                {
                    recreate = true;
                }
                else
                {
                    if (existingAcct is LinkLabel linkLbl)
                    {
                        // Just update account name
                        linkLbl.Text = acctAndBal.AccountName;
                    }
                    else
                    {
                        tbl.Controls.Remove(existingAcct);
                        recreate = true;
                    }
                }

                if (recreate)
                {
                    tbl.Controls.Add(new LinkLabel { Text = acctAndBal.AccountName, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
                }

                UpdateAccountGroupLabel(tbl, 1, row, acctAndBal.Balance, true, 1);

                // Add missing row styles
                if (tbl.RowStyles.Count <= row)
                {
                    tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
                }

                row++;
            }

            // Balance
            UpdateAccountGroupLabel(tbl, 0, row, grp.Footer, false, 1);
            UpdateAccountGroupLabel(tbl, 1, row, grp.Balance, true, 1);
            row++;

            // Truncate extra rows
            tbl.RowCount = row;

            // Set size
            UpdateAccountGroupSize(tbl);
        }

        private void UpdateAccountGroupLabel(TableLayoutPanel tbl, int col, int row, string text, bool right, int span)
        {
            bool recreate = false;
            var align = right ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            var anchor = right ? Anchor = AnchorStyles.Right : Anchor = AnchorStyles.Left;

            recreate = false;
            var existingControl = tbl.GetControlFromPosition(col, row);
            if (existingControl == null)
            {
                recreate = true;
            }
            else
            {
                if (existingControl is Label lbl)
                {
                    // Just update text
                    if (lbl.Text != text)
                    {
                        lbl.Text = text;
                    }

                    if (span != tbl.GetColumnSpan(lbl))
                    {
                        tbl.SetColumnSpan(lbl, span);
                    }

                    if (lbl.TextAlign != align)
                    {
                        lbl.TextAlign = align;
                    }

                    if (lbl.Anchor != anchor)
                    {
                        lbl.Anchor = anchor;
                    }
                }
                else
                {
                    tbl.Controls.Remove(existingControl);
                    recreate = true;
                }
            }

            if (recreate)
            {
                var lbl = new Label { Text = text, TextAlign = align, AutoSize = true, Anchor = anchor };
                tbl.Controls.Add(lbl, col, row);
                tbl.SetColumnSpan(lbl, span);
            }
        }

        private void UpdateAccountGroupSize(TableLayoutPanel tbl)
        {
            // Set size
            tbl.Size = new Size(splitContainerMain.Panel1.Width - scrollBarWidth, tbl.RowCount * rowHeight);
            for (int r = 0; r < tbl.RowStyles.Count; r++)
            {
                tbl.RowStyles[r].SizeType = SizeType.Absolute;
                tbl.RowStyles[r].Height = rowHeight;
            }
        }

        private void SplitContainerMain_SplitterMoved(object sender, SplitterEventArgs e)
        {
            UpdateAccountGroupSize(tableLayoutPanelBankAccounts);
            UpdateAccountGroupSize(tableLayoutPanelInvestmentAccounts);
            UpdateAccountGroupSize(tableLayoutPanelAssetAccounts);
            UpdateAccountGroupSize(tableLayoutPanelNetWorth);
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
                    FileName = System.IO.Path.GetFileName(openFileLogic.File),
                    InitialDirectory = System.IO.Path.GetDirectoryName(openFileLogic.File),
                    RestoreDirectory = false,
                    Filter = openFileLogic.Extensions,
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
