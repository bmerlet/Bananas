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
    public class BankRegisterLogic : LogicBase
    {
        #region Private members

        // Main logic
        private readonly MainWindowLogic mainWindowLogic;

        // Actual collection of transactions backing the Transactions collection view property
        private readonly ObservableCollection<BankingTransactionLogic> transactions = new ObservableCollection<BankingTransactionLogic>();

        // Account ID
        private int accountID;

        #endregion

        #region Constructor

        public BankRegisterLogic(MainWindowLogic mainWindowLogic)
        {
            this.mainWindowLogic = mainWindowLogic;

            // Create transaction collection view, and sort by date
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            Transactions.GroupDescriptions.Add(new PropertyGroupDescription("GroupSorter"));

            DeleteTransaction = new CommandBase(OnDeleteTransaction);
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
            get => IsBank ? mainWindowLogic.UserSettings.WidthOfMediumColumn : 0;
            set
            {
                if (IsBank && mainWindowLogic.UserSettings.WidthOfMediumColumn != value)
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

            // Export if banking account
            IsBank = account.Type == EAccountType.Bank;
            OnPropertyChanged(() => IsBank);
            OnPropertyChanged(() => WidthOfMediumColumn);

            // Find transactions and put them in the transaction list
            BankingTransactionLogic bankingTransaction = null;
            transactions.Clear();
            var accTransRel = household.Relations["FK_Accounts_Transactions"];
            foreach (Household.TransactionsRow trans in account.GetChildRows(accTransRel))
            {
                // Get banking details
                Household.BankingTransactionsRow transBank = null;
                if (account.Type == EAccountType.Bank)
                {
                    transBank = household.BankingTransactions.GetByTransaction(trans);
                }

                // Get line item(s)
                var dbLineItems = household.LineItems.GetByTransaction(trans);
                var lineItems = new List<LineItem>();
                foreach(var dbli in dbLineItems)
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

                var transactionData = new BankingTransactionLogic.BankTransactionData(
                    trans.Date,
                    transBank == null ? ETransactionMedium.None : transBank.Medium,
                    transBank == null ? 0 : (transBank.IsCheckNumberNull() ? 0 : (uint)transBank.CheckNumber),
                    trans.IsPayeeNull() ? "" : trans.Payee,
                    trans.Status,
                    lineItems);

                bankingTransaction = new BankingTransactionLogic(mainWindowLogic, this, accountID, trans.ID, transactionData);
                transactions.Add(bankingTransaction);
            }

            // Add new empty transaction at the bottom
            AddEmptyTransactionAtBottom();

            // Compute balances
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

            foreach(var tr in transactions)
            {
                if (tr.transID >= 0)
                {
                    var trRow = household.Transactions.FindByID(tr.transID);
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

        private void AddEmptyTransactionAtBottom()
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

            if (btl.transID < 0)
            {
                // Can't remove the empty transaction
                return;
            }

            // Cancel all changes
            btl.CancelEdit();

            // Delete from dataset
            var household = mainWindowLogic.Household;
            var transactionRow = household.Transactions.FindByID(btl.transID);

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
