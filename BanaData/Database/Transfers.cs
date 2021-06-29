using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class TransfersDataTable
        {
            public TransfersRow GetByLineItemID(int lineItemID)
            {
                var li = ((Household)DataSet).LineItems.FindByID(lineItemID);
                return GetByLineItem(li);
            }

            public TransfersRow GetByLineItem(LineItemsRow lineItemRow)
            {
                foreach (TransfersRow row in Rows)
                {
                    if (row.SourceLineItemID == lineItemRow.ID)
                    {
                        return row;
                    }
                    if (row.TargetLineItemID == lineItemRow.ID)
                    {
                        return row;
                    }
                }

                return null;
            }

            public LineItemsRow GetOtherEnd(int lineItemID)
            {
                var li = ((Household)DataSet).LineItems.FindByID(lineItemID);
                if (li.AccountID < 0)
                {
                    return null;
                }

                return GetOtherEnd(li);
            }

            public LineItemsRow GetOtherEnd(LineItemsRow lineItemRow)
            {
                int otherID = -1;
                LineItemsRow result = null;

                foreach (TransfersRow row in Rows)
                {
                    if (row.SourceLineItemID == lineItemRow.ID)
                    {
                        otherID = row.TargetLineItemID;
                        break;
                    }
                    if (row.TargetLineItemID == lineItemRow.ID)
                    {
                        otherID = row.SourceLineItemID;
                        break;
                    }
                }

                if (otherID >= 0)
                {
                    result = ((Household)DataSet).LineItems.FindByID(otherID);
                }

                return result;
            }

            public TransfersRow Add(int sourceLineItemID, int targetLineItemID)
            {
                var transferRow = NewTransfersRow();

                UpdateTransfer(transferRow, sourceLineItemID, targetLineItemID);

                Rows.Add(transferRow);

                return transferRow;
            }

            public TransfersRow Update(TransfersRow transferRow, int sourceLineItemID, int targetLineItemID)
            {
                UpdateTransfer(transferRow, sourceLineItemID, targetLineItemID);

                return transferRow;
            }

            private static TransfersRow UpdateTransfer(TransfersRow transferRow, int sourceLineItemID, int targetLineItemID)
            {
                transferRow.SourceLineItemID = sourceLineItemID;
                transferRow.TargetLineItemID = targetLineItemID;

                return transferRow;
            }
        }
    }
}
