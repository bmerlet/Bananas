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
        partial class MemorizedLineItemsRow
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

        partial class MemorizedLineItemsDataTable
        {
            public MemorizedLineItemsRow[] GetByMemorizedPayee(Household.MemorizedPayeesRow memorizedPayeesRow)
            {
                var memorizedPayeesToMemorizedLineItem = ParentRelations["FK_MemorizedPayees_MemorizedLineItems"];
                return memorizedPayeesRow.GetChildRows(memorizedPayeesToMemorizedLineItem).Cast<Household.MemorizedLineItemsRow>().ToArray();
            }

            public MemorizedLineItemsRow Add(MemorizedPayeesRow memorizedPayeesRow, bool transfer, DataRow AccountOrCategory, string memo, decimal amount)
            {
                var memorizedLineItemRow = NewMemorizedLineItemsRow();

                UpdateMemorizedLineItem(memorizedLineItemRow, memorizedPayeesRow, transfer, AccountOrCategory, memo, amount);

                Rows.Add(memorizedLineItemRow);

                return memorizedLineItemRow;
            }

            public MemorizedLineItemsRow Add(MemorizedPayeesRow memorizedPayeesRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                var memorizedLineItemRow = NewMemorizedLineItemsRow();

                UpdateMemorizedLineItem(memorizedLineItemRow, memorizedPayeesRow, categoryId, categoryAccountId, memo, amount);

                Rows.Add(memorizedLineItemRow);

                return memorizedLineItemRow;
            }

            public MemorizedLineItemsRow Update(MemorizedLineItemsRow memorizedLineItemRow, MemorizedPayeesRow memorizedPayeesRow, int categoryId, int categoryAccountId, string memo, decimal amount)
            {
                UpdateMemorizedLineItem(memorizedLineItemRow, memorizedPayeesRow, categoryId, categoryAccountId, memo, amount);

                return memorizedLineItemRow;
            }

            private static MemorizedLineItemsRow UpdateMemorizedLineItem(MemorizedLineItemsRow memorizedLineItemsRow, MemorizedPayeesRow memorizedPayeesRow, bool transfer, DataRow AccountOrCategory, string memo, decimal amount)
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

            private static MemorizedLineItemsRow UpdateMemorizedLineItem(MemorizedLineItemsRow memorizedLineItemsRow, MemorizedPayeesRow memorizedPayeesRow, int categoryId, int categoryAccountId, string memo, decimal amount)
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
