using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Items;
using Toolbox.UILogic;

namespace BanaData.Logic.Main
{
    public abstract class AbstractRegisterLogic : LogicBase
    {
        #region Private members

        // Parent logic
        protected readonly MainWindowLogic mainWindowLogic;

        // Account we are displaying
        protected int accountID = -1;

        #endregion

        #region Constructor

        protected AbstractRegisterLogic(MainWindowLogic _mainWindowLogic) => mainWindowLogic = _mainWindowLogic;

        #endregion

        #region UI properties

        // Name of the account
        public string AccountName { get; private set; }

        // Transactions. The CollectionView type enables sorting on columns, and is generic
        public CollectionView Transactions { get; protected set; }

        #endregion

        #region Actions

        // Set the account to display
        public void SetAccount(int _accountID)
        {
            // Remember which account we are displaying 
            accountID = _accountID;

            // Get the account details
            var household = mainWindowLogic.Household;
            var account = household.Accounts.FindByID(accountID);

            // Export account name
            AccountName = account.Name;
            OnPropertyChanged(() => AccountName);

            // Find transactions and put them in the transaction list
            ClearTransactionList();

            var accTransRel = household.Relations["FK_Accounts_Transactions"];

            foreach (Household.TransactionsRow transRow in account.GetChildRows(accTransRel))
            {
                var lineItems = GetLineItems(transRow);
                AddDBTransactionToList(account, transRow, lineItems, true);
            }

            // Publish the transactions
            PublishTransactionList();

            // Add new empty transaction at the bottom
            AddEmptyTransactionAtBottom();

            // Compute balances
            RecomputeBalances();
        }

        // Add to the transaction list a transaction that was added to the DB "behind our back"
        // (e.g. interest transaction by the reconcile dialog)
        public void AddTransaction(int transactionID)
        {
            var household = mainWindowLogic.Household;
            var account = household.Accounts.FindByID(accountID);
            var transRow = household.Transactions.FindByID(transactionID);
            var lineItems = GetLineItems(transRow);

            AddDBTransactionToList(account, transRow, lineItems, false);

            // Re-compute balances
            RecomputeBalances();
        }

        public void UpdateAllTransactionStatus()
        {
            // Return if we are not active
            if (accountID != mainWindowLogic.DisplayedAccountID)
            {
                return;
            }

            var household = mainWindowLogic.Household;

            foreach (var tr in AbstractTransactions)
            {
                if (tr.TransID >= 0)
                {
                    var trRow = household.Transactions.FindByID(tr.TransID);
                    tr.UpdateStatus(trRow.Status);
                }
            }
        }

        // Get line item(s) from a transaction
        private List<LineItem> GetLineItems(Household.TransactionsRow transRow)
        {
            var household = mainWindowLogic.Household;
            var dbLineItems = household.LineItems.GetByTransaction(transRow);

            var lineItems = new List<LineItem>();
            foreach (var dbli in dbLineItems)
            {
                int catID = -1;
                int catAccntID = -1;
                string category = "";
                if (dbli.IsTransfer && !dbli.IsAccountIDNull())
                {
                    var destAccount = household.Accounts.FindByID(dbli.AccountID);
                    category = "[" + destAccount.Name + "]";
                    catAccntID = dbli.AccountID;
                }
                else if (!dbli.IsCategoryIDNull())
                {
                    var destCategory = household.Categories.FindByID(dbli.CategoryID);
                    category = destCategory.FullName;
                    catID = dbli.CategoryID;
                }
                string memo = dbli.IsMemoNull() ? "" : dbli.Memo;

                var li = new LineItem(mainWindowLogic, dbli.ID, category, catID, catAccntID, memo, dbli.Amount, false);
                lineItems.Add(li);
            }

            return lineItems;
        }

        #endregion

        #region Hooks provided by derived classes

        // For a bulk add of transaction, prepare the transaction list
        protected abstract void ClearTransactionList();

        // Add one transaction to the transaction list.
        // when bullk is true, ClearTransactionList() has been called
        // before a series of call to this method, and
        // PublishTransactionList is called afterwards
        protected abstract void AddDBTransactionToList(
            Household.AccountsRow account,
            Household.TransactionsRow transRow, 
            List<LineItem> lineItems,
            bool bulk);

        // Called at the end of a bulk add of transactions
        protected abstract void PublishTransactionList();

        // Provide an iterator on the transaction list
        protected abstract IEnumerable<AbstractTransactionLogic> AbstractTransactions { get; }

        protected abstract void AddEmptyTransactionAtBottom();
        public abstract void RecomputeBalances();

        #endregion
    }
}
