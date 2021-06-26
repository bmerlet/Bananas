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

namespace BanaData.Logic.Main
{
    public class InvestmentTransactionLogic : AbstractTransactionLogic
    {
        #region Private fields

        // Parent logics
        private readonly InvestmentRegisterLogic investmentRegisterLogic;

        // Our data
        private new readonly InvestmentTransactionData data;

        #endregion

        #region Constructors

        public InvestmentTransactionLogic(
            MainWindowLogic _mainWindowLogic,
            InvestmentRegisterLogic _investmentRegisterLogic,
            int _accountID,
            int transID,
            InvestmentTransactionData _data)
            : base(_mainWindowLogic, _accountID, transID, _data)
        {
            (investmentRegisterLogic, data) =
                (_investmentRegisterLogic, _data);
        }

        public InvestmentTransactionLogic(
            MainWindowLogic _mainWindowLogic,
            InvestmentRegisterLogic _investmentRegisterLogic,
            int _accountID)
            : this(_mainWindowLogic, _investmentRegisterLogic, _accountID, -1,
                  new InvestmentTransactionData(DateTime.Today, "", ETransactionStatus.Pending, new LineItem[] { new LineItem(_mainWindowLogic, -1, "", -1, -1, "", 0, false) },
                    EInvestmentTransactionType.None, -1, 0, 0, 0)) { }

        #endregion

        #region Logic Properties

        public bool IsCashIn =>
            data.Type == EInvestmentTransactionType.Cash ||
            data.Type == EInvestmentTransactionType.InterestIncome ||
            data.Type == EInvestmentTransactionType.Dividends ||
            data.Type == EInvestmentTransactionType.ShortTermCapitalGains ||
            data.Type == EInvestmentTransactionType.LongTermCapitalGains ||
            data.Type == EInvestmentTransactionType.TransferCash ||
            data.Type == EInvestmentTransactionType.TransferCashIn ||
            data.Type == EInvestmentTransactionType.TransferMiscellaneousIncomeIn ||
            data.Type == EInvestmentTransactionType.ReturnOnCapital ||
            data.Type == EInvestmentTransactionType.Exercise ||
            data.Type == EInvestmentTransactionType.Sell;

        public bool IsCashOut =>
            data.Type == EInvestmentTransactionType.TransferCashOut ||
            data.Type == EInvestmentTransactionType.Buy;

        public bool IsSecurityIn =>
            data.Type == EInvestmentTransactionType.SharesIn ||
            data.Type == EInvestmentTransactionType.BuyFromTransferredCash ||
            data.Type == EInvestmentTransactionType.ReinvestDividends ||
            data.Type == EInvestmentTransactionType.ReinvestShortTermCapitalGains ||
            data.Type == EInvestmentTransactionType.ReinvestMediumTermCapitalGains ||
            data.Type == EInvestmentTransactionType.ReinvestLongTermCapitalGains ||
            data.Type == EInvestmentTransactionType.Buy;

        public bool IsSecurityOut =>
            data.Type == EInvestmentTransactionType.SharesOut ||
            data.Type == EInvestmentTransactionType.SellAndTransferCash ||
            data.Type == EInvestmentTransactionType.Sell;

        public override decimal AmountForCashBalance
        {
            get
            {
                decimal result = 0;
                if (data.Amount != 0)
                {
                    if (IsCashIn)
                    {
                        result = data.Amount;
                    }
                    else if (IsCashOut)
                    {
                        result = -data.Amount;
                    }
                }
                return result;
            }
        }

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
            set => data.Type = EnumDescriptionAttribute.MatchDescription<EInvestmentTransactionType>(value);
        }

        public string[] TypesSource => EnumDescriptionAttribute.GetDescriptions(typeof(EInvestmentTransactionType));

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
        public bool IsSecuritySymbolVisible => true; // ZZZ

        //
        // Security price
        //
        public string SecurityPriceString => data.SecurityPrice == 0 ? "" : data.SecurityPrice.ToString("N");
        public decimal SecurityPrice
        {
            get => data.SecurityPrice;
            set => data.SecurityPrice = value;
        }
        public int SecurityQuantityColumnNumber => 5; // ZZZ
        public bool IsSecurityQuantityVisible => true; // ZZZ

        //
        // Security quantity
        //
        public string SecurityQuantityString => data.SecurityQuantity == 0 ? "" : data.SecurityQuantity.ToString("N");
        public decimal SecurityQuantity
        {
            get => data.SecurityQuantity;
            set => data.SecurityQuantity = value;
        }
        public int SecurityPriceColumnNumber => 6; // ZZZ
        public bool IsSecurityPriceVisible => true; // ZZZ

        //
        // Commission
        //
        public string CommissionString => data.Commission == 0 ? "" : data.Commission.ToString("N");
        public decimal Commission
        {
            get => data.Commission;
            set => data.Commission = value;
        }
        public int CommissionColumnNumber => 7; // ZZZ
        public bool IsCommissionVisible => true; // ZZZ

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
                    ShareBalanceString = shareBalance == decimal.MinValue ? "" : shareBalance.ToString("N");
                    OnPropertyChanged(() => ShareBalanceString);
                }
            }
        }

        public string ShareBalanceString { get; private set; } = "";

        //
        // Amount supplement
        //
        public int AmountColumnNumber => 9; // ZZZ
        public bool IsAmountVisible => true; // ZZZ

        //
        // Category supplement
        //
        public int CategoryColumnNumber => 8; // ZZZ
        public bool IsCategoryVisible => true; // ZZZ

        #endregion

        #region IEditable implementation

        public override void BeginEdit()
        {
            // throw new NotImplementedException();
        }

        public override void CancelEdit()
        {
            base.CancelEdit();

            // throw new NotImplementedException();
        }

        // Returns if there is something to commit and if we need to move down
        public override (bool needCommit, bool moveDown) ValidateEndEdit()
        {
            return (false, false);
        }

        public override void EndEdit()
        {
            // throw new NotImplementedException();
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

        #endregion

        #region Supporting classes

        public class InvestmentTransactionData : AbstractTransactionLogic.BaseTransactionData
        {
            // Construct from scratch
            public InvestmentTransactionData(
                DateTime date,
                string payee,
                ETransactionStatus status,
                IEnumerable<LineItem> lineItems,
                EInvestmentTransactionType type,
                int securityID,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
                : base(date, payee, status, lineItems) =>
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
