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

namespace BanaData.Logic.Dialogs
{
    public class SearchResultLogic : LogicBase
    {
        private readonly MainWindowLogic mainWindowLogic;

        public SearchResultLogic(MainWindowLogic _mainWindowLogic, string _searchText)
        {
            (mainWindowLogic, searchText) = (_mainWindowLogic, _searchText);

            FoundItemsSource = (CollectionView)CollectionViewSource.GetDefaultView(foundItems);
            FoundItemsSource.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

            PerformSearch();
        }

        private string searchText;
        public string SearchText
        {
            get => searchText;
            set { searchText = value; PerformSearch(); }
        }

        private readonly WpfObservableRangeCollection<FoundItem> foundItems = new WpfObservableRangeCollection<FoundItem>();
        public CollectionView FoundItemsSource { get; }

        private void PerformSearch()
        {
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

            foreach (Household.TransactionRow transRow in household.Transaction.Rows)
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
                    if (liRows.Sum(l => l.Amount) == searchDecimal)
                    {
                        addIt = true;
                    }
                    else
                    {
                        foreach (var liRow in liRows)
                        {
                            if (liRow.Amount == searchDecimal)
                            {
                                addIt = true;
                            }
                            else if (!liRow.IsMemoNull() && liRow.Memo.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                addIt = true;
                            }
                            else if (!liRow.IsCategoryIDNull() && liRow.CategoryRow.FullName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                addIt = true;
                            }
                            else if (!liRow.IsAccountIDNull() && liRow.AccountRow.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
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
                    else if (!investRow.IsSecurityQuantityNull() && investRow.SecurityQuantity == searchDecimal)
                    {
                        addIt = true;
                    }
                    else if (!investRow.IsSecurityPriceNull() && investRow.SecurityPrice == searchDecimal)
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

        public class FoundItem
        {
            private readonly Household.TransactionRow transRow;

            public FoundItem(Household.TransactionRow _transRow)
            {
                transRow = _transRow;
            }

            public DateTime Date => transRow.Date;
            public string Payee => transRow.IsPayeeNull() ? "" : transRow.Payee;
            public string Memo => transRow.IsMemoNull() ? "" : transRow.Memo;
            public string Category => transRow.GetLineItemRows().Length > 1 ? "<Split>" :
                (!transRow.GetLineItemRows().Single().IsCategoryIDNull() ? transRow.GetLineItemRows().Single().CategoryRow.FullName :
                (!transRow.GetLineItemRows().Single().IsAccountIDNull() ? transRow.GetLineItemRows().Single().AccountRow.Name : ""));
            public decimal Amount => transRow.GetLineItemRows().Sum(li => li.Amount);
        }
    }
}
