using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using Toolbox.Attributes;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Dialogs;

namespace BanaData.Logic.Main
{
    /// <summary>
    /// Logic for one banking transaction
    /// </summary>
    public class BankingTransactionLogic : AbstractTransactionLogic
    {
        #region Private members

        // Parent logic
        private readonly BankRegisterLogic bankRegisterLogic;

        #endregion

        #region Constructor

        public BankingTransactionLogic(MainWindowLogic mainWindowLogic, BankRegisterLogic _bankRegisterLogic, int accountID, int transID, BankTransactionData data)
            : base(mainWindowLogic, accountID, transID, data)
        {
            (bankRegisterLogic, TransID) = (_bankRegisterLogic, transID);

            PayeeSelected = new CommandBase(OnPayeeSelected);
            SplitTransaction = new CommandBase(OnSplitTransaction);
        }

        // To create new transactions (not in DB yet)
        public BankingTransactionLogic(MainWindowLogic _mainWindowLogic, BankRegisterLogic _bankRegisterLogic, int _accountID)
            : this(_mainWindowLogic, _bankRegisterLogic, _accountID, AbstractTransactionLogic.TRANSID_NOT_COMMITTED,
                  new BankTransactionData(DateTime.Today, ETransactionMedium.None, 0, "", "", ETransactionStatus.Pending,
                      new LineItem[] { new LineItem(_mainWindowLogic, -1, "", -1, -1, "", 0, false) }))
        {
        }

        #endregion

        #region Logic properties

        public override decimal AmountForCashBalance => Amount;

        #endregion

        #region UI properties

        // Medium of transaction
        public string Medium 
        {
            get => GetMediumString();
            set => ParseMediumString(value);
        }

        // Strings for medium
        private const string MEDIUM_NEXTCHECKNUM = "Next Check Num";
        static private readonly string MEDIUM_ATM = EnumDescriptionAttribute.GetDescription(ETransactionMedium.ATM);
        static private readonly string MEDIUM_DEPOSIT = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Deposit);
        static private readonly string MEDIUM_DIVIDEND = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Dividend);
        static private readonly string MEDIUM_EFT = EnumDescriptionAttribute.GetDescription(ETransactionMedium.EFT);
        static private readonly string MEDIUM_TRANSFER = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Transfer);

        public string[] MediumSource { get; } =
        {
            MEDIUM_NEXTCHECKNUM, MEDIUM_ATM, MEDIUM_DEPOSIT, MEDIUM_DIVIDEND, MEDIUM_EFT, MEDIUM_TRANSFER
        };

        // Derived from medium being a deposit or not
        public bool IsDepositTabStop { get; private set; }
        public bool IsPaymentTabStop => !IsDepositTabStop;

        // Activated when a payee is selected from the drop down list
        public CommandBase PayeeSelected { get; }

        // Split dialog
        public CommandBase SplitTransaction { get; }

        #endregion

        #region IEditable interface implementation

        public override void BeginEdit()
        {
            // Save existing data
            if (backup == null)
            {
                backup = new BankTransactionData(data as BankTransactionData);
            }

            Console.WriteLine($"Begin edit transaction date {Date.ToShortDateString()} Payee {Payee} amount {Payment}");
        }

        public override void CancelEdit()
        {
            base.CancelEdit();

            // Restore data
            if (backup != null)
            {
                var _data = data as BankTransactionData;
                var _backup = backup as BankTransactionData;

                if (_data.Medium != _backup.Medium)
                {
                    _data.Medium = _backup.Medium;
                    OnPropertyChanged(() => Medium);
                }

                if (_data.CheckNumber != _backup.CheckNumber)
                {
                    _data.CheckNumber = _backup.CheckNumber;
                    OnPropertyChanged(() => Medium);
                }

                backup = null;
            }

            Console.WriteLine($"Cancel edit transaction date {Date.ToShortDateString()} Payee {Payee} amount {Payment}");
        }

        // Is there a change to the transaction?
        public override bool HasTransactionChanged => backup != null && !backup.Equals(data);

        // Returns if there is something to commit
        public override bool DoesTransactionNeedComit
        {
            get
            {
                // Out of sequence
                if (backup == null)
                {
                    BeginEdit();
                    return false;
                }

                Console.WriteLine($"End edit transaction date {Date.ToShortDateString()} Payee {Payee} amount {Payment}");

                // Check the changes
                if (TransID == TRANSID_TRANSFER_FILLIN)
                {
                    // We only allow changing the status, as it is stored in the transfer line item
                    var tmpData = new BankTransactionData(data as BankTransactionData) { Status = backup.Status };

                    if (!tmpData.Equals(backup))
                    {
                        mainWindowLogic.ErrorMessage("Cannot edit this end of the transfer - please edit the other end");
                        CancelEdit();
                        BeginEdit();
                        return false;
                    }
                }

                if (backup.Status == ETransactionStatus.Reconciled && data.Status != ETransactionStatus.Reconciled)
                {
                    if (!mainWindowLogic.YesNoQuestion("Are you sure you want to un-reconcile this transaction"))
                    {
                        CancelEdit();
                        BeginEdit();
                        return false;
                    }
                }

                if (backup.Status == ETransactionStatus.Reconciled && backup.Amount != data.Amount)
                {
                    if (!mainWindowLogic.YesNoQuestion("Are you sure you want to change the amount of this reconciled transaction"))
                    {
                        CancelEdit();
                        BeginEdit();
                        return false;
                    }
                }

                return true;
            }
        }



        public override void EndEdit()
        {
            CommitTransactionToDataSet();

            // Notify the UI
            base.EndEdit();

            var _data = data as BankTransactionData;
            var _backup = backup as BankTransactionData;
            if (_data.Medium != _backup.Medium)
            {
                OnPropertyChanged(() => Medium);
            }

            if (_data.CheckNumber != _backup.CheckNumber)
            {
                OnPropertyChanged(() => Medium);
            }

            // Clear the backup
            backup = null;
        }

        private void CommitTransactionToDataSet()
        {
            var household = mainWindowLogic.Household;
            var accountRow = household.Account.FindByID(accountID);

            if (TransID == TRANSID_NOT_COMMITTED)
            {
                // Create new transaction row
                var transactionRow = household.Transaction.Add(accountRow, data.Date, data.Payee, data.Memo, data.Status, household.Checkpoint.GetMostRecentCheckpointID());
                TransID = transactionRow.ID;

                // Create new banking transaction row if needed
                if (bankRegisterLogic.IsBank)
                {
                    var _data = data as BankTransactionData;
                    household.BankingTransaction.Add(transactionRow, _data.Medium, _data.CheckNumber);
                }

                // Create all line items
                foreach(var li in data.LineItems)
                {
                    ETransactionStatus? transferStatus = (li.CategoryAccountID < 0) ? null : ETransactionStatus.Pending as ETransactionStatus?;
                    var liRow = household.LineItem.Add(transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount, transferStatus);
                    li.ID = liRow.ID;
                }
            }
            else if (TransID == TRANSID_TRANSFER_FILLIN)
            {
                // Modification of status for transfer pseudo-transaction
                // The status for the transactionless end of the transfer is kept in the transfer line item row 
                var liRow = household.LineItem.FindByID(data.LineItems[0].ID);
                liRow.TransferStatus = data.Status;
            }
            else
            {
                // Update transaction row
                var transactionRow = household.Transaction.Update(TransID, accountRow, data.Date, data.Payee, data.Memo, data.Status, household.Checkpoint.GetMostRecentCheckpointID());

                // Update banking transaction if needed
                if (bankRegisterLogic.IsBank)
                {
                    var _data = data as BankTransactionData;
                    household.BankingTransaction.Update(transactionRow, _data.Medium, _data.CheckNumber);
                }

                //
                // Update the line items
                //
                // First find deleted line items and delete them in the DB
                foreach (var li in backup.LineItems)
                {
                    if (li.ID >= 0 && data.LineItems.FirstOrDefault(l => l.ID == li.ID) == null)
                    {
                        household.LineItem.FindByID(li.ID).Delete();
                    }
                }
                // Second update or create the other ones
                foreach (var li in data.LineItems)
                {
                    ETransactionStatus? transferStatus = (li.CategoryAccountID < 0) ? null : ETransactionStatus.Pending as ETransactionStatus?;
                    if (li.ID >= 0)
                    {
                        var liRow = household.LineItem.FindByID(li.ID);
                        household.LineItem.Update(liRow, transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount, transferStatus);
                    }
                    else
                    {
                        var liRow = household.LineItem.Add(transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount, transferStatus);
                        li.ID = liRow.ID;
                    }
                }
            }

            mainWindowLogic.CommitChanges();
        }

        #endregion

        #region Actions

        //
        // Fill out fields when a memorized payee is selected
        //
        private void OnPayeeSelected(object arg)
        {
            if (arg is MemorizedPayeeItem memorizedPayee &&
                memorizedPayee.LineItems.Length > 0 &&
                TransID < 0 &&      // Do this only for new transactions
                backup != null)
            {
                data.LineItems.Clear();
                foreach(var li in memorizedPayee.LineItems)
                {
                    var unsealedLineItem = new LineItem(li) { Sealed = false };
                    data.LineItems.Add(unsealedLineItem);
                }

                OnPropertyChanged(() => Memo);
                OnPropertyChanged(() => Category);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => Deposit);
            }
        }

        private void OnSplitTransaction()
        {
            // Work on a sealed copy of the  LineItems
            var lis = new LineItem[data.LineItems.Count];
            for(int i = 0; i < lis.Length; i++)
            {
                lis[i] = new LineItem(data.LineItems[i]) { Sealed = true };
            }

            // Show the split transaction dialog
            var splitDialog = new EditSplitLogic(mainWindowLogic, lis);
            if (mainWindowLogic.GuiServices.ShowDialog(splitDialog))
            {
                // Copy the result back
                data.LineItems.Clear();
                foreach(var li in splitDialog.NewLineItems)
                {
                    li.Sealed = false;
                    data.LineItems.Add(li);
                }

                OnPropertyChanged(() => Category);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => Deposit);
            }
        }

        private string GetMediumString()
        {
            string rs = "???";

            var _data = data as BankTransactionData;
            if (_data.Medium == ETransactionMedium.Check)
            {
                if (_data.CheckNumber > 0)
                {
                    rs = _data.CheckNumber.ToString();
                }
            }
            else
            {
                rs = EnumDescriptionAttribute.GetDescription(_data.Medium);
            }

            return rs;
        }

        private void ParseMediumString(string type)
        {
            var _data = data as BankTransactionData;
            _data.CheckNumber = 0;

            if (type == MEDIUM_NEXTCHECKNUM)
            {
                _data.Medium = ETransactionMedium.Check;
                _data.CheckNumber = GetNextCheckNumber();
                OnPropertyChanged(() => Medium);
            }
            else if (MediumSource.Contains(type))
            {
                _data.Medium = EnumDescriptionAttribute.MatchDescription<ETransactionMedium>(type);
            }
            else
            {
                if (uint.TryParse(type, out uint checkNum))
                {
                    _data.Medium = ETransactionMedium.Check;
                    _data.CheckNumber = checkNum;
                }
                else
                {
                    _data.Medium = ETransactionMedium.None;
                }
            }

            IsDepositTabStop = _data.Medium == ETransactionMedium.Deposit;
            OnPropertyChanged(() => IsDepositTabStop);
            OnPropertyChanged(() => IsPaymentTabStop);
        }

        private uint GetNextCheckNumber()
        {
            uint result = 0;

            foreach(BankingTransactionLogic btl in bankRegisterLogic.Transactions)
            {
                if (btl != this)
                {
                    result = Math.Max(result, ((BankTransactionData)(btl.data)).CheckNumber);
                }
            }

            return result + 1;
        }

        #endregion

        #region Supporting class

        public class BankTransactionData : AbstractTransactionLogic.BaseTransactionData
        {
            // Construct from scratch
            public BankTransactionData(
                DateTime date,
                ETransactionMedium medium,
                uint checkNumber,
                string payee,
                string memo,
                ETransactionStatus status,
                IEnumerable<LineItem> lineItems)
                : base(date, payee, memo, status, lineItems) => (Medium, CheckNumber) = (medium, checkNumber);

            // Clone
            public BankTransactionData(BankTransactionData src)
                : base(src) => (Medium, CheckNumber) = (src.Medium, src.CheckNumber);

            // Properties
            public ETransactionMedium Medium;
            public uint CheckNumber;

            public override bool Equals(object obj)
            {
                return
                    obj is BankTransactionData o &&
                    base.Equals(o) &&
                    o.Medium == Medium &&
                    o.CheckNumber == CheckNumber;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        #endregion
    }
}
