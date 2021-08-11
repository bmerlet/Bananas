//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Attributes;

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
        }

        partial class BankingTransactionDataTable
        {
            //
            // Medium helpers
            //

            // Strings for medium
            static private readonly string MEDIUM_NEXTCHECKNUM = EnumDescriptionAttribute.GetDescription(ETransactionMedium.NextCheckNum);
            static private readonly string MEDIUM_ATM = EnumDescriptionAttribute.GetDescription(ETransactionMedium.ATM);
            static private readonly string MEDIUM_DEPOSIT = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Deposit);
            static private readonly string MEDIUM_DIVIDEND = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Dividend);
            static private readonly string MEDIUM_EFT = EnumDescriptionAttribute.GetDescription(ETransactionMedium.EFT);
            static private readonly string MEDIUM_TRANSFER = EnumDescriptionAttribute.GetDescription(ETransactionMedium.Transfer);

            public static string[] MediumSource { get; } =
            {
                MEDIUM_NEXTCHECKNUM, MEDIUM_ATM, MEDIUM_DEPOSIT, MEDIUM_DIVIDEND, MEDIUM_EFT, MEDIUM_TRANSFER
            };

            public static string GetMediumString(ETransactionMedium medium, uint checkNumber)
            {
                string rs = "???";

                if (medium == ETransactionMedium.Check)
                {
                    if (checkNumber > 0)
                    {
                        rs = checkNumber.ToString();
                    }
                }
                else
                {
                    rs = EnumDescriptionAttribute.GetDescription(medium);
                }

                return rs;
            }

            public static ETransactionMedium ParseMediumString(string str)
            {
                return EnumDescriptionAttribute.MatchDescription<ETransactionMedium>(str);
            }

            public static (ETransactionMedium medium, decimal checkNumber) ParseMediumString(string str, AccountRow accountRow)
            {
                decimal checkNumber = 0;
                ETransactionMedium medium;

                if (str == MEDIUM_NEXTCHECKNUM)
                {
                    medium = ETransactionMedium.Check;
                    checkNumber = GetNextCheckNumber(accountRow);
                }
                else if (MediumSource.Contains(str))
                {
                    medium = EnumDescriptionAttribute.MatchDescription<ETransactionMedium>(str);
                }
                else
                {
                    if (uint.TryParse(str, out uint checkNum))
                    {
                        medium = ETransactionMedium.Check;
                        checkNumber = checkNum;
                    }
                    else
                    {
                        medium = ETransactionMedium.None;
                    }
                }

                return (medium, checkNumber);
            }

            public static decimal GetNextCheckNumber(AccountRow accountRow)
            {
                decimal checkNumber = accountRow.GetRegularTransactionRows()
                    .Select(tr => tr.GetBankingTransaction())
                    .Where(btr => !btr.IsCheckNumberNull())
                    .Select(btr => btr.CheckNumber)
                    .Max();

                return checkNumber += 1;
            }

            public BankingTransactionRow Add(TransactionRow transactionRow, ETransactionMedium medium, uint checkNumber)
            {
                var bankTransRow = NewBankingTransactionRow();

                UpdateBankTransaction(bankTransRow, transactionRow, medium, checkNumber);

                Rows.Add(bankTransRow);

                return bankTransRow;
            }

            public BankingTransactionRow Update(TransactionRow transactionRow, ETransactionMedium medium, uint checkNumber)
            {
                var bankTransRow = transactionRow.GetBankingTransaction();

                UpdateBankTransaction(bankTransRow, transactionRow, medium, checkNumber);

                return bankTransRow;
            }

            private static BankingTransactionRow UpdateBankTransaction(BankingTransactionRow bankTransRow, TransactionRow transactionRow, ETransactionMedium medium, uint checkNumber)
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
