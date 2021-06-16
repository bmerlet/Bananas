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
        partial class BankingTransactionsRow
        {
            // Bridges to local enum types
            public ETransactionMedium Medium
            {
                get => (ETransactionMedium)IMedium;
                set => IMedium = (int)value;
            }
        }

        partial class BankingTransactionsDataTable
        {
            public BankingTransactionsRow GetByTransaction(TransactionsRow transaction)
            {
                return this.Single(it => it.TransactionID == transaction.ID);
            }

            public BankingTransactionsRow Add(TransactionsRow transactionRow, ETransactionMedium medium, uint checkNumber)
            {
                var bankTransRow = NewBankingTransactionsRow();

                UpdateBankTransaction(bankTransRow, transactionRow, medium, checkNumber);

                Rows.Add(bankTransRow);

                return bankTransRow;
            }

            public BankingTransactionsRow Update(TransactionsRow transactionRow, ETransactionMedium medium, uint checkNumber)
            {
                var bankTransRow = GetByTransaction(transactionRow);

                UpdateBankTransaction(bankTransRow, transactionRow, medium, checkNumber);

                return bankTransRow;
            }

            private static BankingTransactionsRow UpdateBankTransaction(BankingTransactionsRow bankTransRow, TransactionsRow transactionRow, ETransactionMedium medium, uint checkNumber)
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
