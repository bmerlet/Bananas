using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Database;

namespace BanaData.Logic.Main
{
    /// <summary>
    /// Logic for one banking transaction
    /// </summary>
    public class BankingTransactionLogic : LogicBase, IEditableObject
    {
        public class BankTransactionData
        {
            public BankTransactionData(
                DateTime date,
                ETransactionMedium type,
                decimal checkNumber,
                string payee,
                string memo,
                string category,
                ETransactionStatus status,
                decimal amount)
            {
                Date = date;
                Type = type;
                CheckNumber = checkNumber;
                Payee = payee;
                Memo = memo;
                Category = category;
                Status = status;
                Amount = amount;
            }

            public BankTransactionData(BankTransactionData src)
            {
                Date = src.Date;
                Type = src.Type;
                CheckNumber = src.CheckNumber;
                Payee = src.Payee;
                Memo = src.Memo;
                Category = src.Category;
                Status = src.Status;
                Amount = src.Amount;
            }

            public DateTime Date;
            public ETransactionMedium Type;
            public decimal CheckNumber;
            public string Payee;
            public string Memo;
            public string Category;
            public ETransactionStatus Status;
            public decimal Amount;

        }

        // Main logic
        private readonly MainWindowLogic mainWindowLogic;

        // ZZZ
        public readonly int transID;

        private readonly BankTransactionData data;
        private BankTransactionData backup;

        public BankingTransactionLogic(MainWindowLogic mainWindowLogic, int transID, BankTransactionData data, decimal balance)
        {
            this.mainWindowLogic = mainWindowLogic;
            this.transID = transID;
            this.data = data;
            Balance = balance.ToString("N");
        }

        // To create new transactions (not in DB yet)
        public BankingTransactionLogic(MainWindowLogic mainWindowLogic)
        {
            this.mainWindowLogic = mainWindowLogic;
            this.transID = -1;
            this.data = new BankTransactionData(DateTime.Today, ETransactionMedium.None, 0, "", "", "", ETransactionStatus.Pending, 0);
            Balance = "";
        }

        #region UI properties

        // Date of the transaction
        public DateTime Date
        {
            get => data.Date;
            set => data.Date = value;
        }

        // Medium of transaction
        public string Type 
        {
            get => GetTypeString();
            set => ParseTypeString(value);
        }

        // ZZZ
        public string[] TypeSource { get; } = new string[] { "Next Check Num", "ATM", "Deposit", "Transfer", "EFT" };

        // Payee
        public string Payee
        { 
            get => data.Payee;
            set => data.Payee = value;
        }

        // Memorized payees
        public IEnumerable<MemorizedPayeeItem> Payees => mainWindowLogic.MemorizedPayees;

        // Memo
        public string Memo
        {
            get => data.Memo;
            set => data.Memo = value;
        }

        // Category ZZZ
        public string Category 
        {
            get => data.Category;
            set => data.Category = value;
        }

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

        // Balance: How? ZZZZZ
        public string Balance { get; set; }

        #endregion

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

                if (data.Type != backup.Type)
                {
                    data.Type = backup.Type;
                    OnPropertyChanged(() => Type);
                }

                if (data.CheckNumber != backup.CheckNumber)
                {
                    data.CheckNumber = backup.CheckNumber;
                    OnPropertyChanged(() => Type);
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
            }

            Console.WriteLine("Cancel edit transaction  date {Date.ToShortDateString()} Payee {Payee} amount {Payment}");
        }

        public void EndEdit()
        {
            // just clear the backup
            backup = null;

            Console.WriteLine("End edit transaction");
        }

        private string GetTypeString()
        {
            string rs = "???";

            switch (data.Type)
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

        private void ParseTypeString(string type)
        {
            data.CheckNumber = 0;

            switch (type)
            {
                case "PrtCk":
                    data.Type = ETransactionMedium.PrintCheck;
                    break;
                case "ATM":
                    data.Type = ETransactionMedium.ATM;
                    break;
                case "Cash":
                    data.Type = ETransactionMedium.Cash;
                    break;
                case "DEP":
                    data.Type = ETransactionMedium.Deposit;
                    break;
                case "Div":
                    data.Type = ETransactionMedium.Dividend;
                    break;
                case "EFT":
                    data.Type = ETransactionMedium.EFT;
                    break;
                case "":
                    data.Type = ETransactionMedium.None;
                    break;
                default:
                    if (decimal.TryParse(type, out decimal checkNum))
                    {
                        data.Type = ETransactionMedium.Check;
                        data.CheckNumber = checkNum;
                    }
                    else
                    {
                        data.Type = ETransactionMedium.None;
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
    }
}
