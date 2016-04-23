using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bananas.Data;
using Bananas.GUI.Events;

namespace Bananas.GUI
{
    public partial class AccountListUC : UserControl
    {
        private const int rowHeight = 20;
        private const int rowHeightMargin = 3;
        private MainForm mainForm;
        private ToolTip toolTip;
        private List<int> headerRows;

        public AccountListUC()
        {
            InitializeComponent();
            this.toolTip = new ToolTip();
            this.headerRows = new List<int>();
        }

        internal event AccountClickedEventHandler AccountClicked;

        private void AccountListUC_Load(object sender, EventArgs e)
        {
            // Find the main form
            this.mainForm = FindForm() as MainForm;

            // Ask to be notified of data changes
            if (this.mainForm != null)
            {
                this.mainForm.HouseholdChanged += mainForm_HouseholdChanged;

                // Load the current dataset
                if (mainForm.Household != null)
                {
                    LoadAccounts();
                }
            }
 
        }

        void mainForm_HouseholdChanged(object sender, EventArgs e)
        {
            AutoScrollPosition = new Point(0, 0);
            LoadAccounts();
        }

        private void LoadAccounts()
        {
            var household = mainForm.Household;
            int row = 0;

            // Header
            tableLayoutPanelMain.Controls.Clear();
            headerRows.Clear();
            headerRows.Add(row);
            MakeLabel(0, row++, "Accounts", true);
            MakeLink(0, row++, "All banking Transactions", null, true);

            // Banking accounts
            decimal bankingBalance = 0;
            headerRows.Add(row);
            MakeLabel(0, row++, "Banking Accounts:", true);
            foreach (var acc in household.Accounts.GetBankingAccounts())
            {
                var bal = acc.GetBankingBalance();
                bankingBalance += bal;
                    
                MakeLink(0, row, acc.Name, acc, false);
                MakeLabel(1, row++, bal, false);
            }
            MakeLabel(0, row, "Banking Total:", false);
            MakeLabel(1, row++, bankingBalance, false);

            // Investment accounts
            decimal investmentBalance = 0;
            headerRows.Add(row);
            MakeLabel(0, row++, "Investment Accounts:", true);
            foreach (var acc in household.Accounts.GetInvestmentAccounts())
            {
                var bal = acc.GetInvestmentValue();
                investmentBalance += bal;

                MakeLink(0, row, acc.Name, acc, false);
                MakeLabel(1, row++, bal, false);
            }
            MakeLabel(0, row, "Investment Total:", false);
            MakeLabel(1, row++, investmentBalance, false);

            // Asset accounts
            decimal assetBalance = 0;
            headerRows.Add(row);
            MakeLabel(0, row++, "Assets:", true);
            foreach (var acc in household.Accounts.GetAssetAccounts())
            {
                var bal = acc.GetBankingBalance();
                assetBalance += bal;

                MakeLink(0, row, acc.Name, acc, false);
                MakeLabel(1, row++, bal, false);
            }
            MakeLabel(0, row, "Assets Total:", false);
            MakeLabel(1, row++, assetBalance, false);

            decimal netWorth = bankingBalance + investmentBalance + assetBalance;
            MakeLabel(0, row, "Net Worth:", false);
            MakeLabel(1, row, netWorth, false);

            // Set size of the table and each row
            tableLayoutPanelMain.SetBounds(0, 0, Width, (household.Accounts.Rows.Count + 9) * (rowHeight + rowHeightMargin));
            for (int r = 0; r < tableLayoutPanelMain.RowStyles.Count; r++)
            {
                tableLayoutPanelMain.RowStyles[r].SizeType = SizeType.Absolute;
                tableLayoutPanelMain.RowStyles[r].Height = 20;
            }
        }

        private void MakeLabel(int col, int row, decimal value, bool span)
        {
            MakeLabel(col, row, value.ToString("C"), span, true);
        }

        private void MakeLabel(int col, int row, string text, bool span, bool rightAlign = false)
        {
            var l = new Label();
            l.BackColor = Color.Transparent;
            l.Text = text;
            //l.Anchor = AnchorStyles.None;
            l.TextAlign = (rightAlign) ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;

            tableLayoutPanelMain.Controls.Add(l, col, row);
            if (span)
            {
                tableLayoutPanelMain.SetColumnSpan(l, 2);
            }
        }

        private void MakeLink(int col, int row, string text, object data, bool span)
        {
            var l = new LinkLabel();
            l.Text = text;
            l.Links.Add(0, text.Length, data);
            l.LinkClicked += accountLink_LinkClicked;
            //l.Anchor = AnchorStyles.Left;
            l.Width = l.PreferredWidth;
            tableLayoutPanelMain.Controls.Add(l, col, row);
            if (span)
            {
                tableLayoutPanelMain.SetColumnSpan(l, 2);
            }
            toolTip.SetToolTip(l, text);
        }

        void accountLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var acc = e.Link.LinkData as Household.AccountsRow;
            if (AccountClicked != null)
            {
                AccountClicked.Invoke(this, new AccountClickedEventArgs(acc.ID));
            }
        }

        private void tableLayoutPanelMain_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            if (headerRows.Contains(e.Row))
            {
                Graphics g = e.Graphics;
                Rectangle r = e.CellBounds;
                g.FillRectangle(SystemBrushes.Control, r);
            }
        }
    }
}
