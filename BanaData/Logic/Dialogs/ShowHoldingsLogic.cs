using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using BanaData.Database;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs
{
    public class ShowHoldingsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household.AccountsRow accountRow;

        #endregion

        #region Constructor

        public ShowHoldingsLogic(MainWindowLogic _mainWindowLogic, int accountID)
        {
            mainWindowLogic = _mainWindowLogic;
            var household = mainWindowLogic.Household;
            accountRow = household.Accounts.FindByID(accountID);

            // Init data
            SetDate(DateTime.Today);

            SecurityItemSource = (CollectionView)CollectionViewSource.GetDefaultView(securityItems);
            SecurityItemSource.SortDescriptions.Add(new SortDescription("Symbol", ListSortDirection.Ascending));
        }

        #endregion

        #region UI properties

        private DateTime date;
        public DateTime Date
        {
            get => date;
            set => SetDate(value);
        }

        private readonly ObservableCollection<SecurityItem> securityItems = new ObservableCollection<SecurityItem>();
        public CollectionView SecurityItemSource { get; }

        public decimal TotalValue { get; private set; }

        #endregion

        #region Actions

        private void SetDate(DateTime value)
        {
            if (date != value)
            {
                date = value;
                securityItems.Clear();
                TotalValue = 0;

                // Get protfolio at target date
                var portfolio = accountRow.GetPortfolio(date);

                // Get a security list
                foreach (var securityID in portfolio.GetSecurities())
                {
                    var securityRow = mainWindowLogic.Household.Securities.FindByID(securityID);
                    string symbol = securityRow.IsSymbolNull() ? "??" : securityRow.Symbol;

                    // Get lots for this securoty
                    var lots = portfolio.Lots.ToList().FindAll(l => l.Security.ID == securityID);

                    // Get price based on date
                    decimal price = securityRow.GetMostRecentPrice(date);

                    // Update total value
                    TotalValue += price * lots.Sum(l => l.Quantity);

                    // Build list of lots
                    securityItems.Add(new SecurityItem(symbol, lots, price));
                }

                OnPropertyChanged(() => TotalValue);
            }
        }

        #endregion

        #region Supporting classes

        // One security
        public class SecurityItem
        {
            public SecurityItem(string symbol, List<Lot> lots, decimal price)
            {
                Symbol = symbol;
                Quantity = lots.Sum(l => l.Quantity);
                Price = price;

                foreach(var lot in lots)
                {
                    lotItems.Add(new LotItem(lot.Date, lot.Quantity, lot.SecurityPrice));
                }

                LotItemSource = (CollectionView)CollectionViewSource.GetDefaultView(lotItems);
                LotItemSource.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            }

            public string Symbol { get; }
            public decimal Quantity { get; }
            public decimal Price { get; }
            public decimal Value => Quantity * Price;

            private readonly ObservableCollection<LotItem> lotItems = new ObservableCollection<LotItem>();
            public CollectionView LotItemSource { get; }

            // One lot
            public class LotItem
            {
                public LotItem(DateTime date, decimal quantity, decimal price) =>
                    (Date, Quantity, Price) = (date, quantity, price);

                public DateTime Date { get; }
                public decimal Quantity { get; }
                public decimal Price { get; }
                public decimal Value => Quantity * Price;
            }
        }

        #endregion
    }
}
