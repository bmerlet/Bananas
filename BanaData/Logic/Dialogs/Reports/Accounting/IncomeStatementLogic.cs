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
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports.Accounting
{
    public class IncomeStatementLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public IncomeStatementLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Setup date range
            DateRangeLogic = new DateRangeLogic(DateRangeLogic.ERange.YearToDate,
                () => mainWindowLogic.Household.RegularTransactions.Select(tr => tr.Date).Min());
            DateRangeLogic.DateRangeChanged += (s, e) => ComputeIncomeStatement();

            // Setup members
            foreach (var person in mainWindowLogic.Household.Person)
            {
                MembersSource.Add(new MemberItem(person));
            }
            var everybody = MemberItem.GetEverybodyItem();
            MembersSource.Add(everybody);
            selectedMember = everybody;

            // RFU
            PrintCommand = new CommandBase(() => mainWindowLogic.ErrorMessage("Not yet implemented"));

            // Give the list to the UI
            StatementsSource = (CollectionView)CollectionViewSource.GetDefaultView(statements);
            StatementsSource.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
            StatementsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            // Compute!
            ComputeIncomeStatement();
        }

        #endregion

        #region UI properties

        // Date range
        public DateRangeLogic DateRangeLogic { get; }

        // Person
        public List<MemberItem> MembersSource { get; } = new List<MemberItem>();
        private MemberItem selectedMember;
        public MemberItem SelectedMember { get => selectedMember; set { selectedMember = value; ComputeIncomeStatement(); } }

        // Print command
        public CommandBase PrintCommand { get; }

        // Income statements
        private readonly ObservableCollection<IncomeStatementItem> statements = new ObservableCollection<IncomeStatementItem>();
        public CollectionView StatementsSource { get; }

        #endregion

        #region Actions

        private void ComputeIncomeStatement()
        {
            var household = mainWindowLogic.Household;
            var startDate = DateRangeLogic.StartDate;
            var endDate = DateRangeLogic.EndDate;
            var member = selectedMember.Member;
            statements.Clear();

            //
            // Income
            // - Rent
            // - Div
            // - CG
            // - Other (sale of shares or assets)
            // - Change in investment value (?)
            // Total Revenues

            // Expenses
            // - Any and all
            // - cost basis of sold shares
            // Total Expenses

            // => Net income before taxes
            // less taxes
            // => Income from continuing operations
            // => Net income

            decimal totalRevenues = 0;
            decimal totalExpenses = 0;
            bool hasRevenues = false;
            bool hasExpenses = false;

            // Loop on all transaction for the selcted period and selected member
            foreach (var transactionRow in household.RegularTransactions
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate && (member == null || (!tr.AccountRow.IsPersonIDNull() && tr.AccountRow.PersonRow == member))))
            {
                if (transactionRow.AccountRow.Type == EAccountType.Investment)
                {

                }
                else
                {
                }
            }


            // Add necessary titles
            if (hasRevenues)
            {
                statements.Add(IncomeStatementItem.GetTitle("Revenue", "100Revenue"));
            }
            if (hasExpenses)
            {
                statements.Add(IncomeStatementItem.GetTitle("Expenses", "200Expenses"));
            }

            // Compute and add net income before taxes
            statements.Add(IncomeStatementItem.GetTitle("Net Worth", "600NetIncomeBeforeTaxes"));
            statements.Add(IncomeStatementItem.GetItem("Equity", "Equity", "601NetIncomeBeforeTaxes", totalRevenues - totalExpenses));
        }

        #endregion

        #region Support classes

        //
        // One balance sheet item
        //
        public class IncomeStatementItem
        {
            private IncomeStatementItem(string name, string tip, string group, decimal value, bool bold, bool showValue, bool indented)
                => (Name, Tip, Group, Value, Bold, ShowValue) = ((indented ? "\t" : "") + name, tip, group, value, bold, showValue);

            static public IncomeStatementItem GetTitle(string name, string group)
                => new IncomeStatementItem(name, null, group, 0, true, false, false);

            static public IncomeStatementItem GetItem(string name, string tip, string group, decimal value)
                => new IncomeStatementItem(name, tip, group, value, false, true, true);

            static public IncomeStatementItem GetTotal(string group, decimal value)
                => new IncomeStatementItem("Total:", null, group, value, true, true, false);

            static public IncomeStatementItem GetFiller(string group)
                => new IncomeStatementItem("", null, group, 0, false, false, false);

            public string Name { get; }
            public string Tip { get; }
            public string Group { get; }
            public decimal Value { get; }
            public bool ShowValue { get; }
            public bool Bold { get; }
        }

        //
        // One member
        //
        public class MemberItem
        {
            public MemberItem(Household.PersonRow member) =>
                Member = member;

            static public MemberItem GetEverybodyItem()
                => new MemberItem(null);

            public Household.PersonRow Member { get; }

            public override string ToString()
            {
                return Member == null ? "Everybody" : Member.Name;
            }
        }

        #endregion
    }
}
