using System;
using System.Linq;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class TransactionReportRow
        {
            public ETransactionReportFlag Flags
            {
                get => (ETransactionReportFlag)IFlags;
                set => IFlags = (int)value;
            }

            public bool IsFilteringOnAccounts
            {
                get => Flags.HasFlag(ETransactionReportFlag.IsFilteringOnAccounts);
                set { if (value) Flags |= ETransactionReportFlag.IsFilteringOnAccounts; else Flags &= ~ETransactionReportFlag.IsFilteringOnAccounts; }
            }

            public bool IsFilteringOnPayees
            {
                get => Flags.HasFlag(ETransactionReportFlag.IsFilteringOnPayees);
                set { if (value) Flags |= ETransactionReportFlag.IsFilteringOnPayees; else Flags &= ~ETransactionReportFlag.IsFilteringOnPayees; }
            }

            public bool IsFilteringOnCategories
            {
                get => Flags.HasFlag(ETransactionReportFlag.IsFilteringOnCategories);
                set { if (value) Flags |= ETransactionReportFlag.IsFilteringOnCategories; else Flags &= ~ETransactionReportFlag.IsFilteringOnCategories; }
            }
        }

        partial class CheckpointDataTable
        {
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
    }
}
