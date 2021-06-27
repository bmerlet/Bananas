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
        }

        partial class InvestmentTransactionsDataTable
        {
            public InvestmentTransactionsRow GetByTransaction(TransactionsRow transaction)
            {
                return this.Single(it => it.TransactionID == transaction.ID);
            }

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
                InvestmentTransactionsRow invTransRow = GetByTransaction(transactionRow);

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
