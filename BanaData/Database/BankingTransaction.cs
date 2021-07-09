//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class BankingTransactionRow
        {
            // Bridges to local enum types
            public ETransactionMedium Medium
            {
                get => (ETransactionMedium)IMedium;
                set => IMedium = (int)value;
            }

            public bool HasSame(ETransactionMedium medium, uint checkNumber)
            {
                if (Medium != medium)
                {
                    return false;
                }

                if (Medium == ETransactionMedium.Check && CheckNumber != checkNumber)
                {
                    return false;
                }

                return true;
            }

        }

        partial class BankingTransactionDataTable
        {
            public BankingTransactionRow Add(TransactionsRow transactionRow, ETransactionMedium medium, uint checkNumber)
            {
                var bankTransRow = NewBankingTransactionRow();

                UpdateBankTransaction(bankTransRow, transactionRow, medium, checkNumber);

                Rows.Add(bankTransRow);

                return bankTransRow;
            }

            public BankingTransactionRow Update(TransactionsRow transactionRow, ETransactionMedium medium, uint checkNumber)
            {
                var bankTransRow = transactionRow.GetBankingTransaction();

                UpdateBankTransaction(bankTransRow, transactionRow, medium, checkNumber);

                return bankTransRow;
            }

            private static BankingTransactionRow UpdateBankTransaction(BankingTransactionRow bankTransRow, TransactionsRow transactionRow, ETransactionMedium medium, uint checkNumber)
            {
                bankTransRow.TransactionID = transactionRow.ID;
                bankTransRow.Medium = medium;
                if (medium == ETransactionMedium.Check)
                {
                    bankTransRow.CheckNumber = checkNumber;
                }
                else
                {
                    bankTransRow.SetCheckNumberNull();
                }

                return bankTransRow;
            }
        }
    }
}
