using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BanaData.Database;

namespace BanaData.Logic.Main
{
    #region Factory

    /// <summary>
    /// Factory for classes implementing the details of the various investment transaction types
    /// </summary>
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
                case EInvestmentTransactionType.Exercise:
                    return new InvestmentTransactionBuyOrSell(type);

                case EInvestmentTransactionType.BuyFromTransferredCash:
                case EInvestmentTransactionType.SellAndTransferCash:
                    return new InvestmentTransactionBuyOrSellAndTransfer(type);

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
                case EInvestmentTransactionType.Expire:
                    return new InvestmentTransactionNotSupported();
            }

            return new InvestmentTransactionNotSupported();
        }
    }

    #endregion

    #region Interface for investment transaction type helpers

    internal interface IInvestmentTransactionType
    {
        bool IsSecuritySymbolVisible { get; }
        int SecuritySymbolTabIndex { get; }
        bool IsFilteringSecurity { get; }

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

    #endregion

    #region Abstract class for investment transaction type helpers

    internal abstract class AInvestmentTransactionType : IInvestmentTransactionType
    {
        public bool IsSecuritySymbolVisible => SecuritySymbolTabIndex >= 0;
        public virtual int SecuritySymbolTabIndex => -1;
        public virtual bool IsFilteringSecurity => false;

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

    #endregion

    #region Interest helper

    internal class InvestmentTransactionInterest : AInvestmentTransactionType
    {
        public override int AmountTabIndex => 3;
        public override int CategoryTabIndex => 4;
        public override bool IsTransfer => false;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.LineItem.Amount == 0)
            {
                return "Please enter an amount.";
            }

            if (string.IsNullOrWhiteSpace(data.LineItem.Category))
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

    #endregion

    #region CashIn/CashOut helper

    // Cash in/out is same as transfer in/out, except we pick a category in stead of a transfer
    internal class InvestmentTransactionCashInOut : InvestmentTransactionXInOut
    {
        public override bool IsTransfer => false;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (string.IsNullOrWhiteSpace(data.LineItem.Category))
            {
                return "Please choose a category";
            }

            return base.CheckData(data);
        }
    }

    #endregion

    #region XIn/XOut helper

    internal class InvestmentTransactionXInOut : AInvestmentTransactionType
    {
        public override int AmountTabIndex => 3;
        public override int CategoryTabIndex => 4;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.LineItem.Amount == 0)
            {
                return "Please enter an amount.";
            }

            if (string.IsNullOrWhiteSpace(data.LineItem.Category))
            {
                return "Please choose a transfer account";
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

    #endregion

    #region SharesIn helper

    internal class InvestmentTransactionSharesIn : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 4;
        public override int SecurityPriceTabIndex => 5;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityID < 0)
            {
                return "Please enter a security symbol.";
            }

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
            data.LineItem.Amount = 0;
            data.LineItem.Category = "";
        }
    }

    #endregion

    #region SharesOut helper

    internal class InvestmentTransactionSharesOut : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 4;
        public override bool IsFilteringSecurity => true;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityID < 0)
            {
                return "Please enter a security symbol.";
            }

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
            data.LineItem.Amount = 0;
            data.LineItem.Category = "";
        }
    }

    #endregion

    #region Buy/Sell helper

    internal class InvestmentTransactionBuyOrSell : AInvestmentTransactionType
    {
        public InvestmentTransactionBuyOrSell(EInvestmentTransactionType type)
        {
            IsFilteringSecurity = type == EInvestmentTransactionType.Sell || type == EInvestmentTransactionType.SellAndTransferCash;
        }

        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 4;
        public override int SecurityPriceTabIndex => 5;
        public override int CommissionTabIndex => 6;
        public override int AmountTabIndex => 7;
        public override bool IsFilteringSecurity { get; }

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityID < 0)
            {
                return "Please enter a security symbol.";
            }

            if (data.SecurityQuantity == 0)
            {
                return "Please enter a number of shares.";
            }

            if (data.SecurityPrice == 0)
            {
                return "Please enter a share price.";
            }

            // Check amount
            decimal expectedAmount = data.SecurityQuantity * data.SecurityPrice - data.Commission;
            if (data.PositiveAmount != expectedAmount)
            {
                return
                    "The amount should be the number of shares times the share price minus commission" + Environment.NewLine +
                    $"But {data.SecurityQuantity:N4} * {data.SecurityPrice:N4} - {data.Commission:N2} = {expectedAmount:N2}" + Environment.NewLine +
                    $"While the amount is {data.LineItem.Amount:N2}";
            }

            return null;
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.LineItem.Category = "";
        }
    }

    #endregion

    #region BuyX, SellX helper

    // Just like buy or sell but with a transfer
    internal class InvestmentTransactionBuyOrSellAndTransfer : InvestmentTransactionBuyOrSell
    {
        public InvestmentTransactionBuyOrSellAndTransfer(EInvestmentTransactionType type) : base(type) { }

        public override int CategoryTabIndex => 8;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (string.IsNullOrWhiteSpace(data.LineItem.Category))
            {
                return "Please choose a transfer account";
            }

            return base.CheckData(data);
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
        }
    }

    #endregion

    #region Dividends helper

    internal class InvestmentTransactionDivs : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int AmountTabIndex => 4;
        public override bool IsFilteringSecurity => true;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityID < 0)
            {
                return "Please enter a security symbol.";
            }

            if (data.LineItem.Amount == 0)
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
            data.LineItem.Category = "";
        }
    }

    #endregion

    #region DivX helper

    internal class InvestmentTransactionTransferDivs : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int AmountTabIndex => 4;
        public override int CategoryTabIndex => 5;
        public override bool IsFilteringSecurity => true;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityID < 0)
            {
                return "Please enter a security symbol.";
            }

            if (data.LineItem.Amount == 0)
            {
                return "Please enter an amount.";
            }

            if (string.IsNullOrWhiteSpace(data.LineItem.Category))
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

    #endregion

    #region ReinvDiv helper

    internal class InvestmentTransactionReinvestDivs : AInvestmentTransactionType
    {
        public override int SecuritySymbolTabIndex => 3;
        public override int SecurityQuantityTabIndex => 5;
        public override int SecurityPriceTabIndex => 6;
        public override int CommissionTabIndex => 7;
        public override int AmountTabIndex => 4;
        public override bool IsFilteringSecurity => true;

        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            if (data.SecurityID < 0)
            {
                return "Please choose a security symbol.";
            }

            if (data.LineItem.Amount == 0)
            {
                return "Please enter an amount.";
            }

            if (data.SecurityQuantity == 0)
            {
                return "Please enter a number of shares.";
            }

            decimal expectedSecurityPrice = data.LineItem.Amount / data.SecurityQuantity;
            if (data.SecurityPrice != Math.Round(expectedSecurityPrice, 4))
            {
                return
                    "The share price should be the dividend amount divided by the number of shares" + Environment.NewLine +
                    $"But {data.LineItem.Amount:N2} * {data.SecurityQuantity:N4} = {expectedSecurityPrice:N4}" + Environment.NewLine +
                    $"While the share price is {data.SecurityPrice:N4}";
            }


            return null;
        }
    }

    #endregion

    #region Unsupported helper

    internal class InvestmentTransactionNotSupported : AInvestmentTransactionType
    {
        public override string CheckData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            return "Not supported";
        }

        public override void CleanupData(InvestmentTransactionLogic.InvestmentTransactionData data)
        {
            data.SecurityID = -1;
            data.SecurityPrice = 0;
            data.SecurityQuantity = 0;
            data.Commission = 0;
            data.LineItem.Amount = 0;
            data.LineItem.Category = "";
        }
    }

    #endregion
}


