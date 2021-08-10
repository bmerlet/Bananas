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
            public LineItemCategoryRow GetLineItemCategoryRow()
            {
                return GetLineItemCategoryRows().SingleOrDefault();
            }

            public LineItemTransferRow GetLineItemTransferRow()
            {
                return GetLineItemTransferRows().SingleOrDefault();
            }

            public bool HasSame(string memo, decimal amount)
            {
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

            public (int categoryID, int accountID, string category) GetCategory()
            {
                int categoryID = -1;
                int accountID = -1;
                string category = "";
                if (GetLineItemTransferRow() is Household.LineItemTransferRow lineItemTransferRow)
                {
                    category = "[" + lineItemTransferRow.AccountRow.Name + "]";
                    accountID = lineItemTransferRow.AccountID;
                }
                else if (GetLineItemCategoryRow() is Household.LineItemCategoryRow lineItemCategoryRow)
                {
                    category = lineItemCategoryRow.CategoryRow.FullName;
                    categoryID = lineItemCategoryRow.CategoryID;
                }

                return (categoryID, accountID, category);
            }

            public void CascadeDelete()
            {
                if (GetLineItemCategoryRow() is Household.LineItemCategoryRow licr)
                {
                    licr.Delete();
                }

                if (GetLineItemTransferRow() is Household.LineItemTransferRow litr)
                {
                    litr.Delete();
                }

                base.Delete();
            }
        }

        partial class LineItemDataTable
        {
            public LineItemRow Add(TransactionRow transactionRow, string memo, decimal amount)
            {
                var lineItemRow = NewLineItemRow();

                UpdateLineItem(lineItemRow, transactionRow, memo, amount);

                Rows.Add(lineItemRow);

                return lineItemRow;
            }

            public LineItemRow Update(LineItemRow lineItemRow, TransactionRow transactionRow, string memo, decimal amount)
            {
                UpdateLineItem(lineItemRow, transactionRow,  memo, amount);

                return lineItemRow;
            }


            private static LineItemRow UpdateLineItem(
                LineItemRow lineItemRow,
                TransactionRow transactionRow,
                string memo,
                decimal amount)
            {
                lineItemRow.TransactionID = transactionRow.ID;
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
