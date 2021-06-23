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
        #region Supporting class

        public class BankTransactionData : AbstractTransactionLogic.BaseTransactionData
        {
            // Construct from scratch
            public BankTransactionData(
                DateTime date,
                ETransactionMedium medium,
                uint checkNumber,
                string payee,
                ETransactionStatus status,
                IEnumerable<LineItem> lineItems)
                : base(date, payee, status, lineItems) => (Medium, CheckNumber) = (medium, checkNumber);

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
            : this(_mainWindowLogic, _bankRegisterLogic, _accountID, -1,
                  new BankTransactionData(DateTime.Today, ETransactionMedium.None, 0, "", ETransactionStatus.Pending,
                      new LineItem[] { new LineItem(_mainWindowLogic, -1, "", -1, -1, "", 0, false) }))
        {
        }

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
        static private readonly string MEDIUM_CASH = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Cash);
        static private readonly string MEDIUM_DEPOSIT = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Deposit);
        static private readonly string MEDIUM_DIVIDEND = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Dividend);
        static private readonly string MEDIUM_EFT = EnumDescriptionAttribute.GetDescription(ETransactionMedium.EFT);
        static private readonly string MEDIUM_TRANSFER = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Transfer);

        public string[] MediumSource { get; } =
        {
            MEDIUM_NEXTCHECKNUM, MEDIUM_ATM, MEDIUM_CASH, MEDIUM_DEPOSIT, MEDIUM_DIVIDEND, MEDIUM_EFT, MEDIUM_TRANSFER
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

        public override void EndEdit()
        {
            // Out of sequence
            if (backup == null)
            {
                return;
            }

            // No change
            if (backup.Equals(data))
            {
                backup = null;
                bankRegisterLogic.MoveDownOneTransaction(TransID < 0);
                return;
            }

            Console.WriteLine($"End edit transaction date {Date.ToShortDateString()} Payee {Payee} amount {Payment}");

            // Check the changes
            if (backup.Status == ETransactionStatus.Reconciled && data.Status != ETransactionStatus.Reconciled)
            {
                if (!mainWindowLogic.YesNoQuestion("Are you sure you want to un-reconcile this transaction"))
                {
                    CancelEdit();
                    BeginEdit();
                }
            }

            if (backup.Status == ETransactionStatus.Reconciled && backup.Amount != data.Amount)
            {
                if (!mainWindowLogic.YesNoQuestion("Are you sure you want to change the amount of this reconciled transaction"))
                {
                    CancelEdit();
                    BeginEdit();
                }
            }

            // Commit the changes
            bool wasEmptyTransaction = TransID < 0;
            if (wasEmptyTransaction)
            {
                // Remove from the list since we are going to change the ID, which is the equality criteria,
                // and the listview will get mightily confused
                bankRegisterLogic.RemoveTransactionFromList(this);
            }

            CommitTransactionToDataSet();

            if (wasEmptyTransaction)
            {
                // Put the re-id'd transaction back in the list
                bankRegisterLogic.AddTransactionBackToList(this);
            }

            // Recompute the balances
            bankRegisterLogic.RecomputeBalances();

            // Notify the UI
            if (data.Date != backup.Date)
            {
                OnPropertyChanged(() => Date);
            }

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

            if (data.Payee != backup.Payee)
            {
                OnPropertyChanged(() => Payee);
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
                OnPropertyChanged(() => PaymentString);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => DepositString);
                OnPropertyChanged(() => Deposit);
            }

            // Clear the backup
            backup = null;

            // Ask the register to move to the next transaction,
            // creating an empty one if needed
            bankRegisterLogic.MoveDownOneTransaction(wasEmptyTransaction);
        }

        private void CommitTransactionToDataSet()
        {
            var household = mainWindowLogic.Household;
            var accountRow = household.Accounts.FindByID(accountID);

            if (TransID < 0)
            {
                // Create new transaction row
                var transactionRow = household.Transactions.Add(accountRow, data.Date, data.Payee, data.Status);
                TransID = transactionRow.ID;

                // Create new banking transaction row if needed
                if (bankRegisterLogic.IsBank)
                {
                    var _data = data as BankTransactionData;
                    household.BankingTransactions.Add(transactionRow, _data.Medium, _data.CheckNumber);
                }

                // Create all line items
                foreach(var li in data.LineItems)
                {
                    household.LineItems.Add(transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount);
                }
            }
            else
            {
                // Update transaction row
                var transactionRow = household.Transactions.Update(TransID, accountRow, data.Date, data.Payee, data.Status);

                // Update banking transaction if needed
                if (bankRegisterLogic.IsBank)
                {
                    var _data = data as BankTransactionData;
                    household.BankingTransactions.Update(transactionRow, _data.Medium, _data.CheckNumber);
                }

                //
                // Update the line items
                //
                // First find deleted line items and delete them in the DB
                foreach (var li in backup.LineItems)
                {
                    if (li.ID >= 0 && data.LineItems.FirstOrDefault(l => l.ID == li.ID) == null)
                    {
                        household.LineItems.FindByID(li.ID).Delete();
                    }
                }
                // Second update or create the other ones
                foreach (var li in data.LineItems)
                {
                    if (li.ID >= 0)
                    {
                        var liRow = household.LineItems.FindByID(li.ID);
                        household.LineItems.Update(liRow, transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount);
                    }
                    else
                    {
                        var liRow = household.LineItems.Add(transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount);
                        data.LineItems[data.LineItems.IndexOf(li)] = new LineItem(li, liRow.ID);
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
            if (arg is MemorizedPayeeItem memorizedPayee && memorizedPayee.LineItems.Length > 0 && backup != null)
            {
                data.LineItems.Clear();
                foreach(var li in memorizedPayee.LineItems)
                {
                    var unsealedLineItem = new LineItem(li) { Sealed = false };
                    data.LineItems.Add(unsealedLineItem);
                }

                OnPropertyChanged(() => Memo);
                OnPropertyChanged(() => Category);
                OnPropertyChanged(() => PaymentString);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => DepositString);
                OnPropertyChanged(() => Deposit);
            }
        }

        private void OnSplitTransaction()
        {
            // Work on a sealed copy of the  LineItems
            var lis = new LineItem[data.LineItems.Count];
            for(int i = 0; i < lis.Length; i++)
            {
                lis[i] = new LineItem(data.LineItems[i]) { Sealed = false };
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

                OnPropertyChanged(() => Memo);
                OnPropertyChanged(() => Category);
                OnPropertyChanged(() => PaymentString);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => DepositString);
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

        #region Overrides

        public override bool Equals(object obj)
        {
            return obj is BankingTransactionLogic o && TransID == o.TransID;
        }

        public override int GetHashCode()
        {
            return TransID.GetHashCode();
        }

        #endregion
    }
}
