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
    public class BankingTransactionLogic : LogicBase, IEditableObject
    {
        #region Supporting class

        public class BankTransactionData
        {
            // Construct from scratch
            public BankTransactionData(
                DateTime date,
                ETransactionMedium medium,
                uint checkNumber,
                string payee,
                ETransactionStatus status,
                IEnumerable<LineItem> lineItems)
            {
                (Date, Medium, CheckNumber, Payee, Status) =
                    (date, medium, checkNumber, payee, status);

                LineItems.AddRange(lineItems);
            }

            // Clone
            public BankTransactionData(BankTransactionData src)
            {
                (Date, Medium, CheckNumber, Payee, Status) =
                    (src.Date, src.Medium, src.CheckNumber, src.Payee, src.Status);

                src.LineItems.ForEach(li => LineItems.Add(new LineItem(li)));
            }

            // Properties
            public DateTime Date;
            public ETransactionMedium Medium;
            public uint CheckNumber;
            public string Payee;
            public ETransactionStatus Status;
            public readonly List<LineItem> LineItems = new List<LineItem>();

            // Show either the first line item when no split or a summary
            public string Memo => LineItems.Count == 1 ? LineItems[0].Memo : "";
            public string Category => LineItems.Count == 1 ? LineItems[0].Category : "<Split>";
            public decimal Amount => LineItems.Sum(li => li.Amount);

            public override bool Equals(object obj)
            {
                bool equ = false;
                if (obj is BankTransactionData o)
                {
                    equ =
                        o.Date.Equals(Date) &&
                        o.Medium == Medium &&
                        o.CheckNumber == CheckNumber &&
                        o.Payee == Payee &&
                        o.Amount == Amount &&
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

        #region Private members

        // Parent logic
        private readonly MainWindowLogic mainWindowLogic;
        private readonly BankRegisterLogic bankRegisterLogic;

        // Account this transaction is for
        private readonly int accountID;

        // Transaction id, -1 if not in DB yet
        public int transID;

        // Transaction data
        private readonly BankTransactionData data;

        // Backup of data (taken at edit start)
        private BankTransactionData backup;

        #endregion

        #region Constructor

        public BankingTransactionLogic(MainWindowLogic _mainWindowLogic, BankRegisterLogic _bankRegisterLogic, int _accountID, int _transID, BankTransactionData _data)
        {
            (mainWindowLogic, bankRegisterLogic, accountID, transID, data) = (_mainWindowLogic, _bankRegisterLogic, _accountID, _transID, _data);

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

        // Date of the transaction
        public DateTime Date
        {
            get => data.Date;
            set => data.Date = value;
        }

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

        // Payee
        public string Payee
        { 
            get => data.Payee;
            set => data.Payee = value;
        }
        // Memorized payees
        public IEnumerable<MemorizedPayeeItem> Payees => mainWindowLogic.MemorizedPayees;

        // Activated when a payee is selected from the drop down list
        public CommandBase PayeeSelected { get; }

        // Memo
        public string Memo
        {
            get => data.Memo;
            set
            {
                if (data.LineItems.Count == 1)
                {
                    data.LineItems[0].Memo = value;
                }
                else
                {
                    mainWindowLogic.ErrorMessage("Cannot add memo to split transactions");
                }
            }
        }

        // Category
        public string Category 
        {
            get => data.Category;
            set
            {
                if (data.LineItems.Count == 1)
                {
                    data.LineItems[0].Category = value;
                }
                else
                {
                    mainWindowLogic.ErrorMessage("Cannot set category for split transactions");
                }
            }
        }

        public IEnumerable<CategoryItem> Categories => mainWindowLogic.Categories;

        // Amount (not a UI property, needed to recompute balance)
        public decimal Amount => data.Amount;

        // Payment
        public string PaymentString => data.Amount > 0 ? "" : (-data.Amount).ToString("N");
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
                        OnPropertyChanged(() => PaymentString);
                        OnPropertyChanged(() => DepositString);
                        OnPropertyChanged(() => Deposit);
                    }
                    else
                    {
                        mainWindowLogic.ErrorMessage("Cannot set amount on split transactions");
                    }
                }
            }
        }
        public bool IsPaymentTabStop => !IsDepositTabStop;


        public string Status
        {
            get => GetStatusString();
            set => ParseStatusString(value);
        }

        public string[] StatusSource { get; } = new string[] { "", "c", "R" };

        // Deposit
        public string DepositString => data.Amount <= 0 ? "" : data.Amount.ToString("N");
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
                        OnPropertyChanged(() => PaymentString);
                        OnPropertyChanged(() => DepositString);
                        OnPropertyChanged(() => Payment);
                    }
                    else
                    {
                        mainWindowLogic.ErrorMessage("Cannot set amount on split transactions");
                    }
                }
            }
        }
        public bool IsDepositTabStop { get; private set; }

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
                }
            }
        }

        public string BalanceString { get; private set; } = "";

        // Group sorter
        // To have the uncommitted transaction in a different group than the others
        // And always displayed at the bottom of the listview
        // (see PropertyGroupDescription in BankRegisterLogic constructor)
        public string GroupSorter => (transID < 0) ? "Z" : "A";

        // Split dialog
        public CommandBase SplitTransaction { get; }


        #endregion

        #region IEditable interface implementation

        public void BeginEdit()
        {
            // Save existing data
            if (backup == null)
            {
                backup = new BankTransactionData(data);
            }

            Console.WriteLine($"Begin edit transaction date {Date.ToShortDateString()} Payee {Payee} amount {Payment}");
        }

        public void CancelEdit()
        {
            // Restore data
            if (backup != null)
            {
                if (data.Date != backup.Date)
                {
                    data.Date = backup.Date;
                    OnPropertyChanged(() => Date);
                }

                if (data.Medium != backup.Medium)
                {
                    data.Medium = backup.Medium;
                    OnPropertyChanged(() => Medium);
                }

                if (data.CheckNumber != backup.CheckNumber)
                {
                    data.CheckNumber = backup.CheckNumber;
                    OnPropertyChanged(() => Medium);
                }

                if (data.Payee != backup.Payee)
                {
                    data.Payee = backup.Payee;
                    OnPropertyChanged(() => Payee);
                }

                if (data.Status != backup.Status)
                {
                    data.Status = backup.Status;
                    OnPropertyChanged(() => Status);
                }

                data.LineItems.Clear();
                backup.LineItems.ForEach(li => data.LineItems.Add(new LineItem(li)));
                
                OnPropertyChanged(() => Memo);
                OnPropertyChanged(() => Category);
                OnPropertyChanged(() => PaymentString);
                OnPropertyChanged(() => Payment);
                OnPropertyChanged(() => DepositString);
                OnPropertyChanged(() => Deposit);

                backup = null;
            }

            Console.WriteLine($"Cancel edit transaction date {Date.ToShortDateString()} Payee {Payee} amount {Payment}");
        }

        public void EndEdit()
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
                bankRegisterLogic.MoveDownOneTransaction(transID < 0);
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
            bool wasEmptyTransaction = transID < 0;
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

            if (data.Medium != backup.Medium)
            {
                OnPropertyChanged(() => Medium);
            }

            if (data.CheckNumber != backup.CheckNumber)
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

            if (transID < 0)
            {
                // Create new transaction row
                var transactionRow = household.Transactions.Add(accountRow, data.Date, data.Payee, data.Status);
                transID = transactionRow.ID;

                // Create new banking transaction row if needed
                if (bankRegisterLogic.IsBank)
                {
                    household.BankingTransactions.Add(transactionRow, data.Medium, data.CheckNumber);
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
                var transactionRow = household.Transactions.Update(transID, accountRow, data.Date, data.Payee, data.Status);

                // Update banking transaction if needed
                if (bankRegisterLogic.IsBank)
                {
                    household.BankingTransactions.Update(transactionRow, data.Medium, data.CheckNumber);
                }

                //
                // Update the line items
                //
                // First find deleted line items and delete them in the DB
                foreach (var li in backup.LineItems)
                {
                    if (li.ID >= 0 && !data.LineItems.Contains(li))
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

        // Called by on behalf of reconcile to update status on transaction if needed
        public void UpdateStatus(ETransactionStatus newStatus)
        {
            if (data.Status != newStatus)
            {
                data.Status = newStatus;
                OnPropertyChanged(() => Status);
            }
        }

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

            if (data.Medium == ETransactionMedium.Check)
            {
                if (data.CheckNumber > 0)
                {
                    rs = data.CheckNumber.ToString();
                }
            }
            else
            {
                rs = EnumDescriptionAttribute.GetDescription(data.Medium);
            }

            return rs;
        }

        private void ParseMediumString(string type)
        {
            data.CheckNumber = 0;

            if (type == MEDIUM_NEXTCHECKNUM)
            {
                data.Medium = ETransactionMedium.Check;
                data.CheckNumber = GetNextCheckNumber();
                OnPropertyChanged(() => Medium);
            }
            else if (MediumSource.Contains(type))
            {
                data.Medium = EnumDescriptionAttribute.MatchDescription<ETransactionMedium>(type);
            }
            else
            {
                if (uint.TryParse(type, out uint checkNum))
                {
                    data.Medium = ETransactionMedium.Check;
                    data.CheckNumber = checkNum;
                }
                else
                {
                    data.Medium = ETransactionMedium.None;
                }
            }

            IsDepositTabStop = data.Medium == ETransactionMedium.Deposit;
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
                    result = Math.Max(result, btl.data.CheckNumber);
                }
            }

            return result + 1;
        }

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
        }

        #endregion

        #region Overrides

        public override bool Equals(object obj)
        {
            return obj is BankingTransactionLogic o && transID == o.transID;
        }

        public override int GetHashCode()
        {
            return transID.GetHashCode();
        }

        #endregion
    }
}
