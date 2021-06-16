//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
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

            public LineItemsRow Add(TransactionsRow transactionRow, bool transfer, DataRow AccountOrCategory, string memo, decimal amount)
            {
                var lineItemRow = NewLineItemsRow();

                UpdateLineItem(lineItemRow, transactionRow, transfer, AccountOrCategory, memo, amount);

                Rows.Add(lineItemRow);

                return lineItemRow;
            }

            public LineItemsRow Add(TransactionsRow transactionRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                var lineItemRow = NewLineItemsRow();

                UpdateLineItem(lineItemRow, transactionRow, categoryId, categoryAccountId, memo, amount);

                Rows.Add(lineItemRow);

                return lineItemRow;
            }

            public LineItemsRow Update(LineItemsRow lineItemRow, TransactionsRow transactionRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                UpdateLineItem(lineItemRow, transactionRow, categoryId, categoryAccountId, memo, amount);

                return lineItemRow;
            }

            private static LineItemsRow UpdateLineItem(LineItemsRow lineItemRow, TransactionsRow transactionRow, bool transfer, DataRow AccountOrCategory, string memo, decimal amount)
            {
                lineItemRow.TransactionID = transactionRow.ID;

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

            private static LineItemsRow UpdateLineItem(LineItemsRow lineItemRow, TransactionsRow transactionRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                lineItemRow.TransactionID = transactionRow.ID;

                lineItemRow.IsTransfer = categoryAccountId >= 0;
                if (categoryId >= 0)
                {
                    lineItemRow.SetAccountIDNull();
                    lineItemRow.CategoryID = categoryId;
                }
                else if (categoryAccountId >= 0)
                {
                    lineItemRow.AccountID = categoryAccountId;
                    lineItemRow.SetCategoryIDNull();
                }
                else
                {
                    lineItemRow.SetAccountIDNull();
                    lineItemRow.SetCategoryIDNull();
                }

                if (string.IsNullOrWhiteSpace(memo))
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
