//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bananas.Data
{
    public partial class Household
    {
        partial class LineItemsDataTable
        {
            public LineItemsRow[] GetByTransaction(Household.TransactionsRow transactionRow)
            {
                var transToLineItem = ParentRelations["FK_Transactions_LineItems"];
                return transactionRow.GetChildRows(transToLineItem).Cast<Household.LineItemsRow>().ToArray();
            }

            public LineItemsRow Add(TransactionsRow bankTransRow, bool transfer, DataRow AccountOrCategory, string memo, decimal amount)
            {
                var lineItemRow = NewLineItemsRow();

                UpdateLineItem(lineItemRow, bankTransRow, transfer, AccountOrCategory, memo, amount);

                Rows.Add(lineItemRow);

                return lineItemRow;
            }

            private static LineItemsRow UpdateLineItem(LineItemsRow lineItemRow, TransactionsRow bankTransRow, bool transfer, DataRow AccountOrCategory, string memo, decimal amount)
            {
                lineItemRow.TransactionID = bankTransRow.ID;

                lineItemRow.IsTransfer = transfer;
                if (AccountOrCategory == null)
                {
                    lineItemRow.SetAccountIDNull();
                    lineItemRow.SetCategoryIDNull();
                }
                else if (transfer)
                {
                    lineItemRow.AccountID = (int)AccountOrCategory["ID"];
                    lineItemRow.SetCategoryIDNull();
                }
                else
                {
                    lineItemRow.SetAccountIDNull();
                    lineItemRow.CategoryID = (int)AccountOrCategory["ID"];
                }

                if (memo == null)
                {
                    lineItemRow.SetMemoNull();
                }
                else
                {
                    lineItemRow.Memo = memo;
                }

                lineItemRow.Amount = amount;

                return lineItemRow;
            }
        }
    }
}
