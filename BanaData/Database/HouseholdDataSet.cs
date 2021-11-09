using System;
using System.Linq;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class ScheduleRow
        {
            // Bridge to local enum type
            public EScheduleFrequency Frequency
            {
                get => (EScheduleFrequency)IFrequency;
                set => IFrequency = (int)value;
            }

            public EScheduleFlag Flags
            {
                get => (EScheduleFlag)IFlags;
                set => IFlags = (int)value;
            }
        }

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
            public CheckpointRow GetMostRecentCheckpoint()
            {
                CheckpointRow mostRecent = null;

                if (Rows.Count == 0)
                {
                    return CreateNewCheckpoint();
                }

                foreach (CheckpointRow checkpointRow in Rows)
                {
                    if (mostRecent == null || checkpointRow.Date.CompareTo(mostRecent.Date) > 0)
                    {
                        mostRecent = checkpointRow;
                    }
                }

                return mostRecent;
            }

            // Create new checkpoint
            public CheckpointRow CreateNewCheckpoint()
            {
                // Make sure we don't create a duplicate
                if (Rows.Count > 0)
                {
                    CheckpointRow mostRecent = GetMostRecentCheckpoint();
                    while (DateTime.Now.Equals(mostRecent.Date))
                    {
                        _ = System.Threading.Thread.Yield();
                    }
                }

                return AddCheckpointRow(DateTime.Now);
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
                        $"Line item transfer record points to account {lineItemTransfer.AccountRow.Name}" + eol +
                        $"  while the peer transaction points to account {lineItemTransfer.TransactionRow.AccountRow.Name}" + eol;
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
