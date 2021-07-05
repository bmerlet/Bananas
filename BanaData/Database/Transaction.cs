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
        partial class TransactionsRow
        {
            // Bridges to local enum types
            public ETransactionStatus Status
            {
                get { return (ETransactionStatus)IStatus; }
                set { IStatus = (int)value; }
            }

            // Compute amount of a transaction by adding up all lines item
            public decimal GetAmount()
            {
                decimal amount = 0;

                foreach (LineItemsRow lineItemRow in GetLineItemsRows())
                {
                    amount += lineItemRow.Amount;
                }

                return amount;
            }

            public BankingTransactionsRow GetBankingTransaction()
            {
                return GetBankingTransactionsRows().Single();
            }

            public InvestmentTransactionsRow GetInvestmentTransaction()
            {
                return GetInvestmentTransactionsRows().Single();
            }

            public bool HasSame(DateTime date, string payee, string memo, ETransactionStatus status)
            {
                if (Date != date || Status != status)
                {
                    return false;
                }

                if (IsPayeeNull())
                {
                    if (!string.IsNullOrWhiteSpace(payee))
                    {
                        return false;
                    }
                }
                else if (Payee != payee)
                {
                    return false;
                }

                if (IsMemoNull())
                {
                    if (!string.IsNullOrWhiteSpace(memo))
                    {
                        return false;
                    }
                }
                else if (Memo != memo)
                {
                    return false;
                }

                return true;
            }

        }

        partial class TransactionsDataTable
        {
            public TransactionsRow Add(AccountsRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status)
            {
                var transactionRow = NewTransactionsRow();

                UpdateTransaction(transactionRow, accountRow, date, payee, memo, status);

                Rows.Add(transactionRow);

                return transactionRow;
            }

            public TransactionsRow Update(int transactionID, AccountsRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status)
            {
                var transactionRow = FindByID(transactionID);

                UpdateTransaction(transactionRow, accountRow, date, payee, memo, status);

                return transactionRow;
            }

            private static TransactionsRow UpdateTransaction(TransactionsRow transactionRow, AccountsRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status)
            {
                transactionRow.AccountID = accountRow.ID;

                transactionRow.Date = date;

                if (string.IsNullOrWhiteSpace(payee))
                {
                    transactionRow.SetPayeeNull();
                }
                else
                {
                    transactionRow.Payee = payee;
                }

                if (string.IsNullOrWhiteSpace(memo))
                {
                    transactionRow.SetMemoNull();
                }
                else
                {
                    transactionRow.Memo = memo;
                }

                transactionRow.Status = status;

                return transactionRow;
            }
        }
    }
}
