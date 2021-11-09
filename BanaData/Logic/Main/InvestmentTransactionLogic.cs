using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.Attributes;
using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Dialogs.Reports;
using BanaData.Collections;

namespace BanaData.Logic.Main
{
    public class InvestmentTransactionLogic : AbstractTransactionLogic
    {
        #region Private fields

        // Parent logic
        private readonly InvestmentRegisterLogic investmentRegisterLogic;

        // Our data
        private new readonly InvestmentTransactionData data;

        // Behavior of transaction (based on type)
        private IInvestmentTransactionType investmentTransactionType;

        #endregion

        #region Constructors

        public InvestmentTransactionLogic(
            MainWindowLogic _mainWindowLogic,
            InvestmentRegisterLogic _investmentRegisterLogic,
            Household.AccountRow accountRow,
            int transID,
            InvestmentTransactionData _data)
            : base(_mainWindowLogic,  accountRow, transID, _data)
        {
            (investmentRegisterLogic, data) = (_investmentRegisterLogic, _data);

            ShowCapitalGains = new CommandBase(OnShowCapitalGains);
            ShowCapitalGains.SetCanExecute(data.Type == EInvestmentTransactionType.Sell || data.Type == EInvestmentTransactionType.SellAndTransferCash);

            // Security view
            SecuritiesView = (CollectionView)CollectionViewSource.GetDefaultView(securities);
            SecuritiesView.SortDescriptions.Add(new SortDescription("Symbol", ListSortDirection.Ascending));

            // Be notified when securities change
            mainWindowLogic.SecuritiesChanged += (s, e) => UpdateSecurities();
            UpdateSecurities();

            // Category view
            CategoriesOrTransferView = (CollectionView)CollectionViewSource.GetDefaultView(transfers);

            // Be notified when categories change
            mainWindowLogic.CategoriesChanged += (s, e) => UpdateCategories();
            UpdateCategories();

            GotoOtherSideOfTransfer.SetCanExecute(data.LineItem.CategoryAccountID != -1);
        }

        public InvestmentTransactionLogic(
            MainWindowLogic mainWindowLogic,
            InvestmentRegisterLogic investmentRegisterLogic,
             Household.AccountRow accountRow)
            : this(mainWindowLogic, investmentRegisterLogic, accountRow, TRANSID_NOT_COMMITTED,
                  new InvestmentTransactionData(DateTime.Today, "", "", ETransactionStatus.Pending, new LineItem(mainWindowLogic, -1, "", -1, -1, "", 0, false),
                    EInvestmentTransactionType.Dividends, -1, 0, 0, 0)) { }

        #endregion

        #region Logic Properties

        public bool IsCashIn => Household.InvestmentTransactionRow.CashIn(data.Type);

        public bool IsCashOut => Household.InvestmentTransactionRow.CashOut(data.Type);

        public bool IsSecurityIn => Household.InvestmentTransactionRow.SecurityIn(data.Type);

        public bool IsSecurityOut => Household.InvestmentTransactionRow.SecurityOut(data.Type);

        public override decimal AmountForCashBalance => (IsCashIn || IsCashOut) ? data.LineItem.Amount : 0;

        // For register to compute share balances:
        public int SecurityID => data.SecurityID;
        public decimal SecurityQuantityDecimal => data.SecurityQuantity;

        #endregion

        #region UI Properties

        //
        // Type of transaction
        //
        public string Type
        {
            get => EnumDescriptionAttribute.GetDescription(data.Type);
            set => SetType(EnumDescriptionAttribute.MatchDescription<EInvestmentTransactionType>(value), false);
        }

        //
        // Transaction description (generated, read-only)
        //
        public string Description => GetDescription();

        //
        // Security symbol
        //
        public string SecuritySymbol
        {
            get => GetSecuritySymbol(data.SecurityID);
            set => SetSecuritySymbol(value);
        }
        public int SecuritySymbolTabIndex => investmentTransactionType.SecuritySymbolTabIndex;
        public bool IsSecuritySymbolVisible => investmentTransactionType.IsSecuritySymbolVisible;

        private readonly WpfObservableRangeCollection<SecurityItem> securities = new WpfObservableRangeCollection<SecurityItem>();
        public CollectionView SecuritiesView { get; private set; }

        //
        // Security quantity
        //
        public decimal SecurityQuantity
        {
            get => data.SecurityQuantity;
            set { data.SecurityQuantity = value; RecomputeAmount(); OnAmountChanged(); }
        }
        public int SecurityPriceTabIndex => investmentTransactionType.SecurityPriceTabIndex;
        public bool IsSecurityPriceVisible => investmentTransactionType.IsSecurityPriceVisible;

        //
        // Security price
        //
        public decimal SecurityPrice
        {
            get => data.SecurityPrice;
            set { data.SecurityPrice = value; RecomputeAmount(); }
        }
        public int SecurityQuantityTabIndex => investmentTransactionType.SecurityQuantityTabIndex;
        public bool IsSecurityQuantityVisible => investmentTransactionType.IsSecurityQuantityVisible;

        //
        // Commission
        //
        public decimal Commission
        {
            get => data.Commission;
            set { data.Commission = value; RecomputeAmount(); OnAmountChanged(); }
        }
        public int CommissionTabIndex => investmentTransactionType.CommissionTabIndex;
        public bool IsCommissionVisible => investmentTransactionType.IsCommissionVisible;

        // Share balance
        // ShareBalanceString is the UI property, ShareBalance is updated by the logic
        private decimal shareBalance = decimal.MinValue;
        public decimal ShareBalance
        {
            get => shareBalance;
            set
            {
                if (shareBalance != value)
                {
                    shareBalance = value;
                    ShareBalanceString = shareBalance == decimal.MinValue ? "" : shareBalance.ToString("N4");
                    OnPropertyChanged(() => ShareBalanceString);
                }
            }
        }

        public string ShareBalanceString { get; private set; } = "";

        //
        // Amount
        //
        public override decimal Amount
        {
            get => data.PositiveAmount;
            set
            {
                if (data.PositiveAmount != value)
                {
                    data.PositiveAmount = value;
                    OnPropertyChanged(() => AmountState);
                }
                OnAmountChanged();
            }
        }
    
        public int AmountTabIndex => investmentTransactionType.AmountTabIndex;
        public bool IsAmountVisible => investmentTransactionType.IsAmountVisible;

        //
        // Category
        //
        public string Category
        {
            get => data.LineItem.Category;
            set
            {
                try
                {
                    data.LineItem.Category = value;
                }
                catch (ArgumentException e)
                {
                    mainWindowLogic.ErrorMessage(e.Message);
                }
            }
        }

        private readonly WpfObservableRangeCollection<CategoryItem> categories = new WpfObservableRangeCollection<CategoryItem>();
        private readonly WpfObservableRangeCollection<CategoryItem> transfers = new WpfObservableRangeCollection<CategoryItem>();
        public CollectionView CategoriesOrTransferView { get; private set; }

        public IEnumerable<CategoryItem> CategoriesSource =>
            investmentTransactionType.IsTransfer ? mainWindowLogic.Transfers : mainWindowLogic.Categories;

        public int CategoryTabIndex => investmentTransactionType.CategoryTabIndex;
        public bool IsCategoryVisible => investmentTransactionType.IsCategoryVisible;

        //
        // Command to show capital gains on a sale
        //
        public CommandBase ShowCapitalGains { get; }

        #endregion

        #region IEditable implementation

        public override void BeginEdit()
        {
            // Save existing data
            if (backup == null)
            {
                backup = new InvestmentTransactionData(data);
            }

            Console.WriteLine($"Begin edit transaction date {Date.ToShortDateString()} Type {Type} amount {data.LineItem.Amount}");

            // Setup the boxes
            SetType(data.Type, true);
        }

        public override void CancelEdit()
        {
            base.CancelEdit();

            // Restore data
            if (backup != null)
            {
                var _backup = backup as InvestmentTransactionData;

                if (data.Type != _backup.Type)
                {
                    SetType(_backup.Type, false);
                    OnPropertyChanged(() => Type);
                }

                if (data.SecurityID != _backup.SecurityID)
                {
                    data.SecurityID = _backup.SecurityID;
                    OnPropertyChanged(() => SecuritySymbol);
                }

                if (data.SecurityQuantity != _backup.SecurityQuantity)
                {
                    data.SecurityQuantity = _backup.SecurityQuantity;
                    OnPropertyChanged(() => SecurityQuantity);
                }

                if (data.SecurityPrice != _backup.SecurityPrice)
                {
                    data.SecurityPrice = _backup.SecurityPrice;
                    OnPropertyChanged(() => SecurityPrice);
                }

                if (data.Commission != _backup.Commission)
                {
                    data.Commission = _backup.Commission;
                    OnPropertyChanged(() => Commission);
                }

                OnPropertyChanged(() => Description);

                data.LineItem = new LineItem(_backup.LineItem);
                OnPropertyChanged(() => Amount);
                OnPropertyChanged(() => Category);

                //Console.WriteLine("backup = null from CANCEL");
                //Console.WriteLine(System.Environment.StackTrace);
                backup = null;
            }

            Console.WriteLine($"Cancel edit transaction date {Date.ToShortDateString()} Type {Type} amount {Amount}");
        }

        // Is there a change to commit?
        public override bool HasTransactionChanged => backup != null && !backup.Equals(data);

        // Returns if there is something to commit and if we need to move down
        public override bool DoesTransactionNeedComit
        {
            get
            {
                // Out of sequence
                if (backup == null)
                {
                    BeginEdit();
                    return false;
                }

                Console.WriteLine($"End edit transaction date {Date.ToShortDateString()} Type {Type} amount {Amount}");

                //
                // Check the changes
                //

                if (backup.Status == ETransactionStatus.Reconciled && data.Status != ETransactionStatus.Reconciled)
                {
                    if (!mainWindowLogic.YesNoQuestion("Are you sure you want to un-reconcile this transaction"))
                    {
                        CancelEdit();
                        BeginEdit();
                        return false;
                    }
                }

                var _backup = backup as InvestmentTransactionData;
                if (backup.Status == ETransactionStatus.Reconciled && _backup.LineItem.Amount != data.LineItem.Amount)
                {
                    if (!mainWindowLogic.YesNoQuestion("Are you sure you want to change the amount of this reconciled transaction"))
                    {
                        CancelEdit();
                        BeginEdit();
                        return false;
                    }
                }

                if (backup.Status == ETransactionStatus.Reconciled && _backup.SecurityQuantity != data.SecurityQuantity)
                {
                    if (!mainWindowLogic.YesNoQuestion("Are you sure you want to change the number of shares of this reconciled transaction"))
                    {
                        CancelEdit();
                        BeginEdit();
                        return false;
                    }
                }

                if (backup.Status == ETransactionStatus.Reconciled && _backup.SecurityID != data.SecurityID)
                {
                    if (!mainWindowLogic.YesNoQuestion("Are you sure you want to change the security of this reconciled transaction"))
                    {
                        CancelEdit();
                        BeginEdit();
                        return false;
                    }
                }

                if (data.PositiveAmount < 0)
                {
                    mainWindowLogic.ErrorMessage("PLease enter a positive amount");
                    return false;
                }

                // Check based on transaction type
                string error = investmentTransactionType.CheckData(data);
                if (error != null)
                {
                    mainWindowLogic.ErrorMessage(error);
                    return false;
                }

                return true;
            }
        }

        public override void EndEdit()
        {
            CommitTransactionToDataSet();

            // Notify the UI
            base.EndEdit();

            var _backup = backup as InvestmentTransactionData;
            if (data.Type != _backup.Type)
            {
                OnPropertyChanged(() => Type);
            }

            if (data.SecurityID != _backup.SecurityID)
            {
                OnPropertyChanged(() => SecuritySymbol);
            }

            if (data.SecurityQuantity != _backup.SecurityQuantity)
            {
                OnPropertyChanged(() => SecurityQuantity);
            }

            if (data.SecurityPrice != _backup.SecurityPrice)
            {
                OnPropertyChanged(() => SecurityPrice);
            }

            if (data.Commission != _backup.Commission)
            {
                OnPropertyChanged(() => Commission);
            }

            OnPropertyChanged(() => Description);

            if (data.LineItem.Category != _backup.LineItem.Category)
            {
                OnPropertyChanged(() => Category);
            }

            if (data.LineItem.Amount != _backup.LineItem.Amount)
            {
                OnPropertyChanged(() => Amount);
            }

            // Update context menu commands
            GotoOtherSideOfTransfer.SetCanExecute(data.LineItem.CategoryAccountID != -1);
            ShowCapitalGains.SetCanExecute(data.Type == EInvestmentTransactionType.Sell || data.Type == EInvestmentTransactionType.SellAndTransferCash);

            // Re-sort if needed
            investmentRegisterLogic.RegisterItems.Refresh();

            // Clear the backup
            //Console.WriteLine("backup = null from END EDIT");
            //Console.WriteLine(System.Environment.StackTrace);
            backup = null;
        }

        #endregion

        #region Actions

        private string GetDescription()
        {
            if (TransID == TRANSID_NOT_COMMITTED)
            {
                return "";
            }

            return Household.InvestmentTransactionRow.GetDescription(data.Type, data.PositiveAmount, SecuritySymbol, SecurityQuantity, SecurityPrice);
        }

        private void SetType(EInvestmentTransactionType value, bool force)
        {
            if (value == data.Type && !force)
            {
                return;
            }

            data.Type = value;
            investmentTransactionType = InvestmentTransactionType.GetInvestmentTransactionType(value);

            UpdateSecurities();
            OnPropertyChanged(() => Amount);

            // If switching from transfers to categories or vice versa
            var newCategoriesOrTransferView = (CollectionView)CollectionViewSource.GetDefaultView(investmentTransactionType.IsTransfer ? transfers : categories);
            if (CategoriesOrTransferView != newCategoriesOrTransferView)
            {
                CategoriesOrTransferView = newCategoriesOrTransferView;
                data.LineItem.Category = "";
                OnPropertyChanged(() => Category);
            }

            OnPropertyChanged(() => IsSecuritySymbolVisible);
            OnPropertyChanged(() => SecuritySymbolTabIndex);

            OnPropertyChanged(() => IsSecurityQuantityVisible);
            OnPropertyChanged(() => SecurityQuantityTabIndex);

            OnPropertyChanged(() => IsSecurityPriceVisible);
            OnPropertyChanged(() => SecurityPriceTabIndex);

            OnPropertyChanged(() => IsCommissionVisible);
            OnPropertyChanged(() => CommissionTabIndex);

            OnPropertyChanged(() => IsAmountVisible);
            OnPropertyChanged(() => AmountTabIndex);

            OnPropertyChanged(() => IsCategoryVisible);
            OnPropertyChanged(() => CategoryTabIndex);
            OnPropertyChanged(() => CategoriesSource);
        }

        private string GetSecuritySymbol(int id)
        {
            string result = "";

            if (id >= 0)
            {
                var household = mainWindowLogic.Household;
                var security = household.Security.FindByID(id);
                result = security.Symbol;
            }

            return result;
        }

        private void SetSecuritySymbol(string value)
        {
            int id = -1;

            if (!string.IsNullOrWhiteSpace(value))
            {
                var household = mainWindowLogic.Household;
                var securityRow = household.Security.GetBySymbol(value);
                if (securityRow != null)
                {
                    id = securityRow.ID; 
                }
            }

            data.SecurityID = id;
        }

        // For buy/sell, recompute the amount when the #shares, share price or commission is changed
        private void RecomputeAmount()
        {
            if (data.Type == EInvestmentTransactionType.Buy || data.Type == EInvestmentTransactionType.BuyFromTransferredCash ||
                data.Type == EInvestmentTransactionType.Sell || data.Type == EInvestmentTransactionType.SellAndTransferCash)
            {
                // Compute amount
                data.PositiveAmount = data.SecurityQuantity * data.SecurityPrice - data.Commission;
                OnPropertyChanged(() => Amount);
            }
        }

        // For reinvestment, recompute the share price every time the amount or number of shares is changed
        private void OnAmountChanged()
        {
            if (data.Type == EInvestmentTransactionType.ReinvestDividends || data.Type == EInvestmentTransactionType.ReinvestShortTermCapitalGains ||
                data.Type == EInvestmentTransactionType.ReinvestMediumTermCapitalGains || data.Type == EInvestmentTransactionType.ReinvestLongTermCapitalGains)
            {
                // Compute share price
                if (data.SecurityQuantity > 0)
                {
                    data.SecurityPrice = data.PositiveAmount / data.SecurityQuantity;
                    OnPropertyChanged(() => SecurityPrice);
                }
            }
        }

        // When changing date, recompute list of securities present in the account
        protected override void OnDateChanged()
        {
            UpdateSecurities();
        }

        private void UpdateSecurities()
        {
            if (investmentTransactionType != null && investmentTransactionType.IsFilteringSecurity)
            {
                var household = mainWindowLogic.Household;
                var portfolio = accountRow.GetPortfolio(data.Date);
                securities.ReplaceRange(portfolio.GetSecurities().Select<int, SecurityItem>(sid => mainWindowLogic.Securities.First(s => s.ID == sid)));
            }
            else
            {
                securities.ReplaceRange(mainWindowLogic.Securities);
            }
        }

        private void UpdateCategories()
        {
            categories.ReplaceRange(mainWindowLogic.Categories);
            transfers.ReplaceRange(mainWindowLogic.Transfers);
            data.LineItem.UpdateCategoryName();
            OnPropertyChanged(() => Category);
        }

        private void CommitTransactionToDataSet()
        {
            var household = mainWindowLogic.Household;

            // Remember impacted accounts
            var impactedAccounts = new List<int>
            {
                accountRow.ID
            };

            //
            // Remove irrelevant input based on type
            //
            investmentTransactionType.CleanupData(data);

            //
            // Save to DB
            //
            var securityRow = data.SecurityID < 0 ? null : household.Security.FindByID(data.SecurityID);

            if (TransID == TRANSID_NOT_COMMITTED)
            {
                // Create new transaction row
                var transactionRow = household.Transaction.Add(
                    accountRow, 
                    data.Date,
                    data.Payee,
                    data.Memo,
                    data.Status, 
                    household.Checkpoint.GetMostRecentCheckpoint(),
                    ETransactionType.Regular);
                TransID = transactionRow.ID;

                // Create new investment transaction row
                household.InvestmentTransaction.Add(transactionRow, data.Type, securityRow, data.SecurityPrice, data.SecurityQuantity, data.Commission);

                // Create the line item
                CreateLineItemInDB(data.LineItem, transactionRow, impactedAccounts);
            }
            else
            {
                // Update transaction row
                var transactionRow = household.Transaction.Update(
                    TransID, 
                    accountRow,
                    data.Date,
                    data.Memo,
                    data.Payee,
                    data.Status, 
                    household.Checkpoint.GetMostRecentCheckpoint(),
                    ETransactionType.Regular);

                // Update the line item
                UpdateLineItemInDB(data.LineItem, transactionRow, impactedAccounts);

                // Update investment transaction row
                household.InvestmentTransaction.Update(transactionRow, data.Type, securityRow, data.SecurityPrice, data.SecurityQuantity, data.Commission);
            }

            mainWindowLogic.CommitChanges();

            mainWindowLogic.UpdateAccountNamesAndBalances(impactedAccounts);
        }

        private void OnShowCapitalGains()
        {
            mainWindowLogic.GuiServices.ShowDialog(new ShowCapitalGainsLogic(mainWindowLogic, TransID));
        }

        #endregion

        #region Supporting classes

        public class InvestmentTransactionData : AbstractTransactionLogic.BaseTransactionData
        {
            // Construct from scratch
            public InvestmentTransactionData(
                DateTime date,
                string payee,
                string memo,
                ETransactionStatus status,
                LineItem lineItem,
                EInvestmentTransactionType type,
                int securityID,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
                : base(date, payee, memo, status) =>
                (LineItem, Type, SecurityID, SecurityPrice, SecurityQuantity, Commission) =
                    (lineItem, type, securityID, securityPrice, securityQuantity, commission);

            // Clone
            public InvestmentTransactionData(InvestmentTransactionData src)
                : base(src) =>
                (Type, SecurityID, SecurityPrice, SecurityQuantity, Commission, LineItem) =
                    (src.Type, src.SecurityID, src.SecurityPrice, src.SecurityQuantity, src.Commission, new LineItem(src.LineItem));

            // Properties
            public EInvestmentTransactionType Type;
            public int SecurityID;
            public decimal SecurityPrice;
            public decimal SecurityQuantity;
            public decimal Commission;

            public LineItem LineItem;

            // The amount corrected for sign - we show only positive values in the investment register
            public decimal PositiveAmount
            {
                get => LineItem.Amount * AmountSign;
                set => LineItem.Amount = value * AmountSign;
            }

            // transaction types that have negative amounts in the line item
            private decimal AmountSign =>
                Type == EInvestmentTransactionType.CashOut ||
                Type == EInvestmentTransactionType.TransferCashOut ||
                Type == EInvestmentTransactionType.SellAndTransferCash ||
                Type == EInvestmentTransactionType.Buy ||
                Type == EInvestmentTransactionType.TransferDividends ||
                Type == EInvestmentTransactionType.TransferLongTermCapitalGains ||
                Type == EInvestmentTransactionType.TransferShortTermCapitalGains
                ? -1 : 1;

            public override bool Equals(object obj)
            {
                return
                    obj is InvestmentTransactionData o &&
                    base.Equals(o) &&
                    o.Type == Type &&
                    o.SecurityID == SecurityID &&
                    o.SecurityPrice == SecurityPrice &&
                    o.SecurityQuantity == SecurityQuantity &&
                    o.Commission == Commission &&
                    o.LineItem.Equals(LineItem);
            }


            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        #endregion
    }
}
