using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowCashFlowBetweenPersonsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public ShowCashFlowBetweenPersonsLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Setup years
            YearPickerLogic = new YearPickerLogic(mainWindowLogic);
            YearPickerLogic.YearChanged += (s, e) => ComputeCashFlow();

            // Setup members
            Members = mainWindowLogic.Household.Person.ToArray();
            // ZZZ Reorder manually
            if (Members.Length >= 3)
            {
                if (Members[1].Name.StartsWith("Su"))
                {
                    var s = Members[1];
                    Members[1] = Members[0];
                    Members[0] = s;
                }
                else if (Members[2].Name.StartsWith("Su"))
                {
                    var s = Members[2];
                    Members[2] = Members[0];
                    Members[0] = s;
                }
                if (Members[2].Name.StartsWith("Be"))
                {
                    var s = Members[2];
                    Members[2] = Members[1];
                    Members[1] = s;
                }
            }

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
        public CommandBase PickMembersCommand { get; }
        public Household.PersonRow[] Members { get;  }

        // First line
        public CashFlowItem CashFlowFirstItem { get; private set; }

        // Other lines
        public List<CashFlowItem> CashFlowItems { get; } = new List<CashFlowItem>();

        // Last line
        public CashFlowItem CashFlowLastItem { get; private set; }

        // Flag to rebuild table
        public bool RebuildTableSignal { get; private set; }

        #endregion

        #region Actions

        private void ComputeCashFlow()
        {
            // Compute start/end date
            var startDate = new DateTime(YearPickerLogic.SelectedYear, 1, 1);
            var endDate = new DateTime(YearPickerLogic.SelectedYear, 12, 31);
            CashFlowItems.Clear();

            // Compute member assets at beginning of year
            CashFlowFirstItem = ComputeMemberValueAtDate(startDate, "Initial balance");

            // Compute member assets at end of year
            CashFlowLastItem = ComputeMemberValueAtDate(endDate, "End balance");

            // Find transfers between members
            ComputeTransfersBetweenMembers(startDate, endDate);

            // Sort by date
            CashFlowItems.Sort();

            // Consolidate transfers from same account and same date
            ConsolidateTransfersBetweenMembers();

            // Compute per-member expenses
            ComputePerMemberExpenses(startDate, endDate);

            // Sort by date
            CashFlowItems.Sort();

            // Rebuild table
            RebuildTableSignal = !RebuildTableSignal;
            OnPropertyChanged(() => RebuildTableSignal);
        }

        private CashFlowItem ComputeMemberValueAtDate(DateTime date, string description)
        {
            var household = mainWindowLogic.Household;
            var mis = new List<MemberItem>();

            foreach (var member in Members)
            {
                decimal value = household.Account
                    .Where(accnt => !accnt.IsPersonIDNull() && accnt.PersonRow == member)
                    .Select(accnt => accnt.Type == EAccountType.Investment ? accnt.GetInvestmentValue(date) : accnt.GetBalance(date))
                    .Sum();
                mis.Add(new MemberItem(0, false, value, true));
            }

            return new CashFlowItem(date, description, mis.ToArray());
        }

        private void ComputeTransfersBetweenMembers(DateTime startDate, DateTime endDate)
        {
            var household = mainWindowLogic.Household;

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
                        // Gotcha
                        var mis = new List<MemberItem>();
                        foreach (var m in Members)
                        {
                            if (m == transactionRow.AccountRow.PersonRow)
                            {
                                mis.Add(new MemberItem(lineItemRow.Amount, true, 0, false));
                            }
                            else if (m == tx.AccountRow.PersonRow)
                            {
                                mis.Add(new MemberItem(-lineItemRow.Amount, true, 0, false));
                            }
                            else
                            {
                                mis.Add(new MemberItem(0, false, 0, false));
                            }
                        }
                        var desc = transactionRow.IsMemoNull() ? $"{tx.AccountRow.Name} -> {transactionRow.AccountRow.Name}" : transactionRow.Memo;
                        CashFlowItems.Add(new CashFlowItem(transactionRow.Date, desc, mis.ToArray()));
                    }
                }
            }
        }

        private void ConsolidateTransfersBetweenMembers()
        {
            List<CashFlowItem> tmpList = new List<CashFlowItem>(CashFlowItems);
            CashFlowItems.Clear();
            for(int i = 0; i < tmpList.Count; i++)
            {
                var item = tmpList[i];
                for (int j = i + 1; j < tmpList.Count; j++)
                {
                    if (tmpList[j].Date == item.Date && tmpList[j].Description == item.Description)
                    {
                        var mis = new MemberItem[item.MemberItems.Length];
                        for(int k = 0; k < mis.Length; k++)
                        {
                            var amount = item.MemberItems[k].Amount + tmpList[j].MemberItems[k].Amount;
                            mis[k] = new MemberItem(amount, amount != 0, 0, false);
                        }
                        item = new CashFlowItem(item.Date, item.Description, mis);
                        i = j;
                    }
                    else
                    {
                        break;
                    }
                }
                CashFlowItems.Add(item);
            }
        }

        private void ComputePerMemberExpenses(DateTime startDate, DateTime finalEndDate)
        {
            // Amount per member
            Dictionary<Household.PersonRow, decimal> memberAmount = new Dictionary<Household.PersonRow, decimal>();
            foreach (var member in Members)
            {
                memberAmount.Add(member, 0);
            }

            // Get time-sorted transactions for relevant accounts and relevant time period
            List<Household.TransactionRow> transactions;
            transactions = mainWindowLogic.Household.RegularTransactions
                .Where(tr => tr.Date >= startDate && tr.Date <= finalEndDate)
                .Where(tr => !tr.AccountRow.IsPersonIDNull() && Members.Contains(tr.AccountRow.PersonRow))
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
                if (curDate >= endDate)
                {
                    // Create the item
                    var mis = new List<MemberItem>();
                    foreach (var member in Members)
                    {
                        mis.Add(new MemberItem(memberAmount[member], true, 0, false));
                    }
                    CashFlowItems.Add(new CashFlowItem(endDate.AddDays(-1), GetExpenseDescriptionForDate(endDate), mis.ToArray()));

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

        #endregion

        #region Support classes

        public class CashFlowItem : IComparable<CashFlowItem>
        {
            public CashFlowItem(DateTime date, string description, MemberItem[] memberItems) 
                => (Date, Description, MemberItems) = (date, description, memberItems);

            public DateTime Date { get; }
            public string Description { get; }
            public MemberItem[] MemberItems { get; }

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

        public class MemberItem
        {
            public MemberItem(decimal amount, bool showAmount, decimal balance, bool showBalance) =>
                (Amount, ShowAmount, Balance, ShowBalance) = (amount, showAmount, balance, showBalance);
            public decimal Amount { get; }
            public bool ShowAmount { get; }
            public decimal Balance { get; }
            public bool ShowBalance { get; }
        }

        #endregion
    }
}
