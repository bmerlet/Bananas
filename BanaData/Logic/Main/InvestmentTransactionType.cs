using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BanaData.Database;

namespace BanaData.Logic.Main
{
    internal class InvestmentTransactionType
    {
        public static IInvestmentTransactionType GetInvestmentTransactionType(EInvestmentTransactionType type)
        {
            switch (type)
            {
                case EInvestmentTransactionType.InterestIncome:
                    return new InvestmentTransactionInterest();

                case EInvestmentTransactionType.CashIn:
                case EInvestmentTransactionType.CashOut:
                    return new InvestmentTransactionCashInOut();

                case EInvestmentTransactionType.TransferCashIn:
                case EInvestmentTransactionType.TransferCashOut:
                    return new InvestmentTransactionXInOut();

                case EInvestmentTransactionType.SharesIn:
                    return new InvestmentTransactionSharesIn();

                case EInvestmentTransactionType.SharesOut:
                    return new InvestmentTransactionSharesOut();

                case EInvestmentTransactionType.Buy:
                case EInvestmentTransactionType.Sell:
                    return new InvestmentTransactionBuyOrSell();

                case EInvestmentTransactionType.BuyFromTransferredCash:
                case EInvestmentTransactionType.SellAndTransferCash:
                    return new InvestmentTransactionBuyOrSellAndTransfer();

                case EInvestmentTransactionType.Dividends:
                case EInvestmentTransactionType.ShortTermCapitalGains:
                case EInvestmentTransactionType.LongTermCapitalGains:
                    return new InvestmentTransactionDivs();

                case EInvestmentTransactionType.TransferDividends:
                case EInvestmentTransactionType.TransferShortTermCapitalGains:
                case EInvestmentTransactionType.TransferLongTermCapitalGains:
                    return new InvestmentTransactionTransferDivs();

                case EInvestmentTransactionType.ReinvestDividends:
                case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                    return new InvestmentTransactionReinvestDivs();

                case EInvestmentTransactionType.Grant:
                case EInvestmentTransactionType.Vest:
                case EInvestmentTransactionType.Exercise:
                case EInvestmentTransactionType.Expire:
                    return new InvestmentTransactionNotSupported();
            }

            return new InvestmentTransactionNotSupported();
        }
    }

    internal interface IInvestmentTransactionType
    {
        bool IsSecuritySymbolVisible { get; }
        int SecuritySymbolTabIndex { get; }

        bool IsSecurityQuantityVisible { get; }
        int SecurityQuantityTabIndex { get; }

        bool IsSecurityPriceVisible { get; }
        int SecurityPriceTabIndex { get; }

        bool IsCommissionVisible { get; }
        int CommissionTabIndex { get; }

        bool IsAmountVisible { get; }
        int AmountTabIndex { get; }

        bool IsCategoryVisible { get; }
        int CategoryTabIndex { get; }
        bool IsTransfer { get; }

        // Check that the data is OK for the transaction type
        string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data);

        // Zero out not needed values
        void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data);
    }

    internal abstract class AInvestmentTransactionType : IInvestmentTransactionType
    {
        public bool IsSecuritySymbolVisible => SecuritySymbolTabIndex >= 0;
        public virtual int SecuritySymbolTabIndex => -1;

        public bool IsSecurityQuantityVisible => SecurityQuantityTabIndex >= 0;
        public virtual int SecurityQuantityTabIndex => -1;

        public bool IsSecurityPriceVisible => SecurityPriceTabIndex >= 0;
        public virtual int SecurityPriceTabIndex => -1;

        public bool IsCommissionVisible => CommissionTabIndex >= 0;
        public virtual int CommissionTabIndex => -1;

        public bool IsAmountVisible => AmountTabIndex >= 0;
        public virtual int AmountTabIndex => -1;

        public bool IsCategoryVisible => CategoryTabIndex >= 0;
        public virtual int CategoryTabIndex => -1;
        public virtual bool IsTransfer => true;

        public virtual string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data) { return null; }

        public virtual void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data) { }
    }

    internal class InvestmentTransactionInterest : AInvestmentTransactionType
    {
        public override int AmountTabIndex => 3;
        public override int CategoryTabIndex => 4;
        public override bool IsTransfer => false;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.Amount == 0)
            {
                return "Please enter an amount.";
            }

            if (string.IsNullOrWhiteSpace(data.Category))
            {
                return "Please choose a category.";
            }

            return null;
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.SecurityID = -1;
            data.SecurityPrice = 0;
            data.SecurityQuantity = 0;
            data.Commission = 0;
        }
    }

    // Cash in/out is same as transfer in/out, except we pick a category in stead of a transfer
    internal class InvestmentTransactionCashInOut : InvestmentTransactionXInOut
    {
        public override bool IsTransfer => false;
    }

    internal class InvestmentTransactionXInOut : AInvestmentTransactionType
    {
        public override int AmountTabIndex => 3;
        public override int CategoryTabIndex => 4;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.Amount == 0)
            {
                return "Please enter an amount.";
            }

            if (string.IsNullOrWhiteSpace(data.Category))
            {
                return "Please choose a category or transfer account";
            }

            return null;
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.SecurityID = -1;
            data.SecurityPrice = 0;
            data.SecurityQuantity = 0;
            data.Commission = 0;
        }
    }

    internal class InvestmentTransactionSharesIn : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 4;
        public override int SecurityPriceTabIndex => 5;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityQuantity == 0)
            {
                return "Please enter a number of shares.";
            }

            if (data.SecurityPrice == 0)
            {
                return "Please enter a share price.";
            }

            return null;
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.Commission = 0;
            data.LineItems[0].Amount = 0;
            data.LineItems[0].Category = "";
        }
    }

    internal class InvestmentTransactionSharesOut : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 4;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityQuantity == 0)
            {
                return "Please enter a number of shares.";
            }

            return null;
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.SecurityPrice = 0;
            data.Commission = 0;
            data.LineItems[0].Amount = 0;
            data.LineItems[0].Category = "";
        }
    }

    internal class InvestmentTransactionBuyOrSell : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 4;
        public override int SecurityPriceTabIndex => 5;
        public override int CommissionTabIndex => 6;
        public override int AmountTabIndex => 7;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityQuantity == 0)
            {
                return "Please enter a number of shares.";
            }

            if (data.SecurityPrice == 0)
            {
                return "Please enter a share price.";
            }

            return null;
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.LineItems[0].Category = "";
        }
    }

    internal class InvestmentTransactionBuyOrSellAndTransfer : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 4;
        public override int SecurityPriceTabIndex => 5;
        public override int CommissionTabIndex => 6;
        public override int AmountTabIndex => 7;
        public override int CategoryTabIndex => 8;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityQuantity == 0)
            {
                return "Please enter a number of shares.";
            }

            if (data.SecurityPrice == 0)
            {
                return "Please enter a share price.";
            }

            if (string.IsNullOrWhiteSpace(data.Category))
            {
                return "Please choose a transfer account";
            }

            return null;
        }
    }

    internal class InvestmentTransactionDivs : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int AmountTabIndex => 4;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.Amount == 0)
            {
                return "Please enter an amount.";
            }

            return null;
        }
        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.SecurityPrice = 0;
            data.SecurityQuantity = 0;
            data.Commission = 0;
            data.LineItems[0].Amount = 0;
            data.LineItems[0].Category = "";
        }
    }

    internal class InvestmentTransactionTransferDivs : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int AmountTabIndex => 4;
        public override int CategoryTabIndex => 5;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.Amount == 0)
            {
                return "Please enter an amount.";
            }

            if (string.IsNullOrWhiteSpace(data.Category))
            {
                return "Please choose a transfer account";
            }

            return null;
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.SecurityPrice = 0;
            data.SecurityQuantity = 0;
            data.Commission = 0;
        }
    }

    internal class InvestmentTransactionReinvestDivs : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 5;
        public override int SecurityPriceTabIndex => 6;
        public override int CommissionTabIndex => 7;
        public override int AmountTabIndex => 4;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityQuantity == 0)
            {
                return "Please enter a number of shares.";
            }

            if (data.SecurityPrice == 0)
            {
                return "Please enter a share price.";
            }

            if (string.IsNullOrWhiteSpace(data.Category))
            {
                return "Please choose a transfer account";
            }

            return null;
        }
    }

    internal class InvestmentTransactionNotSupported : AInvestmentTransactionType
    {
        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.SecurityID = -1;
            data.SecurityPrice = 0;
            data.SecurityQuantity = 0;
            data.Commission = 0;
            data.LineItems[0].Amount = 0;
            data.LineItems[0].Category = "";
        }
    }

}


