using System;
using System.Collections.Generic;
using System.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Database;

namespace BanaData.Logic.Main
{
    public class BankRegisterLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public BankRegisterLogic(MainWindowLogic mainWindowLogic)
        {
            this.mainWindowLogic = mainWindowLogic;
        }

        #endregion

        #region UI properties

        public string AccountName { get; private set; }

        public DataTable TransactionsDT { get; } = new DataTable();
        public ObservableCollection<BankingTransaction> Transactions { get; } = new ObservableCollection<BankingTransaction>();

        #endregion

        #region Actions

        public void SetAccount(int accountID)
        {
            // Get the account details
            var household = mainWindowLogic.Household;
            var account = household.Accounts.FindByID(accountID);

            AccountName = account.Name;
            OnPropertyChanged(() => AccountName);

            // Format data table
            Transactions.Clear();
            TransactionsDT.Clear();
            TransactionsDT.Columns.Clear();

            TransactionsDT.Columns.Add("Date", typeof(DateTime));
            if (account.Type == EAccountType.Bank)
            {
                TransactionsDT.Columns.Add("Type", typeof(string));
            }
            TransactionsDT.Columns.Add("Payee", typeof(string));
            TransactionsDT.Columns.Add("Memo", typeof(string));
            TransactionsDT.Columns.Add("Category", typeof(string));
            TransactionsDT.Columns.Add("Payment", typeof(string));
            TransactionsDT.Columns.Add("Status", typeof(string));
            TransactionsDT.Columns.Add("Deposit", typeof(string));
            TransactionsDT.Columns.Add("Balance", typeof(string));

            // Find transactions and put them in the data table
            decimal balance = 0;
            var accTransRel = household.Relations["FK_Accounts_Transactions"];
            foreach (var transRow in account.GetChildRows(accTransRel))
            {
                // Get transaction
                var trans = transRow as Household.TransactionsRow;

                // Get banking details
                Household.BankingTransactionsRow transBank = null;
                if (account.Type == EAccountType.Bank)
                {
                    transBank = household.BankingTransactions.GetByTransaction(trans);
                }

                // Get line item(s)
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

                string memo = (lineItems.Length == 1) ? (lineItems[0].IsMemoNull() ? "" : lineItems[0].Memo) : "";

                // Create new row
                var dr = TransactionsDT.NewRow();

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

                TransactionsDT.Rows.Add(dr);

                var bt = new BankingTransaction()
                {
                    Date = trans.Date,
                    Type = transBank == null ? "" : transBank.GetRegisterMediumString(),
                    Payee = trans.IsPayeeNull() ? "" : trans.Payee,
                    Memo = memo,
                    Category = category,
                    Payment = (amount >= 0) ? "" : (-amount).ToString("N"),
                    Status = (trans.Status == ETransactionStatus.Reconciled) ? "R" : ((trans.Status == ETransactionStatus.Cleared) ? "c" : " "),
                    Deposit = (amount >= 0) ? amount.ToString("N") : "",
                    Balance = balance.ToString("N")
                };
                Transactions.Add(bt);
            }

        }

        #endregion

        #region Transaction class

        public class BankingTransaction
        {
            public DateTime Date { get; set; }
            public string Type { get; set; }
            public string Payee { get; set; }
            public string Memo { get; set; }
            public string Category { get; set; }
            public string Payment { get; set; }
            public string Status { get; set; }
            public string Deposit { get; set; }
            public string Balance { get; set; }
        }

    #endregion
}
}
