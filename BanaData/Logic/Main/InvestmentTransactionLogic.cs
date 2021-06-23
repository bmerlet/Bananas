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

        #region UI Properties

        public string Type
        {
            get => EnumDescriptionAttribute.GetDescription(data.Type);
            // ZZZ set
        }

        public string[] TypesSource => EnumDescriptionAttribute.GetDescriptions(typeof(EInvestmentTransactionType));

        public string Description => GetDescription();

        public string SecuritySymbol
        {
            get => GetSecuritySymbol(data.SecurityID);
            // Set
        }

        public string SecurityPrice => data.SecurityPrice == 0 ? "" : data.SecurityPrice.ToString("N");

        public string SecurityQuantity => data.SecurityQuantity == 0 ? "" : data.SecurityQuantity.ToString("N");

        public string Commission => data.Commission == 0 ? "" : data.Commission.ToString("N");

        public string ShareBalance => "ZZZ";

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

                    /*
 
        [EnumDescription("ROC")]
        ReturnOnCapital,

        [EnumDescription("Grant")]
        Grant,

        [EnumDescription("Vest")]
        Vest,

        [EnumDescription("Exercise")]
        Exercise,

        [EnumDescription("Expire")]
        Expire,

                     */
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
