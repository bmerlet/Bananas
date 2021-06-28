using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Collections;
using BanaData.Database;
using BanaData.Logic.Items;
using System.ComponentModel;

namespace BanaData.Logic.Main
{
    public abstract class AbstractRegisterLogic : BaseRegisterLogic
    {
        #region Private members

        // Parent logic
        protected readonly MainWindowLogic mainWindowLogic;

        // Account we are displaying
        protected int accountID = -1;

        // Actual collection of transactions backing the Transactions collection view property
        protected readonly WpfObservableRangeCollection<AbstractTransactionLogic> transactions = new WpfObservableRangeCollection<AbstractTransactionLogic>();
        protected readonly List<AbstractTransactionLogic> temporaryTransactionList = new List<AbstractTransactionLogic>();

        #endregion

        #region Constructor

        protected AbstractRegisterLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Create transaction collection view, and sort by date
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            Transactions.GroupDescriptions.Add(new PropertyGroupDescription("GroupSorter"));

            // Create commands
            DeleteTransaction = new CommandBase(OnDeleteTransaction);
        }

        #endregion

        #region UI properties

        // Name of the account
        public string AccountName { get; private set; }

        //
        // Selected transaction
        //
        private AbstractTransactionLogic selectedTransaction;
        protected bool logicIsChangingSelection;
        public AbstractTransactionLogic SelectedTransaction
        {
            get => selectedTransaction;
            set => SetSelectedTransaction(value);
        }

        // Transaction being edited
        public AbstractTransactionLogic EditedTransaction { get; protected set; }

        // Set to true to indicate that the overlay should focus on the date field
        public bool DateFocus { get; protected set; }

        // Context menu commands
        public CommandBase DeleteTransaction { get; protected set; }

        #endregion

        #region Public Actions

        // Set the account to display
        public void SetAccount(int _accountID)
        {
            // Remember which account we are displaying 
            accountID = _accountID;

            // Get the account details
            var household = mainWindowLogic.Household;
            var account = household.Accounts.FindByID(accountID);

            // Derived class specific action 
            OnNewAccount(account);

            // Export account name
            AccountName = account.Name;
            OnPropertyChanged(() => AccountName);

            // Find transactions and put them in a temp transaction list
            // (for performance)
            temporaryTransactionList.Clear();

            var accTransRel = household.Relations["FK_Accounts_Transactions"];

            foreach (Household.TransactionsRow transRow in account.GetChildRows(accTransRel))
            {
                var lineItems = GetLineItems(transRow);
                var trans = CreateTransactionFromDB(account, transRow, lineItems);
                temporaryTransactionList.Add(trans);
            }

            // Publish the transactions
            transactions.ReplaceRange(temporaryTransactionList);

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

            var trans = CreateTransactionFromDB(account, transRow, lineItems);
            transactions.Add(trans);

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

            foreach (var tr in transactions)
            {
                if (tr.TransID >= 0)
                {
                    var trRow = household.Transactions.FindByID(tr.TransID);
                    tr.UpdateStatus(trRow.Status);
                }
            }
        }

        public override void ProcessEnter()
        {
            var transaction = SelectedTransaction;
            if (transaction != null)
            {
                bool wasEmptyTransaction = transaction.TransID < 0;

                (bool needCommit, bool moveDown) = transaction.ValidateEndEdit();

                if (needCommit)
                {
                    // Remove from transaction list if needed
                    if (wasEmptyTransaction)
                    {
                        // Remove transaction from list as its ID is about to change
                        // And the ID is what is used to determine equality.
                        // We don't want the list to get confused.
                        logicIsChangingSelection = true;
                        SelectedTransaction = null;
                        logicIsChangingSelection = false;
                        transactions.Remove(transaction);
                    }

                    // Commit changes
                    transaction.EndEdit();

                    // Put back in list
                    if (wasEmptyTransaction)
                    {
                        transactions.Add(transaction);
                    }

                    // Update balances
                    RecomputeBalances();
                }

                if (moveDown)
                {
                    if (wasEmptyTransaction)
                    {
                        // Create an empty transaction if we consumed the previous one
                        AddEmptyTransactionAtBottom();
                    }
                    else
                    {
                        // Move the selection down one row otherwise
                        MoveDown();
                    }
                }
            }
        }

        // Recompute cash balance
        public override void RecomputeBalances()
        {
            decimal balance = 0;
            foreach (var o in Transactions)
            {
                if (o is AbstractTransactionLogic atl)
                {
                    // Update running balance
                    balance += atl.AmountForCashBalance;

                    // Update balance in transaction
                    atl.Balance = balance;
                }
            }
        }

        public override void MoveUp()
        {
            if (GetPreviousTransaction(SelectedTransaction) is AbstractTransactionLogic prevTransaction)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = prevTransaction;
                logicIsChangingSelection = false;
            }
        }

        public override void MoveDown()
        {
            if (GetNextTransaction(SelectedTransaction) is AbstractTransactionLogic nextTransaction)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = nextTransaction;
                logicIsChangingSelection = false;
            }
        }

        #endregion

        #region Utilities for derived classes

        private void OnDeleteTransaction(object arg)
        {
            AbstractTransactionLogic atl = arg == null ? SelectedTransaction : arg as AbstractTransactionLogic;

            if (atl == null)
            {
                return;
            }

            if (atl.TransID < 0)
            {
                // Can't remove the empty transaction
                return;
            }

            // We want to select the next transaction afterwards
            var transactionToSelect = GetNextTransaction(atl) as AbstractTransactionLogic;

            // Cancel all changes
            atl.CancelEdit();

            // Delete from dataset
            var household = mainWindowLogic.Household;
            var accountRow = household.Accounts.FindByID(accountID);
            var transactionRow = household.Transactions.FindByID(atl.TransID);

            // Delete all line items
            var lineItems = household.LineItems.GetByTransaction(transactionRow);
            foreach (var lineItem in lineItems)
            {
                lineItem.Delete();
            }

            // Delete banking or investment transaction
            if (accountRow.Type == EAccountType.Bank)
            {
                household.BankingTransactions.GetByTransaction(transactionRow).Delete();
            }
            else if (accountRow.Type == EAccountType.Investment)
            {
                household.InvestmentTransactions.GetByTransaction(transactionRow).Delete();
            }

            // Finally delete the transaction
            transactionRow.Delete();
            mainWindowLogic.CommitChanges();

            // Delete from list
            transactions.Remove(atl);
            Transactions.Refresh();

            // Compute balances
            RecomputeBalances();

            // Re-select
            logicIsChangingSelection = true;
            SelectedTransaction = transactionToSelect;
            logicIsChangingSelection = false;
        }

        #endregion

        #region Private utilities

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
                if (!dbli.IsAccountIDNull())
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

        // Set the selected transaction
        private void SetSelectedTransaction(AbstractTransactionLogic value)
        {
            if (value != selectedTransaction)
            {
                if (logicIsChangingSelection)
                {
                    // This logic is changing the selection (e.g. processing of return key)
                    selectedTransaction = value;
                    EditedTransaction = value;
                    TransactionToScrollTo = value;
                    OnPropertyChanged(() => SelectedTransaction);
                    OnPropertyChanged(() => TransactionToScrollTo);
                }
                else
                {
                    // User changed selection (e.g. by clicking on a row)
                    if (EditedTransaction != null && transactions.Contains(EditedTransaction))
                    {
                        EditedTransaction.CancelEdit();
                    }
                    selectedTransaction = value;
                    EditedTransaction = value;
                }

                if (EditedTransaction != null)
                {
                    EditedTransaction.BeginEdit();

                    DateFocus = false;
                    OnPropertyChanged(() => DateFocus);
                    mainWindowLogic.GuiServices.ExecuteAsync((Action)delegate ()
                    {
                        DateFocus = true;
                        OnPropertyChanged(() => DateFocus);
                    });
                }
                OnPropertyChanged(() => EditedTransaction);
                OnPropertyChanged("UpdateOverlayPosition");
            }
        }

        // Add an empty transaction at the bottom of the register
        private void AddEmptyTransactionAtBottom()
        {
            // Add new empty transaction at the bottom
            var emptyTransaction = CreateEmptyTransaction();
            transactions.Add(emptyTransaction);

            // Select it
            mainWindowLogic.GuiServices.ExecuteAsync((Action)delegate ()
            {
                logicIsChangingSelection = true;
                SelectedTransaction = emptyTransaction;
                logicIsChangingSelection = false;

                // Go to the bottom
                //TransactionToScrollTo = emptyTransaction;
                //OnPropertyChanged(() => TransactionToScrollTo);
                OnPropertyChanged("ScrollToBottom");
            });
        }

        #endregion

        #region Hooks provided by derived classes

        // Called by this class when a new account is set
        protected virtual void OnNewAccount(Household.AccountsRow accountRow) { }

        // Create a transaction from DB info
        protected abstract AbstractTransactionLogic CreateTransactionFromDB(
            Household.AccountsRow account,
            Household.TransactionsRow transRow,
            List<LineItem> lineItems);

        // Creaste an empty transaction
        protected abstract AbstractTransactionLogic CreateEmptyTransaction();

        #endregion
    }
}
