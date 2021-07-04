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

            public bool HasSame(int categoryID, int categoryAccountID, string memo, decimal amount)
            {
                int rowCategoryID = IsCategoryIDNull() ? -1 : CategoryID;
                if (rowCategoryID != categoryID)
                {
                    return false;
                }

                int rowCategoryAccountID = IsAccountIDNull() ? -1 : AccountID;
                if (rowCategoryAccountID != categoryAccountID)
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

                if (Amount != amount)
                {
                    return false;
                }

                return true;
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
