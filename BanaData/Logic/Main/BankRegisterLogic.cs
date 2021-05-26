using System;
using System.Collections.Generic;
using System.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Database;
using System.Windows.Data;
using System.ComponentModel;

namespace BanaData.Logic.Main
{
    public class BankRegisterLogic : LogicBase
    {
        #region Private members

        // Main logic
        private readonly MainWindowLogic mainWindowLogic;

        // Actual collection of transactions backing the Transactions collection view property
        private readonly ObservableCollection<BankingTransaction> transactions = new ObservableCollection<BankingTransaction>();

        // List of memorized payees
        private readonly List<MemorizedPayee> memorizedPayees = new List<MemorizedPayee>();

        #endregion

        #region Constructor

        public BankRegisterLogic(MainWindowLogic mainWindowLogic)
        {
            this.mainWindowLogic = mainWindowLogic;

            // Create transaction collection view, and sort by date
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
        }

        #endregion

        #region UI properties

        // Name of the account
        public string AccountName { get; private set; }

        // If banking account (as opposed to credit card)
        public bool IsBank { get; private set; }

        // Transactions. The CollectionView type enables sorting on columns
        public CollectionView Transactions { get; }

        #endregion

        #region Actions

        public void SetAccount(int accountID)
        {
            // Get the account details
            var household = mainWindowLogic.Household;
            var account = household.Accounts.FindByID(accountID);

            // Export account name
            AccountName = account.Name;
            OnPropertyChanged(() => AccountName);

            // Export if banking account
            IsBank = account.Type == EAccountType.Bank;
            OnPropertyChanged(() => IsBank);

            // Build payee list
            BuildPayeeList();

            // Find transactions and put them in the transaction list
            transactions.Clear();
            decimal balance = 0;
            var accTransRel = household.Relations["FK_Accounts_Transactions"];
            foreach (var transRow in account.GetChildRows(accTransRel))
            {
                // Get transaction
                var trans = transRow as Household.TransactionsRow;

                // Get banking details
                Household.BankingTransactionsRow transBank = null;
                if (account.Type == EAccountType.Bank)
                {
                    transBank = household.BankingTransactions.GetByTransaction(trans);
                }

                // Get line item(s)
                var lineItems = household.LineItems.GetByTransaction(trans);
                decimal amount = lineItems.Sum(li => li.Amount);
                balance += amount;
                string category = "";

                if (lineItems.Length > 1)
                {
                    category = "<Split>";
                }
                else if (lineItems[0].IsTransfer)
                {
                    if (!lineItems[0].IsAccountIDNull())
                    {
                        var destAccount = household.Accounts.FindByID(lineItems[0].AccountID);
                        category = "[" + destAccount.Name + "]";
                    }
                }
                else
                {
                    if (!lineItems[0].IsCategoryIDNull())
                    {
                        var destCategory = household.Categories.FindByID(lineItems[0].CategoryID);
                        category = destCategory.FullName;
                    }
                }

                string memo = (lineItems.Length == 1) ? (lineItems[0].IsMemoNull() ? "" : lineItems[0].Memo) : "";

                var bt = new BankingTransaction(memorizedPayees)
                {
                    Date = trans.Date,
                    Type = transBank == null ? "" : transBank.GetRegisterMediumString(),
                    Payee = trans.IsPayeeNull() ? "" : trans.Payee,
                    Memo = memo,
                    Category = category,
                    Payment = (amount >= 0) ? "" : (-amount).ToString("N"),
                    Status = (trans.Status == ETransactionStatus.Reconciled) ? "R" : ((trans.Status == ETransactionStatus.Cleared) ? "c" : " "),
                    Deposit = (amount >= 0) ? amount.ToString("N") : "",
                    Balance = balance.ToString("N")
                };
                transactions.Add(bt);
            }

        }

        private void BuildPayeeList()
        {
            var household = mainWindowLogic.Household;

            memorizedPayees.Clear();

            foreach (var mpr in mainWindowLogic.Household.MemorizedPayees)
            {
                // Get memorized line item(s)
                var lineItems = household.MemorizedLineItems.GetByMemorizedPayee(mpr);
                decimal amount = lineItems.Sum(li => li.Amount);

                string memo = lineItems[0].IsMemoNull() ? "" : lineItems[0].Memo;
                string category = "";

                if (!lineItems[0].IsCategoryIDNull())
                {
                    var destCategory = household.Categories.FindByID(lineItems[0].CategoryID);
                    category = destCategory.FullName;
                }

                var mp = new MemorizedPayee(mpr.Payee, amount, category, memo);
                memorizedPayees.Add(mp);
            }

            memorizedPayees.Sort();
        }

        #endregion

        #region Transaction class

        // Class representing one banking transaction
        public class BankingTransaction : LogicBase, IEditableObject
        {
            public BankingTransaction(IEnumerable<MemorizedPayee> payees)
            {
                Payees = payees;
            }

            public DateTime Date { get; set; }

            public string Type { get; set; }
            public string[] TypeSource { get; } = new string[] { "Next Check Num", "ATM", "Deposit", "Transfer", "EFT" };

            public string Payee { get; set; }
            public IEnumerable<MemorizedPayee> Payees { get; }

            public string Memo { get; set; }
            public string Category { get; set; }
            public string Payment { get; set; }

            public string Status { get; set; }
            public string[] StatusSource { get; } = new string[] { "", "c", "R" };

            public string Deposit { get; set; }
            public string Balance { get; set; }

            public void BeginEdit()
            {
                Console.WriteLine($"Begin edit transaction date {Date.ToShortDateString()} Payee {Payee} amount {Payment}");
                // ZZZ Backup
            }

            public void CancelEdit()
            {
                Console.WriteLine("Cancel edit transaction");
                // ZZZ Restore backup
            }

            public void EndEdit()
            {
                Console.WriteLine("End edit transaction");
                // ZZZ Clear backup
            }
        }

        #endregion

        #region Memorized payee class

        // Class representing memorized payees, as viewed in the autocomplete payee textbox
        public class MemorizedPayee : IComparable<MemorizedPayee>
        {
            public MemorizedPayee(string payee, decimal amount, string category, string memo)
            {
                Payee = payee;
                Amount = amount.ToString("N");
                Category = category;
                Memo = memo;
            }

            public string Payee { get; }
            public string Amount { get; }
            public string Category { get; }
            public string Memo { get; }

            public int CompareTo(MemorizedPayee other)
            {
                return Payee.CompareTo(other.Payee);
            }

            // string that is used to filter on (and to return) 
            public override string ToString()
            {
                return Payee;
            }
        }

        #endregion
    }
}
