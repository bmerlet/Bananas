using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class MemorizedPayeeRow
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

        partial class MemorizedPayeeDataTable
        {
            public MemorizedPayeeRow Add(string payee, ETransactionStatus status, string memo)
            {
                var memorizedPayeeRow = NewMemorizedPayeeRow();

                UpdateMemorisedPayee(memorizedPayeeRow, payee, status, memo);

                Rows.Add(memorizedPayeeRow);

                return memorizedPayeeRow;
            }

            public MemorizedPayeeRow Update(MemorizedPayeeRow memorizedPayeeRow, string payee, ETransactionStatus status, string memo)
            {
                UpdateMemorisedPayee(memorizedPayeeRow, payee, status, memo);

                return memorizedPayeeRow;
            }

            private static MemorizedPayeeRow UpdateMemorisedPayee(MemorizedPayeeRow memorizedPayeesRow, string payee, ETransactionStatus status, string memo)
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
