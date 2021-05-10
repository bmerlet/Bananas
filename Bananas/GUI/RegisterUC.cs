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
using Bananas.GUI.Widgets;

namespace Bananas.GUI
{
    public partial class RegisterUC : UserControl
    {
        private MainForm mainForm;
        private DataTable displayTable;

        public RegisterUC()
        {
            InitializeComponent();
        }

        private void RegisterUC_Load(object sender, EventArgs e)
        {
            mainForm = FindForm() as MainForm;
            if (mainForm != null)
            {
                mainForm.AccountClicked += mainForm_AccountClicked;

                /*
                int w = 6;
                this.dataGridView.Columns.Clear();
                this.dataGridView.AutoGenerateColumns = true;
                if (w == 0)
                {
                    this.dataGridView.DataSource = mainForm.Household.Accounts;
                    this.dataGridView.Columns["AccountID"].Visible = false;
                    this.dataGridView.Columns["AccountName"].DisplayIndex = 0;
                    this.dataGridView.Columns["AccountDescription"].Visible = false;
                    this.dataGridView.Columns["AccountType"].DisplayIndex = 1;
                    this.dataGridView.Columns["Kind"].Visible = false;
                }
                else if (w == 1)
                {
                    this.dataGridView.DataSource = mainForm.Household.Categories;
                    //this.dataGridView1.Columns["CategoryID"].Visible = false;
                    //this.dataGridView1.Columns["CategoryParent"].DisplayIndex = 0;
                    //this.dataGridView1.Columns["CategoryName"].DisplayIndex = 0;
                    //this.dataGridView1.Columns["CategoryDescription"].Visible = false;
                    //this.dataGridView1.Columns["CategoryIncome"].DisplayIndex = 1;
                    //this.dataGridView1.Columns["CategoryTax"].Visible = false;
                }
                else if (w == 2)
                {
                    this.dataGridView.DataSource = mainForm.Household.Securities;
                }
                else if (w == 3)
                {
                    this.dataGridView.DataSource = mainForm.Household.SecurityPrices;
                }
                else if (w == 4)
                {
                    this.dataGridView.DataSource = mainForm.Household.Transactions;
                }
                else if (w == 5)
                {
                    var household = mainForm.Household;

                    // EnumerableRowCollection<DataRow> query =
                    var query =
                        from trans in household.Transactions.AsEnumerable()
                        join account in household.Accounts.AsEnumerable()
                        on trans.Field<int>("BankTransAccountID") equals account.Field<int>("AccountID")
                        orderby trans.Field<DateTime>("BankTransDate")
                        select new
                        {
                            AccountName = trans.Field<string>("AccountName"),
                            TransDate = trans.Field<DateTime>("BankTransDate"),
                            TransPayee = trans.Field<string>("BankTransPayee")
                        };

                    var query2 = query as EnumerableRowCollection<DataRow>;
                    DataView view = query2.AsDataView();
                    this.dataGridView.DataSource = query;
                }
                else if (w == 6)
                {
                    // Define table to display
                    this.displayTable = new DataTable();
                    displayTable.Columns.Add("Date", typeof(DateTime));
                    displayTable.Columns.Add("Type", typeof(string));
                    displayTable.Columns.Add("Payee", typeof(string));
                    displayTable.Columns.Add("Memo", typeof(string));
                    displayTable.Columns.Add("Category", typeof(string));
                    displayTable.Columns.Add("Payment", typeof(string));
                    displayTable.Columns.Add("Status", typeof(string));
                    displayTable.Columns.Add("Deposit", typeof(string));
                    displayTable.Columns.Add("Balance", typeof(decimal));

                    this.dataGridView.DataSource = displayTable;

                    mainForm.HouseholdChanged += mainForm_HouseholdChanged;
                }*/
            }
        }

        void mainForm_AccountClicked(object sender, Events.AccountClickedEventArgs e)
        {
            // Fetch the account we are working with
            var household = mainForm.Household;
            var account = household.Accounts.FindByID(e.AccountID);

            // Update the account name label
            labelAccount.Text = "Account: " + account.Name;

            // banking/investment/assets switch
            if (account.Type == EAccountType.Investment)
            {
                LoadInvestmentAccount(account);
            }
            else if (account.Type == EAccountType.OtherAsset || account.Type == EAccountType.OtherLiability)
            {
                LoadAssetAccount(account);
            }
            else
            {
                LoadBankingAccount(account);
            }
        }

        private void LoadBankingAccount(Household.AccountsRow account)
        {
            // Define table to display along with grid view editors
            this.displayTable = new DataTable();
            this.dataGridView.AutoGenerateColumns = false;
            this.dataGridView.Columns.Clear();

            var rightAlignedCellStyle = new DataGridViewCellStyle() { Alignment =DataGridViewContentAlignment.MiddleRight };

            displayTable.Columns.Add("Date", typeof(DateTime));
            {
                var col = new CalendarColumn();
                col.Name = "Date";
                col.DataPropertyName = "Date";
                this.dataGridView.Columns.Add(col);
            }

            if (account.Type == EAccountType.Bank)
            {
                displayTable.Columns.Add("Type", typeof(string));
                var col = new DataGridViewTextBoxColumn();
                col.Name = "Type";
                col.DataPropertyName = "Type";
                this.dataGridView.Columns.Add(col);
            }

            displayTable.Columns.Add("Payee", typeof(string));
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = "Payee";
                col.DataPropertyName = "Payee";
                this.dataGridView.Columns.Add(col);
            }

            displayTable.Columns.Add("Memo", typeof(string));
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = "Memo";
                col.DataPropertyName = "Memo";
                this.dataGridView.Columns.Add(col);
            }

            displayTable.Columns.Add("Category", typeof(string));
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = "Category";
                col.DataPropertyName = "Category";
                this.dataGridView.Columns.Add(col);
            }

            displayTable.Columns.Add("Payment", typeof(string));
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = "Payment";
                col.DataPropertyName = "Payment";
                col.DefaultCellStyle = rightAlignedCellStyle;
                this.dataGridView.Columns.Add(col);
            }

            displayTable.Columns.Add("Status", typeof(string));
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = "Status";
                col.DataPropertyName = "Status";
                this.dataGridView.Columns.Add(col);
            }

            displayTable.Columns.Add("Deposit", typeof(string));
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = "Deposit";
                col.DefaultCellStyle = rightAlignedCellStyle;
                col.DataPropertyName = "Deposit";
                this.dataGridView.Columns.Add(col);
            }

            displayTable.Columns.Add("Balance", typeof(string));
            {
                var col = new DataGridViewTextBoxColumn();
                col.Name = "Balance";
                col.DefaultCellStyle = rightAlignedCellStyle;
                col.DataPropertyName = "Balance";
                this.dataGridView.Columns.Add(col);
            }

            // For now
            //this.dataGridView.AutoGenerateColumns = true;

            this.dataGridView.DataSource = displayTable;


            // Load the transactions
            LoadBankingTransactions(account);
        }

        private void LoadInvestmentAccount(Household.AccountsRow account)
        {
            this.dataGridView.Columns.Clear();
        }

        private void LoadAssetAccount(Household.AccountsRow account)
        {
            this.dataGridView.Columns.Clear();
        }

        void mainForm_HouseholdChanged(object sender, EventArgs e)
        {
            // Forget about everything when a new DB is loaded.
            this.labelAccount.Text = "Account: -";
            this.dataGridView.Columns.Clear();
        }

        public void LoadBankingTransactions(Household.AccountsRow acc)
        {
            // Get all transactions for an account and put them in the table
            var household = mainForm.Household;
            decimal balance = 0;

            var accTransRel = household.Relations["FK_Accounts_Transactions"];
            foreach (var transRow in acc.GetChildRows(accTransRel))
            {
                var trans = transRow as Household.TransactionsRow;
                Household.BankingTransactionsRow transBank = null;
                if (acc.Type == EAccountType.Bank)
                {
                    transBank = household.BankingTransactions.GetByTransaction(trans);
                }
                var lineItems = household.LineItems.GetByTransaction(trans);
                decimal amount = lineItems.Sum(li => li.Amount);
                balance += amount;
                string category = "";

                if (lineItems.Length > 1)
                {
                    category = "<Split>";
                }
                else if (lineItems[0].IsTransfer)
                {
                    if (!lineItems[0].IsAccountIDNull())
                    {
                        var destAccount = household.Accounts.FindByID(lineItems[0].AccountID);
                        category = "[" + destAccount.Name + "]";
                    }
                }
                else
                {
                    if (!lineItems[0].IsCategoryIDNull())
                    {
                        var destCategory = household.Categories.FindByID(lineItems[0].CategoryID);
                        category = destCategory.FullName;
                    }
                }

                string memo = (lineItems.Length  == 1) ? (lineItems[0].IsMemoNull() ? "" : lineItems[0].Memo) : "";

                var dr = displayTable.NewRow();

                dr["Date"] = trans.Date;
                if (transBank != null)
                {
                    dr["Type"] = transBank.GetRegisterMediumString();
                }
                dr["Payee"] = (trans.IsPayeeNull()) ? "" : trans.Payee;
                dr["Memo"] = memo;
                dr["Category"] = category;
                dr["Payment"] = (amount >= 0) ? "" : (-amount).ToString("N");
                dr["Status"] = (trans.Status == ETransactionStatus.Reconciled) ? "R" : ((trans.Status == ETransactionStatus.Cleared) ? "c" : " ");
                dr["Deposit"] = (amount >= 0) ? amount.ToString("N") : "";
                dr["Balance"] = balance.ToString("N");

                displayTable.Rows.Add(dr);
            }

            this.dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);
        }
    }
}
