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

        // Strings for 2nd level nodes

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

            // RFU
            PrintCommand = new CommandBase(() => mainWindowLogic.ErrorMessage("Not yet implemented"));

            // Give the list to the UI
            StatementsSource = (CollectionView)CollectionViewSource.GetDefaultView(statements);
            StatementsSource.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
            StatementsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

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

        // Print command
        public CommandBase PrintCommand { get; }

        // Income statements
        private readonly ObservableCollection<IncomeStatementItem> statements = new ObservableCollection<IncomeStatementItem>();
        public CollectionView StatementsSource { get; }

        // Income nodes
        private readonly ObservableCollection<IncomeStatementNode> nodes = new ObservableCollection<IncomeStatementNode>();
        public CollectionView NodesSource { get; }

        #endregion

        #region Actions

        private void ComputeIncomeStatement()
        {
            OldComputeIncomeStatement();

            var household = mainWindowLogic.Household;
            var startDate = DateRangeLogic.StartDate;
            var endDate = DateRangeLogic.EndDate;
            var member = selectedMember.Member;
            nodes.Clear();

            // Setup dictionaries to group the transactions
            var revenues = new Dictionary<CategoryItem, IncomeStatementNode>();
            var expenses = new Dictionary<CategoryItem, IncomeStatementNode>();

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
                        var investmentTransactionRow = transactionRow.GetInvestmentTransaction();
                        IncomeStatementNode parentNode = null;
                        IncomeStatementNode transactionNode = null;
                        decimal amount = Math.Abs(transactionRow.GetAmount());

                        switch (investmentTransactionRow.Type)
                        {
                            case EInvestmentTransactionType.CashIn:
                                parentNode = GetNodeByName(RevenueNode, "Added cash", null, "Added Cash");
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
                                parentNode = GetNodeByName(RevenueNode, INTERESTS_NAME, null, INTERESTS_GROUP);
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
                                    var LTCGNode = LongTermCapitalGainsNode;
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    var symbolNode = GetNodeByName(LTCGNode, symbol, $"Sale of {symbol}", symbol);
                                    var transNode = IncomeStatementNode.GetLeaf(
                                        investmentTransactionRow.GetDescription(),
                                        transTip,
                                        sortableDate,
                                        cg.LongTermGain);
                                    symbolNode.AddChild(transNode);
                                }
                                if (cg.ShortTermGain != 0)
                                {
                                    var STCGNode = ShortTermCapitalGainsNode;
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    var symbolNode = GetNodeByName(STCGNode, symbol, $"Sale of {symbol}", symbol);
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
                                    var dividendNode = GetNodeByName(RevenueNode, DIVIDENDS_NAME, null, DIVIDENDS_GROUP);
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    parentNode = GetNodeByName(dividendNode, symbol, $"Dividend from {symbol}", symbol);
                                    transactionNode = IncomeStatementNode.GetLeaf(investmentTransactionRow.GetDescription(), transTip, sortableDate, amount);
                                }
                                break;

                            case EInvestmentTransactionType.ShortTermCapitalGains:
                            case EInvestmentTransactionType.TransferShortTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                                {
                                    var STCGNode = ShortTermCapitalGainsNode;
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    parentNode = GetNodeByName(STCGNode, symbol, $"STCG from {symbol}", symbol);
                                    transactionNode = IncomeStatementNode.GetLeaf(investmentTransactionRow.GetDescription(), transTip, sortableDate, amount);
                                }
                                break;

                            case EInvestmentTransactionType.LongTermCapitalGains:
                            case EInvestmentTransactionType.TransferLongTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                                {
                                    var LTCGNode = LongTermCapitalGainsNode;
                                    var symbol = investmentTransactionRow.SecurityRow.Symbol;
                                    parentNode = GetNodeByName(LTCGNode, symbol, $"LTCG from {symbol}", symbol);
                                    transactionNode = IncomeStatementNode.GetLeaf(investmentTransactionRow.GetDescription(), transTip, sortableDate, amount);
                                }
                                break;

                            case EInvestmentTransactionType.ReturnOnCapital:
                                {
                                    parentNode = GetNodeByName(RevenueNode, "Return on capital", "Return on capital", "Return on capital");
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
                        // Get to top-level category
                        var cat = lic.CategoryRow;
                        while (!cat.IsParentIDNull())
                        {
                            cat = household.Category.FindByID(cat.ParentID);
                        }

                        // Sort into revenue/expense
                        var topNode = cat.IsIncome ? RevenueNode : ExpenseNode;

                        // Special case for interest, which also shows up in investment transactions
                        bool isInterestIncome = !cat.IsTaxInfoNull() && cat.TaxInfo == "287";
                        string catName = isInterestIncome ? INTERESTS_NAME : cat.Name;
                        string catGroup = isInterestIncome ? INTERESTS_GROUP : cat.Name;

                        // Create/get the node
                        var catNode = GetNodeByName(topNode, catName, null, catGroup);

                        decimal amount = cat.IsIncome ? li.Amount : -li.Amount;

                        string payee = transactionRow.IsPayeeNull() ? "" : transactionRow.Payee;
                        string comment = li.IsMemoNull() ? "" : li.Memo;
                        comment = (comment == "" && !transactionRow.IsMemoNull()) ? transactionRow.Memo : "";
                        string desc = (payee == "") ? comment : (comment == "" ? payee : $"{payee} ({comment})");
                        desc = $"{transactionRow.Date:MM/dd/yyyy}" + (desc == "" ? "" : $": {desc}");
                        
                        var transNode = IncomeStatementNode.GetLeaf(desc, transTip, sortableDate, amount);
                        catNode.AddChild(transNode);
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

        // Investment revenue nodes
        private IncomeStatementNode LongTermCapitalGainsNode => GetNodeByName(RevenueNode, LTCG_NAME, null, LTCG_GROUP);
        private IncomeStatementNode ShortTermCapitalGainsNode => GetNodeByName(RevenueNode, STCG_NAME, null, STCG_GROUP);

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

        private void OldComputeIncomeStatement()
        {
            var household = mainWindowLogic.Household;
            var startDate = DateRangeLogic.StartDate;
            var endDate = DateRangeLogic.EndDate;
            var member = selectedMember.Member;
            statements.Clear();

            decimal totalRevenues = 0;
            decimal totalExpenses = 0;
            var revenues = new Dictionary<CategoryItem, decimal>();
            var expenses = new Dictionary<CategoryItem, decimal>();

            // Loop on all transaction for the selected period and selected member
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

            // Compute portfolio at end date for all investment accounts belonging to member
            decimal changeInInvestmentValue = 0;
            foreach (var accountRow in household.Account
                .Where(acct => acct.Type == EAccountType.Investment && (member == null || (!acct.IsPersonIDNull() && acct.PersonRow == member))))
            {
                var portfolio = accountRow.GetPortfolio(endDate);

                // Compute portfolio value at end date
                decimal endDateValue = portfolio.GetValuation(endDate);

                // Compute base portfolio value: value of lots at start date for lots that are older than start date,
                // and value of lot when acquired for lots acquired after start date
                decimal startDateValue = 0;
                foreach(var lot in portfolio.Lots)
                {
                    decimal securityPrice = lot.Date < startDate ? lot.Security.GetMostRecentPrice(startDate) : securityPrice = lot.SecurityPrice;
                    decimal lotValue = securityPrice * lot.Quantity;
                    startDateValue += lotValue;
                }

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

    /// <summary>
    /// Node in an income statement
    /// </summary>
    public class IncomeStatementNode : LogicBase
    {
        #region Constructor

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

        #endregion

        #region UI properties

        public string Name { get; }
        public string Tip { get; }
        public string Group { get; }
        public decimal Value { get; private set; }
        public bool Bold { get; }

        // Children nodes
        public readonly ObservableCollection<IncomeStatementNode> Children = new ObservableCollection<IncomeStatementNode>();
        public CollectionView ChildrenSource { get; }

        #endregion

        #region Actions

        public void AddChild(IncomeStatementNode child)
        {
            Children.Add(child);
        }

        public void SetValue(decimal value)
        {
            Value = value;
        }

        #endregion
    }
}
