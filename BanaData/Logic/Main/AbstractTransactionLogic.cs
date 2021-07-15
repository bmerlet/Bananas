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
        public const int TRANSID_TRANSFER_FILLIN = -2;
 
        #endregion

        #region Constructor

        protected AbstractTransactionLogic(
            MainWindowLogic _mainWindowLogic, Household.AccountRow _accountRow, int _transID, BaseTransactionData _data)
        {
            (mainWindowLogic, accountRow, TransID, data) = (_mainWindowLogic, _accountRow, _transID, _data);

            GotoOtherSideOfTransfer = new CommandBase(OnGotoOtherSideOfTransfer);
            GotoOtherSideOfTransfer.SetCanExecute(
                TransID == TRANSID_TRANSFER_FILLIN ||
                data.LineItems.Find(li => li.CategoryAccountID >= 0) != null);
        }

        #endregion

        #region Logic properties

        // Transaction id, -1 if not in DB yet, -2 if transfer fill-in
        public int TransID;

        // For fill-in transactions, line item ID
        public int FillInLineItemID => (TransID == TRANSID_TRANSFER_FILLIN) ? data.LineItems[0].ID : -1;

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
            (data.Status == ETransactionStatus.Reconciled ? ETransactionState.Reconciled : ETransactionState.Idle) |
            (TransID == TRANSID_TRANSFER_FILLIN ? ETransactionState.TransferFillIn : ETransactionState.Idle);

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

        #endregion

        #region Actions

        // Called by on behalf of reconcile to update status on transaction if needed
        public void UpdateStatus()
        {
            var household = mainWindowLogic.Household;

            if (TransID == TRANSID_TRANSFER_FILLIN)
            {
                var liRow = household.LineItem.FindByID(data.LineItems[0].ID);
                if (data.Status != liRow.TransferStatus)
                {
                    data.Status = liRow.TransferStatus;
                    OnPropertyChanged(() => Status);
                }
            }
            else if (TransID != TRANSID_NOT_COMMITTED)
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

        private void OnGotoOtherSideOfTransfer()
        {
            var household = mainWindowLogic.Household;

            if (TransID == TRANSID_TRANSFER_FILLIN)
            {
                var liRow = household.LineItem.FindByID(data.LineItems[0].ID);
                var transRow = liRow.TransactionRow;
                mainWindowLogic.GotoTransaction(transRow.AccountID, transRow.ID, data.LineItems[0].ID);
            }
            else
            {
                var li = data.LineItems.Find(l => l.CategoryAccountID >= 0);
                mainWindowLogic.GotoTransaction(li.CategoryAccountID, TRANSID_TRANSFER_FILLIN, li.ID);
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
