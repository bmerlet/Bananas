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
            // Get the current checkpoint ID
            public CheckpointRow GetCurrentCheckpoint()
            {
                // First time around, or if we are dealing with an older database
                if (Rows.Count != 2)
                {
                    InitializeCheckpoints();
                }

                // Find base and current row
                CheckpointRow curRow;
                try
                {
                    var baseRow = this.Where(c => c.Date == DateTime.MinValue).Single();
                    curRow = this.Where(c => c.Date != DateTime.MinValue).Single();
                }
                catch (Exception)
                {
                    // Older database. Fix up and retry
                    InitializeCheckpoints();
                    return GetCurrentCheckpoint();
                }

                return curRow;
            }

            // Create new checkpoint
            public void CreateNewCheckpoint()
            {
                // First time around, or if we are dealing with an older database
                if (Rows.Count != 2)
                {
                    InitializeCheckpoints();
                }

                CheckpointRow baseRow;
                CheckpointRow curRow;

                // Find base and current row
                try
                {
                    baseRow = this.Where(c => c.Date == DateTime.MinValue).Single();
                    curRow = this.Where(c => c.Date != DateTime.MinValue).Single();
                }
                catch (Exception)
                {
                    InitializeCheckpoints();
                    CreateNewCheckpoint();
                    return;
                }

                // Move all transactions to base row
                foreach (var transactionRow in ((Household)DataSet).RegularTransactions)
                {
                    transactionRow.CheckpointRow = baseRow;
                }

                // Time-stamp current row
                curRow.Date = DateTime.Now;
            }

            public void InitializeCheckpoints()
            {
                // See if in correct format
                bool doInit = Rows.Count != 2;
                if (!doInit)
                {
                    try
                    {
                        this.Where(c => c.Date == DateTime.MinValue).Single();
                        this.Where(c => c.Date != DateTime.MinValue).Single();
                    }
                    catch (Exception)
                    {
                        doInit = true;
                    }
                }

                if (doInit)
                {
                    var household = (Household)DataSet;

                    // Create the base and current rows
                    var baseRow = AddCheckpointRow(DateTime.MinValue);
                    var curRow = AddCheckpointRow(DateTime.Now);

                    // Support for older databases:
                    // Migrate all existing transactions to the base row
                    foreach (var transactionRow in household.Transaction.Rows.Cast<Household.TransactionRow>())
                    {
                        transactionRow.CheckpointRow = baseRow;
                    }

                    // Support for older databases:
                    // Delete all old checkpoints
                    foreach (CheckpointRow row in Rows)
                    {
                        if (row != baseRow && row != curRow)
                        {
                            row.Delete();
                        }
                    }

                    household.AcceptChanges();
                }
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
