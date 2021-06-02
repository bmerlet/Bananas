using System;
using System.Collections.Generic;
using System.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Database;
using System.Windows.Data;
using System.ComponentModel;

namespace BanaData.Logic.Main
{
    public class BankRegisterLogic : LogicBase
    {
        #region Private members

        // Main logic
        private readonly MainWindowLogic mainWindowLogic;

        // Actual collection of transactions backing the Transactions collection view property
        private readonly ObservableCollection<BankingTransactionLogic> transactions = new ObservableCollection<BankingTransactionLogic>();

        #endregion

        #region Constructor

        public BankRegisterLogic(MainWindowLogic mainWindowLogic)
        {
            this.mainWindowLogic = mainWindowLogic;

            // Create transaction collection view, and sort by date
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
        }

        #endregion

        #region UI properties

        // Name of the account
        public string AccountName { get; private set; }

        // If banking account (as opposed to credit card)
        public bool IsBank { get; private set; }

        // Transactions. The CollectionView type enables sorting on columns
        public CollectionView Transactions { get; }

        // Transaction to show
        public BankingTransactionLogic TransactionToScrollTo { get; private set; }

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

            // Find transactions and put them in the transaction list
            BankingTransactionLogic bankingTransaction = null;
            transactions.Clear();
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

                var transactionData = new BankingTransactionLogic.BankTransactionData(
                    trans.Date,
                    transBank == null ? ETransactionMedium.None : transBank.Medium,
                    transBank == null ? 0 : (transBank.IsCheckNumberNull() ? 0 : transBank.CheckNumber),
                    trans.IsPayeeNull() ? "" : trans.Payee,
                    memo,
                    category,
                    trans.Status,
                    amount);

                bankingTransaction = new BankingTransactionLogic(mainWindowLogic, this, trans.ID, transactionData);
                transactions.Add(bankingTransaction);
            }

            // Add new empty transaction at the bottom
            bankingTransaction = new BankingTransactionLogic(mainWindowLogic, this);
            transactions.Add(bankingTransaction);

            // Compute balances
            RecomputeBalances();

            // Go to the bottom
            TransactionToScrollTo = bankingTransaction;
            OnPropertyChanged(() => TransactionToScrollTo);

        }

        public void RecomputeBalances()
        {
            decimal balance = 0;
            foreach (var o in Transactions)
            {
                if (o is BankingTransactionLogic btl)
                {
                    // Update running balance
                    balance += btl.Amount;

                    // Update balance in transaction
                    btl.Balance = balance;
                }
            }
        }

        #endregion
    }
}
