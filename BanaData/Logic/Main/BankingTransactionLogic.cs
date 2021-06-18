using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Items;

namespace BanaData.Logic.Main
{
    /// <summary>
    /// Logic for one banking transaction
    /// </summary>
    public class BankingTransactionLogic : LogicBase, IEditableObject
    {
        #region Supporting data class

        public class BankTransactionData
        {
            // Construct from scratch
            public BankTransactionData(
                DateTime date,
                ETransactionMedium medium,
                decimal checkNumber,
                string payee,
                string memo,
                string category,
                ETransactionStatus status,
                decimal amount) => 
                (Date, Medium, CheckNumber, Payee, Memo, Category, Status, Amount) =
                (date, medium, checkNumber, payee, memo, category, status, amount);

            // Clone
            public BankTransactionData(BankTransactionData src) =>
                (Date, Medium, CheckNumber, Payee, Memo, Category, Status, Amount) =
                (src.Date, src.Medium, src.CheckNumber, src.Payee, src.Memo, src.Category, src.Status, src.Amount);

            // Properties
            public DateTime Date;
            public ETransactionMedium Medium;
            public decimal CheckNumber;
            public string Payee;
            public string Memo;
            public string Category;
            public ETransactionStatus Status;
            public decimal Amount;

            public override bool Equals(object obj)
            {
                return
                    obj is BankTransactionData o &&
                    o.Date.Equals(Date) &&
                    o.Medium == Medium &&
                    o.CheckNumber == CheckNumber &&
                    o.Payee == Payee &&
                    o.Memo == Memo &&
                    o.Category == Category &&
                    o.Status == Status &&
                    o.Amount == Amount;
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
        }

        // To create new transactions (not in DB yet)
        public BankingTransactionLogic(MainWindowLogic _mainWindowLogic, BankRegisterLogic _bankRegisterLogic, int _accountID)
            : this(_mainWindowLogic, _bankRegisterLogic, _accountID, -1,
                  new BankTransactionData(DateTime.Today, ETransactionMedium.None, 0, "", "", "", ETransactionStatus.Pending, 0))
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

        // ZZZ
        public string[] MediumSource { get; } = new string[] { "Next Check Num", "ATM", "Deposit", "Transfer", "EFT" };

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
            set => data.Memo = value;
        }

        // Category
        public string Category 
        {
            get => data.Category;
            set => data.Category = value;
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
                    data.Amount = -value;
                    OnPropertyChanged(() => PaymentString);
                    OnPropertyChanged(() => DepositString);
                    OnPropertyChanged(() => Deposit);
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
        public string DepositString => data.Amount <= 0 ? "" : data.Amount.ToString("N");
        public decimal Deposit
        {
            get => data.Amount;
            set
            {
                if (data.Amount != value)
                {
                    data.Amount = value;
                    OnPropertyChanged(() => PaymentString);
                    OnPropertyChanged(() => DepositString);
                    OnPropertyChanged(() => Payment);
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
                }
            }
        }

        public string BalanceString { get; private set; } = "";

        // Group sorter
        // To have the uncommitted transaction in a different group than the others
        // And always displayed at the bottom of the listview
        // (see PropertyGroupDescription in BankRegisterLogic constructor)
        public string GroupSorter => (transID < 0) ? "Z" : "A";

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

                if (data.Memo != backup.Memo)
                {
                    data.Memo = backup.Memo;
                    OnPropertyChanged(() => Memo);
                }

                if (data.Category != backup.Category)
                {
                    data.Category = backup.Category;
                    OnPropertyChanged(() => Category);
                }

                if (data.Status != backup.Status)
                {
                    data.Status = backup.Status;
                    OnPropertyChanged(() => Status);
                }

                if (data.Amount != backup.Amount)
                {
                    data.Amount = backup.Amount;
                    OnPropertyChanged(() => PaymentString);
                    OnPropertyChanged(() => Payment);
                    OnPropertyChanged(() => DepositString);
                    OnPropertyChanged(() => Deposit);
                }

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
                    household.BankingTransactions.Add(transactionRow, data.Medium, (uint)data.CheckNumber);
                }

                // Create all line items (ZZZ only one for now)
                var category = mainWindowLogic.Categories.FirstOrDefault(c => c.FullName == data.Category);
                int categoryId = category == null ? -1 : category.ID;
                int categoryAccountId = category == null ? -1 : category.AccountID;

                household.LineItems.Add(transactionRow, categoryId, categoryAccountId, data.Memo, data.Amount);
            }
            else
            {
                // Update transaction row
                var transactionRow = household.Transactions.Update(transID, accountRow, data.Date, data.Payee, data.Status);

                // Update banking transaction if needed
                if (bankRegisterLogic.IsBank)
                {
                    household.BankingTransactions.Update(transactionRow, data.Medium, (uint)data.CheckNumber);
                }

                // Update lineItem (ZZZ only one for now)
                var category = mainWindowLogic.Categories.FirstOrDefault(c => c.FullName == data.Category);
                int categoryId = category == null ? -1 : category.ID;
                int categoryAccountId = category == null ? -1 : category.AccountID;

                var lineItems = household.LineItems.GetByTransaction(transactionRow);
                var lineItem = lineItems[0]; // ZZZZZZZZZZZ
                household.LineItems.Update(lineItem, transactionRow, categoryId, categoryAccountId, data.Memo, data.Amount);
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
            if (arg is MemorizedPayeeItem memorizedPayee && backup != null)
            {
                if (!string.IsNullOrWhiteSpace(memorizedPayee.Memo))
                {
                    data.Memo = memorizedPayee.Memo;
                    OnPropertyChanged(() => Memo);
                }

                if (!string.IsNullOrWhiteSpace(memorizedPayee.Category))
                {
                    data.Category = memorizedPayee.Category;
                    OnPropertyChanged(() => Category);
                }

                if (memorizedPayee.Amount != 0)
                {
                    data.Amount = memorizedPayee.Amount;
                    OnPropertyChanged(() => PaymentString);
                    OnPropertyChanged(() => Payment);
                    OnPropertyChanged(() => DepositString);
                    OnPropertyChanged(() => Deposit);
                }
            }
        }

        private string GetMediumString()
        {
            string rs = "???";

            switch (data.Medium)
            {
                case ETransactionMedium.Check:
                    if (data.CheckNumber > 0)
                    {
                        rs = data.CheckNumber.ToString();
                    }
                    break;
                case ETransactionMedium.PrintCheck:
                    rs = "PrtCk";
                    break;
                case ETransactionMedium.ATM:
                    rs = "ATM";
                    break;
                case ETransactionMedium.Cash:
                    rs = "Cash";
                    break;
                case ETransactionMedium.Deposit:
                    rs = "DEP";
                    break;
                case ETransactionMedium.Dividend:
                    rs = "Div";
                    break;
                case ETransactionMedium.EFT:
                    rs = "EFT";
                    break;
                case ETransactionMedium.None:
                    rs = "";
                    break;
            }
            return rs;
        }

        private void ParseMediumString(string type)
        {
            data.CheckNumber = 0;

            switch (type)
            {
                case "PrtCk":
                    data.Medium = ETransactionMedium.PrintCheck;
                    break;
                case "ATM":
                    data.Medium = ETransactionMedium.ATM;
                    break;
                case "Cash":
                    data.Medium = ETransactionMedium.Cash;
                    break;
                case "DEP":
                    data.Medium = ETransactionMedium.Deposit;
                    break;
                case "Div":
                    data.Medium = ETransactionMedium.Dividend;
                    break;
                case "EFT":
                    data.Medium = ETransactionMedium.EFT;
                    break;
                case "":
                    data.Medium = ETransactionMedium.None;
                    break;
                default:
                    if (decimal.TryParse(type, out decimal checkNum))
                    {
                        data.Medium = ETransactionMedium.Check;
                        data.CheckNumber = checkNum;
                    }
                    else
                    {
                        data.Medium = ETransactionMedium.None;
                    }
                    break;
            }
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
