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
using BanaData.Logic.Dialogs.Basics;
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

        private readonly Household household;
        private readonly SecurityItem securityItem;

        #endregion

        #region Constructor

        public ListSecurityPricesLogic(MainWindowLogic mainWindowLogic, Household household, SecurityItem securityItem)
        {
            (this.household, this.securityItem) = (household, securityItem);

            Register = new SecurityPricesRegisterLogic(mainWindowLogic, household, this, securityItem.ID);

            DateRangeLogic = new DateRangeLogic(
                DateRangeLogic.ERange.LastYear, 
                () => household.SecurityPrice.Where(sp => sp.SecurityID == securityItem.ID).Min(sp => sp.Date));
            DateRangeLogic.DateRangeChanged += (s, e) => UpdateGraph();

            UpdateGraph();
        }

        #endregion

        #region UI properties

        // Title
        public string Title => $"{securityItem.Symbol} price history";

        // Security prices register
        public SecurityPricesRegisterLogic Register { get; }

        // Date range
        public DateRangeLogic DateRangeLogic { get; }

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

        // Graph generator
        public bool UpdateGraphSignal { get; private set; }

        #endregion

        #region Actions

        public void UpdateGraph()
        {
            var sortableList =
                household.SecurityPrice
                .Where(sp => sp.SecurityID == securityItem.ID)
                .Where(sp => sp.Date.CompareTo(DateRangeLogic.StartDate) >= 0)
                .Where(sp => sp.Date.CompareTo(DateRangeLogic.EndDate) <= 0)
                .Select(sp => new DatePriceGraphItem(sp))
                .ToList();
            sortableList.Sort((sp1, sp2) => sp1.Date.CompareTo(sp2.Date));
            Quotes.ReplaceRange(sortableList);

            ReinvestedDividends.ReplaceRange(
                household.InvestmentTransaction
                .Where(it => it.Type == EInvestmentTransactionType.ReinvestDividends ||
                    it.Type == EInvestmentTransactionType.ReinvestShortTermCapitalGains ||
                    it.Type == EInvestmentTransactionType.ReinvestMediumTermCapitalGains ||
                    it.Type == EInvestmentTransactionType.ReinvestLongTermCapitalGains)
                .Where(it => it.SecurityID == securityItem.ID)
                .Where(it => it.TransactionRow.Date.CompareTo(DateRangeLogic.StartDate) >= 0)
                .Where(it => it.TransactionRow.Date.CompareTo(DateRangeLogic.EndDate) <= 0)
                .Select(it => new DatePriceGraphItem(it)));

            Trades.ReplaceRange(
                household.InvestmentTransaction
                .Where(it => it.Type == EInvestmentTransactionType.Buy ||
                    it.Type == EInvestmentTransactionType.BuyFromTransferredCash ||
                    it.Type == EInvestmentTransactionType.Sell ||
                    it.Type == EInvestmentTransactionType.SellAndTransferCash)
                .Where(it => it.SecurityID == securityItem.ID)
                .Where(it => it.TransactionRow.Date.CompareTo(DateRangeLogic.StartDate) >= 0)
                .Where(it => it.TransactionRow.Date.CompareTo(DateRangeLogic.EndDate) <= 0)
                .Select(it => new DatePriceGraphItem(it)));

            UpdateGraphSignal = !UpdateGraphSignal;
            OnPropertyChanged(() => UpdateGraphSignal);
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
        private readonly Household household;
        private readonly ListSecurityPricesLogic listSecurityPricesLogic;
        private readonly int securityID;

        #endregion

        #region Constructor

        public SecurityPricesRegisterLogic(MainWindowLogic _mainWindowLogic, Household _household, ListSecurityPricesLogic _listSecurityPricesLogic, int _securityID)
        {
            mainWindowLogic = _mainWindowLogic;
            household = _household;
            listSecurityPricesLogic = _listSecurityPricesLogic;
            securityID = _securityID;

            // Build security price collection
            datePriceItems = new ObservableCollection<DatePriceItem>();
            foreach (var securityPrice in household.SecurityPrice.Where(sp => sp.SecurityID == securityID))
            {
                datePriceItems.Add(new DatePriceItem(mainWindowLogic, household, securityPrice));
            }

            // Update graph when a line is deleted
            datePriceItems.CollectionChanged += (o, e) => listSecurityPricesLogic.UpdateGraph();

            // Give the default view to the UI
            RegisterItems = (CollectionView)CollectionViewSource.GetDefaultView(datePriceItems);
            RegisterItems.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));

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

        // To focus the overlay on the date
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

            RegisterItems.MoveCurrentToFirst();
            selectedDatePrice = RegisterItems.CurrentItem as DatePriceItem;
            editedDatePrice = selectedDatePrice;
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
                // Check price
                if (selectedDatePrice.Price == 0)
                {
                    mainWindowLogic.ErrorMessage("Please enter a price.");
                    return;
                }

                // Check no 2 prices for the same day
                if (household.SecurityPrice
                    .Where(sp => sp != selectedDatePrice.SecurityPriceRow &&
                           sp.Date == selectedDatePrice.Date &&
                           sp.SecurityID == securityID)
                    .Count() > 0)
                {
                    mainWindowLogic.ErrorMessage("There is alreasy a price for this date");
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
        private readonly Household household;
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
        public DatePriceItem(MainWindowLogic _mainWindowLogic, Household _household, Household.SecurityPriceRow securityPriceRow)
        {
            mainWindowLogic = _mainWindowLogic;
            household = _household;
            SecurityPriceRow = securityPriceRow;

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

        public Household.SecurityPriceRow SecurityPriceRow { get; private set; }

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
                    if (SecurityPriceRow != null)
                    {
                        SecurityPriceRow.Date = data.Date;
                        SecurityPriceRow.Value = data.Price;
                    }
                    else
                    {
                        var securityRow = household.Security.FindByID(securityID);
                        SecurityPriceRow = household.SecurityPrice.AddSecurityPriceRow(securityRow, data.Date, data.Price);
                    }
                    mainWindowLogic.CommitChanges(household);
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
            if (SecurityPriceRow != null)
            {
                SecurityPriceRow.Delete();
                SecurityPriceRow = null;
                mainWindowLogic.CommitChanges(household);
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
