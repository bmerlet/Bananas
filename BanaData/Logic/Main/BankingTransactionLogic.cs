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

        public BankingTransactionLogic(MainWindowLogic mainWindowLogic, BankRegisterLogic _bankRegisterLogic, Household household, Household.AccountRow accountRow, int transID, BankTransactionData _data)
            : base(mainWindowLogic, household, accountRow, transID, _data)
        {
            (bankRegisterLogic, data) = (_bankRegisterLogic, _data);

            PayeeSelected = new CommandBase(OnPayeeSelected);
            SplitTransaction = new CommandBase(OnSplitTransaction);

            GotoOtherSideOfTransfer.SetCanExecute(data.LineItems.Find(li => li.CategoryAccountID >= 0) != null);
        }

        // To create new transactions (not in DB yet)
        public BankingTransactionLogic(MainWindowLogic _mainWindowLogic, BankRegisterLogic _bankRegisterLogic, Household household, Household.AccountRow _accountRow)
            : this(_mainWindowLogic, _bankRegisterLogic, household, _accountRow, AbstractTransactionLogic.TRANSID_NOT_COMMITTED,
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
            get => Household.BankingTransactionDataTable.GetMediumString(data.Medium, data.CheckNumber);
            set => ParseMediumString(value);
        }

        public string[] MediumSource { get; } = Household.BankingTransactionDataTable.MediumSource;

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
                        InvokePropertyChanged(nameof(IsDepositTabStop));
                        InvokePropertyChanged(nameof(IsPaymentTabStop));
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

        // Line items (to display as a category tooltip)
        public IEnumerable<LineItem> LineItems => data.LineItems;

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
                        InvokePropertyChanged(nameof(Deposit));
                        InvokePropertyChanged(nameof(Amount));
                        InvokePropertyChanged(nameof(AmountState));
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
                        InvokePropertyChanged(nameof(Payment));
                        InvokePropertyChanged(nameof(Amount));
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
            data.LineItems.Any(li => li.CategoryID != -1 && household.Category.FindByID(li.CategoryID).IsIncome);

        // Is the payment box a tab stop
        public bool IsPaymentTabStop =>
            data.Medium != ETransactionMedium.Deposit ||
            data.LineItems.Any(li => li.CategoryID != -1 && !household.Category.FindByID(li.CategoryID).IsIncome);

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
                    InvokePropertyChanged(nameof(Medium));
                }

                if (data.CheckNumber != _backup.CheckNumber)
                {
                    data.CheckNumber = _backup.CheckNumber;
                    InvokePropertyChanged(nameof(Medium));
                }

                data.LineItems.Clear();
                _backup.LineItems.ForEach(li => data.LineItems.Add(new LineItem(li)));

                InvokePropertyChanged(nameof(Amount));
                InvokePropertyChanged(nameof(Category));
                InvokePropertyChanged(nameof(Payment));
                InvokePropertyChanged(nameof(Deposit));

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
                InvokePropertyChanged(nameof(Medium));
            }

            if (_data.CheckNumber != _backup.CheckNumber)
            {
                InvokePropertyChanged(nameof(Medium));
            }

            if (data.Category != _backup.Category)
            {
                InvokePropertyChanged(nameof(Category));
            }

            if (data.Amount != _backup.Amount)
            {
                InvokePropertyChanged(nameof(Amount));
                InvokePropertyChanged(nameof(Payment));
                InvokePropertyChanged(nameof(Deposit));
            }

            // Update goto context menu status
            GotoOtherSideOfTransfer.SetCanExecute(data.LineItems.Find(li => li.CategoryAccountID >= 0) != null);

            // Re-sort if needed
            bankRegisterLogic.RegisterItems.Refresh();

            // Clear the backup
            backup = null;
        }

        private void CommitTransactionToDataSet()
        {
            // Remember impacted accounts
            var impactedAccounts = new List<int>
            {
                accountRow.ID
            };


            if (TransID == TRANSID_NOT_COMMITTED)
            {
                // Create new transaction row
                var transactionRow = household.Transaction.Add(
                    accountRow,
                    data.Date,
                    data.Payee,
                    data.Memo,
                    data.Status,
                    household.Checkpoint.GetCurrentCheckpoint(),
                    ETransactionType.Regular);
                TransID = transactionRow.ID;

                // Create new banking transaction row if needed
                if (bankRegisterLogic.IsBank)
                {
                    household.BankingTransaction.Add(transactionRow, data.Medium, data.CheckNumber);
                }

                // Create all line items
                foreach(var li in data.LineItems)
                {
                    CreateLineItemInDB(li, transactionRow, impactedAccounts, null);
                }
            }
            else
            {
                // Update transaction row
                var transactionRow = household.Transaction.Update(
                    TransID,
                    accountRow,
                    data.Date, 
                    data.Payee,
                    data.Memo,
                    data.Status, 
                    household.Checkpoint.GetCurrentCheckpoint(),
                    ETransactionType.Regular);

                // Update banking transaction if needed
                if (bankRegisterLogic.IsBank)
                {
                    household.BankingTransaction.Update(transactionRow, data.Medium, data.CheckNumber);
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
                        UpdateLineItemInDB(li, transactionRow, impactedAccounts, null);
                    }
                    else
                    {
                        CreateLineItemInDB(li, transactionRow, impactedAccounts, null);
                    }
                }
            }

            mainWindowLogic.CommitChanges(household);

            // Update balances for accounts impacted by this transaction
            mainWindowLogic.UpdateAccountNamesAndBalances(impactedAccounts);
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

                data.Memo = memorizedPayee.Memo;

                InvokePropertyChanged(nameof(Memo));
                InvokePropertyChanged(nameof(Category));
                InvokePropertyChanged(nameof(Payment));
                InvokePropertyChanged(nameof(Deposit));
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
            var splitDialog = new EditSplitLogic(mainWindowLogic, household, lis);
            if (mainWindowLogic.GuiServices.ShowDialog(splitDialog))
            {
                // Copy the result back
                data.LineItems.Clear();
                foreach(var li in splitDialog.NewLineItems)
                {
                    li.Sealed = false;
                    data.LineItems.Add(li);
                }

                InvokePropertyChanged(nameof(Category));
                InvokePropertyChanged(nameof(Payment));
                InvokePropertyChanged(nameof(Deposit));
            }
        }

        private void ParseMediumString(string type)
        {
            (ETransactionMedium medium, decimal checkNumber) = Household.BankingTransactionDataTable.ParseMediumString(type, accountRow);

            if (data.Medium != medium)
            {
                data.Medium = medium;
                InvokePropertyChanged(nameof(Medium));
                InvokePropertyChanged(nameof(IsDepositTabStop));
                InvokePropertyChanged(nameof(IsPaymentTabStop));
            }

            if (data.CheckNumber != (uint)checkNumber)
            {
                data.CheckNumber = (uint)checkNumber;
                InvokePropertyChanged(nameof(Medium));
            }
        }

        public void UpdateCategoryNames()
        {
            data.LineItems.ForEach(li => li.UpdateCategoryName());
            InvokePropertyChanged(nameof(Category));
        }

        // Activated after payee rename
        public void UpdatePayeeNameFromDatabase()
        {
            if (backup == null && TransID != TRANSID_NOT_COMMITTED)
            {
                var transactionRow = household.Transaction.FindByID(TransID);
                if (!transactionRow.IsPayeeNull())
                {
                    if (data.Payee != transactionRow.Payee)
                    {
                        data.Payee = transactionRow.Payee;
                        InvokePropertyChanged(nameof(Payee));
                    }
                }
            }
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
