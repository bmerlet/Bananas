using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Reports
{
    public class TransactionReportLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;
        private readonly TransactionReportItem transactionReportItem;
        private ETransactionReportFlag localFlags;

        #endregion

        #region Constructor

        public TransactionReportLogic(MainWindowLogic _mainWindowLogic, Household _household, TransactionReportItem _transactionReportItem, bool fromMainMenu)
        {
            (mainWindowLogic, household, transactionReportItem, IsEditVisible) = (_mainWindowLogic, _household, _transactionReportItem, fromMainMenu);

            Edit = new CommandBase(OnEdit);

            // Look for the transactions selected by this report
            foreach (Household.TransactionRow transactionRow in household.RegularTransactions)
            {
                // Date check
                if (transactionRow.Date.CompareTo(transactionReportItem.StartDate) < 0 ||
                    transactionRow.Date.CompareTo(transactionReportItem.EndDate) > 0)
                {
                    continue;
                }

                // Account check
                if (transactionReportItem.IsFilteringOnAccounts &&
                    !transactionReportItem.Accounts.Contains(transactionRow.AccountRow))
                {
                    continue;
                }

                // Payee check
                if (transactionReportItem.IsFilteringOnPayees &&
                    (transactionRow.IsPayeeNull() || !transactionReportItem.Payees.Contains(transactionRow.Payee)))
                {
                    continue;
                }

                // Category check
                if (transactionReportItem.IsFilteringOnCategories)
                {
                    foreach (var lineItemRow in transactionRow.GetLineItemRows()
                        .Where(li => li.GetLineItemCategoryRow() is Household.LineItemCategoryRow lineItemCategoryRow &&
                               transactionReportItem.Categories.Contains(lineItemCategoryRow.CategoryRow)))
                    {
                        // Found matching transaction
                        transactions.Add(new TransactionItem(transactionRow, new Household.LineItemRow[] { lineItemRow }, transactionReportItem));
                    }
                }
                else
                {
                    // This transaction passes all the checks!
                    transactions.Add(new TransactionItem(transactionRow, transactionRow.GetLineItemRows(), transactionReportItem));
                }
            }

            // Compute pie slices if needed
            if (!transactionReportItem.IsPieChartNone)
            {
                ComputePieSizes();
            }

            // Compute grand total before adding the subtotals
            decimal grandTotal = transactions.Sum(ti => ti.Amount);

            // Build subtotals
            if (transactionReportItem.IsGroupingByAccount)
            {
                foreach(string accountName in transactions.Select(tr => tr.AccountName).Distinct().ToArray())
                {
                    var trans = transactions.Where(tr => tr.AccountName == accountName).ToArray();

                    // Build subtotal for this account
                    decimal subtotal = trans.Sum(tr => tr.Amount);
                    transactions.Add(new TransactionItem(accountName, subtotal, transactionReportItem));

                    // Build periodic subtotals
                    BuildPeriodicSubtotals(trans, accountName);
                }
            }
            else if (transactionReportItem.IsGroupingByPayee)
            {
                foreach (string payee in transactions.Select(tr => tr.Payee).Distinct().ToArray())
                {
                    var trans = transactions.Where(tr => tr.Payee == payee).ToArray();

                    // Build subtotal for this account
                    decimal subtotal = trans.Sum(tr => tr.Amount);
                    transactions.Add(new TransactionItem(payee, subtotal, transactionReportItem));

                    // Build periodic subtotals
                    BuildPeriodicSubtotals(trans, payee);
                }
            }
            else if (transactionReportItem.IsGroupingByCategory)
            {
                foreach (string category in transactions.Select(tr => tr.Category).Distinct().ToArray())
                {
                    var trans = transactions.Where(tr => tr.Category == category).ToArray();

                    // Build subtotal for this category
                    decimal subtotal = trans.Sum(tr => tr.Amount);
                    transactions.Add(new TransactionItem(category, subtotal, transactionReportItem));

                    // Build periodic subtotals
                    BuildPeriodicSubtotals(trans, category);
                }
            }
            else
            {
                BuildPeriodicSubtotals(transactions.ToArray(), "Overall");
            }

            // Add the grand total (after adding the subtotals so that it is not mistaken for a transaction)
            transactions.Add(new TransactionItem(grandTotal, transactionReportItem));

            // Sort according to grouping
            transactions.Sort();

            // Give its view to the UI
            TransactionsSource = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            TransactionsSource.Filter = TransactionsSourceFilter;

            // Setup what to show
            localFlags = transactionReportItem.Flags;
            SetShow(
                transactionReportItem.IsShowingTransactions && transactionReportItem.IsShowingSubtotals ? SHOW_SUBTOTAL :
                (transactionReportItem.IsShowingTransactions ? SHOW_TRANS : SHOW_SUBTOTALONLY));

            // Now build the columns
            var accountColumn = new ColumnItem(120, "Account", "AccountName", null);
            var payeeColumn = new ColumnItem(200, "Payee", "Payee", null);
            var categoryColumn = new ColumnItem(120, "Category", "Category", null);

            // First column is the "group by" column
            if (transactionReportItem.IsGroupingByAccount)
            {
                Columns.Add(accountColumn);
            }
            else if (transactionReportItem.IsGroupingByPayee)
            {
                Columns.Add(payeeColumn);
            }
            else if (transactionReportItem.IsGroupingByCategory)
            {
                Columns.Add(categoryColumn);
            }

            // Then all the other columns
            if (transactionReportItem.IsShowingTransactions)
            {
                if (transactionReportItem.IsShowingAccountColumn && !transactionReportItem.IsGroupingByAccount)
                {
                    Columns.Add(accountColumn);
                }
                if (transactionReportItem.IsShowingDateColumn)
                {
                    Columns.Add(new ColumnItem(80, "Date", "Date", null, true));
                }
                if (transactionReportItem.IsShowingPayeeColumn && !transactionReportItem.IsGroupingByPayee)
                {
                    Columns.Add(payeeColumn);
                }
                if (transactionReportItem.IsShowingMemoColumn)
                {
                    Columns.Add(new ColumnItem(200, "Memo", "Memo", null));
                }
                if (transactionReportItem.IsShowingCategoryColumn && !transactionReportItem.IsGroupingByCategory)
                {
                    Columns.Add(categoryColumn);
                }
                if (transactionReportItem.IsShowingStatusColumn)
                {
                    Columns.Add(new ColumnItem(30, "Sts", "Status", null));
                }
            }

            Columns.Add(new ColumnItem(90, "Amount", "Amount", "N2", true));
        }

        private void BuildPeriodicSubtotals(IEnumerable<TransactionItem> trans, string item)
        {
            if (!transactionReportItem.IsSubtotalFrequencyNone)
            {
                DateTime startDate = transactionReportItem.StartDate;
                while(startDate.CompareTo(transactionReportItem.EndDate) < 0)
                {
                    DateTime endDate;
                    if (transactionReportItem.IsSubtotalFrequencyWeekly)
                    {
                        endDate = startDate.AddDays(7);
                    }
                    else if (transactionReportItem.IsSubtotalFrequencyMonthly)
                    {
                        endDate = startDate.AddMonths(1);
                    }
                    else
                    {
                        endDate = startDate.AddYears(1);
                    }

                    decimal periodicSubtotal = trans.Where(t => t.RawDate.CompareTo(startDate) >= 0 && t.RawDate.CompareTo(endDate) < 0).Select(t => t.Amount).Sum();
                    transactions.Add(new TransactionItem(startDate, item, periodicSubtotal, transactionReportItem));

                    startDate = endDate;
                }

            }
        }

        private void ComputePieSizes()
        {
            var dic = new Dictionary<string, List<TransactionItem>>();

            foreach (var transaction in transactions)
            {
                string id;
                if (transactionReportItem.IsPieChartCategory)
                {
                    id = transaction.Category;
                }
                else if (transactionReportItem.IsPieChartVendor)
                {
                    id = transaction.Payee;
                }
                else // IsPieAccount
                {
                    id = transaction.AccountName;
                }

                if (!dic.ContainsKey(id))
                {
                    dic.Add(id, new List<TransactionItem>());
                }
                dic[id].Add(transaction);
            }

            PieSlices.Clear();
            foreach(var id in dic.Keys)
            {
                decimal amount = dic[id].Sum(tr => tr.Amount);
                string tip = $"{id}: {amount:N2}";
                PieSlices.Add(new PieSliceLogic(amount, tip));
            }

            PieSlices.Sort();
        }

        #endregion

        #region UI properties

        // Title
        public string Title => transactionReportItem.Name;

        // Edit button
        public CommandBase Edit { get; }
        public bool IsEditVisible { get; }

        // Show transactions and/or subtotals
        private const string SHOW_TRANS = "Transactions";
        private const string SHOW_SUBTOTAL = "Transactions and subtotals";
        private const string SHOW_SUBTOTALONLY = "Subtotals only";
        public string[] ShowSource { get; } = new string[] { SHOW_TRANS, SHOW_SUBTOTAL, SHOW_SUBTOTALONLY };
        private string show;
        public string Show { get => show; set => SetShow(value); }

        // Transactions
        private readonly List<TransactionItem> transactions = new List<TransactionItem>();
        public CollectionView TransactionsSource { get; }

        // Columns
        public ObservableCollection<ColumnItem> Columns { get; } = new ObservableCollection<ColumnItem>();

        // Pie chart
        public bool IsPieChartVisible => !transactionReportItem.IsPieChartNone;
        public List<PieSliceLogic> PieSlices { get; } = new List<PieSliceLogic>();

        #endregion

        #region Actions

        private void OnEdit()
        {
            // Close this view
            CloseView.Invoke(false);

            // Invoke edit of this transaction item through main menu
            mainWindowLogic.MainMenuLogic.EditTransactionReports.Execute(transactionReportItem);
        }

        private void SetShow(string value)
        {
            if (value != show)
            {
                show = value;
                localFlags &= ~(ETransactionReportFlag.ShowTransactions | ETransactionReportFlag.ShowSubtotals);

                switch (Show)
                {
                    case SHOW_TRANS:
                        localFlags |= ETransactionReportFlag.ShowTransactions;
                        break;
                    case SHOW_SUBTOTAL:
                        localFlags |= ETransactionReportFlag.ShowTransactions | ETransactionReportFlag.ShowSubtotals;
                        break;
                    case SHOW_SUBTOTALONLY:
                        localFlags |= ETransactionReportFlag.ShowSubtotals;
                        break;
                }

                TransactionsSource.Refresh();
            }
        }

        // Show transactions and/or subtotals depending on local settings
        private bool TransactionsSourceFilter(object obj)
        {
            bool result = false;

            if (obj is TransactionItem item)
            {
                bool subtotal = item.IsSubtotal || item.IsPeriodicSubtotal || item.IsGrandTotal;
                result =
                    localFlags.HasFlag(ETransactionReportFlag.ShowTransactions) && !subtotal ||
                    localFlags.HasFlag(ETransactionReportFlag.ShowSubtotals) && subtotal;
            }

            return result;
        }

        public void GoTo(TransactionItem item)
        {
            if (item.TransactionRow != null)
            {
                CloseView.Invoke(false);
                mainWindowLogic.GotoTransaction(item.TransactionRow.AccountID, item.TransactionRow.ID);
            }
        }



        // Not used, present so that we can use CloseView()
        protected override bool? Commit()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Class describing a column

        public class ColumnItem
        {
            public ColumnItem(double width, string header, string propertyPath, string format, bool rightAligned = false) =>
                (Width, Header, PropertyPath, Format, RightAligned) = (width, header, propertyPath, format, rightAligned);

            public double Width { get; }
            public string Header { get; }
            public string PropertyPath { get; }
            public string Format { get; }
            public bool RightAligned { get; }
        }

        #endregion

        #region Class describing a transaction or a subtotal or the grand total

        public class TransactionItem : IComparable<TransactionItem>
        {
            private readonly TransactionReportItem transactionReportItem;

            // Build a transaction
            public TransactionItem(
                Household.TransactionRow transactionRow,
                Household.LineItemRow[] lineItemRows,
                TransactionReportItem _transactionReportItem)
            {
                (TransactionRow, transactionReportItem) = (transactionRow, _transactionReportItem);

                RawAccountName = AccountName = transactionRow.AccountRow.Name;
                RawDate = transactionRow.Date;
                Date = transactionRow.Date.ToString("MM/dd/yyyy");
                RawPayee = Payee = transactionRow.IsPayeeNull() ? "" : transactionRow.Payee;
                Memo = transactionRow.IsMemoNull() ?
                    (lineItemRows.Length == 1 && !lineItemRows[0].IsMemoNull() ? lineItemRows[0].Memo : "")
                    : transactionRow.Memo;
                RawCategory = Category =
                    lineItemRows.Length > 1 ? "<Split>" :
                    (lineItemRows[0].GetLineItemCategoryRow() != null ? lineItemRows[0].GetLineItemCategoryRow().CategoryRow.FullName :
                    (lineItemRows[0].GetLineItemTransferRow() != null ? $"[{lineItemRows[0].GetLineItemTransferRow().AccountRow.Name}]" : ""));
                Amount = lineItemRows.Sum(li => li.Amount);
                Status = transactionRow.Status == ETransactionStatus.Pending ? "" : (transactionRow.Status == ETransactionStatus.Cleared ? "c" : "R");
            }

            // Build a subtotal
            public TransactionItem(string rawItem, decimal subtotal, TransactionReportItem _transactionReportItem)
            {
                transactionReportItem = _transactionReportItem;
                string item = rawItem + " subtotal";
                if (transactionReportItem.IsGroupingByAccount)
                {
                    RawAccountName = rawItem;
                    AccountName = item;
                }
                else if (transactionReportItem.IsGroupingByPayee)
                {
                    RawPayee = rawItem;
                    Payee = item;
                }
                else if (transactionReportItem.IsGroupingByCategory)
                {
                    RawCategory = rawItem;
                    Category = item;
                }

                Amount = subtotal;
                IsSubtotal = true;
            }

            // Build a periodic subtotal
            public TransactionItem(DateTime _date, string rawItem, decimal subtotal, TransactionReportItem _transactionReportItem)
            {
                transactionReportItem = _transactionReportItem;
                RawDate = _date;
                Date = RawDate.ToString("MM/dd/yyyy"); ;
                string item = rawItem + " subtotal for " + Date;
                if (transactionReportItem.IsGroupingByAccount)
                {
                    RawAccountName = rawItem;
                    AccountName = item;
                }
                else if (transactionReportItem.IsGroupingByPayee)
                {
                    RawPayee = rawItem;
                    Payee = item;
                }
                else if (transactionReportItem.IsGroupingByCategory)
                {
                    RawCategory = rawItem;
                    Category = item;
                }

                Amount = subtotal;
                IsPeriodicSubtotal = true;
            }

            // Build grand total
            public TransactionItem(decimal grandTotal, TransactionReportItem _transactionReportItem)
            {
                transactionReportItem = _transactionReportItem;

                string name = "Grand total";

                if (transactionReportItem.IsGroupingByAccount)
                {
                    AccountName = name;
                }
                else if (transactionReportItem.IsGroupingByPayee)
                {
                    Payee = name;
                }
                else if (transactionReportItem.IsGroupingByCategory)
                {
                    Category = name;
                }
                else
                {
                    Date = name;
                }
                Amount = grandTotal;
                IsGrandTotal = true;
            }

            public readonly Household.TransactionRow TransactionRow;
            public readonly DateTime RawDate;
            public readonly string RawAccountName;
            public readonly string RawCategory;
            public readonly string RawPayee;

            public string AccountName { get; }
            public string Date { get; }
            public string Payee { get; }
            public string Memo { get; }
            public string Category { get; }
            public decimal Amount { get; }
            public string Status { get; }
            public bool IsSubtotal { get; }
            public bool IsGrandTotal { get; }
            public bool IsPeriodicSubtotal { get; }
            public bool IsBold => IsSubtotal || IsPeriodicSubtotal || IsGrandTotal;

            public int CompareTo(TransactionItem other)
            {
                if (IsGrandTotal)
                {
                    return 1;
                }
                if (other.IsGrandTotal)
                {
                    return -1;
                }

                if (transactionReportItem.IsGroupingByAccount && RawAccountName != other.RawAccountName)
                {
                    return RawAccountName.CompareTo(other.RawAccountName);
                }

                if (transactionReportItem.IsGroupingByPayee && RawPayee != other.RawPayee)
                {
                    return RawPayee.CompareTo(other.RawPayee);
                }

                if (transactionReportItem.IsGroupingByCategory && RawCategory != other.RawCategory)
                {
                    return RawCategory.CompareTo(other.RawCategory);
                }

                if (IsSubtotal && !other.IsSubtotal)
                {
                    return 1;
                }

                if (!IsSubtotal && other.IsSubtotal)
                {
                    return -1;
                }

                return RawDate.CompareTo(other.RawDate) * (transactionReportItem.IsSortDescending ? -1 : 1);
            }
        }

        #endregion

        #region Class describing a slice of the pie chart

        public class PieSliceLogic : IComparable<PieSliceLogic>
        {
            public PieSliceLogic(decimal amount, string tip) => (Amount, Tip) = (amount, tip);

            public decimal Amount { get; } 
            public string Tip { get; }

            public int CompareTo(PieSliceLogic other) => Math.Abs(other.Amount).CompareTo(Math.Abs(Amount));
        }

        #endregion
    }
}
