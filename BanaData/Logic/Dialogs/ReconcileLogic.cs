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
        private readonly decimal interestAmount;

        #endregion

        #region Constructor

        public ReconcileLogic(MainWindowLogic _mainWindowLogic, int _accountID)
        {
            (mainWindowLogic, accountID) = (_mainWindowLogic, _accountID);

            // Get DB
            var household = mainWindowLogic.Household;

            // Get account info
            var accountRow = household.Accounts.FindByID(accountID);
            PriorStatementBalance = accountRow.GetReconciledBalance();

            // Get reconcile info
            var accountsToReconcileInfo = household.ReconcileInfo.ParentRelations["FK_Accounts_ReconcileInfo"];
            var reconcileInfo = accountRow.GetChildRows(accountsToReconcileInfo).Cast<Household.ReconcileInfoRow>().First();
            StatementBalance = reconcileInfo.StatementBalance;
            interestAmount = reconcileInfo.IsInterestAmountNull() ? 0 : reconcileInfo.InterestAmount;

            Title = "Reconcile: " + accountRow.Name;

            // Build properties
            Payments = new TransactionsToReconcile(mainWindowLogic, accountID, false);
            Payments.ClearedBalanceChanged += OnClearedBalanceChanged;

            Deposits = new TransactionsToReconcile(mainWindowLogic, accountID, true);
            Deposits.ClearedBalanceChanged += OnClearedBalanceChanged;

            MarkAll = new CommandBase(OnMarkAll);
            UnmarkAll = new CommandBase(OnUnmarkAll);
            FinishLaterCommand = new CommandBase(OnFinishLaterCommand);

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

        #region Results

        public int InterestTransactionID { get; private set; } = -1;

        #endregion

        #region Actions

        private void OnClearedBalanceChanged(object sender, EventArgs e)
        {
            UpdateBalances();
        }

        private void UpdateBalances()
        {
            ClearedBalance = PriorStatementBalance + Deposits.TotalCleared + interestAmount - Payments.TotalCleared;
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

        // Propagate the cleared transaction to the DB and the register view
        private void OnFinishLaterCommand()
        {
            var household = mainWindowLogic.Household;

            // Update the status of relevant transactions in the DB
            UpdateAllMarkedTransactionsTo(ETransactionStatus.Cleared);

            // Update DB
            mainWindowLogic.CommitChanges();

            // Close the dialog indicating change
            CloseView(true);
        }

        protected override bool? Commit()
        {
            if (BalanceToClear != 0)
            {
                mainWindowLogic.ErrorMessage("Balance is not cleared");
                return null;
            }

            var household = mainWindowLogic.Household;

            // Update the status of relevant transactions in the DB
            UpdateAllMarkedTransactionsTo(ETransactionStatus.Reconciled);

            // Get account and reconcile info rows
            var accountRow = household.Accounts.FindByID(accountID);
            var accountsToReconcileInfo = household.ReconcileInfo.ParentRelations["FK_Accounts_ReconcileInfo"];
            var reconcileInfoRow = accountRow.GetChildRows(accountsToReconcileInfo).Cast<Household.ReconcileInfoRow>().First();

            // Update the last statement date in the account
            accountRow.LastStatementDate = reconcileInfoRow.StatementDate;

            // Create the interest transaction if there is one
            if (interestAmount != 0)
            {
                AddInterestTransactionToDB(accountRow, reconcileInfoRow);
            }

            // Delete the current reconcile info since we are done
            reconcileInfoRow.Delete();

            // Update DB
            mainWindowLogic.CommitChanges();

            // Close the dialog indicating change
            return true;
        }

        private void UpdateAllMarkedTransactionsTo(ETransactionStatus newStatus)
        {
            var household = mainWindowLogic.Household;

            foreach (var trList in new TransactionsToReconcile[] { Payments, Deposits })
            {
                foreach (TransactionToReconcile tr in trList.Transactions)
                {
                    if (tr.IsTransferFillIn)
                    {
                        var liRow = household.LineItems.FindByID(tr.ID);
                        liRow.TransferStatus = tr.IsCleared == true ? newStatus : ETransactionStatus.Pending;
                    }
                    else
                    {
                        var transRow = household.Transactions.FindByID(tr.ID);
                        transRow.Status = tr.IsCleared == true ? newStatus : ETransactionStatus.Pending;
                    }
                }
            }
        }

        private void AddInterestTransactionToDB(Household.AccountsRow accountRow, Household.ReconcileInfoRow reconcileInfoRow)
        {
            var household = mainWindowLogic.Household;

            // Create new transaction row
            var transactionRow = household.Transactions.Add(accountRow, reconcileInfoRow.InterestDate, "Interest Earned", null, ETransactionStatus.Reconciled);

            // Create new banking transaction row
            household.BankingTransactions.Add(transactionRow,ETransactionMedium.None, 0);

            // Create the line items
            household.LineItems.Add(transactionRow, reconcileInfoRow.InterestCategoryID, -1, "", interestAmount);

            // Post newly created transaction ID
            InterestTransactionID = transactionRow.ID;
        }

        #endregion

        #region Transaction list class

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

                // Process regular transactions
                foreach (Household.TransactionsRow tr in accountRow.GetUnreconciledTransactions())
                {
                    // Compute amount
                    var lineItems = tr.GetLineItemsRows();
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
                        var bankTrans = tr.GetBankingTransaction();
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
                        deposit ? amount : -amount,
                        false);

                    transaction.TransactionCleared += OnTransactionCleared;
                    transactions.Add(transaction);
                }

                // Process transfer fill-ins
                foreach(Household.LineItemsRow li in accountRow.GetUnreconciledTransfers())
                {
                    decimal amount = -li.Amount;

                    // We want only deposit or payments
                    if (amount >= 0 ^ deposit)
                    {
                        continue;
                    }

                    var transaction = new TransactionToReconcile(
                        li.ID,
                        li.TransferStatus == ETransactionStatus.Cleared,
                        li.TransactionsRow.Date,
                        "",
                        "",
                        deposit ? amount : -amount,
                        true);

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
                TotalCleared = transactions.Sum(tr => tr.IsCleared == true ? tr.Amount : 0);
                OnPropertyChanged(() => TotalCleared);

                NumberOfCheckedItems = "Cleared transactions: " + transactions.Count(tr => tr.IsCleared == true);
                OnPropertyChanged(() => NumberOfCheckedItems);

                ClearedBalanceChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Transaction class 

        //
        // One transaction to reconcile
        //
        public class TransactionToReconcile : LogicBase
        {
            // Constructor
            public TransactionToReconcile(int id, bool _isCleared, DateTime date, string medium, string payee, decimal amount, bool isTransferFillIn) =>
                (ID, isCleared, Date, Medium, Payee, Amount, IsTransferFillIn) =
                (id, _isCleared, date, medium, payee, amount, isTransferFillIn);

            // Identifier - transaction ID
            public readonly int ID;
            public bool IsTransferFillIn;

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
            public decimal Amount { get; }
        }

        #endregion
    }
}
