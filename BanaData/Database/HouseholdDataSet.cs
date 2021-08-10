using System;
using System.Linq;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class TransactionReportRow
        {
            // Bridge to local enum type
            public ETransactionReportFlag Flags
            {
                get => (ETransactionReportFlag)IFlags;
                set => IFlags = (int)value;
            }
        }

        partial class CheckpointDataTable
        {
            // Get the most recent checkpoint ID
            public int GetMostRecentCheckpointID()
            {
                DateTime mostRecent = DateTime.MinValue;
                int id = -1;

                foreach (CheckpointRow checkpointRow in Rows)
                {
                    if (checkpointRow.Date.CompareTo(mostRecent) > 0)
                    {
                        mostRecent = checkpointRow.Date;
                        id = checkpointRow.ID;
                    }
                }

                return id;
            }
        }

        // Sanity-check the database
        public string SanityCheck()
        {
            string error = "";
            string eol = Environment.NewLine;

            // Check that there are no mismatched transfers
            foreach (var lineItemTransfer in LineItemTransfer)
            {
                if (lineItemTransfer.RowState != System.Data.DataRowState.Deleted &&
                    lineItemTransfer.LineItemRow.TransactionRow.Type == ETransactionType.Regular &&
                    lineItemTransfer.AccountRow != lineItemTransfer.TransactionRow.AccountRow)
                {
                    error +=
                        $"Line item transfer revord points to account {lineItemTransfer.AccountRow.Name}" + eol
                        + "  while the peer transaction points to account {lineItemTransfer.TransactionRow.AccountRow}" + eol;
                }
            }

            if (string.IsNullOrEmpty(error))
            {
                error = null;
            }
            else
            {
                error = error.Substring(0, error.LastIndexOf(eol));
            }

            return error;
        }
    }
}
