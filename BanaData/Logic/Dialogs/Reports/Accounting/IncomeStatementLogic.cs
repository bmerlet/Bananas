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
            // v Rent
            // v Int
            // - Div
            // - CG
            // - Other (sale of shares or assets)
            // - Change in investment value (?)
            // Total Revenues

            // Expenses
            // v Regular expenses
            // - Tx to others
            // - cost basis of sold shares
            // Total Expenses

            // => Net income before taxes
            // less taxes
            // => Income from continuing operations
            // => Net income

            decimal totalRevenues = 0;
            decimal totalExpenses = 0;
            var revenues = new Dictionary<CategoryItem, decimal>();
            var expenses = new Dictionary<CategoryItem, decimal>();

            // Loop on all transaction for the selcted period and selected member
            foreach (var transactionRow in household.RegularTransactions
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate && (member == null || (!tr.AccountRow.IsPersonIDNull() && tr.AccountRow.PersonRow == member))))
            {
                foreach (var li in transactionRow.GetLineItemRows())
                {
                    // Special parsing of investment transactions
                    if (transactionRow.AccountRow.Type == EAccountType.Investment)
                    {
                        var investmentTransactionRow = transactionRow.GetInvestmentTransaction();
                        string explanation = null;
                        Dictionary<CategoryItem, decimal> dico = null;

                        switch (investmentTransactionRow.Type)
                        {
                            case EInvestmentTransactionType.CashIn:
                                dico = revenues;
                                explanation = "Added cash";
                                break;
                            case EInvestmentTransactionType.CashOut:
                                dico = expenses;
                                explanation = "Removed cash";
                                break;
                            case EInvestmentTransactionType.TransferCashIn:
                            case EInvestmentTransactionType.TransferCashOut:
                                break;
                            case EInvestmentTransactionType.InterestIncome:
                                dico = revenues;
                                explanation = "Interest Inc.";
                                break;

                            case EInvestmentTransactionType.SharesIn:
                            case EInvestmentTransactionType.SharesOut:
                                // Used for share exchange, revenue-neutral
                                break;

                            case EInvestmentTransactionType.Buy:
                            case EInvestmentTransactionType.BuyFromTransferredCash:
                                // Buying shares is revenue-neutral
                                break;

                            case EInvestmentTransactionType.Sell:
                            case EInvestmentTransactionType.SellAndTransferCash:
                                var cg = Portfolio.ComputeSaleCapitalGains(household, transactionRow.ID, false);
                                if (cg.LongTermGain != 0)
                                {
                                    var catItem = new CategoryItem("Long term capital gains from sale of " + investmentTransactionRow.SecurityRow.Symbol);
                                    if (!revenues.ContainsKey(catItem))
                                    {
                                        revenues[catItem] = 0;
                                    }
                                    revenues[catItem] += cg.LongTermGain;
                                }
                                if (cg.ShortTermGain != 0)
                                {
                                    var catItem = new CategoryItem("Short term capital gains from sale of " + investmentTransactionRow.SecurityRow.Symbol);
                                    if (!revenues.ContainsKey(catItem))
                                    {
                                        revenues[catItem] = 0;
                                    }
                                    revenues[catItem] += cg.ShortTermGain;
                                }
                                break;

                            case EInvestmentTransactionType.Dividends:
                            case EInvestmentTransactionType.TransferDividends:
                            case EInvestmentTransactionType.ReinvestDividends:
                                dico = revenues;
                                explanation = "Dividends";
                                break;

                            case EInvestmentTransactionType.ShortTermCapitalGains:
                            case EInvestmentTransactionType.TransferShortTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                                dico = revenues;
                                explanation = "Short-term capital gains";
                                break;

                            case EInvestmentTransactionType.LongTermCapitalGains:
                            case EInvestmentTransactionType.TransferLongTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                                dico = revenues;
                                explanation = "Long-term capital gains";
                                break;

                            case EInvestmentTransactionType.ReturnOnCapital:
                                dico = revenues;
                                explanation = "Return on capital";
                                break;
                        }

                        if (dico != null && explanation != null)
                        {
                            var catItem = new CategoryItem(explanation);
                            if (!dico.ContainsKey(catItem))
                            {
                                dico.Add(catItem, 0);
                            }
                            dico[catItem] += Math.Abs(transactionRow.GetAmount());
                        }
                    }

                    //
                    // Process category-based income and expense
                    //
                    if (li.GetLineItemCategoryRow() is Household.LineItemCategoryRow lic)
                    {
                        // Get to top-level category
                        var cat = lic.CategoryRow;
                        while (!cat.IsParentIDNull())
                        {
                            cat = household.Category.FindByID(cat.ParentID);
                        }
                        var catItem = new CategoryItem(cat.Name);

                        if (cat.IsIncome)
                        {
                            if (!revenues.ContainsKey(catItem))
                            {
                                revenues.Add(catItem, 0);
                            }
                            revenues[catItem] += li.Amount;
                        }
                        else
                        {
                            if (!expenses.ContainsKey(catItem))
                            {
                                expenses.Add(catItem, 0);
                            }
                            expenses[catItem] -= li.Amount;
                        }
                    }
                    //
                    // Process transfer-based income and expense
                    //
                    else if (li.GetLineItemTransferRow() is Household.LineItemTransferRow lit)
                    {
                        // Transfer to self does not count
                        if (member == null || (!lit.AccountRow.IsPersonIDNull() && lit.AccountRow.PersonRow == member))
                        {
                            continue;
                        }

                        string transferee = lit.AccountRow.IsPersonIDNull() ? "Unknown person" : lit.AccountRow.PersonRow.Name;
                        if (li.Amount < 0)
                        {
                            var catItem = new CategoryItem($"Transfer(s) to {transferee}");
                            if (!expenses.ContainsKey(catItem))
                            {
                                expenses.Add(catItem, 0);
                            }
                            expenses[catItem] -= li.Amount;
                        }
                        else
                        {
                            var catItem = new CategoryItem($"Transfer(s) from {transferee}");
                            if (!revenues.ContainsKey(catItem))
                            {
                                revenues.Add(catItem, 0);
                            }
                            revenues[catItem] += li.Amount;
                        }
                    }
                }
            }


            // Build income and expense
            if (revenues.Keys.Count != 0)
            {
                statements.Add(IncomeStatementItem.GetTitle("Revenue", "100Revenue"));
                foreach(var cat in revenues.Keys)
                {
                    decimal amount = revenues[cat];
                    statements.Add(IncomeStatementItem.GetItem(cat.Name, null, "101Revenues", amount));
                    totalRevenues += amount;
                }
                statements.Add(IncomeStatementItem.GetTotal("Total revenues", "190Revenue", totalRevenues));
            }
            if (expenses.Keys.Count != 0)
            {
                statements.Add(IncomeStatementItem.GetTitle("Expenses", "200Expenses"));
                foreach (var cat in expenses.Keys)
                {
                    decimal amount = expenses[cat];
                    statements.Add(IncomeStatementItem.GetItem(cat.Name, null, "201Expenses", amount));
                    totalExpenses += amount;
                }
                statements.Add(IncomeStatementItem.GetTotal("Total expenses", "290Expenses", totalExpenses));
            }

            // Compute and add net income before taxes
            statements.Add(IncomeStatementItem.GetTitle("Net income", "600NetIncomeBeforeTaxes"));
            statements.Add(IncomeStatementItem.GetTotal("Net taxable income", "601TaxableIncome", totalRevenues - totalExpenses));

            //
            // Now compute change in investment value
            //
            statements.Add(IncomeStatementItem.GetTitle("Other comprehensive income", "700OtherComprehensiveIncome"));

            // Compute portfolio at end date
            decimal changeInInvestmentValue = 0;
            foreach (var accountRow in household.Account
                .Where(acct => acct.Type == EAccountType.Investment && (member == null || (!acct.IsPersonIDNull() && acct.PersonRow == member))))
            {
                var portfolio = accountRow.GetPortfolio(endDate);
                decimal endDateValue = portfolio.GetValuation(endDate);
                decimal startDateValue = portfolio.GetValuation(startDate);
                changeInInvestmentValue += endDateValue - startDateValue;
            }
            statements.Add(IncomeStatementItem.GetItem("Change in investment value", null, "701ChangeInInvestmentValue", changeInInvestmentValue));

            // Compute and add net income before taxes
            statements.Add(IncomeStatementItem.GetTitle("Total income", "800TotalIncome"));
            statements.Add(IncomeStatementItem.GetTotal("Total income", "801TotalIncome", totalRevenues - totalExpenses + changeInInvestmentValue));
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

            static public IncomeStatementItem GetTotal(string total, string group, decimal value)
                => new IncomeStatementItem(total, null, group, value, true, true, true);

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

        //
        // Simplified category
        //
        class CategoryItem
        {
            public CategoryItem(string name) => Name = name;

            public readonly string Name;

            public override bool Equals(object obj)
            {
                return 
                    obj is CategoryItem o &&
                    Name.Equals(o.Name);
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
        }

        #endregion
    }
}
