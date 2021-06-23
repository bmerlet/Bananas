using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Items;

namespace BanaData.Logic.Main
{
    public class BankRegisterLogic :  AbstractRegisterLogic
    {
        #region Private members

        // Actual collection of transactions backing the Transactions collection view property
        private readonly ObservableCollection<BankingTransactionLogic> transactions = new ObservableCollection<BankingTransactionLogic>();

        #endregion

        #region Constructor

        public BankRegisterLogic(MainWindowLogic mainWindowLogic)
            :  base(mainWindowLogic)
        {
            // Create transaction collection view, and sort by date
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            Transactions.GroupDescriptions.Add(new PropertyGroupDescription("GroupSorter"));

            DeleteTransaction = new CommandBase(OnDeleteTransaction);

            // Create column width manager
            Widths = new ColumnWidths(mainWindowLogic, this);
        }

        #endregion

        #region UI properties

        // If banking account (as opposed to credit card)
        public bool IsBank { get; private set; }

        // Transactions. The CollectionView type enables sorting on columns
        public CollectionView Transactions { get; }

        // Selected transaction
        private BankingTransactionLogic selectedTransaction;
        private bool logicIsChangingSelection;
        public BankingTransactionLogic SelectedTransaction
        {
            get => selectedTransaction;
            set
            {
                if (value != selectedTransaction)
                {
                    if (logicIsChangingSelection)
                    {
                        // This logic is changing the selection (e.g. processing of return key)
                        selectedTransaction = value;
                        editedTransaction = value;
                        OnPropertyChanged(() => SelectedTransaction);
                    }
                    else
                    {
                        // User changed selection (e.g. by clicking on a row)
                        if (editedTransaction != null && transactions.Contains(editedTransaction))
                        {
                            editedTransaction.CancelEdit();
                        }
                        selectedTransaction = value;
                        editedTransaction = value;
                    }

                    if (selectedTransaction != null)
                    {
                        selectedTransaction.BeginEdit();
                    }
                    OnPropertyChanged(() => EditedTransaction);
                    OnPropertyChanged("UpdateOverlayPosition");
                }
            }
        }

        // Transaction to show
        public BankingTransactionLogic TransactionToScrollTo { get; private set; }

        // Transaction being edited
        private BankingTransactionLogic editedTransaction;
        public BankingTransactionLogic EditedTransaction
        {
            get => editedTransaction;
            set { editedTransaction = value; OnPropertyChanged(() => EditedTransaction); }
        }

        // Contect menu commands
        public CommandBase DeleteTransaction { get; }

        // Column widths
        public ColumnWidths Widths { get; }

        #endregion

        #region Actions

        protected override void ClearTransactionList() => transactions.Clear();

        // Routine to get a transaction from the DB into the list
        protected override void AddDBTransactionToList(Household.AccountsRow accountRow, Household.TransactionsRow transRow, List<LineItem> lineItems) 
        {
            var household = mainWindowLogic.Household;

            // Get banking details
            Household.BankingTransactionsRow transBankRow = null;
            if (accountRow.Type == EAccountType.Bank)
            {
                transBankRow = household.BankingTransactions.GetByTransaction(transRow);
            }

            var transactionData = new BankingTransactionLogic.BankTransactionData(
                transRow.Date,
                transBankRow == null ? ETransactionMedium.None : transBankRow.Medium,
                transBankRow == null ? 0 : (transBankRow.IsCheckNumberNull() ? 0 : (uint)transBankRow.CheckNumber),
                transRow.IsPayeeNull() ? "" : transRow.Payee,
                transRow.Status,
                lineItems);

            var bankingTransaction = new BankingTransactionLogic(mainWindowLogic, this, accountID, transRow.ID, transactionData);
            transactions.Add(bankingTransaction);
        }

        public void UpdateAllTransactionStatus()
        {
            // Return if we are not active
            if (accountID != mainWindowLogic.DisplayedAccountID)
            {
                return;
            }

            var household = mainWindowLogic.Household;

            foreach(var tr in transactions)
            {
                if (tr.TransID >= 0)
                {
                    var trRow = household.Transactions.FindByID(tr.TransID);
                    tr.UpdateStatus(trRow.Status);
                }
            }
        }

        public void MoveDownOneTransaction(bool wasEmptyTransaction)
        {
            if (wasEmptyTransaction)
            {
                // Create an empty transaction if we consumed the previous one
                AddEmptyTransactionAtBottom();
            }
            else
            {
                // Move the selection down one row otherwise
                logicIsChangingSelection = true;
                OnPropertyChanged("MoveSelectionDownOneRow");
                logicIsChangingSelection = false;
            }
        }

        public void RemoveTransactionFromList(BankingTransactionLogic btl)
        {
            if (SelectedTransaction == btl)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = null;
                logicIsChangingSelection = false;
            }
            transactions.Remove(btl);
        }

        public void AddTransactionBackToList(BankingTransactionLogic btl)
        {
            transactions.Add(btl);
        }

        protected override void AddEmptyTransactionAtBottom()
        {
            // Add new empty transaction at the bottom
            var emptyTransaction = new BankingTransactionLogic(mainWindowLogic, this, accountID);
            transactions.Add(emptyTransaction);
            //Transactions.Refresh();

            // Select it
            mainWindowLogic.GuiServices.ExecuteAsync((Action)delegate ()
            {
                logicIsChangingSelection = true;
                SelectedTransaction = emptyTransaction;
                logicIsChangingSelection = false;

                // Go to the bottom
                //TransactionToScrollTo = bankingTransaction;
                //OnPropertyChanged(() => TransactionToScrollTo);
                OnPropertyChanged("ScrollToBottom");
            });
        }

        private void OnDeleteTransaction(object arg)
        {
            BankingTransactionLogic btl = arg == null ? EditedTransaction : arg as BankingTransactionLogic;
            if (btl == null)
            {
                return;
            }

            if (btl.TransID < 0)
            {
                // Can't remove the empty transaction
                return;
            }

            // Cancel all changes
            btl.CancelEdit();

            // Delete from dataset
            var household = mainWindowLogic.Household;
            var transactionRow = household.Transactions.FindByID(btl.TransID);

            // Delete all line items
            var lineItems = household.LineItems.GetByTransaction(transactionRow);
            foreach(var lineItem in lineItems)
            {
                lineItem.Delete();
            }

            // Delete banking transaction
            if (IsBank)
            {
                household.BankingTransactions.GetByTransaction(transactionRow).Delete();
            }

            // Finally delete the transaction
            transactionRow.Delete();
            mainWindowLogic.CommitChanges();

            // Delete from list
            transactions.Remove(btl);
            Transactions.Refresh();

            // Move away
            //MoveDownOneTransaction(false);

            // Compute balances
            RecomputeBalances();
        }

        public override void RecomputeBalances()
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

        #region Supporting classes

        public class ColumnWidths : LogicBase
        {
            private readonly MainWindowLogic mainWindowLogic;
            private readonly BankRegisterLogic bankRegisterLogic;

            public ColumnWidths(MainWindowLogic _mainWindowLogic, BankRegisterLogic _bankRegisterLogic)
                => (mainWindowLogic, bankRegisterLogic) = (_mainWindowLogic, _bankRegisterLogic);

            public void IsBankHasChanged()
            {
                OnPropertyChanged(() => WidthOfMediumColumn);
            }

            public double WidthOfDateColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfDateColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfDateColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfDateColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDateColumn);
                    }
                }
            }

            public double WidthOfMediumColumn
            {
                get => bankRegisterLogic.IsBank ? mainWindowLogic.UserSettings.WidthOfMediumColumn : 0;
                set
                {
                    if (bankRegisterLogic.IsBank && mainWindowLogic.UserSettings.WidthOfMediumColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfMediumColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfMediumColumn);
                    }
                }
            }

            public double WidthOfPayeeColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfPayeeColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfPayeeColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfPayeeColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfPayeeColumn);
                    }
                }
            }

            public double WidthOfMemoColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfMemoColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfMemoColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfMemoColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfMemoColumn);
                    }
                }
            }

            public double WidthOfCategoryColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfCategoryColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfCategoryColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfCategoryColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfCategoryColumn);
                    }
                }
            }

            public double WidthOfPaymentColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfPaymentColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfPaymentColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfPaymentColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfPaymentColumn);
                    }
                }
            }

            public double WidthOfStatusColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfStatusColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfStatusColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfStatusColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfStatusColumn);
                    }
                }
            }

            public double WidthOfDepositColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfDepositColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfDepositColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfDepositColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDepositColumn);
                    }
                }
            }

            public double WidthOfBalanceColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfBalanceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfBalanceColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfBalanceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfBalanceColumn);
                    }
                }
            }

        }

        #endregion
    }
}
