using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs
{
    public class ReconcileInvestmentsLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household.AccountsRow accountRow;

        #endregion

        #region Constructor

        public ReconcileInvestmentsLogic(MainWindowLogic _mainWindowLogic, int accountID)
        {
            (mainWindowLogic, accountRow) = (_mainWindowLogic, _mainWindowLogic.Household.Accounts.FindByID(accountID));

            Title = "Reconcile: " + accountRow.Name;

            // Get DB
            var household = mainWindowLogic.Household;

            // Get account info
            decimal priorStatementCashBalance = accountRow.GetReconciledBalance();

            // Get reconcile info
            var reconcileInfo = accountRow.GetReconcileInfoRows().Single();
            decimal statementCashBalance = reconcileInfo.StatementBalance;

            // Add tracker for cash balance
            trackers.Add(new SecurityTracker("Cash", priorStatementCashBalance, 0, statementCashBalance, 0, "C2"));

            // Add tracker for securities
            var priorStatementPortfolio = accountRow.GetPortfolio(accountRow.IsLastStatementDateNull() ? reconcileInfo.StatementDate : accountRow.LastStatementDate);
            foreach(var securityReconcileInfo in reconcileInfo.GetSecurityReconcileInfoRows())
            {
                decimal prioStatementQuantity = priorStatementPortfolio.Lots.Where(l => l.Security == securityReconcileInfo.SecuritiesRow).Sum(l => l.Quantity);
                trackers.Add(new SecurityTracker(securityReconcileInfo.SecuritiesRow.Symbol, prioStatementQuantity, 0, securityReconcileInfo.SecurityQuantity, 0, "N4"));
            }

            TrackersSource = (CollectionView)CollectionViewSource.GetDefaultView(trackers);
            TrackersSource.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));


            // Build properties
            Transactions = new TransactionsToReconcile(mainWindowLogic, accountRow);
            Transactions.ClearedBalanceChanged += OnClearedBalanceChanged;

            MarkAll = new CommandBase(OnMarkAll);
            UnmarkAll = new CommandBase(OnUnmarkAll);
            FinishLaterCommand = new CommandBase(OnFinishLaterCommand);

            UpdateBalances();
        }

        #endregion

        #region UI properties

        // Title
        public string Title { get; }

        // The transactions panel
        public TransactionsToReconcile Transactions { get; }

        // The securities to reconcile
        private readonly ObservableCollection<SecurityTracker> trackers = new ObservableCollection<SecurityTracker>();
        public CollectionView TrackersSource { get; }


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
            foreach(var tracker in trackers)
            {
                if (tracker.Symbol == "Cash")
                {
                    tracker.ClearedBalance = tracker.PriorStatementBalance + Transactions.TotalCleared;
                    tracker.BalanceToClear = tracker.StatementBalance - tracker.ClearedBalance;
                }
                else
                {
                    decimal quantity = tracker.PriorStatementBalance;
                    foreach (TransactionToReconcile trans in Transactions.Transactions)
                    {
                        if (trans.IsCleared == true && !trans.IsTransferFillIn)
                        {
                            var tr = mainWindowLogic.Household.Transactions.FindByID(trans.ID);
                            var itr = tr.GetInvestmentTransaction();
                            if (!itr.IsSecurityIDNull() && itr.SecuritiesRow.Symbol == tracker.Symbol)
                            {
                                quantity += itr.IsSecurityIn ? itr.SecurityQuantity : -itr.SecurityQuantity;
                            }
                        }
                    }
                    tracker.ClearedBalance = quantity;
                    tracker.BalanceToClear = tracker.StatementBalance - tracker.ClearedBalance;
                }
            }
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
            foreach (TransactionToReconcile tr in Transactions.Transactions)
            {
                if (tr.IsCleared != mark)
                {
                    tr.IsCleared = mark;
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
            foreach (var tracker in trackers)
            {
                if (tracker.BalanceToClear != 0)
                {
                    mainWindowLogic.ErrorMessage("Balance is not cleared for " + tracker.Symbol);
                    return null;
                }
            }

            var household = mainWindowLogic.Household;

            // Update the status of relevant transactions in the DB
            UpdateAllMarkedTransactionsTo(ETransactionStatus.Reconciled);

            // Get reconcile info rows
            var reconcileInfoRow = accountRow.GetReconcileInfoRows().Single();

            // Update the last statement date in the account
            accountRow.LastStatementDate = reconcileInfoRow.StatementDate;

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

            foreach (TransactionToReconcile tr in Transactions.Transactions)
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

        #endregion

        #region Transaction list class

        public class TransactionsToReconcile : LogicBase
        {
            public TransactionsToReconcile(MainWindowLogic mainWindowLogic, Household.AccountsRow accountRow)
            {
                // Find all candidates
                var household = mainWindowLogic.Household;

                // Process regular transactions
                foreach (Household.TransactionsRow tr in accountRow.GetUnreconciledTransactions())
                {
                    var investmentTransactionRow = tr.GetInvestmentTransaction();

                    // Compute dollar amount
                    decimal amount = tr.GetAmount();
                    if (investmentTransactionRow.IsCashOut)
                    {
                        amount = -amount;
                    }
                    else if (investmentTransactionRow.IsTransferIn || investmentTransactionRow.IsTransferOut)
                    {
                        // transfers are cash-neutral
                        amount = 0;
                    }

                    var transaction = new TransactionToReconcile(
                        tr.ID,
                        tr.Status == ETransactionStatus.Cleared,
                        tr.Date,
                        null,
                        tr.IsPayeeNull() ? "" : tr.Payee, // ZZZZZZZZZZZ
                        "Description",
                        amount,
                        false);

                    transaction.TransactionCleared += OnTransactionCleared;
                    transactions.Add(transaction);
                }

                // Process transfer fill-ins
                foreach (Household.LineItemsRow li in accountRow.GetUnreconciledTransfers())
                {
                    decimal amount = -li.Amount;

                    var transaction = new TransactionToReconcile(
                        li.ID,
                        li.TransferStatus == ETransactionStatus.Cleared,
                        li.TransactionsRow.Date,
                        "",
                        "",
                        "Description",
                        amount,
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
            public string Title { get; } = "Transactions";

            // Transactions
            private readonly ObservableCollection<TransactionToReconcile> transactions = new ObservableCollection<TransactionToReconcile>();
            public CollectionView Transactions { get; }

            // Total cleared
            public decimal TotalCleared { get; private set; }
            public string NumberOfCheckedItems { get; private set; }

            // If this is a bank
            public bool IsBank => false;
            public double MediumColumnWidth => 0;

            public string DescriptionColumnName => "Description";

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

        #region Tracker class 
        
        // Tracker for one security or cash
        public class SecurityTracker : LogicBase
        {
            public SecurityTracker(string symbol, decimal priorStatementBalance, decimal _clearedBalance, decimal statementBalance, decimal _balanceToClear, string formatString) =>
                (Symbol, PriorStatementBalance, clearedBalance, StatementBalance, balanceToClear, FormatString) =
                (symbol, priorStatementBalance, _clearedBalance, statementBalance, _balanceToClear, formatString);

            // Security symbol
            public string Symbol { get; }

            // Prior balance
            public decimal PriorStatementBalance { get; }

            // Cleared balance
            private decimal clearedBalance;
            public decimal ClearedBalance 
            { 
                get => clearedBalance;
                set { clearedBalance = value; OnPropertyChanged(() => ClearedBalance); } 
            }

            // Statement balance
            public decimal StatementBalance { get; }

            // Delta
            private decimal balanceToClear;
            public decimal BalanceToClear
            {
                get => balanceToClear;
                set { balanceToClear = value; OnPropertyChanged(() => BalanceToClear); }
            }

            // Format string to use
            public string FormatString { get; }
        }

        #endregion

    }
}
