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

        private readonly List<string> payees = new List<string>();

        #endregion

        #region Constructor

        public BankRegisterLogic(MainWindowLogic mainWindowLogic)
        {
            this.mainWindowLogic = mainWindowLogic;
        }

        #endregion

        #region UI properties

        // Name of the account
        public string AccountName { get; private set; }

        // If banking account (as opposed to credit card)
        public bool IsBank { get; private set; }

        public ObservableCollection<BankingTransaction> Transactions { get; } = new ObservableCollection<BankingTransaction>();

        #endregion

        #region Actions

        public void SetAccount(int accountID)
        {
            // Get the account details
            var household = mainWindowLogic.Household;
            var account = household.Accounts.FindByID(accountID);

            // Export account name
            AccountName = account.Name;
            OnPropertyChanged(() => AccountName);

            // Export if banking account
            IsBank = account.Type == EAccountType.Bank;
            OnPropertyChanged(() => IsBank);

            // Build payee list
            BuildPayeeList();

            // Find transactions and put them in the transaction list
            Transactions.Clear();
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

                var bt = new BankingTransaction(payees)
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

        private void BuildPayeeList()
        {
            payees.Clear();

            //mainWindowLogic.Household.Transactions.PayeeColumn.
            payees.AddRange(
                mainWindowLogic.Household.MemorizedPayees.AsEnumerable()
                    .Select(s => s.Field<string>("Payee")));

            payees.Sort();
        }

        #endregion

        #region Transaction class

        public class BankingTransaction
        {
            public BankingTransaction(IEnumerable<string> payees)
            {
                Payees = payees;
            }

            public DateTime Date { get; set; }
            public string Type { get; set; }
            public string Payee { get; set; }
            public string Memo { get; set; }
            public string Category { get; set; }
            public string Payment { get; set; }
            public string Status { get; set; }
            public string Deposit { get; set; }
            public string Balance { get; set; }

            public IEnumerable<string> Payees { get; }
        }

    #endregion
    }
}
