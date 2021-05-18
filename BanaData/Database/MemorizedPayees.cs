using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class MemorizedPayeesRow
        {
            // Bridges to local enum types
            public ETransactionStatus Status
            {
                get { return (ETransactionStatus)IStatus; }
                set { IStatus = (int)value; }
            }

        }

        partial class MemorizedPayeesDataTable
        {
            public MemorizedPayeesRow Add(string payee, ETransactionStatus status)
            {
                var memorizedPayeeRow = NewMemorizedPayeesRow();

                UpdateMemorisedPayee(memorizedPayeeRow, payee, status);

                Rows.Add(memorizedPayeeRow);

                return memorizedPayeeRow;
            }

            private static MemorizedPayeesRow UpdateMemorisedPayee(MemorizedPayeesRow memorizedPayeesRow, string payee, ETransactionStatus status)
            {
                if (payee == null)
                {
                    memorizedPayeesRow.SetPayeeNull();
                }
                else
                {
                    memorizedPayeesRow.Payee = payee;
                }

                memorizedPayeesRow.Status = status;

                return memorizedPayeesRow;
            }
        }
    }
}
