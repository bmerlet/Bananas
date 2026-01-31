using BanaData.Database;
using BanaData.Logic.Main;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Toolbox.UILogic;
using static BanaData.Database.Household;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowReturnsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;

        #endregion

        #region Constructor

        public ShowReturnsLogic(MainWindowLogic _mainWindowLogic, Household _household)
        {
            (mainWindowLogic, household) = (_mainWindowLogic, _household);

            DurationString = DurationSource[1];

            AccountSource = (CollectionView)CollectionViewSource.GetDefaultView(accountItems);
            AccountSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            foreach(var account in household.Account)
            {
                if (account.Type == EAccountType.Investment && (!mainWindowLogic.UserSettings.HideClosedAccounts || !account.Hidden))
                {
                    accountItems.Add(new AccountOrSecurityItem(household, account, duration, annualized));
                }
            }
        }

        #endregion

        #region UI Properties

        // Time range choice for 4th column
        public string[] DurationSource { get; } = new string[] { "Last 2 years", "Last 5 years", "last 10 years" };
        private string durationString;
        private int duration;
        public string DurationString
        {
            get => durationString;

            set
            {
                if (value !=  durationString)
                {
                    durationString = value;
                    if (durationString == DurationSource[0])
                    {
                        duration = 2;
                    }
                    else if (durationString == DurationSource[1])
                    {
                        duration = 5;
                    }
                    else
                    {
                        duration = 10;
                    }

                    foreach (var account in accountItems)
                    {
                        account.UpdateDuration(duration);
                    }
                }
            }
        }

        // The list of accounts
        private readonly ObservableCollection<AccountOrSecurityItem> accountItems = new ObservableCollection<AccountOrSecurityItem>();
        public CollectionView AccountSource { get; }

        // If the results are annualized or not
        private bool annualized = false;
        public bool? Annualized
        {
            get => annualized;
            set
            {
                if (annualized != value)
                {
                    annualized = value == true;
                    foreach (var account in accountItems)
                    {
                        account.UpdateAnnualized(annualized);
                    }
                }
            }
        }

        #endregion

    }

    public class AccountOrSecurityItem : LogicBase
    {
        #region Private members

        private Household household;
        private Household.AccountRow account;
        private int duration;
        private bool annualized;

        #endregion

        #region Constructors

        // Constructor when representing an account
        public AccountOrSecurityItem(Household _household, Household.AccountRow _account, int _duration, bool _annualized)
        {
            household = _household;
            account = _account;
            duration = _duration;
            annualized = _annualized;

            Name = account.Name;

            SecuritiesSource = (CollectionView)CollectionViewSource.GetDefaultView(securityItems);
            SecuritiesSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            // Create portfolio as of today
            var portfolio = account.GetPortfolio(DateTime.Today);

            // Create securities
            foreach (var security in portfolio.GetSecuritiesRows())
            {
                securityItems.Add(new AccountOrSecurityItem(household,security, duration, annualized));
            }

            ComputeHowFarInTheYearWeAre();
            ComputeReturnForAccount(account, portfolio);
        }

        // Constructor when representing a security
        public AccountOrSecurityItem(Household _household, Household.SecurityRow security, int _duration, bool _annualized)
        {
            household = _household;
            account = null;
            duration = _duration;
            annualized = _annualized;

            Name = security.Symbol;

            ComputeHowFarInTheYearWeAre();
            ComputeReturnForSecurity(security);
        }

        #endregion

        #region UI Properties

        // Name (account or security)
        public string Name { get; private set; }

        // Year-to-date return
        private decimal rawYearToDateReturn;
        private decimal yearToDateRatio;
        public decimal ReturnThisYear
        { 
            get
            {
                if (annualized)
                {
                    decimal result = decpow(1M + rawYearToDateReturn, yearToDateRatio) - 1M;
                    return result * 100M;
                }
                else
                {
                    return rawYearToDateReturn * 100M;
                }
            }
        }

        // Last year return
        private decimal lastYearRawReturn;
        public decimal ReturnLastYear
        {
            get
            {
                return lastYearRawReturn * 100M;
            }
        }

        // Last X year returns (X = 2, 5, 10)
        private decimal rawLast2YearsReturn;
        private decimal rawLast5YearsReturn;
        private decimal rawLast10YearsReturn;

        public decimal ReturnLastXYears
        {
            get
            {
                if (annualized)
                {
                    decimal rawReturn;
                    if (duration == 2)
                    {
                        rawReturn = rawLast2YearsReturn;
                    }
                    else if (duration == 5)
                    {
                        rawReturn = rawLast5YearsReturn;
                    }
                    else
                    {
                        rawReturn = rawLast10YearsReturn;
                    }
                    decimal result = decpow(rawReturn + 1M, 1M / (decimal)duration) - 1;
                    return result * 100M;
                }
                else
                {
                    if (duration == 2)
                    {
                        return rawLast2YearsReturn * 100M;
                    }
                    else if (duration == 5)
                    {
                        return rawLast5YearsReturn * 100M;
                    }
                    else
                    {
                        return rawLast10YearsReturn * 100;
                    }
                }
            }
        }

        // Overall return
        private decimal rawReturnAllAvailable;
        private decimal allAvailableTimeSpan = 1;

        public decimal ReturnAll
        {
            get
            {
                if (annualized)
                {
                    decimal result = decpow(rawReturnAllAvailable + 1M, 1M / allAvailableTimeSpan) - 1;
                    return result * 100M;
                }
                else
                {
                    return rawReturnAllAvailable * 100M;
                }
            }
        }

        // List of securities (making up the account, empty for a security)
        public CollectionView SecuritiesSource { get; }
        private readonly ObservableCollection<AccountOrSecurityItem> securityItems = new ObservableCollection<AccountOrSecurityItem>();

        #endregion

        #region Actions

        // Update duration for the variable column
        public void UpdateDuration(int _duration)
        {
            duration = _duration;

            InvokePropertyChanged(nameof(ReturnLastXYears));

            // Propagate downwards
            foreach(var  item in securityItems) 
            { 
                item.UpdateDuration(duration); 
            }
        }

        public void UpdateAnnualized(bool _annualized)
        {
            annualized = _annualized;

            InvokePropertyChanged(nameof(ReturnThisYear));
            InvokePropertyChanged(nameof(ReturnLastYear));
            InvokePropertyChanged(nameof(ReturnLastXYears));
            InvokePropertyChanged(nameof(ReturnAll));

            // Propagate downwards
            foreach (var item in securityItems)
            {
                item.UpdateAnnualized(annualized);
            }
        }

        private void ComputeReturnForAccount(Household.AccountRow account, Portfolio portfolio)
        {
            // Set dates we are looking for
            var today = DateTime.Today;
            var firstOfThisYear = new DateTime(today.Year, 1, 1);
            var firstOfLastYear = new DateTime(today.Year - 1, 1, 1);
            var firstOf2YearsAgo = new DateTime(today.Year - 2, 1, 1);
            var firstOf5YearsAgo = new DateTime(today.Year - 5, 1, 1);
            var firstOf10YearsAgo = new DateTime(today.Year - 10, 1, 1);
            var darkAges = new DateTime(today.Year - 50, 1, 1);

            var transactions = GetTimeSortedRelevantTransactionsForAccount(account);

            rawYearToDateReturn = ComputeReturnForAccount(account, transactions, firstOfThisYear, today);

            // Last year return
            lastYearRawReturn = ComputeReturnForAccount(account, transactions, firstOfLastYear, firstOfThisYear);

            // Last 2, 5, 10 years
            rawLast2YearsReturn = ComputeReturnForAccount(account, transactions, firstOf2YearsAgo, firstOfThisYear);
            rawLast5YearsReturn = ComputeReturnForAccount(account, transactions, firstOf5YearsAgo, firstOfThisYear);
            rawLast10YearsReturn = ComputeReturnForAccount(account, transactions, firstOf10YearsAgo, firstOfThisYear);

            // Find oldest transaction
            DateTime oldestTransactionDate = DateTime.Today;
            foreach (var transactionRow in household.RegularTransactions.Where(tr => tr.AccountRow == account))
            {
                if (transactionRow.Date <  oldestTransactionDate)
                {
                     oldestTransactionDate = transactionRow.Date;
                }
            }

            rawReturnAllAvailable = ComputeReturnForAccount(account, transactions, oldestTransactionDate, today);

            var timeSpan = new TimeSpan(today.Ticks - oldestTransactionDate.Ticks);
            try
            {
                allAvailableTimeSpan = (decimal)timeSpan.TotalDays / 365M;
            }
            catch (OverflowException)
            {
                allAvailableTimeSpan = 1;
            }

        }

        private IEnumerable<MiniTransaction> GetTimeSortedRelevantTransactionsForAccount(Household.AccountRow account)
        {
            var result = new List<MiniTransaction>();

            foreach (var transactionRow in household.RegularTransactions.Where(tr => tr.AccountRow == account))
            {
                // Remove irrelevant transactions, i.e. the ones that do not bring in or out external cash or shares
                var itr = transactionRow.GetInvestmentTransaction();
                decimal amount = 0;

                switch (itr.Type)
                {
                    // Transactions that add external cash
                    case EInvestmentTransactionType.CashIn:
                    case EInvestmentTransactionType.TransferCashIn:
                    case EInvestmentTransactionType.BuyFromTransferredCash:
                        amount = transactionRow.GetAmount();
                        break;

                    // Transactions that sends cash out
                    case EInvestmentTransactionType.CashOut:
                    case EInvestmentTransactionType.TransferCashOut:
                    case EInvestmentTransactionType.SellAndTransferCash:
                    case EInvestmentTransactionType.TransferDividends:
                    case EInvestmentTransactionType.TransferShortTermCapitalGains:
                    case EInvestmentTransactionType.TransferLongTermCapitalGains:
                        amount = transactionRow.GetAmount();
                        break;

                    // Transactions that add external shares
                    case EInvestmentTransactionType.SharesIn:
                    case EInvestmentTransactionType.XSharesIn:
                        amount = itr.SecurityQuantity * itr.SecurityRow.GetMostRecentPrice(transactionRow.Date);
                        break;


                    // Transactions that remove external shares
                    case EInvestmentTransactionType.SharesOut:
                    case EInvestmentTransactionType.XSharesOut:
                        amount = -itr.SecurityQuantity * itr.SecurityRow.GetMostRecentPrice(transactionRow.Date);
                        break;

                    // Transaction that are internal to the account (no external cash in/out)
                    case EInvestmentTransactionType.InterestIncome:
                    case EInvestmentTransactionType.Buy:
                    case EInvestmentTransactionType.Sell:
                    case EInvestmentTransactionType.Dividends:
                    case EInvestmentTransactionType.ReinvestDividends:
                    case EInvestmentTransactionType.ShortTermCapitalGains:
                    case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                    case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                    case EInvestmentTransactionType.LongTermCapitalGains:
                    case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                    case EInvestmentTransactionType.ReturnOnCapital:
                    case EInvestmentTransactionType.Grant:
                    case EInvestmentTransactionType.Vest:
                    case EInvestmentTransactionType.Exercise:
                    case EInvestmentTransactionType.Expire:
                        break;
                }

                if (amount != 0)
                {
                    result.Add(new MiniTransaction(transactionRow.Date, amount));
                }
            }

            // Sort by ascending time
            result.Sort((tr1, tr2) => tr1.Date.CompareTo(tr2.Date));

            // Consolidate transactions on same date
            var consolidatedList = new List<MiniTransaction>();

            for (int i = 0; i < result.Count; i++)
            {
                MiniTransaction curTrans = result[i];
                while (i < result.Count - 1 && result[i + 1].Date.CompareTo(curTrans.Date) == 0)
                {
                    curTrans = new MiniTransaction(curTrans.Date, curTrans.Amount + result[i + 1].Amount);
                    i += 1;
                }
                consolidatedList.Add(curTrans);
            }

            return consolidatedList;
        }

        private decimal ComputeReturnForAccount(Household.AccountRow account, IEnumerable<MiniTransaction> transactions, DateTime startDate, DateTime endDate)
        {
            decimal lastValue = account.GetInvestmentValue(startDate);
            decimal growthFactor = 1;

            foreach (var miniTrans in transactions.Where(tr => tr.Date >= startDate && tr.Date < endDate))
            {
                decimal valueNow = account.GetInvestmentValue(miniTrans.Date);
                if (lastValue != 0 && valueNow != miniTrans.Amount)
                {
                    decimal periodGrowthFactor = (valueNow - miniTrans.Amount) / lastValue;
                    growthFactor *= periodGrowthFactor;
                }
                lastValue = valueNow;
            }

            // Final value
            decimal finalValue = account.GetInvestmentValue(endDate);
            if (finalValue != 0)
            {
                growthFactor *= finalValue / lastValue;
            }

            return growthFactor - 1M;
        }

        // Compute the returns for a security
        private void ComputeReturnForSecurity(Household.SecurityRow security)
        {
            // Set dates we are looking for
            var today = DateTime.Today;
            var firstOfThisYear = new DateTime(today.Year, 1, 1);
            var firstOfLastYear = new DateTime(today.Year - 1, 1, 1);
            var firstOf2YearsAgo = new DateTime(today.Year - 2, 1, 1);
            var firstOf5YearsAgo = new DateTime(today.Year - 5, 1, 1);
            var firstOf10YearsAgo = new DateTime(today.Year - 10, 1, 1);
            var darkAges = new DateTime(today.Year - 50, 1, 1);

            // Find prices closest to these dates
            Household.SecurityPriceRow mostRecent = GetClosestSecurityPriceFromDate(security, today);
            Household.SecurityPriceRow closestToFirstOfYear = GetClosestSecurityPriceFromDate(security, firstOfThisYear);
            Household.SecurityPriceRow closestToFirstOfLastYear = GetClosestSecurityPriceFromDate(security, firstOfLastYear);
            Household.SecurityPriceRow closestToFirstOf2YearsAgo = GetClosestSecurityPriceFromDate(security, firstOf2YearsAgo);
            Household.SecurityPriceRow closestToFirstOf5YearsAgo = GetClosestSecurityPriceFromDate(security, firstOf5YearsAgo);
            Household.SecurityPriceRow closestToFirstOf10YearsAgo = GetClosestSecurityPriceFromDate(security, firstOf10YearsAgo);
            Household.SecurityPriceRow mostAncient = GetClosestSecurityPriceFromDate(security, darkAges);

            // Compute the returns

            // YTD return
            rawYearToDateReturn = (mostRecent.Value - closestToFirstOfYear.Value) / closestToFirstOfYear.Value;

            // Last year return
            lastYearRawReturn = (closestToFirstOfYear.Value - closestToFirstOfLastYear.Value) / closestToFirstOfLastYear.Value;

            // Last 2, 5, 10 years
            rawLast2YearsReturn = (closestToFirstOfYear.Value - closestToFirstOf2YearsAgo.Value) / closestToFirstOf2YearsAgo.Value;
            rawLast5YearsReturn = (closestToFirstOfYear.Value - closestToFirstOf5YearsAgo.Value) / closestToFirstOf5YearsAgo.Value;
            rawLast10YearsReturn = (closestToFirstOfYear.Value - closestToFirstOf10YearsAgo.Value) / closestToFirstOf10YearsAgo.Value;

            // All available
            rawReturnAllAvailable = (mostRecent.Value - mostAncient.Value) / mostAncient.Value;
            var timeSpan = new TimeSpan(today.Ticks - mostAncient.Date.Ticks);
            try
            {
                allAvailableTimeSpan = (decimal)timeSpan.TotalDays / 365M;
            }
            catch(OverflowException)
            {
                allAvailableTimeSpan = 1;
            }
        }

        private Household.SecurityPriceRow GetClosestSecurityPriceFromDate(Household.SecurityRow security, DateTime date)
        {
            Household.SecurityPriceRow result = null;
            long curDist = 0;

            foreach (var price in security.GetSecurityPriceRows())
            {
                if (result == null)
                {
                    result = price;
                    curDist = Math.Abs((price.Date - date).Ticks);
                }
                else
                {
                    long newDist = Math.Abs((price.Date - date).Ticks);
                    if (newDist < curDist)
                    {
                        result = price;
                        curDist = newDist;
                        if (curDist == 0)
                        {
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private decimal decpow(decimal x, decimal y)
        {
            double r = Math.Pow((double)x, (double)y);
            try
            {
                return (decimal)r;
            }
            catch (OverflowException)
            {
                return 0;
            }
        }

        // How far in the year we are
        private void ComputeHowFarInTheYearWeAre()
        {
            var today = DateTime.Today;
            decimal daysInYear = DateTime.IsLeapYear(today.Year) ? 366 : 365;
            yearToDateRatio = daysInYear / today.DayOfYear;
        }

        #endregion

        #region Supporting classes

        internal class MiniTransaction
        {
            public readonly DateTime Date;
            public readonly decimal Amount;

            internal MiniTransaction(DateTime date, decimal amount)
            {
                Date = date;
                Amount = amount;
            }
        }

        #endregion
    }
}
