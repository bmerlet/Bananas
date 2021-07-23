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
        private readonly TransactionReportItem transactionReportItem;
        private ETransactionReportFlag localFlags;

        #endregion

        #region Constructor

        public TransactionReportLogic(MainWindowLogic _mainWindowLogic, TransactionReportItem _transactionReportItem, bool fromMainMenu)
        {
            (mainWindowLogic, transactionReportItem, IsEditVisible) = (_mainWindowLogic, _transactionReportItem, fromMainMenu);

            Edit = new CommandBase(OnEdit);

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
                    lineItemRows = transactionRow.GetLineItemRows()
                        .Where(li => li.GetLineItemCategoryRow() is Household.LineItemCategoryRow lineItemCategoryRow &&
                               transactionReportItem.Categories.Contains(lineItemCategoryRow.CategoryRow))
                        .ToArray();
                    if (lineItemRows.Length == 0)
                    {
                        continue;
                    }
                }

                // This transaction passes all the checks!
                transactions.Add(new TransactionItem(transactionRow, lineItemRows, transactionReportItem));
            }

            // Compute grand total
            decimal grandTotal = transactions.Sum(ti => ti.Amount);

            // Build subtotals
            if (transactionReportItem.IsGroupingByAccount)
            {
                foreach(string accountName in transactions.Select(tr => tr.AccountName).Distinct().ToArray())
                {
                    decimal subtotal = transactions.Where(tr => tr.AccountName == accountName).Sum(tr => tr.Amount);
                    transactions.Add(new TransactionItem(accountName, subtotal, transactionReportItem));
                }
            }
            else if (transactionReportItem.IsGroupingByPayee)
            {
                foreach (string payee in transactions.Select(tr => tr.Payee).Distinct().ToArray())
                {
                    decimal subtotal = transactions.Where(tr => tr.Payee == payee).Sum(tr => tr.Amount);
                    transactions.Add(new TransactionItem(payee, subtotal, transactionReportItem));
                }
            }
            else if (transactionReportItem.IsGroupingByCategory)
            {
                foreach (string category in transactions.Select(tr => tr.Category).Distinct().ToArray())
                {
                    decimal subtotal = transactions.Where(tr => tr.Category == category).Sum(tr => tr.Amount);
                    transactions.Add(new TransactionItem(category, subtotal, transactionReportItem));
                }
            }

            // Build grand total
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
            var accountColumn = new ColumnItem(120, 145, "Account", "AccountName", null);
            var payeeColumn = new ColumnItem(200, 145, "Payee", "Payee", null);
            var categoryColumn = new ColumnItem(120, 145, "Category", "Category", null);

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
                    Columns.Add(new ColumnItem(80, 66, "Date", "Date", null, true));
                }
                if (transactionReportItem.IsShowingPayeeColumn && !transactionReportItem.IsGroupingByPayee)
                {
                    Columns.Add(payeeColumn);
                }
                if (transactionReportItem.IsShowingMemoColumn)
                {
                    Columns.Add(new ColumnItem(200, 145, "Memo", "Memo", null));
                }
                if (transactionReportItem.IsShowingCategoryColumn && !transactionReportItem.IsGroupingByCategory)
                {
                    Columns.Add(categoryColumn);
                }
            }

            Columns.Add(new ColumnItem(90, 60, "Amount", "Amount", "N2", true));
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
                result =
                    localFlags.HasFlag(ETransactionReportFlag.ShowTransactions) && !item.IsSubtotal ||
                    localFlags.HasFlag(ETransactionReportFlag.ShowSubtotals) && item.IsSubtotal;
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
            public ColumnItem(double width, double printWidth, string header, string propertyPath, string format, bool rightAligned = false) =>
                (Width, PrintWidth, Header, PropertyPath, Format, RightAligned) = (width, printWidth, header, propertyPath, format, rightAligned);

            public double Width { get; }
            public double PrintWidth { get; }
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
            private readonly DateTime date;

            // Build a transaction
            public TransactionItem(
                Household.TransactionRow transactionRow,
                Household.LineItemRow[] lineItemRows,
                TransactionReportItem _transactionReportItem)
            {
                (TransactionRow, transactionReportItem) = (transactionRow, _transactionReportItem);

                AccountName = transactionRow.AccountRow.Name;
                date = transactionRow.Date;
                Date = transactionRow.Date.ToString("MM/dd/yyyy");
                Payee = transactionRow.IsPayeeNull() ? "" : transactionRow.Payee;
                Memo = transactionRow.IsMemoNull() ? "" : transactionRow.Memo;
                Category =
                    lineItemRows.Length > 1 ? "<Split>" :
                    (lineItemRows[0].GetLineItemCategoryRow() != null ? lineItemRows[0].GetLineItemCategoryRow().CategoryRow.FullName :
                    (lineItemRows[0].GetLineItemTransferRow() != null ? $"[{lineItemRows[0].GetLineItemTransferRow().AccountRow.Name}]" : ""));
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

            public string AccountName { get; }
            public string Date { get; }
            public string Payee { get; }
            public string Memo { get; }
            public string Category { get; }
            public decimal Amount { get; }
            public bool IsSubtotal { get; }
            public bool IsGrandTotal { get; }
            public bool IsBold => IsSubtotal || IsGrandTotal;

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

                return date.CompareTo(other.date);
            }
        }

        #endregion
    }
}
