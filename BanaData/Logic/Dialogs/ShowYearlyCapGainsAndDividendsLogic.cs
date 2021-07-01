using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Logic.Main;

namespace BanaData.Logic.Dialogs
{
    public class ShowYearlyCapGainsAndDividendsLogic : LogicBase
    {
        private readonly MainWindowLogic mainWindowLogic;

        public ShowYearlyCapGainsAndDividendsLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));

            transactions.Add(new TransactionItem(DateTime.Today, "No interest", 0, 0, 0, 0));
            transactions.Add(new TransactionItem(DateTime.Today, "Interest", 0, 0, 0, 6.66M));
        }


        #region UI properties

        public DateTime SelectedYear { get; set; }
        public DateTime[] YearSource { get; }

        public bool? IsShowingInterest { get; set; }

        // Transactions
        private readonly ObservableCollection<TransactionItem> transactions = new ObservableCollection<TransactionItem>();
        public CollectionView Transactions { get; }

        // Totals
        public decimal TotalDividend { get; set; }
        public decimal TotalSTCG { get; set; }
        public decimal TotalLTCG { get; set; }
        public decimal TotalInterest { get; set; }

        // Widths
        public double DividendColumnWidth { get; set; } = 80;
        public double STCGColumnWidth { get; set; } = 80;
        public double LTCGColumnWidth { get; set; } = 80;
        public double InterestColumnWidth { get; set; } = 80;

        #endregion

        #region Supporting classes

        public class TransactionItem
        {
            public TransactionItem(DateTime date, string description, decimal dividend, decimal stcg, decimal ltcg, decimal interest) =>
                (Date, Description, Dividend, STCG, LTCG, Interest) = 
                    (date,
                    description, 
                    dividend == 0 ? "" : dividend.ToString("C2"), 
                    stcg == 0 ? "" : stcg.ToString("C2"),
                    ltcg == 0 ? "" : ltcg.ToString("C2"),
                    interest == 0 ? "" : interest.ToString("C2"));

            public DateTime Date { get; }
            public string Description { get; }
            public string Dividend { get; }
            public string STCG { get; }
            public string LTCG { get; }
            public string Interest { get; }
        }

        #endregion
    }
}
