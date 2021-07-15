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
            // Create column width manager
            Widths = new ColumnWidths(mainWindowLogic);
        }

        #endregion

        #region UI properties

        // Commands to show holdings and rebalance dashboard 
        public CommandBase ShowHoldingsCommand => mainWindowLogic.MainMenuLogic.ShowHoldings;
        public CommandBase ShowRebalanceCommand => mainWindowLogic.MainMenuLogic.ShowRebalance;

        // Investment transaction type list
        public CollectionView TypesSource => mainWindowLogic.InvestmentTransactionTypesView;

        // Column widths
        public ColumnWidths Widths { get; }

        #endregion

        #region Actions & Hooks for abstract base class

        // Routine to create a transaction from the DB
        protected override AbstractTransactionLogic CreateTransactionFromDB(Household.AccountRow accountRow, Household.TransactionRow transRow, List<LineItem> lineItems)
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

            var investmentTransaction = new InvestmentTransactionLogic(mainWindowLogic, accountRow, transRow.ID, transactionData);

            return investmentTransaction;
        }

        protected override AbstractTransactionLogic CreateEmptyTransaction()
        {
            return new InvestmentTransactionLogic(mainWindowLogic, accountRow);
        }

        // Create a mirror pseudo-transaction for transfers
        protected override AbstractTransactionLogic CreateMirrorTransaction(
            Household.AccountRow accountRow,
            Household.LineItemRow otherLineItemRow)
        {
            var otherTransRow = otherLineItemRow.TransactionRow;
            var otherAccountRow = otherTransRow.AccountRow;

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

            var investmentTransaction = new InvestmentTransactionLogic(mainWindowLogic,  accountRow, AbstractTransactionLogic.TRANSID_TRANSFER_FILLIN, transactionData);

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
