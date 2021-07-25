using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BanaData.Database;
using BanaData.Logic.Items;
using Toolbox.UILogic;

namespace BanaData.Logic.Main
{
    public abstract class AbstractTransactionLogic : LogicBase, IEditableObject
    {
        #region Protected members

        // Parent logic
        protected readonly MainWindowLogic mainWindowLogic;

        // Account this transaction is for
        protected readonly Household.AccountRow accountRow;

        // Transaction data
        protected readonly BaseTransactionData data;

        // Backup of data (taken at edit start)
        protected BaseTransactionData backup;

        public const int TRANSID_NOT_COMMITTED = -1;
 
        #endregion

        #region Constructor

        protected AbstractTransactionLogic(
            MainWindowLogic _mainWindowLogic, Household.AccountRow _accountRow, int _transID, BaseTransactionData _data)
        {
            (mainWindowLogic, accountRow, TransID, data) = (_mainWindowLogic, _accountRow, _transID, _data);

            GotoOtherSideOfTransfer = new CommandBase(OnGotoOtherSideOfTransfer);
        }

        #endregion

        #region Logic properties

        // Transaction id, -1 if not in DB yet, -2 if transfer fill-in
        public int TransID;

        // Amount to use for cash balance computation
        public abstract decimal AmountForCashBalance { get; }

        #endregion

        #region UI properties

        // Date of the transaction
        public DateTime Date
        {
            get => data.Date;
            set { data.Date = value; OnDateChanged(); }
        }

        // Payee
        public string Payee
        {
            get => data.Payee;
            set => data.Payee = value;
        }

        // Memo
        public string Memo
        {
            get => data.Memo;
            set => data.Memo = value;
        }

        // Status
        public string Status
        {
            get => GetStatusString();
            set => ParseStatusString(value);
        }

        // Amount, as implemented by derived classes
        public abstract decimal Amount { get; set; }

        public string[] StatusSource { get; } = new string[] { "", "c", "R" };

        // Balance
        // BalanceString is the UI property, Balance is updated by the logic
        private decimal balance = decimal.MinValue;
        public decimal Balance
        {
            get => balance;
            set
            {
                if (balance != value)
                {
                    balance = value;
                    BalanceString = balance.ToString("N");
                    OnPropertyChanged(() => BalanceString);
                    OnPropertyChanged(() => BalanceState);
                }
            }
        }

        public string BalanceString { get; private set; } = "";

        // Group sorter
        // To have the uncommitted transaction in a different group than the others
        // And always displayed at the bottom of the listview
        // (see PropertyGroupDescription in BankRegisterLogic constructor)
        public string GroupSorter => (TransID == -1) ? "Z" : "A";

        // Composite transaction status, for the forecolor of the transaction
        public ETransactionState TransactionState =>
            (data.Status == ETransactionStatus.Reconciled ? ETransactionState.Reconciled : ETransactionState.Idle);

        // Composite transaction status, for the forecolor of the amount
        public ETransactionState AmountState => TransactionState | (Amount < 0 ? ETransactionState.NegativeAmount : ETransactionState.Idle);

        // Composite transaction status, for the forecolor of the balance
        public ETransactionState BalanceState => TransactionState | (balance < 0 ? ETransactionState.NegativeAmount : ETransactionState.Idle);

        // Commands
        public CommandBase GotoOtherSideOfTransfer { get; }

        #endregion

        #region Abstract implementation of IEditableObject

        public abstract void BeginEdit();

        public virtual void CancelEdit()
        {
            // Restore data
            if (backup != null)
            {
                if (data.Date != backup.Date)
                {
                    data.Date = backup.Date;
                    OnPropertyChanged(() => Date);
                }

                if (data.Payee != backup.Payee)
                {
                    data.Payee = backup.Payee;
                    OnPropertyChanged(() => Payee);
                }

                if (data.Memo != backup.Memo)
                {
                    data.Memo = backup.Memo;
                    OnPropertyChanged(() => Memo);
                }

                if (data.Status != backup.Status)
                {
                    data.Status = backup.Status;
                    OnPropertyChanged(() => Status);
                }
            }
        }

        // Provided by derived classes to know if the transaction was changed
        public abstract bool HasTransactionChanged { get; }

        // Provided by derived classes to know if the transaction should be committed
        public abstract bool DoesTransactionNeedComit { get; }


        public virtual void EndEdit()
        {
            if (data.Date != backup.Date)
            {
                OnPropertyChanged(() => Date);
            }

            if (data.Payee != backup.Payee)
            {
                OnPropertyChanged(() => Payee);
            }

            if (data.Memo != backup.Memo)
            {
                OnPropertyChanged(() => Memo);
            }

            if (data.Memo != backup.Memo)
            {
                OnPropertyChanged(() => Memo);
            }

            if (data.Status != backup.Status)
            {
                OnPropertyChanged(() => Status);
            }

            if (mainWindowLogic.UserSettings.PlayKaChingSound)
            {
                mainWindowLogic.GuiServices.KaChing();
            }
        }

        protected void CreateLineItemInDB(LineItem li, Household.TransactionRow transactionRow, List<int> impactedAccounts)
        {
            var household = mainWindowLogic.Household;

            var liRow = household.LineItem.Add(transactionRow, li.Memo, li.Amount);
            li.ID = liRow.ID;

            if (li.CategoryID != -1)
            {
                household.LineItemCategory.AddLineItemCategoryRow(liRow, household.Category.FindByID(li.CategoryID));
            }
            else if (li.CategoryAccountID != -1)
            {
                impactedAccounts.Add(li.CategoryAccountID);
                CreatePeerTransaction(li.CategoryAccountID, transactionRow, liRow, -li.Amount);
            }
        }

        protected void UpdateLineItemInDB(LineItem li, Household.TransactionRow transactionRow, List<int> impactedAccounts)
        {
            var household = mainWindowLogic.Household;

            var liRow = household.LineItem.FindByID(li.ID);
            var liCategoryRow = liRow.GetLineItemCategoryRow();
            var liTransferRow = liRow.GetLineItemTransferRow();

            // Update the line item
            household.LineItem.Update(liRow, transactionRow, li.Memo, li.Amount);

            // See if the updated transaction is a transfer.
            if (li.CategoryAccountID != -1)
            {
                impactedAccounts.Add(li.CategoryAccountID);

                // Delete former category row if it existed
                if (liCategoryRow != null)
                {
                    liCategoryRow.Delete();
                }

                if (liTransferRow == null)
                {
                    // The line item was not a transfer: Make it one
                    CreatePeerTransaction(li.CategoryAccountID, transactionRow, liRow, -li.Amount);
                }
                else
                {
                    // The line item was a transfer. Update the account, amount and memo in peer transaction
                    // Note: this kind of breaks down if the peer transaction has several transfers to the same account,
                    // which is why the split editor forbids it.
                    var peerTransactionRow = liTransferRow.TransactionRow;
                    if (liTransferRow.AccountID != peerTransactionRow.AccountID)
                    {
                        impactedAccounts.Add(liTransferRow.TransactionRow.AccountID);
                        peerTransactionRow.AccountID = liTransferRow.AccountID;
                    }
                    if (string.IsNullOrWhiteSpace(data.Memo))
                    {
                        peerTransactionRow.SetMemoNull();
                    }
                    else
                    {
                        peerTransactionRow.Memo = data.Memo;
                    }

                    foreach(var peerLineItemRow in peerTransactionRow.GetLineItemRows())
                    {
                        if (peerLineItemRow.GetLineItemTransferRow() is Household.LineItemTransferRow lineItemTransferRow &&
                            lineItemTransferRow.AccountRow == accountRow)
                        {
                            peerLineItemRow.Amount = -li.Amount;
                            if (string.IsNullOrEmpty(li.Memo))
                            {
                                peerLineItemRow.SetMemoNull();
                            }
                            else
                            {
                                peerLineItemRow.Memo = li.Memo;
                            }
                            break;
                        }
                    }
                }
            }
            else if (li.CategoryID != -1)
            {
                // The updated transaction uses a regular category
                if (liTransferRow != null)
                {
                    // The transaction used to be a transfer: Delete the peer transaction
                    impactedAccounts.Add(liTransferRow.AccountID);
                    DeletePeerTransaction(liTransferRow);
                    liTransferRow.Delete();
                }

                if (liCategoryRow == null)
                {
                    // Create new category line item
                    household.LineItemCategory.AddLineItemCategoryRow(liRow, household.Category.FindByID(li.CategoryID));
                }
                else
                {
                    // update category line item
                    liCategoryRow.CategoryID = li.CategoryID;
                }
            }
            else
            {
                // The updated transaction has no category and no transfer
                if (liCategoryRow != null)
                {
                    liCategoryRow.Delete();
                }

                if (liTransferRow != null)
                {
                    // The transaction used to be a transfer: Delete the peer transaction
                    impactedAccounts.Add(liTransferRow.AccountID);
                    DeletePeerTransaction(liTransferRow);
                    liTransferRow.Delete();
                }
            }
        }

        private Household.TransactionRow CreatePeerTransaction(int targetAccountID, Household.TransactionRow transactionRow, Household.LineItemRow liRow, decimal peerAmount)
        {
            var household = mainWindowLogic.Household;

            var targetAccountRow = household.Account.FindByID(targetAccountID);

            // Add transaction on "other side"
            var peerTransactionRow = household.Transaction.Add(targetAccountRow, data.Date, "", data.Memo, ETransactionStatus.Pending, household.Checkpoint.GetMostRecentCheckpointID());
            var peerLiRow = household.LineItem.Add(peerTransactionRow, null, peerAmount);

            // Create the investment/banking transactions
            if (targetAccountRow.Type == EAccountType.Bank)
            {
                household.BankingTransaction.Add(peerTransactionRow, ETransactionMedium.None, 0);
            }
            else if (targetAccountRow.Type == EAccountType.Investment)
            {
                var type = peerLiRow.Amount >= 0 ? EInvestmentTransactionType.TransferCashIn : EInvestmentTransactionType.TransferCashOut;
                household.InvestmentTransaction.Add(peerTransactionRow, type, null, 0, 0, 0);
            }

            // Create the transfer line items
            household.LineItemTransfer.AddLineItemTransferRow(liRow, targetAccountRow, peerTransactionRow);
            household.LineItemTransfer.AddLineItemTransferRow(peerLiRow, accountRow, transactionRow);

            return peerTransactionRow;
        }

        #endregion

        #region Actions

        // Called by on behalf of reconcile to update status on transaction if needed
        public void UpdateStatus()
        {
            var household = mainWindowLogic.Household;

            if (TransID != TRANSID_NOT_COMMITTED)
            {
                var trRow = household.Transaction.FindByID(TransID);

                if (data.Status != trRow.Status)
                {
                    data.Status = trRow.Status;
                    OnPropertyChanged(() => Status);
                }
            }
        }

        public void DeleteTransactionFromDataset(int transID)
        {
            // Remember impacted accounts
            var impactedAccounts = new List<int>
            {
                accountRow.ID
            };

            // Delete from dataset
            var household = mainWindowLogic.Household;
            var transactionRow = household.Transaction.FindByID(transID);

            // Delete all line items
            var lineItems = transactionRow.GetLineItemRows();
            foreach (var lineItem in lineItems)
            {
                if (lineItem.GetLineItemCategoryRow() is Household.LineItemCategoryRow lineItemCategoryRow)
                {
                    lineItemCategoryRow.Delete();
                }

                if (lineItem.GetLineItemTransferRow() is Household.LineItemTransferRow lineItemTransferRow)
                {
                    // Remeber the target account is impacted
                    impactedAccounts.Add(lineItemTransferRow.AccountID);

                    // Also delete peer transaction or peer line item
                    DeletePeerTransaction(lineItemTransferRow);
                    lineItemTransferRow.Delete();
                }

                lineItem.Delete();
            }

            // Delete banking or investment transaction
            if (accountRow.Type == EAccountType.Bank)
            {
                transactionRow.GetBankingTransaction().Delete();
            }
            else if (accountRow.Type == EAccountType.Investment)
            {
                transactionRow.GetInvestmentTransaction().Delete();
            }

            // Finally delete the transaction
            transactionRow.Delete();

            mainWindowLogic.CommitChanges();

            mainWindowLogic.UpdateAccountNamessAndBalances(impactedAccounts);
        }

        protected void DeletePeerTransaction(Household.LineItemTransferRow lineItemTransferRow)
        {
            var peerTransactionRow = lineItemTransferRow.TransactionRow;
            var peerLineItemRows = peerTransactionRow.GetLineItemRows();

            foreach (var peerLineItemRow in peerLineItemRows)
            {
                // Find and delete the peer line item
                if (peerLineItemRow.GetLineItemTransferRow() is Household.LineItemTransferRow peerLineItemTransferRow  &&
                    lineItemTransferRow.AccountRow == accountRow)
                {
                    peerLineItemTransferRow.Delete();
                    peerLineItemRow.Delete();
                    break;
                }
            }

            // Also delete the transaction if only one line item
            if (peerLineItemRows.Length == 1)
            {
                if (peerTransactionRow.AccountRow.Type == EAccountType.Bank)
                {
                    peerTransactionRow.GetBankingTransaction().Delete();
                }
                else if (peerTransactionRow.AccountRow.Type == EAccountType.Investment)
                {
                    peerTransactionRow.GetInvestmentTransaction().Delete();
                }
                peerTransactionRow.Delete();
            }
        }

        private void OnGotoOtherSideOfTransfer()
        {
            var household = mainWindowLogic.Household;
            var transRow = household.Transaction.FindByID(TransID);

            foreach (var liRow in transRow.GetLineItemRows())
            {
                if (liRow.GetLineItemTransferRow() is Household.LineItemTransferRow lineItemTransferRow)
                {
                    mainWindowLogic.GotoTransaction(lineItemTransferRow.AccountID, lineItemTransferRow.PeerTransID);
                    break;
                }
            }
        }

        // Status string management
        private string GetStatusString()
        {
            string rs = "???";

            switch (data.Status)
            {
                case ETransactionStatus.Pending:
                    rs = "";
                    break;
                case ETransactionStatus.Cleared:
                    rs = "c";
                    break;
                case ETransactionStatus.Reconciled:
                    rs = "R";
                    break;
            }
            return rs;
        }

        private void ParseStatusString(string status)
        {
            switch (status)
            {
                case "":
                    data.Status = ETransactionStatus.Pending;
                    break;
                case "c":
                    data.Status = ETransactionStatus.Cleared;
                    break;
                case "R":
                    data.Status = ETransactionStatus.Reconciled;
                    break;
            }
            OnPropertyChanged(() => Status);
        }

        // Used in investment derived class
        protected virtual void OnDateChanged() { }

        #endregion

        #region Data base class

        public class BaseTransactionData
        {
            // Explicit constructor
            protected BaseTransactionData(
                DateTime date,
                string payee,
                string memo,
                ETransactionStatus status) =>
                (Date, Payee, Memo, Status) =
                    (date, payee, memo, status);

            // Clone
            protected BaseTransactionData(BaseTransactionData src)
            {
                (Date, Payee, Memo, Status) =
                    (src.Date, src.Payee, src.Memo, src.Status);
            }

            // Properties
            public DateTime Date;
            public string Payee;
            public string Memo;
            public ETransactionStatus Status;

            public override bool Equals(object obj)
            {
                return
                    obj is BaseTransactionData o &&
                        o.Date.Equals(Date) &&
                        o.Payee == Payee &&
                        o.Memo == Memo &&
                        o.Status == Status;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        #endregion
    }
}
