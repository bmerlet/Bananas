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
using BanaData.Logic.Dialogs.Editors;

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
        private new readonly BankTransactionData data;

        #endregion

        #region Constructor

        public BankingTransactionLogic(MainWindowLogic mainWindowLogic, BankRegisterLogic _bankRegisterLogic, Household.AccountRow accountRow, int transID, BankTransactionData _data)
            : base(mainWindowLogic, accountRow, transID, _data)
        {
            (bankRegisterLogic, data) = (_bankRegisterLogic, _data);

            PayeeSelected = new CommandBase(OnPayeeSelected);
            SplitTransaction = new CommandBase(OnSplitTransaction);

            GotoOtherSideOfTransfer.SetCanExecute(data.LineItems.Find(li => li.CategoryAccountID >= 0) != null);
        }

        // To create new transactions (not in DB yet)
        public BankingTransactionLogic(MainWindowLogic _mainWindowLogic, BankRegisterLogic _bankRegisterLogic, Household.AccountRow _accountRow)
            : this(_mainWindowLogic, _bankRegisterLogic, _accountRow, AbstractTransactionLogic.TRANSID_NOT_COMMITTED,
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
                        OnPropertyChanged(() => IsDepositTabStop);
                        OnPropertyChanged(() => IsPaymentTabStop);
                    }
                    catch (ArgumentException e)
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

        // Amount (used to recompute balance)
        public override decimal Amount
        {
            get => data.Amount;
            set => throw new InvalidOperationException();
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

        // Is the deposit box a tab stop
        public bool IsDepositTabStop => 
            data.Medium == ETransactionMedium.Deposit ||
            data.LineItems.Any(li => li.CategoryID != -1 && mainWindowLogic.Household.Category.FindByID(li.CategoryID).IsIncome);

        // Is the payment box a tab stop
        public bool IsPaymentTabStop =>
            data.Medium != ETransactionMedium.Deposit ||
            data.LineItems.Any(li => li.CategoryID != -1 && !mainWindowLogic.Household.Category.FindByID(li.CategoryID).IsIncome);

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
                var _backup = backup as BankTransactionData;

                if (data.Medium != _backup.Medium)
                {
                    data.Medium = _backup.Medium;
                    OnPropertyChanged(() => Medium);
                }

                if (data.CheckNumber != _backup.CheckNumber)
                {
                    data.CheckNumber = _backup.CheckNumber;
                    OnPropertyChanged(() => Medium);
                }

                data.LineItems.Clear();
                _backup.LineItems.ForEach(li => data.LineItems.Add(new LineItem(li)));

                OnPropertyChanged(() => Amount);
                OnPropertyChanged(() => Category);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => Deposit);

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
                if (backup.Status == ETransactionStatus.Reconciled && data.Status != ETransactionStatus.Reconciled)
                {
                    if (!mainWindowLogic.YesNoQuestion("Are you sure you want to un-reconcile this transaction"))
                    {
                        CancelEdit();
                        BeginEdit();
                        return false;
                    }
                }

                var _backup = backup as BankTransactionData;
                if (backup.Status == ETransactionStatus.Reconciled && _backup.Amount != data.Amount)
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

            if (data.Category != _backup.Category)
            {
                OnPropertyChanged(() => Category);
            }

            if (data.Amount != _backup.Amount)
            {
                OnPropertyChanged(() => Amount);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => Deposit);
            }

            // Update goto context menu status
            GotoOtherSideOfTransfer.SetCanExecute(data.LineItems.Find(li => li.CategoryAccountID >= 0) != null);

            // Clear the backup
            backup = null;
        }

        private void CommitTransactionToDataSet()
        {
            var household = mainWindowLogic.Household;

            // Remember impacted accounts
            var impactedAccounts = new List<int>
            {
                accountRow.ID
            };


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
                    CreateLineItemInDB(li, transactionRow, impactedAccounts);
                }
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
                var _backup = backup as BankTransactionData;
                foreach (var li in _backup.LineItems)
                {
                    if (li.ID >= 0 && data.LineItems.FirstOrDefault(l => l.ID == li.ID) == null)
                    {
                        var lineItemRow = household.LineItem.FindByID(li.ID);

                        if (lineItemRow.GetLineItemCategoryRow() is Household.LineItemCategoryRow lineItemCategoryRow)
                        {
                            lineItemCategoryRow.Delete();
                        }

                        if (lineItemRow.GetLineItemTransferRow() is Household.LineItemTransferRow lineItemTransferRow)
                        {
                            impactedAccounts.Add(lineItemTransferRow.AccountID);
                            DeletePeerTransaction(lineItemTransferRow);
                            lineItemTransferRow.Delete();
                        }

                        lineItemRow.Delete();
                    }
                }

                // Second update or create the other ones
                foreach (var li in data.LineItems)
                {
                    if (li.ID >= 0)
                    {
                        UpdateLineItemInDB(li, transactionRow, impactedAccounts);
                    }
                    else
                    {
                        CreateLineItemInDB(li, transactionRow, impactedAccounts);
                    }
                }
            }

            mainWindowLogic.CommitChanges();

            // Update balances for accounts impacted by this transaction
            mainWindowLogic.UpdateBalances(impactedAccounts);
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
                : base(date, payee, memo, status)
            {
                (Medium, CheckNumber) = (medium, checkNumber);

                LineItems.AddRange(lineItems);
            }

            // Clone
            public BankTransactionData(BankTransactionData src)
                : base(src)
            {
                (Medium, CheckNumber) = (src.Medium, src.CheckNumber);

                src.LineItems.ForEach(li => LineItems.Add(new LineItem(li)));
            }

            // Properties
            public ETransactionMedium Medium;
            public uint CheckNumber;

            public readonly List<LineItem> LineItems = new List<LineItem>();

            // Show either the first line item when no split or a summary
            public string Category => LineItems.Count == 1 ? LineItems[0].Category : "<Split>";

            public decimal Amount => LineItems.Sum(li => li.Amount);

            public override bool Equals(object obj)
            {
                bool equ = false;
                if (obj is BankTransactionData o)
                {
                    equ = base.Equals(o) &&
                    o.Amount == Amount &&
                    o.Medium == Medium &&
                    o.CheckNumber == CheckNumber &&
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
