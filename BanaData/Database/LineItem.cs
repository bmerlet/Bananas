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
        partial class LineItemsRow
        {
            // Bridges to local enum types
            public ETransactionStatus TransferStatus
            {
                get { return (ETransactionStatus)ITransferStatus; }
                set { ITransferStatus = (int)value; }
            }
        }

        partial class LineItemsDataTable
        {
            public LineItemsRow[] GetByTransaction(Household.TransactionsRow transactionRow)
            {
                var transToLineItem = ParentRelations["FK_Transactions_LineItems"];
                return transactionRow.GetChildRows(transToLineItem).Cast<Household.LineItemsRow>().ToArray();
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

            public bool HasSame(LineItemsRow lineItemRow, int categoryID, int categoryAccountID, string memo, decimal amount)
            {
                int rowCategoryID = lineItemRow.IsCategoryIDNull() ? -1 : lineItemRow.CategoryID;
                if (rowCategoryID != categoryID)
                {
                    return false;
                }

                int rowCategoryAccountID = lineItemRow.IsAccountIDNull() ? -1 : lineItemRow.AccountID;
                if (rowCategoryAccountID != categoryAccountID)
                {
                    return false;
                }

                if (lineItemRow.IsMemoNull())
                {
                    if (!string.IsNullOrWhiteSpace(memo))
                    {
                        return false;
                    }
                }
                else if (lineItemRow.Memo != memo)
                {
                    return false;
                }

                if (lineItemRow.Amount != amount)
                {
                    return false;
                }

                return true;
            }

            private static LineItemsRow UpdateLineItem(LineItemsRow lineItemRow, TransactionsRow transactionRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                lineItemRow.TransactionID = transactionRow.ID;

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
