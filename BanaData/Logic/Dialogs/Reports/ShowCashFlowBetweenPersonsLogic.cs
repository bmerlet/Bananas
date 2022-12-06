using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Main;
using BanaData.Serializations;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowCashFlowBetweenPersonsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;
        private readonly UserSettings userSettings;

        #endregion

        #region Constructor

        public ShowCashFlowBetweenPersonsLogic(MainWindowLogic _mainWindowLogic, Household _household, UserSettings _userSettings)
        {
            mainWindowLogic = _mainWindowLogic;
            household = _household;
            userSettings = _userSettings;

            // Setup years
            YearPickerLogic = new YearPickerLogic(household, userSettings);
            YearPickerLogic.YearChanged += (s, e) => ComputeCashFlow();

            // Setup members
            members = household.Person.ToArray();
            var tmpList = new List<string>();
            GenerateMemberOrder(tmpList, "", members.Select(m => m.Name).ToArray());
            MemberOrderSource = tmpList.ToArray();

            if (userSettings.MemberOrderForCashFlowDialog != null)
            {
                ParseMemberOrder(userSettings.MemberOrderForCashFlowDialog, false);
            }

            // Give the cash flow item list to the UI
            CashFlowItemsSource = (CollectionView)CollectionViewSource.GetDefaultView(cashFlowItems);
            CashFlowItemsSource.SortDescriptions.Add(new SortDescription("Sorter", ListSortDirection.Ascending));

            // Compute!
            ComputeCashFlow();
        }

        #endregion

        #region UI properties

        // Year
        public YearPickerLogic YearPickerLogic { get; }

        // Frequency of expense/income report
        private const string FREQ_MONTHLY = "month";
        private const string FREQ_QUARTERLY = "quarter";
        private const string FREQ_YEARLY = "year";
        public string[] FrequencySource { get; } = new string[] { FREQ_MONTHLY, FREQ_QUARTERLY, FREQ_YEARLY };

        private string selectedFrequency = FREQ_QUARTERLY;
        public string SelectedFrequency { get => selectedFrequency; set { selectedFrequency = value; ComputeCashFlow(); } }

        // If grouping similar accounts
        private bool isGroupingAccounts = false;
        public bool? IsGroupingAccounts { get => isGroupingAccounts; set { isGroupingAccounts = value == true; ComputeCashFlow(); } }

        // RFU
        public string[] MemberOrderSource { get; }
        public string MemberOrder { get => String.Join(", ", members.Select(m => m.Name)); set => ParseMemberOrder(value, true); }
        private Household.PersonRow[] members;

        // Description of the columns
        public IEnumerable<ColumnDescription> ColumnDescriptions => GetColumnDescriptions();

        // Cash flow items
        private readonly ObservableCollection<CashFlowItem> cashFlowItems = new ObservableCollection<CashFlowItem>();
        public CollectionView CashFlowItemsSource { get; }

        #endregion

        #region Actions

        private void ComputeCashFlow()
        {
            // Compute start/end date
            var startDate = new DateTime(YearPickerLogic.SelectedYear, 1, 1);
            var endDate = new DateTime(YearPickerLogic.SelectedYear, 12, 31);
            cashFlowItems.Clear();

            // Find transfers between members
            ComputeTransfersBetweenMembers(startDate, endDate);

            // Consolidate transfers from same account and same date
            if (isGroupingAccounts)
            {
                ConsolidateTransfersBetweenMembers();
            }

            // Compute per-member expenses
            ComputePerMemberExpenses(startDate, endDate);

            // Balances
            ComputeBalances();
        }

        private void ComputeTransfersBetweenMembers(DateTime startDate, DateTime endDate)
        {
            // Find transfers from an account owned by this member to an account owned by another member
            foreach (var transactionRow in household.RegularTransactions
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate && !tr.AccountRow.IsPersonIDNull()))
            {
                foreach (var lineItemRow in transactionRow.GetLineItemRows())
                {
                    if (lineItemRow.Amount > 0 &&
                        lineItemRow.GetLineItemTransferRow() is Household.LineItemTransferRow tx &&
                        !tx.AccountRow.IsPersonIDNull() && tx.AccountRow.PersonRow != transactionRow.AccountRow.PersonRow)
                    {
                        // This is a transfer - Create the transfer cash flow item
                        var mis = new List<MemberItem>();
                        foreach (var m in members)
                        {
                            if (m == transactionRow.AccountRow.PersonRow)
                            {
                                mis.Add(MemberItem.GetAmountMemberItem(m, lineItemRow.Amount));
                            }
                            else if (m == tx.AccountRow.PersonRow)
                            {
                                mis.Add(MemberItem.GetAmountMemberItem(m, -lineItemRow.Amount));
                            }
                            else
                            {
                                mis.Add(MemberItem.GetAmountMemberItem(m, 0));
                            }
                        }
                        var desc = transactionRow.IsMemoNull() ? $"{tx.AccountRow.Name} -> {transactionRow.AccountRow.Name}" : transactionRow.Memo;
                        var accountToUseForSorter = transactionRow.AccountRow.Type == EAccountType.Investment ? transactionRow.AccountRow : tx.AccountRow;
                        cashFlowItems.Add(new CashFlowItem(transactionRow.Date, desc, mis.ToArray(), $"5{transactionRow.Date:yyyy/MM/dd}{accountToUseForSorter.Name}1"));

                        // If this is a compound operations (divx, soldx, ..), create a cash flow item explaining that
                        // the money was first received before being transfered (or was first received then spent, for a boughtx)
                        CreateCashFlowItemForCompoundTransaction(transactionRow, lineItemRow.Amount);
                        CreateCashFlowItemForCompoundTransaction(tx.TransactionRow, -lineItemRow.Amount);
                    }
                }
            }
        }

        private void CreateCashFlowItemForCompoundTransaction(Household.TransactionRow transactionRow, decimal amount)
        {
            if (transactionRow.AccountRow.Type == EAccountType.Investment &&
                transactionRow.GetInvestmentTransaction() is Household.InvestmentTransactionRow investmentTransactionRow)
            {
                bool compound = false;
                if (investmentTransactionRow.Type == EInvestmentTransactionType.BuyFromTransferredCash)
                {
                    compound = true;
                }
                else if (investmentTransactionRow.Type == EInvestmentTransactionType.SellAndTransferCash ||
                    investmentTransactionRow.Type == EInvestmentTransactionType.TransferDividends ||
                    investmentTransactionRow.Type == EInvestmentTransactionType.TransferShortTermCapitalGains ||
                    investmentTransactionRow.Type == EInvestmentTransactionType.TransferLongTermCapitalGains)
                {
                    compound = true;
                    amount = -amount;
                }

                if (compound)
                {
                    var mis = new List<MemberItem>();
                    foreach (var m in members)
                    {
                        if (m == transactionRow.AccountRow.PersonRow)
                        {
                            mis.Add(MemberItem.GetAmountMemberItem(m, amount));
                        }
                        else
                        {
                            mis.Add(MemberItem.GetAmountMemberItem(m, 0));
                        }
                    }

                    string desc = "??";
                    string accountName = transactionRow.AccountRow.Name;
                    string symbol = investmentTransactionRow.SecurityRow.Symbol;
                    if (investmentTransactionRow.Type == EInvestmentTransactionType.BuyFromTransferredCash)
                    {
                        desc = $"{accountName}  Bought {symbol} shares";
                    }
                    else if (investmentTransactionRow.Type == EInvestmentTransactionType.SellAndTransferCash)
                    {
                        desc = $"{accountName}  Sold {symbol} shares";
                    }
                    else if (investmentTransactionRow.Type == EInvestmentTransactionType.TransferDividends)
                    {
                        desc = $"{accountName}  Received dividends";
                        if (!isGroupingAccounts)
                        {
                            desc += $" from {symbol}";
                        }
                    }
                    else if (investmentTransactionRow.Type == EInvestmentTransactionType.TransferShortTermCapitalGains)
                    {
                        desc = $"{accountName}  Received STCG";
                        if (!isGroupingAccounts)
                        {
                            desc += $" from {symbol}";
                        }
                    }
                    else if (investmentTransactionRow.Type == EInvestmentTransactionType.TransferLongTermCapitalGains)
                    {
                        desc = $"{accountName}  Received LTCG";
                        if (!isGroupingAccounts)
                        {
                            desc += $" from {symbol}";
                        }
                    }
                    cashFlowItems.Add(new CashFlowItem(transactionRow.Date, desc, mis.ToArray(), $"5{transactionRow.Date:yyyy/MM/dd}{transactionRow.AccountRow.Name}0"));
                }
            }
        }

        private void ConsolidateTransfersBetweenMembers()
        {
            List<CashFlowItem> tmpList = new List<CashFlowItem>(cashFlowItems);
            tmpList.Sort((i1, i2) => i1.Sorter.CompareTo(i2.Sorter));

            cashFlowItems.Clear();

            for(int i = 0; i < tmpList.Count; i++)
            {
                var item = tmpList[i];
                for (int j = i + 1; j < tmpList.Count; j++)
                {
                    if (tmpList[j].Sorter == item.Sorter)
                    {
                        var mis = new MemberItem[item.MemberItems.Length];
                        for(int k = 0; k < mis.Length; k++)
                        {
                            var amount = item.MemberItems[k].Amount + tmpList[j].MemberItems[k].Amount;
                            mis[k] = MemberItem.GetAmountMemberItem(item.MemberItems[k].Member, amount);
                        }
                        item = new CashFlowItem(item.Date, item.Description, mis, item.Sorter);
                        i = j;
                    }
                    else
                    {
                        break;
                    }
                }
                cashFlowItems.Add(item);
            }
        }

        private void ComputePerMemberExpenses(DateTime startDate, DateTime finalEndDate)
        {
            // Amount per member
            Dictionary<Household.PersonRow, decimal> memberAmount = new Dictionary<Household.PersonRow, decimal>();
            foreach (var member in members)
            {
                memberAmount.Add(member, 0);
            }

            // Get time-sorted transactions for relevant accounts and relevant time period
            List<Household.TransactionRow> transactions;
            transactions = household.RegularTransactions
                .Where(tr => tr.Date >= startDate && tr.Date <= finalEndDate)
                .Where(tr => !tr.AccountRow.IsPersonIDNull() && members.Contains(tr.AccountRow.PersonRow))
                .ToList();
            transactions.Sort((t1, t2) =>
            {
                // Sort by date
                int ret = t1.Date.CompareTo(t2.Date);
                if (ret == 0)
                {
                    // The sort by account
                    ret = t1.AccountID.CompareTo(t2.AccountID);
                    if (ret == 0 && t1.AccountRow.Type == EAccountType.Investment)
                    {
                        // Shares in first to avoid empty lot issues
                        var i1 = t1.GetInvestmentTransaction();
                        var i2 = t2.GetInvestmentTransaction();
                        if (i1.IsSecurityIn && i2.IsSecurityIn)
                        {
                            ret = 0;
                        }
                        else if (i1.IsSecurityIn)
                        {
                            ret = -1;
                        }
                        else if (i2.IsSecurityIn)
                        {
                            ret = 1;
                        }
                    }

                    // Don't let anybody think they are equal
                    if (ret == 0)
                    {
                        return t1.ID.CompareTo(t2.ID);
                    }
                }
                return ret;
            });

            // Get the first end date
            DateTime endDate = GetNextEndDate(startDate);
            var transactionEnum = transactions.GetEnumerator();
            bool moreTransactions = transactionEnum.MoveNext();

            // Loop over all the dates
            for (DateTime curDate = startDate; curDate <= finalEndDate; curDate = curDate.AddDays(1))
            {
                if (curDate >= endDate || curDate == finalEndDate)
                {
                    // Create the item
                    var mis = new List<MemberItem>();
                    foreach (var member in members)
                    {
                        mis.Add(MemberItem.GetAmountMemberItem(member, memberAmount[member]));
                        memberAmount[member] = 0;
                    }
                    var date = endDate.AddDays(-1);
                    cashFlowItems.Add(new CashFlowItem(date, GetExpenseDescriptionForDate(endDate), mis.ToArray(), $"5{date:yyyy/MM/dd}1"));

                    // Get next end date
                    endDate = GetNextEndDate(endDate);
                }

                while(moreTransactions && transactionEnum.Current is Household.TransactionRow transaction && transaction.Date <= curDate)
                {
                    var lis = transaction.GetLineItemRows();
                    foreach (var li in lis)
                    {
                        if (li.GetLineItemCategoryRow() is Household.LineItemCategoryRow licr)
                        {
                            // Count all income/expense
                            memberAmount[transaction.AccountRow.PersonRow] += li.Amount;
                        }
                    }

                    moreTransactions = transactionEnum.MoveNext();
                }
            }
        }

        private DateTime GetNextEndDate(DateTime start)
        {
            DateTime end = start;

            switch(selectedFrequency)
            {
                case FREQ_MONTHLY:
                    end = start.AddMonths(1);
                    break;
                case FREQ_QUARTERLY:
                    end = start.AddMonths(3);
                    break;
                case FREQ_YEARLY:
                    end = start.AddYears(1);
                    break;
            }

            return end;
        }

        private string GetExpenseDescriptionForDate(DateTime endDate)
        {
            string desc = "???";

            switch (selectedFrequency)
            {
                case FREQ_MONTHLY:
                    desc = $"Income/expenses for the month of {endDate.AddDays(-1):MMMM}";
                    break;
                case FREQ_QUARTERLY:
                    desc = $"Income/expenses for Q{(endDate.AddDays(-1).Month / 4) + 1}";
                    break;
                case FREQ_YEARLY:
                    desc = "Income/expenses for the year";
                    break;
            }
            return desc;
        }

        private void ComputeBalances()
        {
            // Per-member balance
            Dictionary<Household.PersonRow, decimal> memberBalance = new Dictionary<Household.PersonRow, decimal>();
            foreach (var member in members)
            {
                memberBalance.Add(member, 0);
            }

            // Update in the correct order
            CashFlowItemsSource.Refresh();

            foreach (CashFlowItem cashFlowItem in CashFlowItemsSource)
            {
                foreach (var m in cashFlowItem.MemberItems)
                {
                    memberBalance[m.Member] += m.Amount;
                    if (m.Amount != 0)
                    {
                        m.SetBalance(memberBalance[m.Member]);
                    }
                }
            }

            // Debug
            //var mis = new List<MemberItem>();
            //foreach (var member in Members)
            //{
            //    mis.Add(MemberItem.GetBalanceMemberItem(member, memberBalance[member]));
            //}
            //cashFlowItems.Add(new CashFlowItem(new DateTime(YearPickerLogic.SelectedYear, 12, 31), "EXPECTED BALANCES", mis.ToArray(), "8"));
        }

        //
        // Generate possible member orders
        //
        private void GenerateMemberOrder(List<string> list, string partialNameList, string[] remainingNames)
        {
            if (remainingNames.Length == 1)
            {
                list.Add(partialNameList + ", " + remainingNames[0]);
            }
            else
            {
                for (int i = 0; i < remainingNames.Length; i++)
                {
                    var newPartialNameList = (partialNameList == "" ? "" : partialNameList + ", ") + remainingNames[i];
                    var newRemainingNames = new string[remainingNames.Length - 1];
                    int k = 0;
                    for(int j = 0; j < remainingNames.Length; j++)
                    {
                        if (j != i)
                        {
                            newRemainingNames[k++] = remainingNames[j];
                        }
                    }

                    GenerateMemberOrder(list, newPartialNameList, newRemainingNames);
                }
            }
        }

        //
        // Parse member order
        //
        private void ParseMemberOrder(string value, bool publish)
        {
            var newMembers = new Household.PersonRow[members.Length];
            int mIx = 0;
            foreach(var name in value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                newMembers[mIx++] = members.Where(m => m.Name == name).Single();
            }

            members = newMembers;

            if (publish)
            {
                if (userSettings.MemberOrderForCashFlowDialog != value)
                {
                    userSettings.MemberOrderForCashFlowDialog = value;
                    mainWindowLogic.SaveUserSettings();
                }
                InvokePropertyChanged(nameof(ColumnDescriptions));
                ComputeCashFlow();
            }
        }

        //
        // Build column descriptions based on members
        //
        private IEnumerable<ColumnDescription> GetColumnDescriptions()
        {
            var cols = new List<ColumnDescription>()
            {
                new ColumnDescription("Date", "Date", "MM/dd/yyyy", 80, false),
                new ColumnDescription("Description", "Description", null, 0, false)
            };

            for(int i = 0; i < members.Length; i++)
            {
                cols.Add(new ColumnDescription(members[i].Name + "\nAmount" , $"MemberItems[{i}].Amount", "N2", 90, true));
                cols.Add(new ColumnDescription("\nBalance", $"MemberItems[{i}].Balance", "N2", 90, true));
            }

            return cols;
        }

        #endregion

        #region Support classes

        /// <summary>
        /// One line of the cash flow report
        /// </summary>
        public class CashFlowItem : IComparable<CashFlowItem>
        {
            public CashFlowItem(DateTime date, string description, MemberItem[] memberItems, string sorter) 
                => (Date, Description, MemberItems, Sorter) = (date, description, memberItems, sorter);

            public DateTime Date { get; }
            public string Description { get; }
            public MemberItem[] MemberItems { get; }

            // String to sort on
            public string Sorter { get; }

            public int CompareTo(CashFlowItem other)
            {
                int result = Date.CompareTo(other.Date);
                if (result == 0)
                {
                    result = Description.CompareTo(other.Description);
                }
                return result;
            }
        }

        public class MemberItem : LogicBase
        {
            private MemberItem(Household.PersonRow member, decimal amount, decimal balance) =>
                (Member, Amount, Balance) = (member, amount, balance);

            static public MemberItem GetAmountMemberItem(Household.PersonRow member, decimal amount)
                => new MemberItem(member, amount, 0);

            static public MemberItem GetBalanceMemberItem(Household.PersonRow member, decimal balance)
                => new MemberItem(member, 0, balance);

            public void SetBalance(decimal balance)
            {
                Balance = balance;
                InvokePropertyChanged(nameof(Balance));
            }

            public readonly Household.PersonRow Member;
            public decimal Amount { get; }
            public decimal Balance { get; private set; }
        }

        /// <summary>
        /// Desacribe one column of this report
        /// </summary>
        public class ColumnDescription
        {
            public ColumnDescription(string column, string value, string format, double width, bool isAmount) =>
                (ColumnName, ValueName, Format, Width, IsAmount) = (column, value, format, width, isAmount);
            public readonly string ColumnName;
            public readonly string ValueName;
            public readonly string Format;
            public readonly double Width;
            public readonly bool IsAmount;
        }

        #endregion
    }
}
