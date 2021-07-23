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
            GotoOtherSideOfTransfer.SetCanExecute(data.LineItems.Find(li => li.CategoryAccountID >= 0) != null);
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

        // Category
        public string Category
        {
            get => data.Category;
            set
            {
                if (data.LineItems.Count == 1)
                {
                    try
                    {
                        data.LineItems[0].Category = value;
                    }
                    catch(ArgumentException e)
                    {
                        mainWindowLogic.ErrorMessage(e.Message);
                    }
                }
                else
                {
                    mainWindowLogic.ErrorMessage("Cannot set category for split transaction");
                }
            }
        }

        // Amount (UI property for investments, and also used to recompute balance)
        public decimal Amount
        {
            get => data.Amount;
            set
            {
                if (data.Amount != value)
                {
                    if (data.LineItems.Count == 1)
                    {
                        data.LineItems[0].Amount = value;
                        OnPropertyChanged(() => Payment);
                        OnPropertyChanged(() => Deposit);
                        OnPropertyChanged(() => AmountState);
                        OnAmountChanged();
                    }
                    else
                    {
                        mainWindowLogic.ErrorMessage("Cannot set amount on split transactions");
                    }
                }
            }
        }

        // Payment
        public decimal Payment
        {
            get => -data.Amount;
            set
            {
                if (data.Amount != -value)
                {
                    if (data.LineItems.Count == 1)
                    {
                        data.LineItems[0].Amount = -value;
                        OnPropertyChanged(() => Deposit);
                        OnPropertyChanged(() => Amount);
                        OnPropertyChanged(() => AmountState);
                    }
                    else
                    {
                        mainWindowLogic.ErrorMessage("Cannot set amount on split transactions");
                    }
                }
            }
        }


        public string Status
        {
            get => GetStatusString();
            set => ParseStatusString(value);
        }

        public string[] StatusSource { get; } = new string[] { "", "c", "R" };

        // Deposit
        public decimal Deposit
        {
            get => data.Amount;
            set
            {
                if (data.Amount != value)
                {
                    if (data.LineItems.Count == 1)
                    {
                        data.LineItems[0].Amount = value;
                        OnPropertyChanged(() => Payment);
                        OnPropertyChanged(() => Amount);
                    }
                    else
                    {
                        mainWindowLogic.ErrorMessage("Cannot set amount on split transactions");
                    }
                }
            }
        }

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
        public ETransactionState AmountState => TransactionState | (data.Amount < 0 ? ETransactionState.NegativeAmount : ETransactionState.Idle);

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

                data.LineItems.Clear();
                backup.LineItems.ForEach(li => data.LineItems.Add(new LineItem(li)));

                OnPropertyChanged(() => Amount);
                OnPropertyChanged(() => Category);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => Deposit);
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

            if (data.Category != backup.Category)
            {
                OnPropertyChanged(() => Category);
            }

            if (data.Status != backup.Status)
            {
                OnPropertyChanged(() => Status);
            }

            if (data.Amount != backup.Amount)
            {
                OnPropertyChanged(() => Amount);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => Deposit);
            }

            if (mainWindowLogic.UserSettings.PlayKaChingSound)
            {
                mainWindowLogic.GuiServices.KaChing();
            }
        }

        protected void CreateLineItemInDB(LineItem li, Household.TransactionRow transactionRow)
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
                CreatePeerTransaction(li.CategoryAccountID, transactionRow, liRow);
            }
        }

        protected void UpdateLineItemInDB(LineItem li, Household.TransactionRow transactionRow)
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
                // Delete former category row if it existed
                if (liCategoryRow != null)
                {
                    liCategoryRow.Delete();
                }

                if (liTransferRow == null)
                {
                    // The line item was not a transfer: Make it one
                    CreatePeerTransaction(li.CategoryAccountID, transactionRow, liRow);
                }
                else
                {
                    // The line item was a transfer. Update the account, amount and memo in peer transaction
                    // Note: this kind of breaks down if the peer transaction has several transfers to the same account,
                    // which is why the split editor forbids it.
                    var otherSideTransactionRow = liTransferRow.TransactionRow;
                    otherSideTransactionRow.AccountID = liTransferRow.AccountID;
                    foreach(var otherSideLineItemRow in otherSideTransactionRow.GetLineItemRows())
                    {
                        if (otherSideLineItemRow.GetLineItemTransferRow() is Household.LineItemTransferRow lineItemTransferRow &&
                            lineItemTransferRow.AccountRow == accountRow)
                        {
                            otherSideLineItemRow.Amount = -li.Amount;
                            if (string.IsNullOrEmpty(li.Memo))
                            {
                                otherSideLineItemRow.SetMemoNull();
                            }
                            else
                            {
                                otherSideLineItemRow.Memo = li.Memo;
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
                    liTransferRow.TransactionRow.Delete();
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
                    liTransferRow.TransactionRow.Delete();
                    liTransferRow.Delete();
                }
            }
        }

        private Household.TransactionRow CreatePeerTransaction(int targetAccountID, Household.TransactionRow transactionRow, Household.LineItemRow liRow)
        {
            var household = mainWindowLogic.Household;

            var targetAccountRow = household.Account.FindByID(targetAccountID);

            // Add transaction on "other side"
            var peerTransactionRow = household.Transaction.Add(targetAccountRow, data.Date, "", data.Memo, ETransactionStatus.Pending, household.Checkpoint.GetMostRecentCheckpointID());
            var peerLiRow = household.LineItem.Add(peerTransactionRow, null, -liRow.Amount);

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
        }

        private void DeletePeerTransaction(Household.LineItemTransferRow lineItemTransferRow)
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
        protected virtual void OnAmountChanged() { }
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
                ETransactionStatus status,
                IEnumerable<LineItem> lineItems)
            {
                (Date, Payee, Memo, Status) =
                    (date, payee, memo, status);

                LineItems.AddRange(lineItems);
            }

            // Clone
            protected BaseTransactionData(BaseTransactionData src)
            {
                (Date, Payee, Memo, Status) =
                    (src.Date, src.Payee, src.Memo, src.Status);

                src.LineItems.ForEach(li => LineItems.Add(new LineItem(li)));
            }

            // Properties
            public DateTime Date;
            public string Payee;
            public string Memo;
            public ETransactionStatus Status;
            public readonly List<LineItem> LineItems = new List<LineItem>();

            // Show either the first line item when no split or a summary
            public string Category => LineItems.Count == 1 ? LineItems[0].Category : "<Split>";

            public decimal Amount => LineItems.Sum(li => li.Amount);

            public override bool Equals(object obj)
            {
                bool equ = false;
                if (obj is BaseTransactionData o)
                {
                    equ =
                        o.Date.Equals(Date) &&
                        o.Payee == Payee &&
                        o.Amount == Amount &&
                        o.Memo == Memo &&
                        o.Status == Status &&
                        o.LineItems.Count == LineItems.Count;

                    if (equ)
                    {
                        for (int i = 0; i < LineItems.Count; i++)
                        {
                            equ &= o.LineItems[i].Equals(LineItems[i]);
                        }
                    }
                }

                return equ;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        #endregion
    }
}
