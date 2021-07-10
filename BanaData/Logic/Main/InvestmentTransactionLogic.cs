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

        // Behavior of transaction (based on type)
        private IInvestmentTransactionType investmentTransactionType;

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
                    EInvestmentTransactionType.Dividends, -1, 0, 0, 0)) { }

        #endregion

        #region Logic Properties

        public bool IsCashIn => Household.InvestmentTransactionRow.CashIn(data.Type);

        public bool IsCashOut => Household.InvestmentTransactionRow.CashOut(data.Type);

        public bool IsSecurityIn => Household.InvestmentTransactionRow.SecurityIn(data.Type);

        public bool IsSecurityOut => Household.InvestmentTransactionRow.SecurityOut(data.Type);

        public override decimal AmountForCashBalance => (IsCashIn || IsCashOut) ? data.Amount : 0;

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

        //
        // Security price
        //
        public decimal SecurityPrice
        {
            get => data.SecurityPrice;
            set => data.SecurityPrice = value;
        }
        public int SecurityQuantityTabIndex => investmentTransactionType.SecurityQuantityTabIndex;
        public bool IsSecurityQuantityVisible => investmentTransactionType.IsSecurityQuantityVisible;

        //
        // Security quantity
        //
        public decimal SecurityQuantity
        {
            get => data.SecurityQuantity;
            set => data.SecurityQuantity = value;
        }
        public int SecurityPriceTabIndex => investmentTransactionType.SecurityPriceTabIndex;
        public bool IsSecurityPriceVisible => investmentTransactionType.IsSecurityPriceVisible;

        //
        // Commission
        //
        public decimal Commission
        {
            get => data.Commission;
            set => data.Commission = value;
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
        // Amount supplement
        //
        public int AmountTabIndex => investmentTransactionType.AmountTabIndex;
        public bool IsAmountVisible => investmentTransactionType.IsAmountVisible;

        //
        // Category supplement
        //
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

            // Check based on transaction type
            string error = investmentTransactionType.CheckData(data);
            if (error != null)
            {
                mainWindowLogic.ErrorMessage(error);
                return (false, false);
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
            if (TransID == TRANSID_NOT_COMMITTED)
            {
                return "";
            }

            return Household.InvestmentTransactionRow.GetDescription(data.Type, Amount, SecuritySymbol, SecurityQuantity, SecurityPrice);
        }

        private void SetType(EInvestmentTransactionType value, bool force)
        {
            if (value == data.Type && !force)
            {
                return;
            }

            data.Type = value;
            investmentTransactionType = InvestmentTransactionType.GetInvestmentTransactionType(value);

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

        private void CommitTransactionToDataSet()
        {
            var household = mainWindowLogic.Household;
            var accountRow = household.Account.FindByID(accountID);

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
                var transactionRow = household.Transaction.Add(accountRow, data.Date, data.Payee, data.Memo, data.Status, household.Checkpoint.GetMostRecentCheckpointID());
                TransID = transactionRow.ID;

                // Create new investment transaction row
                household.InvestmentTransaction.Add(transactionRow, data.Type, securityRow, data.SecurityPrice, data.SecurityQuantity, data.Commission);

                // Create the line item
                var li = data.LineItems[0];
                var liRow = household.LineItem.Add(transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount);
                li.ID = liRow.ID;
            }
            else if (TransID == TRANSID_TRANSFER_FILLIN)
            {
                // Modification of status for transfer pseudo-transaction
                // The status for the transactionless end of the transfer is kept in the transfer line item row 
                var liRow = household.LineItem.FindByID(data.LineItems[0].ID);
                liRow.TransferStatus = data.Status;
            }
            else
            {
                // Update transaction row
                var transactionRow = household.Transaction.Update(TransID, accountRow, data.Date, data.Memo, data.Payee, data.Status, household.Checkpoint.GetMostRecentCheckpointID());

                // Update investment transaction
                household.InvestmentTransaction.Update(transactionRow, data.Type, securityRow, data.SecurityPrice, data.SecurityQuantity, data.Commission);

                // Update the line item
                var li = data.LineItems[0];
                var liRow = household.LineItem.FindByID(li.ID);
                household.LineItem.Update(liRow, transactionRow, li.CategoryID, li.CategoryAccountID, li.Memo, li.Amount);
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
