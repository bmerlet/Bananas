using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public partial class Household
    {
        #region Extensions to the Account Row

        partial class StatementAccountHintRow
        {
            // Bridges to local enum types
            public EInstitution Institution
            {
                get => (EInstitution)IInstitution;
                set => IInstitution = (int)value;
            }
        }

        #endregion

        partial class StatementAccountHintDataTable
        {
            // Adding/updating rows
            public StatementAccountHintRow Add(EInstitution institution, int accountID, int minPage, int maxPage)
            {
                var newRow = NewStatementAccountHintRow();

                Update(newRow, institution, accountID, minPage, maxPage);

                Rows.Add(newRow);

                return newRow;
            }

            public void Update(StatementAccountHintRow row, EInstitution institution, int accountID, int minPage, int maxPage)
            {
                row.IInstitution = (int)institution;
                row.AccountID = accountID;
                row.MinPage = minPage;
                row.MaxPage = maxPage;
            }

        }
    }
}
