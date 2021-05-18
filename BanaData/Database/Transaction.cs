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

                var transToLineItem = Table.ChildRelations["FK_Transactions_LineItems"];
                foreach (var lineItemRow in GetChildRows(transToLineItem))
                {
                    amount += (lineItemRow as Household.LineItemsRow).Amount;
                }

                return amount;
            }
        }

        partial class TransactionsDataTable
        {
            public TransactionsRow Add(AccountsRow accountRow, DateTime date, string payee, ETransactionStatus status)
            {
                var transactionRow = NewTransactionsRow();

                UpdateTransaction(transactionRow, accountRow, date, payee, status);

                Rows.Add(transactionRow);

                return transactionRow;
            }

            private static TransactionsRow UpdateTransaction(TransactionsRow transactionRow, AccountsRow accountRow, DateTime date, string payee, ETransactionStatus status)
            {
                transactionRow.AccountID = accountRow.ID;

                transactionRow.Date = date;

                if (payee == null)
                {
                    transactionRow.SetPayeeNull();
                }
                else
                {
                    transactionRow.Payee = payee;
                }

                transactionRow.Status = status;

                return transactionRow;
            }
        }
    }
}
