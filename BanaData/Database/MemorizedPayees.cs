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

            public bool HasSame(string payee, ETransactionStatus status, string memo)
            {
                if (Payee != payee || Status != status)
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

                return true;
            }
        }

        partial class MemorizedPayeesDataTable
        {
            public MemorizedPayeesRow Add(string payee, ETransactionStatus status, string memo)
            {
                var memorizedPayeeRow = NewMemorizedPayeesRow();

                UpdateMemorisedPayee(memorizedPayeeRow, payee, status, memo);

                Rows.Add(memorizedPayeeRow);

                return memorizedPayeeRow;
            }

            public MemorizedPayeesRow Update(MemorizedPayeesRow memorizedPayeeRow, string payee, ETransactionStatus status, string memo)
            {
                UpdateMemorisedPayee(memorizedPayeeRow, payee, status, memo);

                return memorizedPayeeRow;
            }

            private static MemorizedPayeesRow UpdateMemorisedPayee(MemorizedPayeesRow memorizedPayeesRow, string payee, ETransactionStatus status, string memo)
            {
                memorizedPayeesRow.Payee = payee;
                memorizedPayeesRow.Status = status;

                if (string.IsNullOrWhiteSpace(memo))
                {
                    memorizedPayeesRow.SetMemoNull();
                }
                else
                {
                    memorizedPayeesRow.Memo = memo;
                }

                return memorizedPayeesRow;
            }
        }
    }
}
