using System;
using System.Linq;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class AccountsDataTable
        {
        }

        partial class TransactionsDataTable
        {
        }

        partial class SecuritiesDataTable
        {
        }

        partial class SecurityPricesDataTable
        {
        }

        partial class CheckpointsDataTable
        {
            public int GetMostRecentCheckpointID()
            {
                DateTime mostRecent = DateTime.MinValue;
                int id = -1;

                foreach (CheckpointsRow checkpointRow in Rows)
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
