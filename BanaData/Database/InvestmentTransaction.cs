//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Attributes;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class InvestmentTransactionRow
        {
            // Bridges to local enum types
            public EInvestmentTransactionType Type
            {
                get { return (EInvestmentTransactionType)IType; }
                set { IType = (int)value; }
            }

            public bool IsCashIn => CashIn(Type);
            public bool IsCashOut => CashOut(Type);
            public bool IsTransferIn => TransferIn(Type);
            public bool IsTransferOut => TransferOut(Type);
            public bool IsSecurityIn => SecurityIn(Type);
            public bool IsSecurityOut => SecurityOut(Type);

            // Transactions that add to the cash balance
            static public bool CashIn(EInvestmentTransactionType type) =>
                type == EInvestmentTransactionType.InterestIncome ||
                type == EInvestmentTransactionType.Dividends ||
                type == EInvestmentTransactionType.ShortTermCapitalGains ||
                type == EInvestmentTransactionType.LongTermCapitalGains ||
                type == EInvestmentTransactionType.CashIn ||
                type == EInvestmentTransactionType.TransferCashIn ||
                type == EInvestmentTransactionType.ReturnOnCapital ||
                type == EInvestmentTransactionType.Sell;

            // Transactions that remove from the cash balance
            static public bool CashOut(EInvestmentTransactionType type) =>
                type == EInvestmentTransactionType.CashOut ||
                type == EInvestmentTransactionType.TransferCashOut ||
                type == EInvestmentTransactionType.Buy;

            // Transactions that transfer cash into the account 
            static public bool TransferIn(EInvestmentTransactionType type) =>
                type == EInvestmentTransactionType.TransferCashIn ||
                type == EInvestmentTransactionType.BuyFromTransferredCash;

            // Transactions that transfer cash out of the the account 
            static public bool TransferOut(EInvestmentTransactionType type) =>
                type == EInvestmentTransactionType.TransferCashOut ||
                type == EInvestmentTransactionType.SellAndTransferCash ||
                type == EInvestmentTransactionType.TransferDividends ||
                type == EInvestmentTransactionType.TransferShortTermCapitalGains ||
                type == EInvestmentTransactionType.TransferLongTermCapitalGains;

            // Transactions that adds more shares of a security
            static public bool SecurityIn(EInvestmentTransactionType type) =>
                type == EInvestmentTransactionType.SharesIn ||
                type == EInvestmentTransactionType.BuyFromTransferredCash ||
                type == EInvestmentTransactionType.ReinvestDividends ||
                type == EInvestmentTransactionType.ReinvestShortTermCapitalGains ||
                type == EInvestmentTransactionType.ReinvestMediumTermCapitalGains ||
                type == EInvestmentTransactionType.ReinvestLongTermCapitalGains ||
                type == EInvestmentTransactionType.Buy;

            // Transactions that removes some shares of a security
            static public bool SecurityOut(EInvestmentTransactionType type) =>
                type == EInvestmentTransactionType.SharesOut ||
                type == EInvestmentTransactionType.SellAndTransferCash ||
                type == EInvestmentTransactionType.Sell;

            // Build a description of a transaction
            public string GetDescription()
            {
                return GetDescription(
                    Type,
                    TransactionsRow.GetAmount(),
                    IsSecurityIDNull() ? "" : SecuritiesRow.Symbol,
                    IsSecurityQuantityNull() ? 0 : SecurityQuantity,
                    IsSecurityPriceNull() ? 0 : SecurityPrice);
            }

            public static string GetDescription(
                EInvestmentTransactionType type, decimal amount, string symbol, decimal quantity, decimal price)
            {
                string desc = "";

                switch (type)
                {
                    case EInvestmentTransactionType.CashIn:
                        desc = $"Added {amount:C2}";
                        break;

                    case EInvestmentTransactionType.CashOut:
                        desc = $"Removed {-amount:C2}";
                        break;

                    case EInvestmentTransactionType.TransferCashIn:
                        desc = $"Transfered {amount:C2} in";
                        break;

                    case EInvestmentTransactionType.TransferCashOut:
                        desc = $"Transfered {-amount:C2} out";
                        break;

                    case EInvestmentTransactionType.InterestIncome:
                        desc = $"Received ${amount:C2} in interest";
                        break;

                    case EInvestmentTransactionType.SharesIn:
                        desc = $"Received {quantity} {symbol} @ ${price}";
                        break;

                    case EInvestmentTransactionType.SharesOut:
                        desc = $"Removed {quantity} {symbol}";
                        break;

                    case EInvestmentTransactionType.Buy:
                    case EInvestmentTransactionType.BuyFromTransferredCash:
                        desc = $"Bought {quantity} {symbol} @ ${price}";
                        break;

                    case EInvestmentTransactionType.Sell:
                    case EInvestmentTransactionType.SellAndTransferCash:
                        desc = $"Sold {quantity} {symbol} @ ${price}";
                        break;

                    case EInvestmentTransactionType.Dividends:
                        desc = $"Received {amount:C2} in dividends from {symbol}";
                        break;

                    case EInvestmentTransactionType.TransferDividends:
                        desc = $"Received {-amount:C2} in dividends from {symbol}";
                        break;

                    case EInvestmentTransactionType.ReinvestDividends:
                    case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                    case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                    case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                        desc = $"Reinvested {amount:C2} as {quantity} shares of {symbol}";
                        break;

                    case EInvestmentTransactionType.ShortTermCapitalGains:
                        desc = $"Received {amount:C2} in ST CG from {symbol}";
                        break;

                    case EInvestmentTransactionType.TransferShortTermCapitalGains:
                        desc = $"Received {-amount:C2} in ST CG from {symbol}";
                        break;

                    case EInvestmentTransactionType.LongTermCapitalGains:
                        desc = $"Received {amount:C2} in LT CG from {symbol}";
                        break;

                    case EInvestmentTransactionType.TransferLongTermCapitalGains:
                        desc = $"Received {-amount:C2} in LT CG from {symbol}";
                        break;

                    case EInvestmentTransactionType.Grant:
                    case EInvestmentTransactionType.Vest:
                    case EInvestmentTransactionType.Exercise:
                    case EInvestmentTransactionType.Expire:
                        desc = $"{EnumDescriptionAttribute.GetDescription(type)}: Not supported";
                        break;
                }

                return desc;
            }

            public bool HasSame(
                EInvestmentTransactionType type,
                SecurityRow securityRow,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
            {
                if (Type != type)
                {
                    return false;
                }

                if (IsSecurityIDNull())
                {
                    if (securityRow != null || securityPrice != 0 || securityQuantity != 0)
                    {
                        return false;
                    }
                }
                else if (SecurityID != securityRow.ID ||
                         SecurityPrice != securityPrice ||
                         SecurityQuantity != securityQuantity)
                {
                    return false;
                }

                if (Commission != commission)
                {
                    return false;
                }

                return true;
            }
        }

        partial class InvestmentTransactionDataTable
        {
            public InvestmentTransactionRow Add(
                TransactionRow transactionRow,
                EInvestmentTransactionType type,
                SecurityRow security,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
            {
                var invTransRow = NewInvestmentTransactionRow();

                UpdateInvestmentTransaction(invTransRow, transactionRow, type, security, securityPrice, securityQuantity, commission);

                Rows.Add(invTransRow);

                return invTransRow;
            }

            public InvestmentTransactionRow Update(
                TransactionRow transactionRow,
                EInvestmentTransactionType type,
                SecurityRow security,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
            {
                InvestmentTransactionRow invTransRow = transactionRow.GetInvestmentTransaction();

                return UpdateInvestmentTransaction(invTransRow, transactionRow, type, security, securityPrice, securityQuantity, commission);
            }

            private static InvestmentTransactionRow UpdateInvestmentTransaction(
                InvestmentTransactionRow invTransRow,
                TransactionRow transactionRow,
                EInvestmentTransactionType type,
                SecurityRow securityRow,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
            {
                invTransRow.TransactionID = transactionRow.ID;

                invTransRow.Type = type;
                if (securityRow != null)
                {
                    invTransRow.SecurityID = securityRow.ID;
                    invTransRow.SecurityPrice = securityPrice;
                    invTransRow.SecurityQuantity = securityQuantity;
                }
                else
                {
                    invTransRow.SetSecurityIDNull();
                    invTransRow.SetSecurityPriceNull();
                    invTransRow.SetSecurityQuantityNull();
                }
                invTransRow.Commission = commission;

                return invTransRow;
            }
        }
    }
}
