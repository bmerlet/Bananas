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
        public void SetAccount(int _accountID, int transactionID, int lineItemID)
        {
            // Remember which account we are displaying 
            accountID = _accountID;

            // Get the account details
            var household = mainWindowLogic.Household;
            var account = household.Account.FindByID(accountID);

            // Derived class specific action 
            OnNewAccount(account);

            // Export account name
            AccountName = account.Name;
            OnPropertyChanged(() => AccountName);

            // Find transactions and put them in a temp transaction list
            // (for performance)
            temporaryTransactionList.Clear();

            foreach (Household.TransactionRow transRow in account.GetTransactionRows())
            {
                var lineItems = GetLineItems(transRow);
                var trans = CreateTransactionFromDB(account, transRow, lineItems);
                temporaryTransactionList.Add(trans);
            }

            // Now find all transfers to this account ID from a difffernet account and create placeholder transactions
            foreach (Household.LineItemRow lineItemRow in household.LineItem.Rows)
            {
                if (!lineItemRow.IsAccountIDNull() && lineItemRow.AccountID == accountID)
                {
                    var transferTransactionRow = lineItemRow.TransactionRow;
                    if (transferTransactionRow.AccountID != accountID)
                    {
                        var trans = CreateMirrorTransaction(account, lineItemRow);
                        temporaryTransactionList.Add(trans);
                    }
                }
            }

            // Publish the transactions
            transactions.ReplaceRange(temporaryTransactionList);

            // If asked to go to a specific transaction, go there
            if (transactionID != int.MinValue && lineItemID != int.MinValue)
            {
                // Add new empty transaction at the bottom but don't select it
                AddEmptyTransactionAtBottom(true);

                // Find the transaction to select
                AbstractTransactionLogic transToSelect;
                if (transactionID == AbstractTransactionLogic.TRANSID_TRANSFER_FILLIN)
                {
                    transToSelect = transactions.FirstOrDefault(t => t.TransID == transactionID && t.FillInLineItemID == lineItemID);
                }
                else
                {
                    transToSelect = transactions.FirstOrDefault(t => t.TransID == transactionID);
                }

                if (transToSelect != null)
                {
                    mainWindowLogic.GuiServices.ExecuteAsync((Action)delegate ()
                    {
                        logicIsChangingSelection = true;
                        SelectedTransaction = transToSelect;
                        logicIsChangingSelection = false;

                        TransactionToScrollTo = transToSelect;
                        OnPropertyChanged(() => TransactionToScrollTo);
                    });

                }

            }
            else
            {
                // Add new empty transaction at the bottom and select it
                AddEmptyTransactionAtBottom(true);
            }

            // Compute balances
            RecomputeBalances();
        }

        // Add to the transaction list a transaction that was added to the DB "behind our back"
        // (e.g. interest transaction by the reconcile dialog)
        public void AddTransaction(int transactionID)
        {
            var household = mainWindowLogic.Household;
            var account = household.Account.FindByID(accountID);
            var transRow = household.Transaction.FindByID(transactionID);
            var lineItems = GetLineItems(transRow);

            var trans = CreateTransactionFromDB(account, transRow, lineItems);
            transactions.Add(trans);

            // Re-compute balances
            RecomputeBalances();
        }

        // Update transaction status (e.g. after reconcile)
        public void UpdateAllTransactionStatus()
        {
            // Return if we are not active
            if (accountID != mainWindowLogic.DisplayedAccountID)
            {
                return;
            }

            foreach (var tr in transactions)
            {
                tr.UpdateStatus();
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

        // User hit enter on the register
        public override void ProcessEnter()
        {
            var transaction = SelectedTransaction;
            if (transaction != null)
            {
                bool transactionChanged = transaction.HasTransactionChanged;
                if (!transactionChanged)
                {
                    // Nothing has changed, treat this as a move down
                    transaction.CancelEdit();
                    MoveDownInternal();
                }
                else
                {
                    bool wasEmptyTransaction = transaction.TransID == AbstractTransactionLogic.TRANSID_NOT_COMMITTED;

                    bool wasCommitted = CommitTransactionIfNeeded(transaction);

                    if (wasCommitted)
                    {
                        // Move down
                        if (wasEmptyTransaction)
                        {
                            // Create an empty transaction since we consumed the previous one
                            AddEmptyTransactionAtBottom(true);
                        }
                        else
                        {
                            // Move the selection down one row otherwise
                            MoveDownInternal();
                        }
                    }
                }
            }
        }

        // Down arrow action
        public override void MoveDown()
        {
            var transaction = SelectedTransaction;
            if (transaction != null)
            {
                if (transaction.HasTransactionChanged)
                {
                    bool wasEmptyTransaction = transaction.TransID == AbstractTransactionLogic.TRANSID_NOT_COMMITTED;

                    if (mainWindowLogic.YesNoQuestion("Do you want to save the changes to the transaction you were on?"))
                    {
                        bool wasCommitted = CommitTransactionIfNeeded(transaction);
                        if (wasCommitted)
                        {
                            // Move down
                            if (wasEmptyTransaction)
                            {
                                // Create an empty transaction since we consumed the previous one
                                AddEmptyTransactionAtBottom(true);
                            }
                            else
                            {
                                // Move the selection down one row otherwise
                                MoveDownInternal();
                            }
                        }
                    }
                    else
                    {
                        transaction.CancelEdit();
                        MoveDownInternal();
                    }
                }
                else
                {
                    transaction.CancelEdit();
                    MoveDownInternal();
                }
            }
        }

        // Up arrow action
        public override void MoveUp()
        {
            var transaction = SelectedTransaction;
            if (transaction != null)
            {
                if (transaction.HasTransactionChanged)
                {
                    if (mainWindowLogic.YesNoQuestion("Do you want to save the changes to the transaction you were on?"))
                    {
                        bool wasCommitted = CommitTransactionIfNeeded(transaction);
                        if (wasCommitted)
                        {
                            // Move the selection up one row
                            MoveUpInternal();
                        }
                    }
                    else
                    {
                        transaction.CancelEdit();
                        MoveUpInternal();
                    }
                }
                else
                {
                    transaction.CancelEdit();
                    MoveUpInternal();
                }
            }
        }

        private bool CommitTransactionIfNeeded(AbstractTransactionLogic transaction)
        {
            bool wasEmptyTransaction = transaction.TransID == AbstractTransactionLogic.TRANSID_NOT_COMMITTED;

            bool needCommit = transaction.DoesTransactionNeedComit;

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

            return needCommit;
        }

        private void MoveDownInternal()
        {
            if (GetNextTransaction(SelectedTransaction) is AbstractTransactionLogic nextTransaction)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = nextTransaction;
                logicIsChangingSelection = false;
            }
        }

        private void MoveUpInternal()
        {
            if (GetPreviousTransaction(SelectedTransaction) is AbstractTransactionLogic prevTransaction)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = prevTransaction;
                logicIsChangingSelection = false;
            }
        }

        #endregion

        #region Utilities for derived classes

        private void OnDeleteTransaction(object arg)
        {
            var atl = SelectedTransaction;
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
            atl.DeleteTransactionFromDataset(atl.TransID);

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
        private List<LineItem> GetLineItems(Household.TransactionRow transRow)
        {
            var dbLineItems = transRow.GetLineItemRows();

            var lineItems = new List<LineItem>();
            foreach (var dbli in dbLineItems)
            {
                int catID = -1;
                int catAccntID = -1;
                string category = "";
                if (!dbli.IsAccountIDNull())
                {
                    category = "[" + dbli.AccountRow.Name + "]";
                    catAccntID = dbli.AccountID;
                }
                else if (!dbli.IsCategoryIDNull())
                {
                    category = dbli.CategoryRow.FullName;
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

                    UpdateOverlayPosition = () =>
                    {
                        DateFocus = true;
                        OnPropertyChanged(() => DateFocus);
                    };
                }
                else
                {
                    UpdateOverlayPosition = null;
                }

                OnPropertyChanged(() => EditedTransaction);
                OnPropertyChanged(() => UpdateOverlayPosition);
            }
        }

        // Add an empty transaction at the bottom of the register
        private void AddEmptyTransactionAtBottom(bool selectIt)
        {
            // Add new empty transaction at the bottom
            var emptyTransaction = CreateEmptyTransaction();
            transactions.Add(emptyTransaction);

            // Select it
            if (selectIt)
            {
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
        }

        #endregion

        #region Hooks provided by derived classes

        // Called by this class when a new account is set
        protected virtual void OnNewAccount(Household.AccountRow accountRow) { }

        // Create a transaction from DB info
        protected abstract AbstractTransactionLogic CreateTransactionFromDB(
            Household.AccountRow account,
            Household.TransactionRow transRow,
            List<LineItem> lineItems);

        // Create a mirror pseudo-transaction for transfers
        protected abstract AbstractTransactionLogic CreateMirrorTransaction(
            Household.AccountRow account,
            Household.LineItemRow lineItem);

        // Creaste an empty transaction
        protected abstract AbstractTransactionLogic CreateEmptyTransaction();

        #endregion
    }
}
