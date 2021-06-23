using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Items;

namespace BanaData.Logic.Main
{
    public class InvestmentRegisterLogic : LogicBase
    {
        #region Private members

        // Main logic
        private readonly MainWindowLogic mainWindowLogic;

        // Actual collection of transactions backing the Transactions collection view property
        private readonly ObservableCollection<InvestmentTransactionLogic> transactions = new ObservableCollection<InvestmentTransactionLogic>();

        // Account ID
        private int accountID;

        #endregion

        #region Constructor

        public InvestmentRegisterLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Create transaction collection view, and sort by date
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            Transactions.GroupDescriptions.Add(new PropertyGroupDescription("GroupSorter"));

            DeleteTransaction = new CommandBase(OnDeleteTransaction);

            // Create column width manager
            Widths = new ColumnWidths(mainWindowLogic);
        }

        #endregion

        #region UI properties

        // Name of the account
        public string AccountName { get; private set; }

        // If banking account (as opposed to credit card)
        public bool IsBank { get; private set; }

        // Transactions. The CollectionView type enables sorting on columns
        public CollectionView Transactions { get; }

        // Selected transaction
        private InvestmentTransactionLogic selectedTransaction;
        private bool logicIsChangingSelection;
        public InvestmentTransactionLogic SelectedTransaction
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
        public InvestmentTransactionLogic TransactionToScrollTo { get; private set; }

        // Transaction being edited
        private InvestmentTransactionLogic editedTransaction;
        public InvestmentTransactionLogic EditedTransaction
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

        public void SetAccount(int _accountID)
        {
            // Remember
            accountID = _accountID;

            // Get the account details
            var household = mainWindowLogic.Household;
            var account = household.Accounts.FindByID(accountID);

            // Export account name
            AccountName = account.Name;
            OnPropertyChanged(() => AccountName);

            // Find transactions and put them in the transaction list
            transactions.Clear();
            var accTransRel = household.Relations["FK_Accounts_Transactions"];

            foreach (Household.TransactionsRow trans in account.GetChildRows(accTransRel))
            {
                AddDBTransactionToList(account, trans);
            }

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
            var trans = household.Transactions.FindByID(transactionID);

            AddDBTransactionToList(account, trans);
        }

        // Routine to get a transaction from the DB into the list
        private void AddDBTransactionToList(Household.AccountsRow accountRow, Household.TransactionsRow transRow)
        {
            var household = mainWindowLogic.Household;

            // Get banking details
            Household.BankingTransactionsRow transBankRow = null;
            if (accountRow.Type == EAccountType.Bank)
            {
                transBankRow = household.BankingTransactions.GetByTransaction(transRow);
            }

            // Get line item(s)
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

            // ZZZZZZZZZZZZZZ
            var transactionData = new InvestmentTransactionLogic.InvestmentTransactionData(
                transRow.Date,
                transRow.IsPayeeNull() ? "" : transRow.Payee,
                transRow.Status,
                lineItems,
                EInvestmentTransactionType.Buy,
                -1, // SecurityID
                0, // SecurityPrice
                0, // security quantity
                0); // Commission

            var bankingTransaction = new InvestmentTransactionLogic(mainWindowLogic, this, accountID, transRow.ID, transactionData);
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

            foreach (var tr in transactions)
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

        public void RemoveTransactionFromList(InvestmentTransactionLogic itl)
        {
            if (SelectedTransaction == itl)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = null;
                logicIsChangingSelection = false;
            }
            transactions.Remove(itl);
        }

        public void AddTransactionBackToList(InvestmentTransactionLogic itl)
        {
            transactions.Add(itl);
        }

        private void AddEmptyTransactionAtBottom()
        {
            // Add new empty transaction at the bottom
            var emptyTransaction = new InvestmentTransactionLogic(mainWindowLogic, this, accountID);
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
            InvestmentTransactionLogic btl = arg == null ? EditedTransaction : arg as InvestmentTransactionLogic;
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
            foreach (var lineItem in lineItems)
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

        #region Supporting classes

        public class ColumnWidths : LogicBase
        {
            private readonly MainWindowLogic mainWindowLogic;

            public ColumnWidths(MainWindowLogic _mainWindowLogic) => mainWindowLogic = _mainWindowLogic;

            public double WidthOfStatusColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfStatusColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfStatusColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfStatusColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfStatusColumn);
                    }
                }
            }

            public double WidthOfDateColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfDateColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfDateColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfDateColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDateColumn);
                    }
                }
            }

            public double WidthOfTypeColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfTypeColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfTypeColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfTypeColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfTypeColumn);
                    }
                }
            }

            public double WidthOfPayeeColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfPayeeColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfPayeeColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfPayeeColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfPayeeColumn);
                    }
                }
            }

            public double WidthOfMemoColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfMemoColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfMemoColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfMemoColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfMemoColumn);
                    }
                }
            }

            public double WidthOfCategoryColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfCategoryColumn);
                    }
                }
            }

            public double WidthOfPaymentColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfPaymentColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfPaymentColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfPaymentColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfPaymentColumn);
                    }
                }
            }

            public double WidthOfDepositColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfDepositColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfDepositColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfDepositColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDepositColumn);
                    }
                }
            }

            public double WidthOfBalanceColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfBalanceColumn);
                    }
                }
            }

        }

        #endregion
    }
}
