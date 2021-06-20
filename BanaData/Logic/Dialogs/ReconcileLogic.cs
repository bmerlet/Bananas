using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Database;

namespace BanaData.Logic.Dialogs
{
    public class ReconcileLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly int accountID;

        #endregion

        #region Constructor

        public ReconcileLogic(MainWindowLogic _mainWindowLogic, int _accountID)
        {
            (mainWindowLogic, accountID) = (_mainWindowLogic, _accountID);

            // Get DB
            var household = mainWindowLogic.Household;

            // Get account info
            var accountRow = household.Accounts.FindByID(accountID);
            PriorStatementBalance = accountRow.GetBankingReconciledBalance();

            // Get reconcile info
            var accountsToReconcileInfo = household.ReconcileInfo.ParentRelations["FK_Accounts_ReconcileInfo"];
            var reconcileInfo = accountRow.GetChildRows(accountsToReconcileInfo).Cast<Household.ReconcileInfoRow>().First();
            StatementBalance = reconcileInfo.StatementBalance;

            Title = "Reconcile: " + accountRow.Name;

            // Build properties
            Payments = new TransactionsToReconcile(mainWindowLogic, accountID, false);
            Payments.ClearedBalanceChanged += OnClearedBalanceChanged;

            Deposits = new TransactionsToReconcile(mainWindowLogic, accountID, true);
            Deposits.ClearedBalanceChanged += OnClearedBalanceChanged;

            MarkAll = new CommandBase(OnMarkAll);
            UnmarkAll = new CommandBase(OnUnmarkAll);

            UpdateBalances();
        }

        #endregion

        #region UI properties

        // Title
        public string Title { get; }

        // The deposit panel
        public TransactionsToReconcile Deposits { get; }

        // The payments panel
        public TransactionsToReconcile Payments { get; }

        // Prior balance
        public decimal PriorStatementBalance { get; }

        // Cleared balance
        public decimal ClearedBalance { get; private set; }

        // Statement balance
        public decimal StatementBalance { get; }

        // Delta
        public decimal BalanceToClear { get; private set; }

        // Commands
        public CommandBase MarkAll { get; }
        public CommandBase UnmarkAll { get; }

        public CommandBase FinishLaterCommand { get; }

        #endregion

        #region Actions

        private void OnClearedBalanceChanged(object sender, EventArgs e)
        {
            UpdateBalances();
        }

        private void UpdateBalances()
        {
            ClearedBalance = PriorStatementBalance + Deposits.TotalCleared - Payments.TotalCleared;
            BalanceToClear = StatementBalance - ClearedBalance;

            OnPropertyChanged(() => ClearedBalance);
            OnPropertyChanged(() => BalanceToClear);
        }

        private void OnMarkAll()
        {
            OnMarkOrUnmarkAll(true);
        }

        private void OnUnmarkAll()
        {
            OnMarkOrUnmarkAll(false);
        }

        private void OnMarkOrUnmarkAll(bool mark)
        {
            foreach (var trList in new TransactionsToReconcile[] { Payments, Deposits })
            {
                foreach (TransactionToReconcile tr in trList.Transactions)
                {
                    if (tr.IsCleared != mark)
                    {
                        tr.IsCleared = mark;
                    }
                }
            }
        }

        protected override bool? Commit()
        {
            return true;
        }

        #endregion

        #region Supporting classes

        //
        // One transaction list
        //
        public class TransactionsToReconcile : LogicBase
        {
            public TransactionsToReconcile(MainWindowLogic mainWindowLogic, int accountID, bool deposit)
            {
                // Title
                Title = deposit ? "Deposits:" : "Payments:";

                // Find all candidates
                var household = mainWindowLogic.Household;
                var accountRow = household.Accounts.FindByID(accountID);
                IsBank = accountRow.Type == EAccountType.Bank;

                foreach (Household.TransactionsRow tr in accountRow.GetUnreconciledTransactions())
                {
                    // Compute amount
                    var lineItems = household.LineItems.GetByTransaction(tr);
                    decimal amount = lineItems.Sum(li => li.Amount);

                    // We want only deposit or payments
                    if (amount >= 0 ^ deposit)
                    {
                        continue;
                    }

                    // Compute medium
                    string medium = "";
                    if (IsBank)
                    {
                        var bankTrans = household.BankingTransactions.GetByTransaction(tr);
                        if (bankTrans.Medium == ETransactionMedium.Check && !bankTrans.IsCheckNumberNull())
                        {
                            medium = bankTrans.CheckNumber.ToString();
                        }
                        else
                        {
                            medium = Toolbox.Attributes.EnumDescriptionAttribute.GetDescription(bankTrans.Medium);
                        }
                    }

                    var transaction = new TransactionToReconcile(
                        tr.ID,
                        tr.Status == ETransactionStatus.Cleared,
                        tr.Date,
                        medium,
                        tr.IsPayeeNull() ? "" : tr.Payee,
                        deposit ? amount : -amount);

                    transaction.TransactionCleared += OnTransactionCleared;
                    transactions.Add(transaction);
                }

                // Give the transactions to the UI
                Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
                Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));

                // Compute initial total
                UpdateClearedTotal();
            }

            //
            // Events
            //
            public event EventHandler ClearedBalanceChanged;

            //
            // UI Properties
            //

            // Title
            public string Title { get; }

            // Transactions
            private readonly ObservableCollection<TransactionToReconcile> transactions = new ObservableCollection<TransactionToReconcile>();
            public CollectionView Transactions { get; }

            // Total cleared
            public decimal TotalCleared { get; private set; }
            public string NumberOfCheckedItems { get; private set; }

            // If this is a bank
            public bool IsBank { get; }
            public double MediumColumnWidth => IsBank ? 80 : 0;

            //
            // Actions
            //

            // Activated when the cleared status change for a transaction
            private void OnTransactionCleared(object sender, EventArgs e)
            {
                UpdateClearedTotal();
            }

            private void UpdateClearedTotal()
            {
                TotalCleared = transactions.Sum(tr => tr.IsCleared == true ? tr.DecimalAmount : 0);
                OnPropertyChanged(() => TotalCleared);

                NumberOfCheckedItems = "Cleared transactions: " + transactions.Count(tr => tr.IsCleared == true);
                OnPropertyChanged(() => NumberOfCheckedItems);

                ClearedBalanceChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        //
        // One transaction to reconcile
        //
        public class TransactionToReconcile : LogicBase
        {
            // Constructor
            public TransactionToReconcile(int id, bool _isCleared, DateTime date, string medium, string payee, decimal amount) =>
                (ID, isCleared, Date, Medium, Payee, DecimalAmount) =
                (id, _isCleared, date, medium, payee, amount);

            // Identifier - transaction ID
            public readonly int ID;
            public readonly decimal DecimalAmount;

            // Event
            public event EventHandler TransactionCleared;

            // Only modifiable UI property: If the item is checked
            private bool isCleared;
            public bool? IsCleared
            {
                get => isCleared;
                set
                {
                    if (isCleared != value)
                    {
                        isCleared = value == true;
                        OnPropertyChanged(() => IsCleared);
                        TransactionCleared?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            // Read-only UI properties
            public DateTime Date { get; }
            public string Medium { get; }
            public string Payee { get; }
            public string Amount => DecimalAmount.ToString("N");
        }

        #endregion
    }
}
