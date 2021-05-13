using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BanaData.Logic.Main;

namespace WinformsUI.UserControls
{
    public partial class AccountGroup : UserControl
    {
        #region Private members

        // Graphical constants
        private const int rowHeight = 20;
        private const int scrollBarWidth = 23;

        private MainWindowLogic logic;

        #endregion

        #region Constructor

        public AccountGroup()
        {
            InitializeComponent();
        }

        public void Init(MainWindowLogic logic)
        {
            this.logic = logic;

            // Subscribe to logic events
            logic.PropertyChanged += OnPropertyChanged;
            logic.BankAccountGroup.PropertyChanged += OnBankAccountGroupPropertyChanged;
            logic.InvestmentAccountGroup.PropertyChanged += OnInvestmentAccountGroupPropertyChanged;
            logic.AssetAccountGroup.PropertyChanged += OnAssetAccountGroupPropertyChanged;

            // To be clean, we should also subscribe to collection changhed events

            // Force init
            UpdateAccountGroup(tableLayoutPanelBankAccounts, logic.BankAccountGroup);
            UpdateAccountGroup(tableLayoutPanelInvestmentAccounts, logic.InvestmentAccountGroup);
            UpdateAccountGroup(tableLayoutPanelAssetAccounts, logic.AssetAccountGroup);
            UpdateSize();
        }

        #endregion

        #region Events

        public class AccountClickedEventArgs : EventArgs
        {
            public AccountClickedEventArgs(int id)
            {
                AccountID = id;
            }

            public readonly int AccountID;
        }

        public EventHandler<AccountClickedEventArgs> AccountClicked;

        #endregion

        #region React to logic changes

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

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "NetWorth":
                    labelNetWorthValue.Text = logic.NetWorth;
                    UpdateSize(tableLayoutPanelNetWorth);
                    break;
            }
        }

        // Go over the account table and (re)create what is missing
        private void UpdateAccountGroup(TableLayoutPanel tbl, AccountGroupLogic grp)
        {
            UpdateLabel(tbl, 0, 0, grp.Header, false, 2);

            int row = 1;
            foreach (var acctAndBal in grp.AccountsAndBalances)
            {
                UpdateAccountGroupLink(tbl, row, acctAndBal.AccountName, acctAndBal.AccountID);
                UpdateLabel(tbl, 1, row, acctAndBal.Balance, true, 1);

                row++;
            }

            // Balance
            UpdateLabel(tbl, 0, row, grp.Footer, false, 1);
            UpdateLabel(tbl, 1, row, grp.Balance, true, 1);
            row++;

            // Add missing row styles
            while (tbl.RowStyles.Count <= row)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
            }

            // Truncate extra rows
            tbl.RowCount = row;

            // Set size
            UpdateSize(tbl);
        }

        private void UpdateLabel(TableLayoutPanel tbl, int col, int row, string text, bool right, int span)
        {
            bool recreate = false;
            var align = right ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            var anchor = right ? Anchor = AnchorStyles.Right : Anchor = AnchorStyles.Left;

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

        private void UpdateAccountGroupLink(TableLayoutPanel tbl, int row, string text, int id)
        {
            bool recreate = false;

            var existingControl = tbl.GetControlFromPosition(0, row);
            if (existingControl == null)
            {
                recreate = true;
            }
            else
            {
                if (existingControl is LinkLabel linkLbl)
                {
                    if (linkLbl.Text != text)
                    {
                        linkLbl.Text = text;
                    }

                    if ((int)(linkLbl.Links[0].LinkData) != id)
                    {
                        linkLbl.Links[0].LinkData = id;
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
                var lnk = new LinkLabel { Text = text, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left, AutoSize = true };
                lnk.Links.Add(0, text.Length, id);
                tbl.Controls.Add(lnk, 0, row);

                lnk.LinkClicked += OnAccountLinkClicked;
            }
        }

        private void OnAccountLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AccountClicked?.Invoke(this, new AccountClickedEventArgs((int)(e.Link.LinkData)));
        }

        #endregion

        #region Resize

        public void UpdateSize()
        {
            UpdateSize(tableLayoutPanelBankAccounts);
            UpdateSize(tableLayoutPanelInvestmentAccounts);
            UpdateSize(tableLayoutPanelAssetAccounts);
            UpdateSize(tableLayoutPanelNetWorth);

            // ZZZZ Why?
            Dock = DockStyle.Fill;
        }

        private void UpdateSize(TableLayoutPanel tbl)
        {
            // Set size
            int width = this.Parent.Width;
            tbl.Size = new Size(width - scrollBarWidth, tbl.RowCount * rowHeight);

            // This helps the table, but should not be necessary
            for (int r = 0; r < tbl.RowStyles.Count; r++)
            {
                tbl.RowStyles[r].SizeType = SizeType.Absolute;
                tbl.RowStyles[r].Height = rowHeight;
            }
        }

        #endregion
    }
}
