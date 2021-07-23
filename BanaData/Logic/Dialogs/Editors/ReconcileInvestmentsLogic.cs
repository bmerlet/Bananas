using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using BanaData.Database;
using BanaData.Logic.Controls;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Editors
{
    public class ReconcileInvestmentsLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household.AccountRow accountRow;

        #endregion

        #region Constructor

        public ReconcileInvestmentsLogic(MainWindowLogic _mainWindowLogic, int accountID)
        {
            (mainWindowLogic, accountRow) = (_mainWindowLogic, _mainWindowLogic.Household.Account.FindByID(accountID));

            Title = "Reconcile: " + accountRow.Name;

            // Get account info
            decimal priorStatementCashBalance = accountRow.GetReconciledBalance();

            // Get reconcile info
            var reconcileInfo = accountRow.GetReconcileInfoRows().Single();
            decimal statementCashBalance = reconcileInfo.StatementBalance;

            // Create tracker for cash balance
            trackers.Add(new SecurityTracker("Cash", "Cash", priorStatementCashBalance, 0, statementCashBalance, 0, "C2"));

            // Add trackers for securities
            var reconciledPortfolio = accountRow.GetPortfolio(null, null, ETransactionStatus.Reconciled);
            foreach (var securityReconcileInfo in reconcileInfo.GetSecurityReconcileInfoRows())
            {
                decimal prioStatementQuantity = reconciledPortfolio.Lots.Where(l => l.Security == securityReconcileInfo.SecurityRow).Sum(l => l.Quantity);
                trackers.Add(new SecurityTracker(
                    securityReconcileInfo.SecurityRow.Name,
                    securityReconcileInfo.SecurityRow.Symbol,
                    prioStatementQuantity, 0,
                    securityReconcileInfo.SecurityQuantity, 0, "N4"));
            }

            TrackersSource = (CollectionView)CollectionViewSource.GetDefaultView(trackers);
            TrackersSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));


            // Build transaction list
            Transactions = new ReconcileGridLogic(accountRow, "Transactions:", BuildTransactionList(reconcileInfo));
            Transactions.ClearedBalanceChanged += OnClearedBalanceChanged;

            MarkAll = new CommandBase(OnMarkAll);
            UnmarkAll = new CommandBase(OnUnmarkAll);
            FinishLaterCommand = new CommandBase(OnFinishLaterCommand);

            UpdateBalances();
        }

        private IEnumerable<TransactionToReconcile> BuildTransactionList(Household.ReconcileInfoRow reconcileInfoRow)
        {
            // Find all candidates
            var transactions = new List<TransactionToReconcile>();
            // Process regular transactions
            foreach (Household.TransactionRow tr in accountRow.GetUnreconciledTransactions())
            {
                // Ignore transactions after the statement date
                if (tr.Date.CompareTo(reconcileInfoRow.StatementDate) > 0)
                {
                    continue;
                }

                var investmentTransactionRow = tr.GetInvestmentTransaction();

                // Compute dollar amount
                decimal amount = 0;
                if (investmentTransactionRow.IsCashOut || investmentTransactionRow.IsCashIn)
                {
                    amount = tr.GetAmount();
                }

                var transaction = new TransactionToReconcile(
                    tr.ID,
                    tr.Status == ETransactionStatus.Cleared,
                    tr.Date,
                    null,
                    investmentTransactionRow.GetDescription(),
                    investmentTransactionRow.IsSecurityIDNull() ? null : investmentTransactionRow.SecurityRow.Symbol,
                    amount);

                transactions.Add(transaction);
            }

            return transactions;
        }

        #endregion

        #region UI properties

        // Title
        public string Title { get; }

        // The transactions panel
        public ReconcileGridLogic Transactions { get; }

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
            foreach (var tracker in trackers)
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
                        if (trans.IsCleared == true)
                        {
                            var tr = mainWindowLogic.Household.Transaction.FindByID(trans.ID);
                            var itr = tr.GetInvestmentTransaction();
                            if (!itr.IsSecurityIDNull() && itr.SecurityRow.Symbol == tracker.Symbol)
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
            var latestCheckpoint = household.Checkpoint.GetMostRecentCheckpointID();

            foreach (TransactionToReconcile tr in Transactions.Transactions)
            {
                var transRow = household.Transaction.FindByID(tr.ID);
                transRow.Status = tr.IsCleared == true ? newStatus : ETransactionStatus.Pending;
                transRow.CheckpointID = latestCheckpoint;
            }
        }

        #endregion

        #region Tracker class 

        // Tracker for one security or cash
        public class SecurityTracker : LogicBase
        {
            public SecurityTracker(string name, string symbol, decimal priorStatementBalance, decimal _clearedBalance, decimal statementBalance, decimal _balanceToClear, string formatString) =>
                (Name, Symbol, PriorStatementBalance, clearedBalance, StatementBalance, balanceToClear, FormatString) =
                (name, symbol, priorStatementBalance, _clearedBalance, statementBalance, _balanceToClear, formatString);

            // Security name
            public string Name { get; }

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
