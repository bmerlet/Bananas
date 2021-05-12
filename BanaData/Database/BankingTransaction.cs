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
                get { return (ETransactionMedium)IMedium; }
                set { IMedium = (int)value; }
            }

            public string GetRegisterMediumString()
            {
                string rs = "???";

                switch (Medium)
                {
                    case ETransactionMedium.Check:
                        if (!IsCheckNumberNull())
                        {
                            rs = CheckNumber.ToString();
                        }
                        break;
                    case ETransactionMedium.PrintCheck:
                        rs = "PrtCk";
                        break;
                    case ETransactionMedium.ATM:
                        rs = "ATM";
                        break;
                    case ETransactionMedium.Cash:
                        rs = "Cash";
                        break;
                    case ETransactionMedium.Deposit:
                        rs = "DEP";
                        break;
                    case ETransactionMedium.Dividend:
                        rs = "Div";
                        break;
                    case ETransactionMedium.EFT:
                        rs = "EFT";
                        break;
                    case ETransactionMedium.None:
                        rs = "";
                        break;
                }
                return rs;
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
