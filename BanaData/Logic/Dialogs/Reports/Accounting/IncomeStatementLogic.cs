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

        // Strings for top nodes
        private const string REVENUE_NAME = "Revenue";
        private const string REVENUE_GROUP = "100Revenue";
        private const string EXPENSE_NAME = "Expenses";
        private const string EXPENSE_GROUP = "200Expenses";
        private const string INCOME_NAME = "Income";
        private const string INCOME_GROUP = "500Income";
        private const string INVESTMENTVALUE_NAME = "Change in investment value";
        private const string INVESTMENTVALUE_GROUP = "800CahngeInInvestmentValue";

        // Strings for 2nd level nodes
        private const string TAXABLE_NAME = "Taxable";
        private const string TAXABLE_GROUP = "100Taxable";
        private const string NONTAXABLE_NAME = "Non-taxable";
        private const string NONTAXABLE_GROUP = "200NonTaxable";

        // Strings for revenue categories
        private const string INTERESTS_NAME = "Interest";
        private const string INTERESTS_GROUP = "101Interest";
        private const string DIVIDENDS_NAME = "Dividends";
        private const string DIVIDENDS_GROUP = "102Dividends";
        private const string LTCG_NAME = "Long-term capital gains";
        private const string LTCG_GROUP = "103Long-term capital gains";
        private const string STCG_NAME = "Short-term capital gains";
        private const string STCG_GROUP = "104Short-term capital gains";

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

            // Give the nodes to the UI
            NodesSource = (CollectionView)CollectionViewSource.GetDefaultView(nodes);
            NodesSource.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
            NodesSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

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

        // Income nodes
        private readonly ObservableCollection<IncomeStatementNode> nodes = new ObservableCollection<IncomeStatementNode>();
        public CollectionView NodesSource { get; }

        #endregion

        #region Actions

        private void ComputeIncomeStatement()
        {
            var household = mainWindowLogic.Household;
            var startDate = DateRangeLogic.StartDate;
            var endDate = DateRangeLogic.EndDate;
            var member = selectedMember.Member;
            nodes.Clear();

            // Loop on all transaction for the selected period and selected member
            foreach (var transactionRow in household.RegularTransactions
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate && (member == null || (!tr.AccountRow.IsPersonIDNull() && tr.AccountRow.PersonRow == member))))
            {
                string sortableDate = $"{transactionRow.Date:yyyy/MM/dd}";
                string transTip = $"{transactionRow.AccountRow.Name} on {transactionRow.Date:MM/dd/yyyy}";

                foreach (var li in transactionRow.GetLineItemRows())
                {
                    // Special parsing of investment transactions
                    if (transactionRow.AccountRow.Type == EAccountType.Investment)
                    {
                        bool taxable =
                            transactionRow.AccountRow.Kind != EInvestmentKind.TraditionalIRA &&
                            transactionRow.AccountRow.Kind != EInvestmentKind._401k;
                        var taxNode = taxable ? TaxableNode : NonTaxableNode;
                        var investmentTransactionRow = transactionRow.GetInvestmentTransaction();
                        IncomeStatementNode parentNode = null;
                        IncomeStatementNode transactionNode = null;
                        decimal amount = Math.Abs(transactionRow.GetAmount());

                        switch (investmentTransactionRow.Type)
                        {
                            case EInvestmentTransactionType.CashIn:
                                parentNode = GetNodeByName(taxNode, "Added cash", null, "Added Cash");
                                transactionNode = IncomeStatementNode.GetLeaf("Added cash", transTip, sortableDate, amount);
                                break;

                            case EInvestmentTransactionType.CashOut:
                                parentNode = GetNodeByName(ExpenseNode, "Removed cash", null, "Removed Cash");
                                transactionNode = IncomeStatementNode.GetLeaf("Removed cash", transTip, sortableDate, amount);
                                break;

                            case EInvestmentTransactionType.TransferCashIn:
                            case EInvestmentTransactionType.TransferCashOut:
                                // Transfers are processed below
                                break;

                            case EInvestmentTransactionType.InterestIncome:
                                parentNode = GetNodeByName(taxNode, INTERESTS_NAME, null, INTERESTS_GROUP);
                                transactionNode = IncomeStatementNode.GetLeaf("Interest", transTip, sortableDate, amount);
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
                                    var LTCGNode = GetLongTermCapitalGainsNode(taxNode);
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    var symbolNode = GetNodeByName(LTCGNode, symbol, $"Sale of {investmentTransactionRow.SecurityRow.Name}", symbol);
                                    var transNode = IncomeStatementNode.GetLeaf(
                                        investmentTransactionRow.GetDescription(),
                                        transTip,
                                        sortableDate,
                                        cg.LongTermGain);
                                    symbolNode.AddChild(transNode);
                                }
                                if (cg.ShortTermGain != 0)
                                {
                                    var STCGNode = GetShortTermCapitalGainsNode(taxNode);
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    var symbolNode = GetNodeByName(STCGNode, symbol, $"Sale of {investmentTransactionRow.SecurityRow.Name}", symbol);
                                    var transNode = IncomeStatementNode.GetLeaf(
                                        investmentTransactionRow.GetDescription(),
                                        transTip ,
                                        sortableDate,
                                        cg.ShortTermGain);
                                    symbolNode.AddChild(transNode);
                                }
                                break;

                            case EInvestmentTransactionType.Dividends:
                            case EInvestmentTransactionType.TransferDividends:
                            case EInvestmentTransactionType.ReinvestDividends:
                                {
                                    var dividendNode = GetNodeByName(taxNode, DIVIDENDS_NAME, null, DIVIDENDS_GROUP);
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    parentNode = GetNodeByName(dividendNode, symbol, investmentTransactionRow.SecurityRow.Name, symbol);
                                    transactionNode = IncomeStatementNode.GetLeaf(investmentTransactionRow.GetDescription(), transTip, sortableDate, amount);
                                }
                                break;

                            case EInvestmentTransactionType.ShortTermCapitalGains:
                            case EInvestmentTransactionType.TransferShortTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                                {
                                    var STCGNode = GetShortTermCapitalGainsNode(taxNode);
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    parentNode = GetNodeByName(STCGNode, symbol, $"STCG from {investmentTransactionRow.SecurityRow.Name}", symbol);
                                    transactionNode = IncomeStatementNode.GetLeaf(investmentTransactionRow.GetDescription(), transTip, sortableDate, amount);
                                }
                                break;

                            case EInvestmentTransactionType.LongTermCapitalGains:
                            case EInvestmentTransactionType.TransferLongTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                                {
                                    var LTCGNode = GetLongTermCapitalGainsNode(taxNode);
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    parentNode = GetNodeByName(LTCGNode, symbol, $"LTCG from {investmentTransactionRow.SecurityRow.Name}", symbol);
                                    transactionNode = IncomeStatementNode.GetLeaf(investmentTransactionRow.GetDescription(), transTip, sortableDate, amount);
                                }
                                break;

                            case EInvestmentTransactionType.ReturnOnCapital:
                                {
                                    parentNode = GetNodeByName(taxNode, "Return on capital", "Return on capital", "Return on capital");
                                    transactionNode = IncomeStatementNode.GetLeaf("Return on capital", transTip, sortableDate, amount);
                                }
                                break;
                        }

                        if (parentNode != null && transactionNode != null)
                        {
                            parentNode.AddChild(transactionNode);
                        }
                    }

                    //
                    // Process category-based income and expense
                    //
                    if (li.GetLineItemCategoryRow() is Household.LineItemCategoryRow lic)
                    {
                        // Get category node
                        var categoryNode = GetCategoryNode(lic.CategoryRow);

                        // Create the transaction node
                        decimal amount = lic.CategoryRow.IsIncome ? li.Amount : -li.Amount;
                        string payee = transactionRow.IsPayeeNull() ? "" : transactionRow.Payee;
                        string comment = li.IsMemoNull() ? "" : li.Memo;
                        comment = (comment == "" && !transactionRow.IsMemoNull()) ? transactionRow.Memo : "";
                        string desc = (payee == "") ? comment : (comment == "" ? payee : $"{payee} ({comment})");
                        desc = $"{transactionRow.Date:MM/dd/yyyy}" + (desc == "" ? "" : $": {desc}");
                        var transNode = IncomeStatementNode.GetLeaf(desc, transTip, sortableDate, amount);

                        // Create/get the node
                        categoryNode.AddChild(transNode);
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
                        string date = $"{transactionRow.Date:MM/dd/yyyy}";
                        if (li.Amount < 0)
                        {
                            string title = $"Transfer(s) to {transferee}";
                            var catNode = GetNodeByName(ExpenseNode, title, title, title);
                            var transNode = IncomeStatementNode.GetLeaf(date, transTip, sortableDate, -li.Amount);
                            catNode.AddChild(transNode);
                        }
                        else
                        {
                            string title = $"Transfer(s) from {transferee}";
                            var catNode = GetNodeByName(RevenueNode, title, title, title);
                            var transNode = IncomeStatementNode.GetLeaf(date, transTip, sortableDate, li.Amount);
                            catNode.AddChild(transNode);
                        }
                    }
                }
            }

            // Compute totals
            foreach (var topNode in nodes)
            {
                ComputeTotals(topNode);
            }

            // Add income node
            var incomeNode = IncomeStatementNode.GetTitle(INCOME_NAME, INCOME_GROUP);
            incomeNode.SetValue(RevenueNode.Value - ExpenseNode.Value);
            nodes.Add(incomeNode);

            // Add Change in investment value node
            ComputeChangeInInvestmentValue();
        }

        //
        // Top nodes
        //
        private IncomeStatementNode RevenueNode => GetTopNodeByName(REVENUE_NAME, REVENUE_GROUP);
        private IncomeStatementNode ExpenseNode => GetTopNodeByName(EXPENSE_NAME, EXPENSE_GROUP);

        private IncomeStatementNode GetTopNodeByName(string name, string group)
        {
            var node = nodes.FirstOrDefault(n => n.Name == name);
            if (node == null)
            {
                node = IncomeStatementNode.GetTitle(name, group);
                nodes.Add(node);
            }

            return node;
        }

        //
        // Taxable/non-taxable revenue nodes
        //
        private IncomeStatementNode TaxableNode => GetNodeByName(RevenueNode, TAXABLE_NAME, null, TAXABLE_GROUP);
        private IncomeStatementNode NonTaxableNode => GetNodeByName(RevenueNode, NONTAXABLE_NAME, null, NONTAXABLE_GROUP);

        // Investment revenue nodes
        private IncomeStatementNode GetLongTermCapitalGainsNode(IncomeStatementNode taxNode) =>
            GetNodeByName(taxNode, LTCG_NAME, null, LTCG_GROUP);
        private IncomeStatementNode GetShortTermCapitalGainsNode(IncomeStatementNode taxNode) => 
            GetNodeByName(taxNode, STCG_NAME, null, STCG_GROUP);

        private IncomeStatementNode GetNodeByName(IncomeStatementNode parent, string name, string tip, string group)
        {
            var node = parent.Children.FirstOrDefault(n => n.Name == name);
            if (node == null)
            {
                node = IncomeStatementNode.GetItem(name, tip, group);
                parent.AddChild(node);
            }

            return node;
        }

        // Category node
        private IncomeStatementNode GetCategoryNode(Household.CategoryRow category)
        {
            IncomeStatementNode parentNode;
            if (category.IsParentIDNull())
            {
                if (category.IsIncome)
                {
                    parentNode = category.IsTaxInfoNull() ? NonTaxableNode : TaxableNode;
                }
                else
                {
                    parentNode = ExpenseNode;
                }
            }
            else
            {
                 parentNode = GetCategoryNode(category.GetParentCategoryRow());
            }

            // Special case for interest, which also shows up in investment transactions
            bool isInterestIncome = !category.IsTaxInfoNull() && category.TaxInfo == "287";
            string categoryName = isInterestIncome ? INTERESTS_NAME : category.Name;
            string categoryGroup = isInterestIncome ? INTERESTS_GROUP : category.Name;

            return GetNodeByName(parentNode, categoryName, null, categoryGroup);
        }

        // Recursively populate value of nodes with total
        private void ComputeTotals(IncomeStatementNode node)
        {
            if (node.Children.Count > 0)
            {
                decimal sum = 0;
                foreach (var child in node.Children)
                {
                    ComputeTotals(child);
                    sum += child.Value;
                }
                node.SetValue(sum);
            }
        }

        //
        // Compute change in investment value
        //
        private void ComputeChangeInInvestmentValue()
        {
            var household = mainWindowLogic.Household;
            var startDate = DateRangeLogic.StartDate;
            var endDate = DateRangeLogic.EndDate;
            var member = selectedMember.Member;

            var investmentValueNode = IncomeStatementNode.GetTitle(INVESTMENTVALUE_NAME, INVESTMENTVALUE_GROUP);
            nodes.Add(investmentValueNode);

            // Compute portfolio at end date for all investment accounts belonging to member
            foreach (var accountRow in household.Account
                .Where(acct => acct.Type == EAccountType.Investment && (member == null || (!acct.IsPersonIDNull() && acct.PersonRow == member))))
            {
                var portfolio = accountRow.GetPortfolio(endDate);

                // Compute portfolio value at end date
                decimal endDateValue = portfolio.GetValuation(endDate);

                // Compute base portfolio value: value of lots at start date for lots that are older than start date,
                // and value of lot when acquired for lots acquired after start date
                decimal startDateValue = 0;
                foreach (var lot in portfolio.Lots)
                {
                    decimal securityPrice = lot.Date < startDate ? lot.Security.GetMostRecentPrice(startDate) : securityPrice = lot.SecurityPrice;
                    decimal lotValue = securityPrice * lot.Quantity;
                    startDateValue += lotValue;
                }

                if (endDateValue != startDateValue)
                {
                    var valueChangeNode = IncomeStatementNode.GetLeaf(accountRow.Name, $"Change in value for {accountRow.Name}", accountRow.Name, endDateValue - startDateValue);
                    investmentValueNode.AddChild(valueChangeNode);
                }
            }

            ComputeTotals(investmentValueNode);
        }

        #endregion

        #region Support classes

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
        // Node in an income statement
        //
        public class IncomeStatementNode : LogicBase
        {
            private IncomeStatementNode(string name, string tip, string group, decimal value, bool bold)
            {
                (Name, Tip, Group, Value, Bold) = (name, tip, group, value, bold);

                // Give the list to the UI
                ChildrenSource = (CollectionView)CollectionViewSource.GetDefaultView(Children);
                ChildrenSource.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
                ChildrenSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }

            static public IncomeStatementNode GetTitle(string name, string group)
                => new IncomeStatementNode(name, null, group, 0, true);

            static public IncomeStatementNode GetItem(string name, string tip, string group)
                => new IncomeStatementNode(name, tip, group, 0, false);

            static public IncomeStatementNode GetLeaf(string name, string tip, string group, decimal amount)
                => new IncomeStatementNode(name, tip, group, amount, false);

            // UI properties
            public string Name { get; }
            public string Tip { get; }
            public string Group { get; }
            public decimal Value { get; private set; }
            public bool Bold { get; }

            // Children nodes
            public readonly ObservableCollection<IncomeStatementNode> Children = new ObservableCollection<IncomeStatementNode>();
            public CollectionView ChildrenSource { get; }

            // Actions
            public void AddChild(IncomeStatementNode child)
            {
                Children.Add(child);
            }

            public void SetValue(decimal value)
            {
                Value = value;
            }
        }
        #endregion
    }
}
