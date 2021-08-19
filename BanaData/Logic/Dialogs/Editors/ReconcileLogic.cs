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
using BanaData.Logic.Items;
using BanaData.Logic.Controls;

namespace BanaData.Logic.Dialogs.Editors
{
    public class ReconcileLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household.AccountRow accountRow;
        private readonly decimal interestAmount;

        #endregion

        #region Constructor

        public ReconcileLogic(MainWindowLogic _mainWindowLogic, int accountID)
        {
            (mainWindowLogic, accountRow) = (_mainWindowLogic, _mainWindowLogic.Household.Account.FindByID(accountID));

            // Get account info
            PriorStatementBalance = accountRow.GetReconciledBalance();

            // Get reconcile info
            var reconcileInfo = accountRow.GetReconcileInfoRows().Single();
            StatementBalance = reconcileInfo.StatementBalance;
            interestAmount = reconcileInfo.IsInterestAmountNull() ? 0 : reconcileInfo.InterestAmount;

            Title = "Reconcile: " + accountRow.Name;

            // Build properties
            Payments = new ReconcileGridLogic(accountRow, "Payments:", BuildTransactionList(reconcileInfo, false));
            Payments.ClearedBalanceChanged += OnClearedBalanceChanged;

            Deposits = new ReconcileGridLogic(accountRow, "Deposits:", BuildTransactionList(reconcileInfo, true));
            Deposits.ClearedBalanceChanged += OnClearedBalanceChanged;

            MarkAll = new CommandBase(OnMarkAll);
            UnmarkAll = new CommandBase(OnUnmarkAll);
            FinishLaterCommand = new CommandBase(OnFinishLaterCommand);

            UpdateBalances();
        }

        private IEnumerable<TransactionToReconcile> BuildTransactionList(Household.ReconcileInfoRow reconcileInfoRow, bool deposit)
        {
            // Find all candidates
            var transactions = new List<TransactionToReconcile>();
            var household = mainWindowLogic.Household;

            // Process regular transactions
            foreach (Household.TransactionRow tr in accountRow.GetUnreconciledTransactions())
            {
                // Ignore transactions after the statement date
                if (tr.Date.CompareTo(reconcileInfoRow.StatementDate) > 0)
                {
                    continue;
                }

                // Compute amount
                var lineItems = tr.GetLineItemRows();
                decimal amount = lineItems.Sum(li => li.Amount);

                // We want only deposit or payments
                if (amount >= 0 ^ deposit)
                {
                    continue;
                }

                // Compute medium
                string medium = null;
                if (accountRow.Type == EAccountType.Bank)
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
                    null,
                    deposit ? amount : -amount);

                transactions.Add(transaction);
            }

            return transactions;
        }

        #endregion

        #region UI properties

        // Title
        public string Title { get; }

        // The deposit panel
        public ReconcileGridLogic Deposits { get; }

        // The payments panel
        public ReconcileGridLogic Payments { get; }

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
            foreach (var trList in new ReconcileGridLogic[] { Payments, Deposits })
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

            // Update the status of relevant transactions in the DB
            UpdateAllMarkedTransactionsTo(ETransactionStatus.Reconciled);

            // Get account and reconcile info rows
            var reconcileInfoRow = accountRow.GetReconcileInfoRows().Single();

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
            var latestCheckpoint = household.Checkpoint.GetMostRecentCheckpoint();

            foreach (var trList in new ReconcileGridLogic[] { Payments, Deposits })
            {
                foreach (TransactionToReconcile tr in trList.Transactions)
                {
                    var transRow = household.Transaction.FindByID(tr.ID);
                    transRow.Status = tr.IsCleared == true ? newStatus : ETransactionStatus.Pending;
                    transRow.CheckpointRow = latestCheckpoint;
                }
            }
        }

        private void AddInterestTransactionToDB(Household.AccountRow accountRow, Household.ReconcileInfoRow reconcileInfoRow)
        {
            var household = mainWindowLogic.Household;

            // Create new transaction row
            var transactionRow = household.Transaction.Add(
                accountRow, 
                reconcileInfoRow.InterestDate,
                "Interest Earned",
                null, 
                ETransactionStatus.Reconciled, 
                household.Checkpoint.GetMostRecentCheckpoint(),
                ETransactionType.Regular);

            // Create new banking transaction row
            household.BankingTransaction.Add(transactionRow, ETransactionMedium.None, 0);

            // Create the line item
            var liRow = household.LineItem.Add(transactionRow, null, interestAmount);

            // Create the category line item
            household.LineItemCategory.AddLineItemCategoryRow(liRow, household.Category.FindByID(reconcileInfoRow.InterestCategoryID));

            // Post newly created transaction ID
            InterestTransactionID = transactionRow.ID;
        }

        #endregion
    }
}
