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
        partial class MemorizedLineItemRow
        {
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

        partial class MemorizedLineItemDataTable
        {
            public MemorizedLineItemRow Add(MemorizedPayeeRow memorizedPayeesRow, bool transfer, DataRow AccountOrCategory, string memo, decimal amount)
            {
                var memorizedLineItemRow = NewMemorizedLineItemRow();

                UpdateMemorizedLineItem(memorizedLineItemRow, memorizedPayeesRow, transfer, AccountOrCategory, memo, amount);

                Rows.Add(memorizedLineItemRow);

                return memorizedLineItemRow;
            }

            public MemorizedLineItemRow Add(MemorizedPayeeRow memorizedPayeesRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                var memorizedLineItemRow = NewMemorizedLineItemRow();

                UpdateMemorizedLineItem(memorizedLineItemRow, memorizedPayeesRow, categoryId, categoryAccountId, memo, amount);

                Rows.Add(memorizedLineItemRow);

                return memorizedLineItemRow;
            }

            public MemorizedLineItemRow Update(MemorizedLineItemRow memorizedLineItemRow, MemorizedPayeeRow memorizedPayeesRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                UpdateMemorizedLineItem(memorizedLineItemRow, memorizedPayeesRow, categoryId, categoryAccountId, memo, amount);

                return memorizedLineItemRow;
            }

            private static MemorizedLineItemRow UpdateMemorizedLineItem(MemorizedLineItemRow memorizedLineItemsRow, MemorizedPayeeRow memorizedPayeesRow, bool transfer, DataRow AccountOrCategory, string memo, decimal amount)
            {
                memorizedLineItemsRow.MemorizedPayeeID = memorizedPayeesRow.ID;

                memorizedLineItemsRow.IsTransfer = transfer;
                if (AccountOrCategory == null)
                {
                    memorizedLineItemsRow.SetAccountIDNull();
                    memorizedLineItemsRow.SetCategoryIDNull();
                }
                else if (transfer)
                {
                    memorizedLineItemsRow.AccountID = (int)AccountOrCategory["ID"];
                    memorizedLineItemsRow.SetCategoryIDNull();
                }
                else
                {
                    memorizedLineItemsRow.SetAccountIDNull();
                    memorizedLineItemsRow.CategoryID = (int)AccountOrCategory["ID"];
                }

                if (string.IsNullOrWhiteSpace(memo))
                {
                    memorizedLineItemsRow.SetMemoNull();
                }
                else
                {
                    memorizedLineItemsRow.Memo = memo;
                }

                memorizedLineItemsRow.Amount = amount;

                return memorizedLineItemsRow;
            }

            private static MemorizedLineItemRow UpdateMemorizedLineItem(MemorizedLineItemRow memorizedLineItemsRow, MemorizedPayeeRow memorizedPayeesRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                memorizedLineItemsRow.MemorizedPayeeID = memorizedPayeesRow.ID;

                memorizedLineItemsRow.IsTransfer = categoryAccountId >= 0;
                if (categoryId >= 0)
                {
                    memorizedLineItemsRow.SetAccountIDNull();
                    memorizedLineItemsRow.CategoryID = categoryId;
                }
                else if (categoryAccountId >= 0)
                {
                    memorizedLineItemsRow.AccountID = categoryAccountId;
                    memorizedLineItemsRow.SetCategoryIDNull();
                }
                else
                {
                    memorizedLineItemsRow.SetAccountIDNull();
                    memorizedLineItemsRow.SetCategoryIDNull();
                }

                if (string.IsNullOrWhiteSpace(memo))
                {
                    memorizedLineItemsRow.SetMemoNull();
                }
                else
                {
                    memorizedLineItemsRow.Memo = memo;
                }

                memorizedLineItemsRow.Amount = amount;

                return memorizedLineItemsRow;
            }
        }
    }
}
