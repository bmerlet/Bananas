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
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowWealthOverTimeLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Dictionary<int, List<SecurityPrice>> securityPrices = new Dictionary<int, List<SecurityPrice>>();
        private List<Household.TransactionRow> transactions;

        #endregion

        #region Constructor

        public ShowWealthOverTimeLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Init account list
            AccountListLogic = new AccountListLogic(mainWindowLogic);
            foreach(AccountListLogic.AccountPickerItem accountItem in AccountListLogic.Accounts)
            {
                accountItem.IsSelected = true;
            }

            AccountListLogic.SelectionChanged += (s, e) =>
            {
                UpdateTransactions();
                UpdateGraph();
            };

            // Setup dictionary of sorted security prices. The key is the security ID
            foreach(var securityRow in mainWindowLogic.Household.Security)
            {
                var prices = new List<SecurityPrice>();
                foreach(var price in securityRow.GetSecurityPriceRows())
                {
                    prices.Add(new SecurityPrice(price.Date, price.Value));
                }
                prices.Sort();
                securityPrices[securityRow.ID] = prices;
            }

            // Get time-sorted transactions for selected accounts
            UpdateTransactions();

            // Setup date range
            DateRangeLogic = new DateRangeLogic(DateRangeLogic.ERange.LastTenYears, () =>
                // Return start date of all available transactions
                mainWindowLogic.Household.RegularTransactions
                    .Where(tr => AccountListLogic.IsAccountSelected(tr.AccountRow))
                    .Min(tr => tr.Date)
            );

            // Get notified of date changes
            DateRangeLogic.DateRangeChanged += (s, e) => UpdateGraph();

            // Build graph
            UpdateGraph();
        }

        #endregion

        #region UI properties

        //
        // List of accounts with checkboxes
        //
        public AccountListLogic AccountListLogic { get; }

        //
        // Date Range
        //
        public DateRangeLogic DateRangeLogic { get; }

        //
        // Frequency
        //
        private const string FREQUENCY_DAY = "day";
        private const string FREQUENCY_WEEK = "week";
        private const string FREQUENCY_MONTH = "month";
        private const string FREQUENCY_QUARTER = "quarter";
        private const string FREQUENCY_YEAR = "year";
        public string[] FrequencySource { get; } = new string[]
        {
            FREQUENCY_DAY,
            FREQUENCY_WEEK,
            FREQUENCY_MONTH,
            FREQUENCY_QUARTER,
            FREQUENCY_YEAR
        };

        private string frequency = FREQUENCY_YEAR;
        public string Frequency { get => frequency; set { frequency = value; UpdateGraph(); } }

        // 0-based Y axis
        private bool zeroYAxis;
        public bool? ZeroYAxis { get => zeroYAxis; set { zeroYAxis = value == true; RedrawGraph(); } }

        // Show payout
        private bool showPayout;
        public bool? ShowPayout {  get => showPayout; set { showPayout = value == true; UpdateGraph(); } }

        //
        // Graph points
        //
        public List<DateValue> DateValues { get; } = new List<DateValue>();
        public List<DateValue> PayoutDateValues { get; } = new List<DateValue>();

        // Graph generator
        public bool UpdateGraphSignal { get; private set; }

        #endregion

        #region Actions

        private void UpdateTransactions()
        {
            // Get time-sorted transactions for relevant accounts
            transactions = mainWindowLogic.Household.RegularTransactions
                .Where(tr => AccountListLogic.IsAccountSelected(tr.AccountRow))
                .ToList();
            transactions.Sort((t1, t2) =>
            {
                // Sort by date
                int ret = t1.Date.CompareTo(t2.Date);
                if (ret == 0)
                {
                    // The sort by account
                    ret = t1.AccountID.CompareTo(t2.AccountID);
                    if (ret == 0 && t1.AccountRow.Type == EAccountType.Investment)
                    {
                        // Shares in first to avoid empty lot issues
                        var i1 = t1.GetInvestmentTransaction();
                        var i2 = t2.GetInvestmentTransaction();
                        if (i1.IsSecurityIn && i2.IsSecurityIn)
                        {
                            ret = 0;
                        }
                        else if (i1.IsSecurityIn)
                        {
                            ret = -1;
                        }
                        else if (i2.IsSecurityIn)
                        {
                            ret = 1;
                        }
                    }

                    // Don't let anybody think they are equal
                    if (ret == 0)
                    {
                        return t1.ID.CompareTo(t2.ID);
                    }
                }
                return ret;
            });
        }

        private void UpdateGraph()
        {
            mainWindowLogic.GuiServices.SetCursor(true);
            DateValues.Clear();
            PayoutDateValues.Clear();

            var date = DateRangeLogic.StartDate;
            var portfolio = new Portfolio(); // Combined portfolio of all selected investment accounts
            decimal bankBalance = 0; // Combined bank balance of all selected bank accounts
            decimal payout = 0;
            decimal wealth;

            // Iterate over the transaction list
            foreach (var transaction in transactions)
            {
                // Get to next date if we reached the current date
                while (transaction.Date >= date)
                {
                    // Create graph point for this date
                    wealth = bankBalance + portfolio.GetValuation(date, GetSecurityPriceOnDate);
                    DateValues.Add(new DateValue(date, wealth));
                    PayoutDateValues.Add(new DateValue(date, payout));

                    // End if reached the end of the date range
                    if (date > DateRangeLogic.EndDate)
                    {
                        break;
                    }

                    switch (frequency)
                    {
                        case FREQUENCY_DAY:
                            date = date.AddDays(1);
                            break;
                        case FREQUENCY_WEEK:
                            date = date.AddDays(7);
                            break;
                        case FREQUENCY_MONTH:
                            date = date.AddMonths(1);
                            break;
                        case FREQUENCY_QUARTER:
                            date = date.AddMonths(3);
                            break;
                        case FREQUENCY_YEAR:
                            date = date.AddYears(1);
                            break;
                    }
                }

                // Process this transaction
                if (transaction.AccountRow.Type == EAccountType.Investment)
                {
                    // Update portfolio for this transaction
                    portfolio.ApplyTransaction(transaction, false);

                    // Update payout for this transaction
                    var investmentTransactionRow = transaction.GetInvestmentTransaction();
                    if (investmentTransactionRow.IsTransferIn || investmentTransactionRow.IsTransferOut)
                    {
                        payout += transaction.GetAmount();
                    }
                }
                else
                {
                    // Update bank balance for this transaction
                    bankBalance += transaction.GetAmount();

                }
            }

            // Create graph point for last date
            wealth = bankBalance + portfolio.GetValuation(date, GetSecurityPriceOnDate);
            DateValues.Add(new DateValue(date, wealth));
            PayoutDateValues.Add(new DateValue(date, payout));

            RedrawGraph();

            mainWindowLogic.GuiServices.SetCursor(false);
        }

        // Get security price at a specified date knowing that the list is sorted by date
        private decimal GetSecurityPriceOnDate(int securityID, DateTime date)
        {
            var prices = securityPrices[securityID];

            decimal price = 0;
            int index = prices.BinarySearch(new SecurityPrice(date, 0));
            if (index >= 0)
            {
                price = prices[index].Price;
            }
            else
            {
                index = ~index;
                if (index > 0)
                {
                    price = prices[index - 1].Price;
                }
            }

            return price;
        }

        private void RedrawGraph()
        {
            UpdateGraphSignal = !UpdateGraphSignal;
            OnPropertyChanged(() => UpdateGraphSignal);
        }

        #endregion

        #region Supporting classes

        public class DateValue
        {
            public readonly DateTime Date;
            public readonly decimal Value;
            public readonly string Tip;

            public DateValue(DateTime date, decimal value) =>
                (Date, Value, Tip) = (date, value, $"{date:MM/dd/yyyy}: {value:N2}");
        }

        public class SecurityPrice : IComparable<SecurityPrice>
        {
            public readonly DateTime Date;
            public readonly decimal Price;

            public SecurityPrice(DateTime date, decimal price) =>
                (Date, Price) = (date, price);

            public int CompareTo(SecurityPrice other) => Date.CompareTo(other.Date);
        }

        #endregion
    }
}
