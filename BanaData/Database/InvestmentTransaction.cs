//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class InvestmentTransactionsRow
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
                type == EInvestmentTransactionType.Cash ||
                type == EInvestmentTransactionType.InterestIncome ||
                type == EInvestmentTransactionType.Dividends ||
                type == EInvestmentTransactionType.ShortTermCapitalGains ||
                type == EInvestmentTransactionType.LongTermCapitalGains ||
                type == EInvestmentTransactionType.TransferCash ||
                type == EInvestmentTransactionType.TransferCashIn ||
                type == EInvestmentTransactionType.TransferMiscellaneousIncomeIn ||
                type == EInvestmentTransactionType.ReturnOnCapital ||
                type == EInvestmentTransactionType.Sell;

            // Transactions that remove from the cash balance
            static public bool CashOut(EInvestmentTransactionType type) =>
                type == EInvestmentTransactionType.TransferCashOut ||
                type == EInvestmentTransactionType.Buy;

            // Transactions that transfer cash into the account 
            static public bool TransferIn(EInvestmentTransactionType type) =>
                type == EInvestmentTransactionType.TransferCash ||
                type == EInvestmentTransactionType.TransferCashIn ||
                type == EInvestmentTransactionType.BuyFromTransferredCash ||
                type == EInvestmentTransactionType.TransferMiscellaneousIncomeIn;

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

            public bool HasSame(
                EInvestmentTransactionType type,
                SecuritiesRow securityRow,
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

        partial class InvestmentTransactionsDataTable
        {
            //public InvestmentTransactionsRow GetByTransaction(TransactionsRow transaction)
            //{
            //    return this.Single(it => it.TransactionID == transaction.ID);
            //}

            public InvestmentTransactionsRow Add(
                Household.TransactionsRow transactionRow,
                EInvestmentTransactionType type,
                Household.SecuritiesRow security,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
            {
                var invTransRow = NewInvestmentTransactionsRow();

                UpdateInvestmentTransaction(invTransRow, transactionRow, type, security, securityPrice, securityQuantity, commission);

                Rows.Add(invTransRow);

                return invTransRow;
            }

            public InvestmentTransactionsRow Update(
                Household.TransactionsRow transactionRow,
                EInvestmentTransactionType type,
                Household.SecuritiesRow security,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
            {
                InvestmentTransactionsRow invTransRow = transactionRow.GetInvestmentTransaction();

                return UpdateInvestmentTransaction(invTransRow, transactionRow, type, security, securityPrice, securityQuantity, commission);
            }

            private static InvestmentTransactionsRow UpdateInvestmentTransaction(
                Household.InvestmentTransactionsRow invTransRow,
                Household.TransactionsRow transactionRow,
                EInvestmentTransactionType type,
                Household.SecuritiesRow securityRow,
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
