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
        partial class LineItemRow
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

        partial class LineItemDataTable
        {
            public LineItemRow Add(TransactionRow transactionRow, int categoryId, int categoryAccountId, string memo, decimal amount, ETransactionStatus? transferStatus)
            {
                var lineItemRow = NewLineItemRow();

                UpdateLineItem(lineItemRow, transactionRow, categoryId, categoryAccountId, memo, amount, transferStatus);

                Rows.Add(lineItemRow);

                return lineItemRow;
            }

            public LineItemRow Update(LineItemRow lineItemRow, TransactionRow transactionRow, int categoryId, int categoryAccountId, string memo, decimal amount, ETransactionStatus? transferStatus)
            {
                UpdateLineItem(lineItemRow, transactionRow, categoryId, categoryAccountId, memo, amount, transferStatus);

                return lineItemRow;
            }


            private static LineItemRow UpdateLineItem(
                LineItemRow lineItemRow,
                TransactionRow transactionRow,
                int categoryId,
                int categoryAccountId, 
                string memo,
                decimal amount, 
                ETransactionStatus? transferStatus)
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

                if (transferStatus.HasValue)
                {
                    lineItemRow.TransferStatus = transferStatus.Value;
                }
                else
                {
                    lineItemRow.SetITransferStatusNull();
                }

                return lineItemRow;
            }
        }
    }
}
