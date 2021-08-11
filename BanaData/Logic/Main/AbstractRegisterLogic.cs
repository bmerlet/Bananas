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
using BanaData.Logic.Dialogs.Pickers;

namespace BanaData.Logic.Main
{
    public abstract class AbstractRegisterLogic : BaseRegisterLogic
    {
        #region Private members

        // Parent logic
        protected readonly MainWindowLogic mainWindowLogic;

        // Account we are displaying
        protected Household.AccountRow accountRow = null;

        // Actual collection of transactions backing the Transactions collection view property
        protected readonly WpfObservableRangeCollection<AbstractTransactionLogic> transactions = new WpfObservableRangeCollection<AbstractTransactionLogic>();
        protected readonly List<AbstractTransactionLogic> temporaryTransactionList = new List<AbstractTransactionLogic>();

        #endregion

        #region Constructor

        protected AbstractRegisterLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Create transaction collection view, and sort by date
            RegisterItems = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            RegisterItems.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            RegisterItems.GroupDescriptions.Add(new PropertyGroupDescription("GroupSorter"));

            // Create commands
            DeleteTransaction = new CommandBase(OnDeleteTransaction);
            MoveTransaction = new CommandBase(OnMoveTransaction);
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
        public CommandBase DeleteTransaction { get; }
        public CommandBase MoveTransaction { get; }

        #endregion

        #region Public Actions

        // Set the account to display
        public void SetAccount(int accountID, int transactionID)
        {
            // Get the account details
            var household = mainWindowLogic.Household;
            accountRow = household.Account.FindByID(accountID);

            // Derived class specific action 
            OnNewAccount();

            // Export account name
            AccountName = accountRow.Name;
            OnPropertyChanged(() => AccountName);

            // Find transactions and put them in a temp transaction list
            // (for performance)
            temporaryTransactionList.Clear();

            foreach (Household.TransactionRow transRow in accountRow.GetRegularTransactionRows())
            {
                var lineItems = GetLineItems(transRow);
                var trans = CreateTransactionFromDB(accountRow, transRow, lineItems);
                temporaryTransactionList.Add(trans);
            }

            // Publish the transactions
            transactions.ReplaceRange(temporaryTransactionList);

            // If asked to go to a specific transaction, go there
            if (transactionID != int.MinValue)
            {
                // Add new empty transaction at the bottom but don't select it
                AddEmptyTransactionAtBottom(false);

                // Find the transaction to select
                AbstractTransactionLogic transToSelect = transactions.FirstOrDefault(t => t.TransID == transactionID);

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
            var transRow = household.Transaction.FindByID(transactionID);
            var lineItems = GetLineItems(transRow);

            var trans = CreateTransactionFromDB(accountRow, transRow, lineItems);
            transactions.Add(trans);

            // Re-compute balances
            RecomputeBalances();
        }

        // Update transaction status (e.g. after reconcile)
        public void UpdateAllTransactionStatus()
        {
            // Return if we are not active
            if (accountRow.ID != mainWindowLogic.DisplayedAccountID)
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
            foreach (var o in RegisterItems)
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
                    if (MoveDownInternal())
                    {
                        transaction.CancelEdit();
                    }
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
                bool moveUp = false;

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
                        moveUp = true;
                    }
                }
                else
                {
                    moveUp = true;
                }

                if (moveUp)
                {
                    if (MoveUpInternal())
                    {
                        transaction.CancelEdit();
                    }
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

        private bool MoveDownInternal()
        {
            if (GetNextTransaction(SelectedTransaction) is AbstractTransactionLogic nextTransaction)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = nextTransaction;
                logicIsChangingSelection = false;
                return true;
            }

            return false;
        }

        private bool MoveUpInternal()
        {
            if (GetPreviousTransaction(SelectedTransaction) is AbstractTransactionLogic prevTransaction)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = prevTransaction;
                logicIsChangingSelection = false;
                return true;
            }

            return false;
        }

        #endregion

        #region Local actions

        private void OnDeleteTransaction(object arg)
        {
            var atl = SelectedTransaction;
            if (atl == null)
            {
                return;
            }

            if (atl.TransID == AbstractTransactionLogic.TRANSID_NOT_COMMITTED)
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
            RegisterItems.Refresh();

            // Compute balances
            RecomputeBalances();

            // Re-select
            logicIsChangingSelection = true;
            SelectedTransaction = transactionToSelect;
            logicIsChangingSelection = false;
        }

        private void OnMoveTransaction()
        {
            var atl = SelectedTransaction;
            if (atl == null)
            {
                return;
            }

            if (atl.TransID == AbstractTransactionLogic.TRANSID_NOT_COMMITTED)
            {
                // Can't remove the empty transaction
                return;
            }

            // Ask what account to move this transaction to
            var logic = new AccountPickerLogic(mainWindowLogic, accountRow);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Cancel all changes
                atl.CancelEdit();

                // We want to select the next transaction afterwards
                var transactionToSelect = GetNextTransaction(atl) as AbstractTransactionLogic;

                // Do move the transaction in the DB
                var household = mainWindowLogic.Household;
                var transRow = household.Transaction.FindByID(atl.TransID);
                transRow.AccountID = logic.PickedAccount.ID;
                transRow.CheckpointID = household.Checkpoint.GetMostRecentCheckpointID();
                mainWindowLogic.CommitChanges();

                // Delete from list
                transactions.Remove(atl);
                RegisterItems.Refresh();

                // Compute balances
                RecomputeBalances();

                // Re-select
                logicIsChangingSelection = true;
                SelectedTransaction = transactionToSelect;
                logicIsChangingSelection = false;
            }
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
                lineItems.Add(new LineItem(mainWindowLogic, dbli, false));
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
                    // Enable/disable context menu commands
                    bool realTrans = EditedTransaction.TransID != AbstractTransactionLogic.TRANSID_NOT_COMMITTED;
                    DeleteTransaction.SetCanExecute(realTrans);
                    MoveTransaction.SetCanExecute(realTrans);

                    // Edit this transaction
                    EditedTransaction.BeginEdit();

                    // Set focus on date field
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
                    DeleteTransaction.SetCanExecute(false);
                    MoveTransaction.SetCanExecute(false);
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
        protected virtual void OnNewAccount() { }

        // Create a transaction from DB info
        protected abstract AbstractTransactionLogic CreateTransactionFromDB(
            Household.AccountRow account,
            Household.TransactionRow transRow,
            List<LineItem> lineItems);

        // Creaste an empty transaction
        protected abstract AbstractTransactionLogic CreateEmptyTransaction();

        #endregion
    }
}
