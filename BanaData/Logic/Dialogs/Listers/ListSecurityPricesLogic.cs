using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Collections;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Listers
{
    /// <summary>
    /// Security price logic: Handles the security price register and the security value graph
    /// </summary>
    public class ListSecurityPricesLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly SecurityItem securityItem;

        #endregion

        #region Constructor

        public ListSecurityPricesLogic(MainWindowLogic mainWindowLogic, SecurityItem securityItem)
        {
            (this.mainWindowLogic, this.securityItem) = (mainWindowLogic, securityItem);

            Register = new SecurityPricesRegisterLogic(mainWindowLogic, this, securityItem.ID);
            SetDateRange(dateRange);
            UpdateGraph();
        }

        #endregion

        #region UI properties

        // Title
        public string Title => $"{securityItem.Symbol} price history";

        // Security prices register
        public SecurityPricesRegisterLogic Register { get; }

        // Date range combo box
        private const string DATE_RANGE_ONE_WEEK = "One week";
        private const string DATE_RANGE_ONE_MONTH = "One month";
        private const string DATE_RANGE_ONE_YEAR = "One year";
        private const string DATE_RANGE_YTD = "Year to date";
        private const string DATE_RANGE_FIVE_YEAR = "5 years";
        private const string DATE_RANGE_ALL = "All available";
        private const string DATE_RANGE_CUSTOM = "Custom";
        public string[] DateRangesSource { get; } = new string[] {
            DATE_RANGE_ONE_WEEK, 
            DATE_RANGE_ONE_MONTH,
            DATE_RANGE_ONE_YEAR,
            DATE_RANGE_YTD,
            DATE_RANGE_FIVE_YEAR,
            DATE_RANGE_ALL,
            DATE_RANGE_CUSTOM
        };

        private string dateRange = DATE_RANGE_ONE_YEAR;
        public string DateRange { get => dateRange; set => SetDateRange(value); }

        // Custom dates enabled
        public bool? AreDatesEnabled => dateRange == DATE_RANGE_CUSTOM;

        // Custom start date
        private DateTime startDate;
        public DateTime StartDate { get => startDate; set => SetCustomDateRange(value, endDate); }

        // Custom end date
        private DateTime endDate;
        public DateTime EndDate { get => endDate; set => SetCustomDateRange(startDate, value); }

        // Price points
        public WpfObservableRangeCollection<DatePriceGraphItem> Quotes { get; } =
            new WpfObservableRangeCollection<DatePriceGraphItem>();

        // Dividend reinvestments date and price
        private bool showReinvDivs = true;
        public bool? ShowReinvDivs { get => showReinvDivs; set { showReinvDivs = value == true; OnPropertyChanged(() => ShowReinvDivs); } }
        public WpfObservableRangeCollection<DatePriceGraphItem> ReinvestedDividends { get; } =
            new WpfObservableRangeCollection<DatePriceGraphItem>();

        // Sale/Purchase date and price
        private bool showTrades = true;
        public bool? ShowTrades { get => showTrades; set { showTrades = value == true; OnPropertyChanged(() => ShowTrades); } }
        public WpfObservableRangeCollection<DatePriceGraphItem> Trades { get; } =
            new WpfObservableRangeCollection<DatePriceGraphItem>();

        #endregion

        #region Actions

        private void SetDateRange(string value)
        {
            dateRange = value;

            switch(dateRange)
            {
                case DATE_RANGE_ONE_WEEK:
                    endDate = DateTime.Today;
                    startDate = endDate.AddDays(-6);
                    break;
                case DATE_RANGE_ONE_MONTH:
                    endDate = DateTime.Today;
                    startDate = endDate.AddMonths(-1).AddDays(1);
                    break;
                case DATE_RANGE_ONE_YEAR:
                    endDate = DateTime.Today;
                    startDate = endDate.AddYears(-1).AddDays(1);
                    break;
                case DATE_RANGE_YTD:
                    endDate = DateTime.Today;
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    break;
                case DATE_RANGE_FIVE_YEAR:
                    endDate = DateTime.Today;
                    startDate = endDate.AddYears(-5).AddDays(1);
                    break;
                case DATE_RANGE_ALL:
                    endDate = DateTime.Today;
                    startDate = mainWindowLogic.Household.SecurityPrice.Where(sp => sp.SecurityID == securityItem.ID).Min(sp => sp.Date);
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

        public void UpdateGraph()
        {
            Quotes.ReplaceRange(
                mainWindowLogic.Household.SecurityPrice
                .Where(sp => sp.SecurityID == securityItem.ID)
                .Where(sp => sp.Date.CompareTo(startDate) >= 0)
                .Where(sp => sp.Date.CompareTo(endDate) <= 0)
                .Select(sp => new DatePriceGraphItem(sp)));

            ReinvestedDividends.ReplaceRange(
                mainWindowLogic.Household.InvestmentTransaction
                .Where(it => it.Type == EInvestmentTransactionType.ReinvestDividends ||
                    it.Type == EInvestmentTransactionType.ReinvestShortTermCapitalGains ||
                    it.Type == EInvestmentTransactionType.ReinvestMediumTermCapitalGains ||
                    it.Type == EInvestmentTransactionType.ReinvestLongTermCapitalGains)
                .Where(it => it.SecurityID == securityItem.ID)
                .Where(it => it.TransactionRow.Date.CompareTo(startDate) >= 0)
                .Where(it => it.TransactionRow.Date.CompareTo(endDate) <= 0)
                .Select(it => new DatePriceGraphItem(it)));

            Trades.ReplaceRange(
                mainWindowLogic.Household.InvestmentTransaction
                .Where(it => it.Type == EInvestmentTransactionType.Buy ||
                    it.Type == EInvestmentTransactionType.BuyFromTransferredCash ||
                    it.Type == EInvestmentTransactionType.Sell ||
                    it.Type == EInvestmentTransactionType.SellAndTransferCash)
                .Where(it => it.SecurityID == securityItem.ID)
                .Where(it => it.TransactionRow.Date.CompareTo(startDate) >= 0)
                .Where(it => it.TransactionRow.Date.CompareTo(endDate) <= 0)
                .Select(it => new DatePriceGraphItem(it)));
        }

        #endregion
    }

    /// <summary>
    /// Security price register
    /// </summary>
    public class SecurityPricesRegisterLogic : BaseRegisterLogic
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly ListSecurityPricesLogic listSecurityPricesLogic;
        private readonly int securityID;

        #endregion

        #region Constructor

        public SecurityPricesRegisterLogic(MainWindowLogic _mainWindowLogic, ListSecurityPricesLogic _listSecurityPricesLogic, int _securityID)
        {
            mainWindowLogic = _mainWindowLogic;
            listSecurityPricesLogic = _listSecurityPricesLogic;
            securityID = _securityID;

            // Build security price collection
            var household = mainWindowLogic.Household;
            datePriceItems = new ObservableCollection<DatePriceItem>();
            foreach (var securityPrice in household.SecurityPrice.Where(sp => sp.SecurityID == securityID))
            {
                datePriceItems.Add(new DatePriceItem(mainWindowLogic, securityPrice));
            }

            // Update graph when a line is deleted
            datePriceItems.CollectionChanged += (o, e) => listSecurityPricesLogic.UpdateGraph();

            // Give the default view to the UI
            RegisterItems = (CollectionView)CollectionViewSource.GetDefaultView(datePriceItems);

            // Delete line item command
            DeleteDatePrice = new CommandBase(OnDeleteLineItem);
        }

        #endregion

        #region UI properties

        // Collection of date/prices
        private readonly ObservableCollection<DatePriceItem> datePriceItems;

        // Selected date/price
        private DatePriceItem selectedDatePrice;
        private bool logicIsChangingSelection;
        public DatePriceItem SelectedDatePrice
        {
            get => selectedDatePrice;
            set
            {
                if (value != selectedDatePrice)
                {
                    if (logicIsChangingSelection)
                    {
                        // This logic is changing the selection (e.g. processing of return key)
                        selectedDatePrice = value;
                        editedDatePrice = value;
                        OnPropertyChanged(() => SelectedDatePrice);
                    }
                    else
                    {
                        // User changed selection (e.g. by clicking on a row)
                        if (editedDatePrice != null && datePriceItems.Contains(editedDatePrice))
                        {
                            editedDatePrice.CancelEdit();
                        }
                        selectedDatePrice = value;
                        editedDatePrice = value;
                    }

                    if (selectedDatePrice != null)
                    {
                        selectedDatePrice.BeginEdit();

                        DateFocus = false;
                        OnPropertyChanged(() => DateFocus);
                        UpdateOverlayPosition = () =>
                        {
                            DateFocus = true;
                            OnPropertyChanged(() => DateFocus);
                        };
                    }
                    else
                    {
                        UpdateOverlayPosition = null;
                    }

                    OnPropertyChanged(() => EditedDatePrice);
                    OnPropertyChanged(() => UpdateOverlayPosition);
                }
            }
        }

        // Date/price being edited
        private DatePriceItem editedDatePrice;
        public DatePriceItem EditedDatePrice
        {
            get => editedDatePrice;
            set { editedDatePrice = value; OnPropertyChanged(() => EditedDatePrice); }
        }

        // Delete command from context menu
        public CommandBase DeleteDatePrice { get; }

        // To focus the overlay on the category
        public bool DateFocus { get; private set; }

        // Column widths
        private double widthOfDateColumn = 80;
        public double WidthOfDateColumn
        {
            get => widthOfDateColumn;
            set { widthOfDateColumn = value; OnPropertyChanged(() => WidthOfDateColumn); }
        }

        private double widthOfPriceColumn = 90;
        public double WidthOfPriceColumn
        {
            get => widthOfPriceColumn;
            set { widthOfPriceColumn = value; OnPropertyChanged(() => WidthOfPriceColumn); }
        }

        #endregion

        #region Actions

        // Activated when the window is loaded
        public void OnLoaded()
        {
            // Select first date/price
            if (datePriceItems.Count == 0)
            {
                // Need to add a new item
                datePriceItems.Add(new DatePriceItem(mainWindowLogic, securityID));
            }

            selectedDatePrice = datePriceItems[0];
            editedDatePrice = datePriceItems[0];
            OnPropertyChanged(() => SelectedDatePrice);
            OnPropertyChanged(() => EditedDatePrice);
            OnPropertyChanged("UpdateOverlayPosition");
        }

        public override void MoveUp()
        {
            if (GetPreviousTransaction(SelectedDatePrice) is DatePriceItem prevDatePrice)
            {
                logicIsChangingSelection = true;
                SelectedDatePrice = prevDatePrice;
                logicIsChangingSelection = false;
            }
        }

        public override void MoveDown()
        {
            if (GetNextTransaction(SelectedDatePrice) is DatePriceItem nextDatePrice)
            {
                logicIsChangingSelection = true;
                SelectedDatePrice = nextDatePrice;
                logicIsChangingSelection = false;
            }
        }


        public override void ProcessEnter()
        {
            if (selectedDatePrice != null)
            {
                if (selectedDatePrice.Price == 0)
                {
                    mainWindowLogic.ErrorMessage("Please enter a price.");
                    return;
                }

                selectedDatePrice.EndEdit();
                listSecurityPricesLogic.UpdateGraph();
            }

            if (GetNextTransaction(selectedDatePrice) is DatePriceItem nextDatePrice)
            {
                logicIsChangingSelection = true;
                SelectedDatePrice = nextDatePrice;
                logicIsChangingSelection = false;
            }
            else
            {
                // Need to add a new item
                var dpi = new DatePriceItem(mainWindowLogic, securityID);
                datePriceItems.Add(dpi);

                logicIsChangingSelection = true;
                SelectedDatePrice = dpi;
                logicIsChangingSelection = false;
            }
        }


        public override void RecomputeBalances()
        {
            listSecurityPricesLogic.UpdateGraph();
        }

        private void OnDeleteLineItem()
        {
            var dpi = editedDatePrice;
            if (dpi == null)
            {
                return;
            }

            if (datePriceItems.Count == 1)
            {
                mainWindowLogic.ErrorMessage("Cannot remove the last line item");
                return;
            }

            // Select next line item (if it exists) or previous one
            int ix = datePriceItems.IndexOf(dpi);
            if (ix < datePriceItems.Count - 1)
            {
                ix += 1;
            }
            else
            {
                ix -= 1;
            }

            logicIsChangingSelection = true;
            SelectedDatePrice = datePriceItems[ix];
            logicIsChangingSelection = false;

            dpi.Delete();
            datePriceItems.Remove(dpi);
        }

        #endregion
    }

    /// <summary>
    /// Price-at a date class for register
    /// </summary>
    public class DatePriceItem : LogicBase, IEditableObject
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private Household.SecurityPriceRow securityPriceRow;
        private readonly int securityID;

        private struct Data
        {
            public DateTime Date;
            public decimal Price;
        }

        private Data data;
        private Data backup;
        private bool editing;

        #endregion

        #region Constructors

        // Create a date price from a security price row
        public DatePriceItem(MainWindowLogic _mainWindowLogic, Household.SecurityPriceRow _securityPriceRow)
        {
            mainWindowLogic = _mainWindowLogic;
            securityPriceRow = _securityPriceRow;

            data.Date = securityPriceRow.Date;
            data.Price = securityPriceRow.Value;
        }

        // Create the open date/price
        public DatePriceItem(MainWindowLogic _mainWindowLogic, int _securityID)
        {
            mainWindowLogic = _mainWindowLogic;
            securityID = _securityID;

            data.Date = DateTime.Now;
        }

        #endregion

        #region Logic properties


        #endregion

        #region UI properties

        public DateTime Date { get => data.Date; set => data.Date = value; }
        public decimal Price { get => data.Price; set => data.Price = value; }

        public string Tip { get; private set; }

        #endregion

        #region Editable object implementation

        public void BeginEdit()
        {
            if (!editing)
            {
                // Backup the data
                backup = data;
                editing = true;
            }
        }

        public void EndEdit()
        {
            if (editing)
            {
                // Publish data
                editing = false;
                OnPropertyChanged(() => Date);
                OnPropertyChanged(() => Price);
                OnPropertyChanged(() => Tip);

                // Save in DB
                if (mainWindowLogic != null)
                {
                    if (securityPriceRow != null)
                    {
                        securityPriceRow.Date = data.Date;
                        securityPriceRow.Value = data.Price;
                    }
                    else
                    {
                        var securityRow = mainWindowLogic.Household.Security.FindByID(securityID);
                        securityPriceRow = mainWindowLogic.Household.SecurityPrice.AddSecurityPriceRow(securityRow, data.Date, data.Price);
                    }
                    mainWindowLogic.CommitChanges();
                }
            }
        }

        public void CancelEdit()
        {
            if (editing)
            {
                // Recover from backup data
                editing = false;
                data = backup;
                OnPropertyChanged(() => Date);
                OnPropertyChanged(() => Price);
            }
        }

        public void Delete()
        {
            if (securityPriceRow != null)
            {
                securityPriceRow.Delete();
                securityPriceRow = null;
                mainWindowLogic.CommitChanges();
            }
        }

        #endregion

        #region Overrides

        public override bool Equals(object obj)
        {
            return
                obj is DatePriceItem o &&
                o.Date == Date &&
                o.Price == Price;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Price-at a date class for graph
    /// </summary>
    public class DatePriceGraphItem : LogicBase
    {
        #region Constructors

        // Create a date price from a security price row
        public DatePriceGraphItem(Household.SecurityPriceRow securityPriceRow)
        {
            SecurityPriceRow = securityPriceRow;
            InvestmentTransactionRow = null;
            Date = securityPriceRow.Date;
            Price = securityPriceRow.Value;
            UpdateTip();
        }

        // Create a date price from an investment transaction row
        public DatePriceGraphItem(Household.InvestmentTransactionRow investmentTransactionRow)
        {
            SecurityPriceRow = null;
            InvestmentTransactionRow = investmentTransactionRow;
            Date = investmentTransactionRow.TransactionRow.Date;
            Price = investmentTransactionRow.SecurityPrice;
            UpdateTip();
        }

        private void UpdateTip()
        {
            if (SecurityPriceRow != null)
            {
                Tip = $"Quote: {Price:N2} on {Date:MM/dd/yyyy}";
            }
            else if (InvestmentTransactionRow != null)
            {
                if (InvestmentTransactionRow.Type == EInvestmentTransactionType.ReinvestDividends ||
                    InvestmentTransactionRow.Type == EInvestmentTransactionType.ReinvestShortTermCapitalGains ||
                    InvestmentTransactionRow.Type == EInvestmentTransactionType.ReinvestMediumTermCapitalGains ||
                    InvestmentTransactionRow.Type == EInvestmentTransactionType.ReinvestLongTermCapitalGains)
                {
                    Tip = $"Reinvestment: {Price:N2} on {Date:MM/dd/yyyy}";
                }
                else if (InvestmentTransactionRow.Type == EInvestmentTransactionType.Sell ||
                         InvestmentTransactionRow.Type == EInvestmentTransactionType.SellAndTransferCash)
                {
                    Tip = $"Sale: {Price:N2} on {Date:MM/dd/yyyy}";
                }
                else
                {
                    Tip = $"Purchase: {Price:N2} on {Date:MM/dd/yyyy}";
                }
            }
            else
            {
                Tip = $"Manually entered: {Price:N2} on {Date:MM/dd/yyyy}";
            }
        }

        #endregion

        #region Logic properties

        public Household.SecurityPriceRow SecurityPriceRow;
        public Household.InvestmentTransactionRow InvestmentTransactionRow;

        #endregion

        #region UI properties

        public DateTime Date { get; }
        public decimal Price { get; }

        public string Tip { get; private set; }

        #endregion

        #region Overrides

        public override bool Equals(object obj)
        {
            return
                obj is DatePriceGraphItem o &&
                o.SecurityPriceRow == SecurityPriceRow &&
                o.InvestmentTransactionRow == InvestmentTransactionRow &&
                o.Date == Date &&
                o.Price == Price;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
