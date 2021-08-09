using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Collections;
using BanaData.Database;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports
{
    public class SearchResultLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public SearchResultLogic(MainWindowLogic _mainWindowLogic, string searchText)
        {
            (mainWindowLogic, SearchText) = (_mainWindowLogic, searchText);

            FoundItemsSource = (CollectionView)CollectionViewSource.GetDefaultView(foundItems);
            FoundItemsSource.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

            Search = new CommandBase(OnSearch);

            OnSearch(SearchText);
        }

        #endregion

        #region UI properties

        public string SearchText { get; set; }

        public CommandBase Search { get; }

        private readonly WpfObservableRangeCollection<FoundItem> foundItems = new WpfObservableRangeCollection<FoundItem>();
        public CollectionView FoundItemsSource { get; }

        #endregion

        #region Actions

        public void GoTo(FoundItem item)
        {
            mainWindowLogic.GotoTransaction(item.TransRow.AccountID, item.TransRow.ID);
        }

        private void OnSearch(object arg)
        {
            string searchText = arg as string;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            var household = mainWindowLogic.Household;
            var tmpList = new List<FoundItem>();

            if (!decimal.TryParse(searchText, out decimal searchDecimal))
            {
                searchDecimal = decimal.MinValue;
            }

            if (!DateTime.TryParse(SearchText, out DateTime searchDate))
            {
                searchDate = DateTime.MinValue;
            }

            foreach (Household.TransactionRow transRow in household.RegularTransactions)
            {
                bool addIt = false;

                if (transRow.Date == searchDate)
                {
                    addIt = true;
                }
                else if (!transRow.IsPayeeNull() && transRow.Payee.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    addIt = true;
                }
                else if (!transRow.IsMemoNull() && transRow.Memo.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    addIt = true;
                }
                else
                {
                    var liRows = transRow.GetLineItemRows();
                    if (Math.Abs(liRows.Sum(l => l.Amount)) == Math.Abs(searchDecimal))
                    {
                        addIt = true;
                    }
                    else
                    {
                        foreach (var liRow in liRows)
                        {
                            if (Math.Abs(liRow.Amount) == Math.Abs(searchDecimal))
                            {
                                addIt = true;
                            }
                            else if (!liRow.IsMemoNull() && liRow.Memo.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                addIt = true;
                            }
                            else if (liRow.GetLineItemCategoryRow() is Household.LineItemCategoryRow lineItemCategoryRow &&
                                     lineItemCategoryRow.CategoryRow.FullName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                addIt = true;
                            }
                            else if (liRow.GetLineItemTransferRow() is Household.LineItemTransferRow lineItemTransferRow &&
                                    lineItemTransferRow.AccountRow.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                addIt = true;
                            }
                        }
                    }
                }

                if (!addIt && transRow.AccountRow.Type == EAccountType.Bank)
                {
                    var bankRow = transRow.GetBankingTransaction();
                    if (!bankRow.IsCheckNumberNull() && bankRow.CheckNumber == searchDecimal)
                    {
                        addIt = true;
                    }
                }

                if (!addIt && transRow.AccountRow.Type == EAccountType.Investment)
                {
                    var investRow = transRow.GetInvestmentTransaction();
                    if (!investRow.IsSecurityIDNull() && investRow.SecurityRow.Symbol.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        addIt = true;
                    }
                    else if (!investRow.IsSecurityQuantityNull() && investRow.SecurityQuantity == Math.Abs(searchDecimal))
                    {
                        addIt = true;
                    }
                    else if (!investRow.IsSecurityPriceNull() && investRow.SecurityPrice == Math.Abs(searchDecimal))
                    {
                        addIt = true;
                    }
                }

                if (addIt)
                {
                    tmpList.Add(new FoundItem(transRow));
                }
            }

            // Performance: replace all at once
            foundItems.ReplaceRange(tmpList);
        }

        #endregion

        #region FoundItem class

        public class FoundItem
        {
            public FoundItem(Household.TransactionRow transRow)
            {
                TransRow = transRow;
            }

            public readonly Household.TransactionRow TransRow;

            public string Account => TransRow.AccountRow.Name;
            public DateTime Date => TransRow.Date;
            public string Payee => TransRow.IsPayeeNull() ? "" : TransRow.Payee;
            public string Memo => TransRow.IsMemoNull() ? "" : TransRow.Memo;
            public string Category => TransRow.GetLineItemRows().Length > 1 ? "<Split>" :
                (TransRow.GetLineItemRows().Single().GetLineItemCategoryRow() != null ? TransRow.GetLineItemRows().Single().GetLineItemCategoryRow().CategoryRow.FullName :
                (TransRow.GetLineItemRows().Single().GetLineItemTransferRow() != null ? TransRow.GetLineItemRows().Single().GetLineItemTransferRow().AccountRow.Name : ""));
            public decimal Amount => TransRow.GetLineItemRows().Sum(li => li.Amount);
        }

        #endregion
    }
}
