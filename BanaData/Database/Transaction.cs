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
                get => (ETransactionStatus)IStatus;
                set => IStatus = (int)value;
            }

            public ETransactionType Type
            {
                get => (ETransactionType)IType;
                set => IType = (int)value;
            }

            // Compute amount of a transaction by adding up all lines item
            public decimal GetAmount()
            {
                return GetLineItemRows().Sum(lir => lir.Amount);
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

        public IEnumerable<TransactionRow> RegularTransactions => Transaction.Rows.Cast<Household.TransactionRow>().Where(t => t.Type == ETransactionType.Regular);
        public IEnumerable<TransactionRow> MemorizedPayees => Transaction.Rows.Cast<Household.TransactionRow>().Where(t => t.Type == ETransactionType.MemorizedPayee);
        public IEnumerable<TransactionRow> ScheduledTransactions => Transaction.Rows.Cast<Household.TransactionRow>().Where(t => t.Type == ETransactionType.ScheduledTransaction);

        partial class TransactionDataTable
        {
            public TransactionRow Add(AccountRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status, int checkpointID, ETransactionType type)
            {
                var transactionRow = NewTransactionRow();

                UpdateTransaction(transactionRow, accountRow, date, payee, memo, status, checkpointID, type);

                Rows.Add(transactionRow);

                return transactionRow;
            }

            public TransactionRow Update(int transactionID, AccountRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status, int checkpointID, ETransactionType type)
            {
                var transactionRow = FindByID(transactionID);

                UpdateTransaction(transactionRow, accountRow, date, payee, memo, status, checkpointID, type);

                return transactionRow;
            }

            private static TransactionRow UpdateTransaction(
                TransactionRow transactionRow, 
                AccountRow accountRow,
                DateTime date, 
                string payee, 
                string memo, 
                ETransactionStatus status,
                int checkpointID,
                ETransactionType type)
            {
                if (accountRow == null)
                {
                    transactionRow.SetAccountIDNull();
                }
                else
                {
                    transactionRow.AccountID = accountRow.ID;
                }

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
                transactionRow.Type = type;

                return transactionRow;
            }
        }
    }
}
