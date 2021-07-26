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

            public bool IsShowingTransactions
            {
                get => Flags.HasFlag(ETransactionReportFlag.ShowTransactions);
                set { if (value) Flags |= ETransactionReportFlag.ShowTransactions; else Flags &= ~ETransactionReportFlag.ShowTransactions; }
            }

            public bool IsShowingSubtotals
            {
                get => Flags.HasFlag(ETransactionReportFlag.ShowSubtotals);
                set { if (value) Flags |= ETransactionReportFlag.ShowSubtotals; else Flags &= ~ETransactionReportFlag.ShowSubtotals; }
            }

            public bool IsGroupingByAccount
            {
                get => Flags.HasFlag(ETransactionReportFlag.GroupByAccount);
                set { if (value) Flags |= ETransactionReportFlag.GroupByAccount; else Flags &= ~ETransactionReportFlag.GroupByAccount; }
            }

            public bool IsGroupingByPayee
            {
                get => Flags.HasFlag(ETransactionReportFlag.GroupByPayee);
                set { if (value) Flags |= ETransactionReportFlag.GroupByPayee; else Flags &= ~ETransactionReportFlag.GroupByPayee; }
            }

            public bool IsGroupingByCategory
            {
                get => Flags.HasFlag(ETransactionReportFlag.GroupByCategory);
                set { if (value) Flags |= ETransactionReportFlag.GroupByCategory; else Flags &= ~ETransactionReportFlag.GroupByCategory; }
            }

            public bool IsShowingAccountColumn
            {
                get => Flags.HasFlag(ETransactionReportFlag.ShowAccountColumn);
                set { if (value) Flags |= ETransactionReportFlag.ShowAccountColumn; else Flags &= ~ETransactionReportFlag.ShowAccountColumn; }
            }

            public bool IsShowingDateColumn
            {
                get => Flags.HasFlag(ETransactionReportFlag.ShowDateColumn);
                set { if (value) Flags |= ETransactionReportFlag.ShowDateColumn; else Flags &= ~ETransactionReportFlag.ShowDateColumn; }
            }

            public bool IsShowingPayeeColumn
            {
                get => Flags.HasFlag(ETransactionReportFlag.ShowPayeeColumn);
                set { if (value) Flags |= ETransactionReportFlag.ShowPayeeColumn; else Flags &= ~ETransactionReportFlag.ShowPayeeColumn; }
            }

            public bool IsShowingMemoColumn
            {
                get => Flags.HasFlag(ETransactionReportFlag.ShowMemoColumn);
                set { if (value) Flags |= ETransactionReportFlag.ShowMemoColumn; else Flags &= ~ETransactionReportFlag.ShowMemoColumn; }
            }

            public bool IsShowingCategoryColumn
            {
                get => Flags.HasFlag(ETransactionReportFlag.ShowCategoryColumn);
                set { if (value) Flags |= ETransactionReportFlag.ShowCategoryColumn; else Flags &= ~ETransactionReportFlag.ShowCategoryColumn; }
            }

            public bool IsShowingStatusColumn
            {
                get => Flags.HasFlag(ETransactionReportFlag.ShowStatusColumn);
                set { if (value) Flags |= ETransactionReportFlag.ShowStatusColumn; else Flags &= ~ETransactionReportFlag.ShowStatusColumn; }
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
