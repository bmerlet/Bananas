using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.Attributes;
using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Dialogs;

namespace BanaData.Logic.Main
{
    public class InvestmentTransactionLogic : AbstractTransactionLogic
    {
        #region Private fields

        // Parent logics
        //private readonly InvestmentRegisterLogic investmentRegisterLogic;

        // Our data
        private new readonly InvestmentTransactionData data;

        #endregion

        #region Constructors

        public InvestmentTransactionLogic(
            MainWindowLogic _mainWindowLogic,
            int _accountID,
            int transID,
            InvestmentTransactionData _data)
            : base(_mainWindowLogic, _accountID, transID, _data)
        {
            data = _data;

            ShowCapitalGains = new CommandBase(OnShowCapitalGains);
            ShowCapitalGains.SetCanExecute(data.Type == EInvestmentTransactionType.Sell || data.Type == EInvestmentTransactionType.SellAndTransferCash);
        }

        public InvestmentTransactionLogic(
            MainWindowLogic _mainWindowLogic,
            int _accountID)
            : this(_mainWindowLogic, _accountID, AbstractTransactionLogic.TRANSID_NOT_COMMITTED,
                  new InvestmentTransactionData(DateTime.Today, "", "", ETransactionStatus.Pending, new LineItem[] { new LineItem(_mainWindowLogic, -1, "", -1, -1, "", 0, false) },
                    EInvestmentTransactionType.None, -1, 0, 0, 0)) { }

        #endregion

        #region Logic Properties

        public bool IsCashIn => Household.InvestmentTransactionsRow.CashIn(data.Type);

        public bool IsCashOut => Household.InvestmentTransactionsRow.CashOut(data.Type);

        public bool IsSecurityIn => Household.InvestmentTransactionsRow.SecurityIn(data.Type);

        public bool IsSecurityOut => Household.InvestmentTransactionsRow.SecurityOut(data.Type);

        public override decimal AmountForCashBalance => (IsCashIn || IsCashOut) ? data.Amount : 0;

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
        public int SecuritySymbolColumnNumber { get; private set; }
        public int SecuritySymbolTabIndex { get; private set; }
        public bool IsSecuritySymbolVisible { get; private set; }

        //
        // Security price
        //
        public decimal SecurityPrice
        {
            get => data.SecurityPrice;
            set => data.SecurityPrice = value;
        }
        public int SecurityQuantityColumnNumber { get; private set; }
        public int SecurityQuantityTabIndex { get; private set; }
        public bool IsSecurityQuantityVisible { get; private set; }

        //
        // Security quantity
        //
        public decimal SecurityQuantity
        {
            get => data.SecurityQuantity;
            set => data.SecurityQuantity = value;
        }
        public int SecurityPriceColumnNumber { get; private set; }
        public int SecurityPriceTabIndex { get; private set; }
        public bool IsSecurityPriceVisible { get; private set; }

        //
        // Commission
        //
        public decimal Commission
        {
            get => data.Commission;
            set => data.Commission = value;
        }
        public int CommissionColumnNumber { get; private set; }
        public int CommissionTabIndex { get; private set; }
        public bool IsCommissionVisible { get; private set; }

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
        // Amount supplement
        //
        public int AmountColumnNumber { get; private set; }
        public int AmountTabIndex { get; private set; }
        public bool IsAmountVisible { get; private set; }

        //
        // Category supplement
        //
        public int CategoryColumnNumber { get; private set; }
        public int CategoryTabIndex { get; private set; }
        public bool IsCategoryVisible { get; private set; }

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

            Console.WriteLine($"Begin edit transaction date {Date.ToShortDateString()} Type {Type} amount {Amount}");

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

                backup = null;
            }

            Console.WriteLine($"Cancel edit transaction date {Date.ToShortDateString()} Type {Type} amount {Payment}");
        }

        // Returns if there is something to commit and if we need to move down
        public override (bool needCommit, bool moveDown) ValidateEndEdit()
        {
            // Out of sequence
            if (backup == null)
            {
                return (false, TransID >= 0);
            }

            // No change
            if (backup.Equals(data))
            {
                if (TransID >= 0)
                {
                    backup = null;
                    return (false, true);
                }
                else
                {
                    return (false, false);
                }
            }

            Console.WriteLine($"End edit transaction date {Date.ToShortDateString()} Type {Type} amount {Payment}");

            //
            // Check the changes
            //

            // Dead end of transfer
            if (TransID == TRANSID_TRANSFER_FILLIN)
            {
                // We only allow changing the status, as it is stored in the transfer line item
                var tmpData = new InvestmentTransactionData(data) { Status = backup.Status };
                if (!tmpData.Equals(backup))
                {
                    mainWindowLogic.ErrorMessage("Cannot edit this end of the transfer - please edit the other end");
                    CancelEdit();
                    BeginEdit();
                    return (false, false);
                }
            }

            // No type
            if (data.Type == EInvestmentTransactionType.None)
            {
                mainWindowLogic.ErrorMessage("Please choose a transaction type");
                return (false, false);
            }

            // No quantity ZZZZZZZZZZZZZZZZ More

            if (backup.Status == ETransactionStatus.Reconciled && data.Status != ETransactionStatus.Reconciled)
            {
                if (!mainWindowLogic.YesNoQuestion("Are you sure you want to un-reconcile this transaction"))
                {
                    CancelEdit();
                    BeginEdit();
                    return (false, false);
                }
            }

            if (backup.Status == ETransactionStatus.Reconciled && backup.Amount != data.Amount)
            {
                if (!mainWindowLogic.YesNoQuestion("Are you sure you want to change the amount of this reconciled transaction"))
                {
                    CancelEdit();
                    BeginEdit();
                    return (false, false);
                }
            }

            var _backup = backup as InvestmentTransactionData;
            if (backup.Status == ETransactionStatus.Reconciled && _backup.SecurityQuantity != data.SecurityQuantity)
            {
                if (!mainWindowLogic.YesNoQuestion("Are you sure you want to change the number of shares of this reconciled transaction"))
                {
                    CancelEdit();
                    BeginEdit();
                    return (false, false);
                }
            }

            if (backup.Status == ETransactionStatus.Reconciled && _backup.SecurityID != data.SecurityID)
            {
                if (!mainWindowLogic.YesNoQuestion("Are you sure you want to change the security of this reconciled transaction"))
                {
                    CancelEdit();
                    BeginEdit();
                    return (false, false);
                }
            }

            return (true, true);
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

            // Clear the backup
            backup = null;
        }

        #endregion

        #region Actions

        private string GetDescription()
        {
            string desc = "";

            switch(data.Type)
            {
                case EInvestmentTransactionType.Cash:
                    desc = "Opening balance";
                    break;

                case EInvestmentTransactionType.InterestIncome:
                    desc = $"Received ${AmountString} in interest";
                    break;

                case EInvestmentTransactionType.TransferCash:
                case EInvestmentTransactionType.TransferCashIn:
                    desc = $"Transfered ${AmountString} in";
                    break;

                case EInvestmentTransactionType.TransferCashOut:
                    desc = $"Transfered ${AmountString} out";
                    break;

                case EInvestmentTransactionType.TransferMiscellaneousIncomeIn:
                    desc = $"Transfer ${AmountString}";
                    break;

                case EInvestmentTransactionType.SharesIn:
                    desc = $"Received {SecurityQuantity} {SecuritySymbol} @ ${SecurityPrice}";
                    break;

                case EInvestmentTransactionType.SharesOut:
                    desc = $"Lost {SecurityQuantity} {SecuritySymbol} @ ${SecurityPrice}";
                    break;

                case EInvestmentTransactionType.Buy:
                case EInvestmentTransactionType.BuyFromTransferredCash:
                    desc = $"Bought {SecurityQuantity} {SecuritySymbol} @ ${SecurityPrice}";
                    break;

                case EInvestmentTransactionType.Sell:
                case EInvestmentTransactionType.SellAndTransferCash:
                    desc = $"Sold {SecurityQuantity} {SecuritySymbol} @ ${SecurityPrice}";
                    break;

                case EInvestmentTransactionType.Dividends:
                case EInvestmentTransactionType.TransferDividends:
                    desc = $"Received ${AmountString} in dividends from {SecuritySymbol}";
                    break;

                case EInvestmentTransactionType.ReinvestDividends:
                case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                    desc = $"Reinvested ${AmountString} as {SecurityQuantity} shares of {SecuritySymbol}";
                    break;

                case EInvestmentTransactionType.ShortTermCapitalGains:
                case EInvestmentTransactionType.TransferShortTermCapitalGains:
                    desc = $"Received ${AmountString} in ST CG from {SecuritySymbol}";
                    break;

                case EInvestmentTransactionType.LongTermCapitalGains:
                case EInvestmentTransactionType.TransferLongTermCapitalGains:
                    desc = $"Received ${AmountString} in LT CG from {SecuritySymbol}";
                    break;

                case EInvestmentTransactionType.Grant:
                case EInvestmentTransactionType.Vest:
                case EInvestmentTransactionType.Exercise:
                case EInvestmentTransactionType.Expire:
                    desc = $"{EnumDescriptionAttribute.GetDescription(data.Type)}: Not supported";
                    break;
            }

            return desc;
        }

        private void SetType(EInvestmentTransactionType value, bool force)
        {
            if (value == data.Type && !force)
            {
                return;
            }

            data.Type = value;

            int column = 4;

            switch (data.Type)
            {
                case EInvestmentTransactionType.Cash:
                case EInvestmentTransactionType.InterestIncome:
                    column = ShowSecuritySymbol(false, column);
                    column = ShowSecurityTextBoxes(false, false, column);
                    column = ShowAmountBox(true, column);
                    ShowTransferBox(false, column);
                    break;

                case EInvestmentTransactionType.TransferCash:
                case EInvestmentTransactionType.TransferCashIn:
                case EInvestmentTransactionType.TransferCashOut:
                case EInvestmentTransactionType.TransferMiscellaneousIncomeIn:
                    column = ShowSecuritySymbol(false, column);
                    column = ShowSecurityTextBoxes(false, false, column);
                    column = ShowAmountBox(true, column);
                    ShowTransferBox(true, column);
                    break;

                case EInvestmentTransactionType.SharesIn:
                case EInvestmentTransactionType.SharesOut:
                    column = ShowSecuritySymbol(true, column);
                    column = ShowSecurityTextBoxes(true, false, column);
                    column = ShowAmountBox(false, column);
                    ShowTransferBox(false, column);
                    break;

                case EInvestmentTransactionType.Buy:
                case EInvestmentTransactionType.Sell:
                    column = ShowSecuritySymbol(true, column);
                    column = ShowSecurityTextBoxes(true, true, column);
                    column = ShowAmountBox(true, column);
                    ShowTransferBox(false, column);
                    break;

                case EInvestmentTransactionType.BuyFromTransferredCash:
                case EInvestmentTransactionType.SellAndTransferCash:
                    column = ShowSecuritySymbol(true, column);
                    column = ShowSecurityTextBoxes(true, true, column);
                    column = ShowAmountBox(true, column);
                    ShowTransferBox(true, column);
                    break;

                case EInvestmentTransactionType.Dividends:
                case EInvestmentTransactionType.ShortTermCapitalGains:
                case EInvestmentTransactionType.LongTermCapitalGains:
                    column = ShowSecuritySymbol(true, column);
                    column = ShowSecurityTextBoxes(false, false, column);
                    column = ShowAmountBox(true, column);
                    ShowTransferBox(false, column);
                    break;

                case EInvestmentTransactionType.TransferDividends:
                case EInvestmentTransactionType.TransferShortTermCapitalGains:
                case EInvestmentTransactionType.TransferLongTermCapitalGains:
                    column = ShowSecuritySymbol(true, column);
                    column = ShowSecurityTextBoxes(false, false, column);
                    column = ShowAmountBox(true, column);
                    ShowTransferBox(true, column);
                    break;

                case EInvestmentTransactionType.ReinvestDividends:
                case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                    column = ShowAmountBox(true, column);
                    column = ShowSecuritySymbol(true, column);
                    column = ShowSecurityTextBoxes(true, false, column);
                    ShowTransferBox(false, column);
                    break;

                case EInvestmentTransactionType.Grant:
                case EInvestmentTransactionType.Vest:
                case EInvestmentTransactionType.Exercise:
                case EInvestmentTransactionType.Expire:
                case EInvestmentTransactionType.None:
                    column = ShowAmountBox(false, column);
                    column = ShowSecuritySymbol(false, column);
                    column = ShowSecurityTextBoxes(false, false, column);
                    ShowTransferBox(false, column);
                    break;
            }
        }

        private int ShowSecuritySymbol(bool show, int column)
        {
            IsSecuritySymbolVisible = show;
            OnPropertyChanged(() => IsSecuritySymbolVisible);

            if (show)
            {
                SecuritySymbolTabIndex = column - 1;
                SecuritySymbolColumnNumber = column++;
                OnPropertyChanged(() => SecuritySymbolColumnNumber);
                OnPropertyChanged(() => SecuritySymbolTabIndex);
            }

            return column;
        }

        private int ShowSecurityTextBoxes(bool show, bool showCommission, int column)
        {
            IsSecurityQuantityVisible = show;
            IsSecurityPriceVisible = show;
            IsCommissionVisible = showCommission;

            OnPropertyChanged(() => IsSecurityQuantityVisible);
            OnPropertyChanged(() => IsSecurityPriceVisible);
            OnPropertyChanged(() => IsCommissionVisible);

            if (show)
            {
                SecurityQuantityTabIndex = column - 1;
                SecurityQuantityColumnNumber = column++;
                SecurityPriceTabIndex = column - 1;
                SecurityPriceColumnNumber = column++;

                OnPropertyChanged(() => SecurityQuantityColumnNumber);
                OnPropertyChanged(() => SecurityQuantityTabIndex);
                OnPropertyChanged(() => SecurityPriceColumnNumber);
                OnPropertyChanged(() => SecurityPriceTabIndex);

                if (showCommission)
                {
                    CommissionTabIndex = column - 1;
                    CommissionColumnNumber = column++;
                    OnPropertyChanged(() => CommissionColumnNumber);
                    OnPropertyChanged(() => CommissionTabIndex);
                }
            }

            return column;
        }

        private int ShowAmountBox(bool show, int column)
        {
            IsAmountVisible = show;
            OnPropertyChanged(() => IsAmountVisible);

            if (show)
            {
                AmountTabIndex = column - 1;
                AmountColumnNumber = column++;
                OnPropertyChanged(() => AmountColumnNumber);
                OnPropertyChanged(() => AmountTabIndex);
            }

            return column;
        }

        private int ShowTransferBox(bool show, int column)
        {
            IsCategoryVisible = show;
            OnPropertyChanged(() => IsCategoryVisible);

            if (show)
            {
                CategoryTabIndex = column - 1;
                CategoryColumnNumber = column++;
                OnPropertyChanged(() => CategoryColumnNumber);
                OnPropertyChanged(() => CategoryTabIndex);
            }

            return column;
        }

        private string GetSecuritySymbol(int id)
        {
            string result = "";

            if (id >= 0)
            {
                var household = mainWindowLogic.Household;
                var security = household.Securities.FindByID(id);
                result = security.IsSymbolNull() ? "" : security.Symbol;
            }

            return result;
        }

        private void SetSecuritySymbol(string value)
        {
            int id = -1;

            if (!string.IsNullOrWhiteSpace(value))
            {
                var household = mainWindowLogic.Household;
                var securityRow = household.Securities.GetBySymbol(value);
                if (securityRow != null)
                {
                    id = securityRow.ID; 
                }
            }

            data.SecurityID = id;
        }

        private void CommitTransactionToDataSet()
        {
            var household = mainWindowLogic.Household;
            var accountRow = household.Accounts.FindByID(accountID);

            //
            // Remove irrelevant input based on type
            //
            switch (data.Type)
            {
                case EInvestmentTransactionType.Cash:
                case EInvestmentTransactionType.InterestIncome:
                    data.SecurityID = -1;
                    data.SecurityPrice = 0;
                    data.SecurityQuantity = 0;
                    data.Commission = 0;
                    data.LineItems[0].Category = "";
                    break;

                case EInvestmentTransactionType.TransferCash:
                case EInvestmentTransactionType.TransferCashIn:
                case EInvestmentTransactionType.TransferCashOut:
                case EInvestmentTransactionType.TransferMiscellaneousIncomeIn:
                    data.SecurityID = -1;
                    data.SecurityPrice = 0;
                    data.SecurityQuantity = 0;
                    data.Commission = 0;
                    break;

                case EInvestmentTransactionType.SharesIn:
                case EInvestmentTransactionType.SharesOut:
                    data.Commission = 0;
                    data.LineItems[0].Amount = 0;
                    data.LineItems[0].Category = "";
                    break;

                case EInvestmentTransactionType.Buy:
                case EInvestmentTransactionType.Sell:
                    data.LineItems[0].Category = "";
                    break;

                case EInvestmentTransactionType.BuyFromTransferredCash:
                case EInvestmentTransactionType.SellAndTransferCash:
                    break;

                case EInvestmentTransactionType.Dividends:
                case EInvestmentTransactionType.ShortTermCapitalGains:
                case EInvestmentTransactionType.LongTermCapitalGains:
                    data.SecurityPrice = 0;
                    data.SecurityQuantity = 0;
                    data.Commission = 0;
                    data.LineItems[0].Category = "";
                    break;

                case EInvestmentTransactionType.TransferDividends:
                case EInvestmentTransactionType.TransferShortTermCapitalGains:
                case EInvestmentTransactionType.TransferLongTermCapitalGains:
                    data.SecurityPrice = 0;
                    data.SecurityQuantity = 0;
                    data.Commission = 0;
                    break;

                case EInvestmentTransactionType.ReinvestDividends:
                case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                    data.Commission = 0;
                    data.LineItems[0].Category = "";
                    break;

                case EInvestmentTransactionType.Grant:
                case EInvestmentTransactionType.Vest:
                case EInvestmentTransactionType.Exercise:
                case EInvestmentTransactionType.Expire:
                case EInvestmentTransactionType.None:
                    data.SecurityID = -1;
                    data.SecurityPrice = 0;
                    data.SecurityQuantity = 0;
                    data.Commission = 0;
                    data.LineItems[0].Amount = 0;
                    data.LineItems[0].Category = "";
                    break;
            }

            //
            // Save to DB
            //
            var securityRow = data.SecurityID < 0 ? null : household.Securities.FindByID(data.SecurityID);

            if (TransID == TRANSID_NOT_COMMITTED)
            {
                // Create new transaction row
                var transactionRow = household.Transactions.Add(accountRow, data.Date, data.Payee, data.Memo, data.Status);
                TransID = transactionRow.ID;

                // Create new investment transaction row
                household.InvestmentTransactions.Add(transactionRow, data.Type, securityRow, data.SecurityPrice, data.SecurityQuantity, data.Commission);

                // Create the line item
                var li = data.LineItems[0];
                var liRow = household.LineItems.Add(transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount);
                li.ID = liRow.ID;
            }
            else if (TransID == TRANSID_TRANSFER_FILLIN)
            {
                // Modification of status for transfer pseudo-transaction
                // The status for the transactionless end of the transfer is kept in the transfer line item row 
                var liRow = household.LineItems.FindByID(data.LineItems[0].ID);
                liRow.TransferStatus = data.Status;
            }
            else
            {
                // Update transaction row
                var transactionRow = household.Transactions.Update(TransID, accountRow, data.Date, data.Memo, data.Payee, data.Status);

                // Update investment transaction
                household.InvestmentTransactions.Update(transactionRow, data.Type, securityRow, data.SecurityPrice, data.SecurityQuantity, data.Commission);

                // Update the line item
                var li = data.LineItems[0];
                var liRow = household.LineItems.FindByID(li.ID);
                household.LineItems.Update(liRow, transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount);
            }

            mainWindowLogic.CommitChanges();
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
                IEnumerable<LineItem> lineItems,
                EInvestmentTransactionType type,
                int securityID,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
                : base(date, payee, memo, status, lineItems) =>
                (Type, SecurityID, SecurityPrice, SecurityQuantity, Commission) =
                    (type, securityID, securityPrice, securityQuantity, commission);

            // Clone
            public InvestmentTransactionData(InvestmentTransactionData src)
                : base(src) =>
                (Type, SecurityID, SecurityPrice, SecurityQuantity, Commission) =
                    (src.Type, src.SecurityID, src.SecurityPrice, src.SecurityQuantity, src.Commission);

            // Properties
            public EInvestmentTransactionType Type;
            public int SecurityID;
            public decimal SecurityPrice;
            public decimal SecurityQuantity;
            public decimal Commission;

            public override bool Equals(object obj)
            {
                return
                    obj is InvestmentTransactionData o &&
                    base.Equals(o) &&
                    o.Type == Type &&
                    o.SecurityID == SecurityID &&
                    o.SecurityPrice == SecurityPrice &&
                    o.SecurityQuantity == SecurityQuantity &&
                    o.Commission == Commission;
            }


            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        #endregion
    }
}
