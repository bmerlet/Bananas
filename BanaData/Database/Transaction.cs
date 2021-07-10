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
        partial class TransactionRow
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

                foreach (LineItemRow lineItemRow in GetLineItemRows())
                {
                    amount += lineItemRow.Amount;
                }

                return amount;
            }

            public BankingTransactionRow GetBankingTransaction()
            {
                return GetBankingTransactionRows().Single();
            }

            public InvestmentTransactionRow GetInvestmentTransaction()
            {
                return GetInvestmentTransactionRows().Single();
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

        partial class TransactionDataTable
        {
            public TransactionRow Add(AccountRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status, int checkpointID)
            {
                var transactionRow = NewTransactionRow();

                UpdateTransaction(transactionRow, accountRow, date, payee, memo, status, checkpointID);

                Rows.Add(transactionRow);

                return transactionRow;
            }

            public TransactionRow Update(int transactionID, AccountRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status, int checkpointID)
            {
                var transactionRow = FindByID(transactionID);

                UpdateTransaction(transactionRow, accountRow, date, payee, memo, status, checkpointID);

                return transactionRow;
            }

            private static TransactionRow UpdateTransaction(TransactionRow transactionRow, AccountRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status, int checkpointID)
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
                transactionRow.CheckpointID = checkpointID;

                return transactionRow;
            }
        }
    }
}
