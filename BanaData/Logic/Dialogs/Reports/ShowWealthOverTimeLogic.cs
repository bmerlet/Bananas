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
    public class ShowWealthOverTimeLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private bool internalUpdate = false;

        #endregion

        #region Constructor

        public ShowWealthOverTimeLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            foreach (Household.AccountRow accountRow in mainWindowLogic.Household.Account)
            {
                var accountItem = new AccountPickerItem(accountRow, true);
                accountItem.PropertyChanged += (s, e) => { if (!internalUpdate) UpdateGraph(); };
                accounts.Add(accountItem);
            }

            // Setup account view
            Accounts = (CollectionView)CollectionViewSource.GetDefaultView(accounts);
            Accounts.SortDescriptions.Add(new SortDescription("AccountItem.Hidden", ListSortDirection.Ascending));
            Accounts.SortDescriptions.Add(new SortDescription("AccountItem.Name", ListSortDirection.Ascending));

            // Setup commands
            ClearAllCommand = new CommandBase(OnClearAllCommand);
            SelectAllCommand = new CommandBase(OnSelectAllCommand);
            SelectInvestmentCommand = new CommandBase(OnSelectInvestmentCommand);
            SelectBankingCommand = new CommandBase(OnSelectBankingCommand);

            SetDateRange(dateRange);
        }

        #endregion

        #region UI properties

        //
        // List of accounts
        //
        private readonly ObservableCollection<AccountPickerItem> accounts = new ObservableCollection<AccountPickerItem>();
        public CollectionView Accounts { get; }

        //
        // Account selection buttons
        //
        public CommandBase ClearAllCommand { get; }
        public CommandBase SelectAllCommand { get; }
        public CommandBase SelectInvestmentCommand { get; }
        public CommandBase SelectBankingCommand { get; }

        //
        // Date range combo box
        //
        private const string DATE_RANGE_ONE_WEEK = "One week";
        private const string DATE_RANGE_ONE_MONTH = "One month";
        private const string DATE_RANGE_ONE_YEAR = "One year";
        private const string DATE_RANGE_YTD = "Year to date";
        private const string DATE_RANGE_FIVE_YEARS = "5 years";
        private const string DATE_RANGE_TEN_YEARS = "10 years";
        private const string DATE_RANGE_ALL = "All available";
        private const string DATE_RANGE_CUSTOM = "Custom";
        public string[] DateRangesSource { get; } = new string[] {
            DATE_RANGE_ONE_WEEK,
            DATE_RANGE_ONE_MONTH,
            DATE_RANGE_ONE_YEAR,
            DATE_RANGE_YTD,
            DATE_RANGE_FIVE_YEARS,
            DATE_RANGE_TEN_YEARS,
            DATE_RANGE_ALL,
            DATE_RANGE_CUSTOM
        };

        private string dateRange = DATE_RANGE_TEN_YEARS;
        public string DateRange { get => dateRange; set => SetDateRange(value); }

        // Custom dates enabled
        public bool? AreDatesEnabled => dateRange == DATE_RANGE_CUSTOM;

        // Custom start date
        private DateTime startDate;
        public DateTime StartDate { get => startDate; set => SetCustomDateRange(value, endDate); }

        // Custom end date
        private DateTime endDate;
        public DateTime EndDate { get => endDate; set => SetCustomDateRange(startDate, value); }

        //
        // Frequency
        //
        private const string FREQUENCY_DAY = "day";
        private const string FREQUENCY_WEEK = "week";
        private const string FREQUENCY_MONTH = "month";
        private const string FREQUENCY_YEAR = "year";
        public string[] FrequencySource { get; } = new string[]
        {
            FREQUENCY_DAY,
            FREQUENCY_WEEK,
            FREQUENCY_MONTH,
            FREQUENCY_YEAR
        };

        private string frequency = FREQUENCY_YEAR;
        public string Frequency { get => frequency; set { frequency = value; UpdateGraph(); } }

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

        private void OnClearAllCommand()
        {
            internalUpdate = true;

            foreach (var acct in accounts)
            {
                acct.IsSelected = false;
            }

            internalUpdate = false;
            UpdateGraph();
        }

        private void OnSelectAllCommand()
        {
            internalUpdate = true;

            foreach (var acct in accounts)
            {
                acct.IsSelected = true;
            }

            internalUpdate = false;
            UpdateGraph();
        }

        private void OnSelectInvestmentCommand()
        {
            internalUpdate = true;

            foreach (var acct in accounts)
            {
                acct.IsSelected = acct.AccountRow.Type == EAccountType.Investment;
            }

            internalUpdate = false;
            UpdateGraph();
        }

        private void OnSelectBankingCommand()
        {
            internalUpdate = true;

            foreach (var acct in accounts)
            {
                acct.IsSelected = acct.AccountRow.Type == EAccountType.Bank;
            }

            internalUpdate = false;
            UpdateGraph();
        }

        private void SetDateRange(string value)
        {
            dateRange = value;

            switch (dateRange)
            {
                case DATE_RANGE_ONE_WEEK:
                    endDate = DateTime.Today;
                    startDate = endDate.AddDays(-7);
                    break;
                case DATE_RANGE_ONE_MONTH:
                    endDate = DateTime.Today;
                    startDate = endDate.AddMonths(-1);
                    break;
                case DATE_RANGE_ONE_YEAR:
                    endDate = DateTime.Today;
                    startDate = endDate.AddYears(-1);
                    break;
                case DATE_RANGE_YTD:
                    endDate = DateTime.Today;
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    break;
                case DATE_RANGE_FIVE_YEARS:
                    endDate = DateTime.Today;
                    startDate = endDate.AddYears(-5);
                    break;
                case DATE_RANGE_TEN_YEARS:
                    endDate = DateTime.Today;
                    startDate = endDate.AddYears(-10);
                    break;
                case DATE_RANGE_ALL:
                    endDate = DateTime.Today;
                    startDate = mainWindowLogic.Household.RegularTransactions.Where(tr => accounts.First(a => a.AccountRow == tr.AccountRow).IsSelected == true).Min(tr => tr.Date);
                    break;
                case DATE_RANGE_CUSTOM:
                    break;
            }

            OnPropertyChanged(() => StartDate);
            OnPropertyChanged(() => EndDate);
            OnPropertyChanged(() => AreDatesEnabled);

            UpdateGraph();
        }

        private void SetCustomDateRange(DateTime startDate, DateTime endDate)
        {
            this.startDate = startDate;
            this.endDate = endDate;

            UpdateGraph();
        }

        private void UpdateGraph()
        {
            mainWindowLogic.GuiServices.SetCursor(true);
            DateValues.Clear();
            PayoutDateValues.Clear();

            // Get time-sorted transactions for relevant accounts
            var transactions = mainWindowLogic.Household.RegularTransactions
                .Where(tr => accounts.First(a => a.AccountRow == tr.AccountRow).IsSelected == true)
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

            var date = startDate;
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
                    wealth = bankBalance + portfolio.GetValuation(date);
                    DateValues.Add(new DateValue(date, wealth));
                    PayoutDateValues.Add(new DateValue(date, payout));

                    // End if reached the end of the date range
                    if (date > endDate)
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
                        case FREQUENCY_YEAR:
                            date = date.AddYears(1);
                            break;
                    }
                }

                // Process this transaction
                if (transaction.AccountRow.Type == EAccountType.Investment)
                {
                    // Update portfolio for this transaction
                    portfolio.ApplyTransaction(transaction);

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
            wealth = bankBalance + portfolio.GetValuation(date);
            DateValues.Add(new DateValue(date, wealth));
            PayoutDateValues.Add(new DateValue(date, payout));

            UpdateGraphSignal = !UpdateGraphSignal;
            OnPropertyChanged(() => UpdateGraphSignal);

            mainWindowLogic.GuiServices.SetCursor(false);
        }

        #endregion

        #region Supporting classes

        public class AccountPickerItem : LogicBase
        {
            public AccountPickerItem(Household.AccountRow accountRow, bool selected) =>
                (AccountRow, AccountItem, isSelected) = (accountRow, AccountItem.CreateFromDB(accountRow), selected);

            public readonly Household.AccountRow AccountRow;

            public AccountItem AccountItem { get; }

            private bool? isSelected;
            public bool? IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                        OnPropertyChanged(() => IsSelected);
                    }
                }
            }
        }

        public class DateValue
        {
            public readonly DateTime Date;
            public readonly decimal Value;
            public readonly string Tip;

            public DateValue(DateTime date, decimal value) =>
                (Date, Value, Tip) = (date, value, $"{date:MM/dd/yyyy}: {value:N2}");
        }

        #endregion
    }
}
