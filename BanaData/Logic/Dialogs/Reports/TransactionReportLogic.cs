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

namespace BanaData.Logic.Dialogs.Reports
{
    public class TransactionReportLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly TransactionReportItem transactionReportItem;

        #endregion

        #region Constructor

        public TransactionReportLogic(MainWindowLogic _mainWindowLogic, TransactionReportItem _transactionReportItem)
        {
            (mainWindowLogic, transactionReportItem) = (_mainWindowLogic, _transactionReportItem);

            // Look for the transactions selected by this report
            var household = mainWindowLogic.Household;
            foreach (Household.TransactionRow transactionRow in household.Transaction)
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
                var lineItemRows = transactionRow.GetLineItemRows();
                if (transactionReportItem.IsFilteringOnCategories)
                {
                    lineItemRows = transactionRow.GetLineItemRows().Where(li => !li.IsCategoryIDNull() && transactionReportItem.Categories.Contains(li.CategoryRow)).ToArray();
                    if (lineItemRows.Length == 0)
                    {
                        continue;
                    }
                }

                // This transaction passes all the checks!
                TransactionsSource.Add(new TransactionItem(transactionRow, lineItemRows, transactionReportItem));
            }

            // Build subtotals
            if (transactionReportItem.IsShowingSubtotals)
            {
                if (transactionReportItem.IsGroupingByAccount)
                {
                    foreach(string accountName in TransactionsSource.Select(tr => tr.AccountName).Distinct().ToArray())
                    {
                        decimal subtotal = TransactionsSource.Where(tr => tr.AccountName == accountName).Sum(tr => tr.Amount);
                        TransactionsSource.Add(new TransactionItem(accountName, subtotal, transactionReportItem));
                    }
                }
                else if (transactionReportItem.IsGroupingByPayee)
                {
                    foreach (string payee in TransactionsSource.Select(tr => tr.Payee).Distinct().ToArray())
                    {
                        decimal subtotal = TransactionsSource.Where(tr => tr.Payee == payee).Sum(tr => tr.Amount);
                        TransactionsSource.Add(new TransactionItem(payee, subtotal, transactionReportItem));
                    }
                }
                else if (transactionReportItem.IsGroupingByCategory)
                {
                    foreach (string category in TransactionsSource.Select(tr => tr.Category).Distinct().ToArray())
                    {
                        decimal subtotal = TransactionsSource.Where(tr => tr.Category == category).Sum(tr => tr.Amount);
                        TransactionsSource.Add(new TransactionItem(category, subtotal, transactionReportItem));
                    }
                }
            }

            // Sort according to grouping
            TransactionsSource.Sort();

            // Remove all the transactions if showing only subtotals
            if (!transactionReportItem.IsShowingTransactions)
            {
                TransactionsSource.RemoveAll(tr => !tr.IsSubtotal);
            }

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
            }
            Columns.Add(new ColumnItem(90, "Amount", "Amount", "N2", true));

            // ZZZZ Grand total

        }

        #endregion

        #region UI properties

        public string Title => transactionReportItem.Name;

        public List<TransactionItem> TransactionsSource { get; } = new List<TransactionItem>();

        public ObservableCollection<ColumnItem> Columns { get; } = new ObservableCollection<ColumnItem>();

        #endregion

        #region Actions

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

        #region Class describing a transaction or a subtotal

        public class TransactionItem : IComparable<TransactionItem>
        {
            private readonly TransactionReportItem transactionReportItem;

            // Build a transaction
            public TransactionItem(
                Household.TransactionRow transactionRow,
                Household.LineItemRow[] lineItemRows,
                TransactionReportItem _transactionReportItem)
            {
                transactionReportItem = _transactionReportItem;
                //(transactionRow, lineItemRows, transactionReportItem) =
                //(_transactionRow, _lineItemRows, _transactionReportItem);

                AccountName = transactionRow.AccountRow.Name;
                Date = transactionRow.Date.ToString("MM/dd/yyyy");
                Payee = transactionRow.IsPayeeNull() ? "" : transactionRow.Payee;
                Memo = transactionRow.IsMemoNull() ? "" : transactionRow.Memo;
                Category =
                    lineItemRows.Length > 1 ? "<Split>" :
                    (!lineItemRows[0].IsCategoryIDNull() ? lineItemRows[0].CategoryRow.FullName :
                    (!lineItemRows[0].IsAccountIDNull() ? $"[{lineItemRows[0].AccountRow.Name}]" : ""));
                Amount = lineItemRows.Sum(li => li.Amount);
            }

            // Build a subtotal
            public TransactionItem(string item, decimal subtotal, TransactionReportItem _transactionReportItem)
            {
                transactionReportItem = _transactionReportItem;
                item += " subtotal";
                if (transactionReportItem.IsGroupingByAccount)
                {
                    AccountName = item;
                }
                else if (transactionReportItem.IsGroupingByPayee)
                {
                    Payee = item;
                }
                else if (transactionReportItem.IsGroupingByCategory)
                {
                    Category = item;
                }

                Amount = subtotal;
                IsSubtotal = true;
            }

            public string AccountName { get; }
            public string Date { get; }
            public string Payee { get; }
            public string Memo { get; }
            public string Category { get; }
            public decimal Amount { get; }
            public bool IsSubtotal { get; }

            public int CompareTo(TransactionItem other)
            {
                if (transactionReportItem.IsGroupingByAccount && AccountName != other.AccountName)
                {
                    return AccountName.CompareTo(other.AccountName);
                }

                if (transactionReportItem.IsGroupingByPayee && Payee != other.Payee)
                {
                    return Payee.CompareTo(other.Payee);
                }

                if (transactionReportItem.IsGroupingByCategory && Category != other.Category)
                {
                    return Category.CompareTo(other.Category);
                }

                if (IsSubtotal && !other.IsSubtotal)
                {
                    return 1;
                }
                if (!IsSubtotal && other.IsSubtotal)
                {
                    return -1;
                }

                return Date.CompareTo(other.Date);
            }
        }

        #endregion
    }
}
