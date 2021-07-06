using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Collections;
using BanaData.Logic.Dialogs;

namespace BanaData.Logic.Main
{
    public class InvestmentRegisterLogic : AbstractRegisterLogic
    {
        #region Constructor

        public InvestmentRegisterLogic(MainWindowLogic mainWindowLogic)
            : base(mainWindowLogic)
        {
            // Create list of all transfer categories
            UpdateTransfersSource();

            // Create commands
            ShowHoldingsCommand = new CommandBase(OnShowHoldingsCommand);

            // Create column width manager
            Widths = new ColumnWidths(mainWindowLogic);
        }

        #endregion

        #region UI properties

        // Command to show holdings
        public CommandBase ShowHoldingsCommand { get; }

        // Investment transaction type list
        public CollectionView TypesSource => mainWindowLogic.InvestmentTransactionTypesView;

        // Security list
        public CollectionView SecuritiesView => mainWindowLogic.SecuritiesView;

        // Transfers
        private readonly WpfObservableRangeCollection<CategoryItem> transfersSource = new WpfObservableRangeCollection<CategoryItem>();
        public IEnumerable<CategoryItem> TransfersSource => transfersSource;

        // Column widths
        public ColumnWidths Widths { get; }

        #endregion

        #region Actions & Hooks for abstract base class

        // Create list of all transfer categories
        public void UpdateTransfersSource()
        {
            transfersSource.ReplaceRange(mainWindowLogic.Categories.Where(c => c.AccountID >= 0));
        }

        // Routine to create a transaction from the DB
        protected override AbstractTransactionLogic CreateTransactionFromDB(Household.AccountsRow accountRow, Household.TransactionsRow transRow, List<LineItem> lineItems)
        {
            // Get investment transaction info
            var investmentTransRow = transRow.GetInvestmentTransaction();

            // Create data
            var transactionData = new InvestmentTransactionLogic.InvestmentTransactionData(
                transRow.Date,
                transRow.IsPayeeNull() ? "" : transRow.Payee,
                transRow.IsMemoNull() ? "" : transRow.Memo,
                transRow.Status,
                lineItems,
                investmentTransRow.Type,
                investmentTransRow.IsSecurityIDNull() ? -1 : investmentTransRow.SecurityID,
                investmentTransRow.IsSecurityPriceNull() ?  0 : investmentTransRow.SecurityPrice,
                investmentTransRow.IsSecurityQuantityNull() ? 0 : investmentTransRow.SecurityQuantity,
                investmentTransRow.Commission);

            var investmentTransaction = new InvestmentTransactionLogic(mainWindowLogic, accountID, transRow.ID, transactionData);

            return investmentTransaction;
        }

        protected override AbstractTransactionLogic CreateEmptyTransaction()
        {
            return new InvestmentTransactionLogic(mainWindowLogic, accountID);
        }

        // Create a mirror pseudo-transaction for transfers
        protected override AbstractTransactionLogic CreateMirrorTransaction(
            Household.AccountsRow accountRow,
            Household.LineItemsRow otherLineItemRow)
        {
            var otherTransRow = otherLineItemRow.TransactionsRow;
            var otherAccountRow = otherTransRow.AccountsRow;

            var lineItem = new LineItem(
                mainWindowLogic,
                otherLineItemRow.ID,        // A bit confusing, but we store the transfer line item ID here to be able to modify the TransferStatus
                "[" + otherAccountRow.Name + "]",
                -1,
                otherAccountRow.ID,
                otherLineItemRow.IsMemoNull() ? "" : otherLineItemRow.Memo,
                -otherLineItemRow.Amount, false);

            var transactionData = new InvestmentTransactionLogic.InvestmentTransactionData(
                otherTransRow.Date,
                "",
                otherTransRow.IsMemoNull() ? "" : otherTransRow.Memo,
                otherLineItemRow.TransferStatus,
                new LineItem[] { lineItem },
                lineItem.Amount > 0 ? EInvestmentTransactionType.TransferCashIn : EInvestmentTransactionType.TransferCashOut,
                -1, 0, 0, 0);

            var investmentTransaction = new InvestmentTransactionLogic(mainWindowLogic, accountID, -2, transactionData);

            return investmentTransaction;
        }

        // Override to compute share balances in addition to cash balance
        public override void RecomputeBalances()
        {
            // Compute cash balance
            base.RecomputeBalances();

            // Compute share balances

            // securityId -> share balance
            var dico = new Dictionary<int, decimal>();

            foreach (InvestmentTransactionLogic itl in Transactions)
            {
                decimal balance = decimal.MinValue;
                var secuID = itl.SecurityID;
 
                if (itl.IsSecurityIn)
                {
                    balance = itl.SecurityQuantityDecimal;
                    if (dico.ContainsKey(secuID))
                    {
                        balance += dico[secuID];
                        dico.Remove(secuID);
                    }
                    dico.Add(itl.SecurityID, balance);
                }
                else if (itl.IsSecurityOut)
                {
                    balance = -itl.SecurityQuantityDecimal;
                    if (dico.ContainsKey(secuID))
                    {
                        balance += dico[secuID];
                        dico.Remove(secuID);
                    }
                    dico.Add(itl.SecurityID, balance);
                }

                itl.ShareBalance = balance;
            }
        }

        // Show holdings
        private void OnShowHoldingsCommand()
        {
            var logic = new ShowHoldingsLogic(mainWindowLogic, accountID);
            mainWindowLogic.GuiServices.ShowDialog(logic);
        }

        #endregion

        #region Supporting classes

        public class ColumnWidths : LogicBase
        {
            private readonly MainWindowLogic mainWindowLogic;

            public ColumnWidths(MainWindowLogic _mainWindowLogic) => mainWindowLogic = _mainWindowLogic;

            public double WidthOfStatusColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfStatusColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfStatusColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfStatusColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfStatusColumn);
                    }
                }
            }

            public double WidthOfDateColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfDateColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfDateColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfDateColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDateColumn);
                    }
                }
            }

            public double WidthOfTypeColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfTypeColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfTypeColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfTypeColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfTypeColumn);
                    }
                }
            }

            public double WidthOfDescriptionColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfDescriptionColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfDescriptionColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfDescriptionColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDescriptionColumn);
                    }
                }
            }

            public double WidthOfMemoColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfMemoColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfMemoColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfMemoColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfMemoColumn);
                    }
                }
            }

            public double WidthOfCategoryColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfCategoryColumn);
                    }
                }
            }

            public double WidthOfSecuritySymbolColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfSecuritySymbolColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfSecuritySymbolColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfSecuritySymbolColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfSecuritySymbolColumn);
                    }
                }
            }

            public double WidthOfSecurityQuantityColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfSecurityQuantityColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfSecurityQuantityColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfSecurityQuantityColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfSecurityQuantityColumn);
                    }
                }
            }

            public double WidthOfSecurityPriceColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfSecurityPriceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfSecurityPriceColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfSecurityPriceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfSecurityPriceColumn);
                    }
                }
            }

            public double WidthOfCommissionColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfCommissionColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfCommissionColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfCommissionColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfCommissionColumn);
                    }
                }
            }

            public double WidthOfSecurityBalanceColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfSecurityBalanceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfSecurityBalanceColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfSecurityBalanceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfSecurityBalanceColumn);
                    }
                }
            }

            public double WidthOfAmountColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfAmountColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfAmountColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfAmountColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfAmountColumn);
                    }
                }
            }

            public double WidthOfBalanceColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfBalanceColumn);
                    }
                }
            }

        }

        #endregion
    }
}
