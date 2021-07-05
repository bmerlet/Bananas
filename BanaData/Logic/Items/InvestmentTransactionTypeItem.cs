using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.Attributes;
using BanaData.Database;

namespace BanaData.Logic.Items
{
    public class InvestmentTransactionTypeItem
    {
        public InvestmentTransactionTypeItem(EInvestmentTransactionType type)
        {
            Type = type;
            TypeString = EnumDescriptionAttribute.GetDescription(type);

            switch(type)
            {
                case EInvestmentTransactionType.CashIn:
                    Description = "Add cash";
                    break;

                case EInvestmentTransactionType.CashOut:
                    Description = "Remove cash";
                    break;

                case EInvestmentTransactionType.TransferCashIn:
                    Description = "Transfer cash in";
                    break;

                case EInvestmentTransactionType.TransferCashOut:
                    Description = "Transfer cash out";
                    break;

                case EInvestmentTransactionType.InterestIncome:
                    Description = "Interest income";
                    break;

                case EInvestmentTransactionType.SharesIn:
                    Description = "Add shares";
                    break;

                case EInvestmentTransactionType.SharesOut:
                    Description = "Remove shares";
                    break;

                case EInvestmentTransactionType.Buy:
                    Description = "Bought shares";
                    break;

                case EInvestmentTransactionType.BuyFromTransferredCash:
                    Description = "Bought shares from transferred cash";
                    break;

                case EInvestmentTransactionType.Sell:
                    Description = "Sold shares";
                    break;

                case EInvestmentTransactionType.SellAndTransferCash:
                    Description = "Sold shares and transferred proceeds";
                    break;

                case EInvestmentTransactionType.Dividends:
                    Description = "Dividends";
                    break;

                case EInvestmentTransactionType.TransferDividends:
                    Description = "Transfer dividends";
                    break;

                case EInvestmentTransactionType.ReinvestDividends:
                    Description = "Reinvest dividends";
                    break;

                case EInvestmentTransactionType.ShortTermCapitalGains:
                    Description = "Short term captial gains";
                    break;

                case EInvestmentTransactionType.TransferShortTermCapitalGains:
                    Description = "Transfer short term capital gains";
                    break;

                case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                    Description = "Reinvest short term capital gains";
                    break;

                case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                    Description = "Reinvest medium term capital gains";
                    break;

                case EInvestmentTransactionType.LongTermCapitalGains:
                    Description = "Long term capital gains";
                    break;

                case EInvestmentTransactionType.TransferLongTermCapitalGains:
                    Description = "Transfer long term capital gains";
                    break;

                case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                    Description = "Reinvest long term capital gains";
                    break;

                case EInvestmentTransactionType.ReturnOnCapital:
                    Description = "Return on capital";
                    break;

                case EInvestmentTransactionType.Grant:
                    Description = "Grant";
                    break;

                case EInvestmentTransactionType.Vest:
                    Description = "Vest";
                    break;

                case EInvestmentTransactionType.Exercise:
                    Description = "Exercise";
                    break;

                case EInvestmentTransactionType.Expire:
                    Description = "Expire";
                    break;
            }
        }

        public EInvestmentTransactionType Type { get; }
        public string TypeString { get; }
        public string Description { get; }
    }
}
