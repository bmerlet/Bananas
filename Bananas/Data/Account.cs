//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bananas.Data
{
    // Extensions to the account table
    public partial class Household
    {
        partial class AccountsRow
        {
            // Bridges to local enum types
            public EAccountType Type
            {
                get { return (EAccountType)IType; }
                set { IType = (int)value; }
            }

            public EInvestmentKind Kind
            {
                get { return (EInvestmentKind)IKind; }
                set { IKind = (int)value; }
            }

            public void SetKindNull()
            {
                SetIKindNull();
            }

            public bool IsKindNull()
            {
                return IsIKindNull();
            }

            // Get the balance of a banking account
            public decimal GetBankingBalance()
            {
                decimal balance = 0;

                var accountToTransactions = Table.ChildRelations["FK_Accounts_Transactions"];
                foreach (var transaction in GetChildRows(accountToTransactions))
                {
                    balance += (transaction as Household.TransactionsRow).GetAmount();
                }

                return balance;
            }

            // Get the balance of an investment account
            public decimal GetInvestmentValue()
            {
                // Handle to the dataset
                var household = Table.DataSet as Household;

                // Compute the portfolio
                var portfolio = new Portfolio();
                var accountToTransaction = Table.ChildRelations["FK_Accounts_Transactions"];
                foreach (var transRow in GetChildRows(accountToTransaction))
                {
                    portfolio.ApplyTransaction(household, transRow as Household.TransactionsRow);
                }

                // Get latest price for the securities in the portfolio
                return portfolio.GetValuation(household);
            }
        }

        partial class AccountsDataTable
        {
            // Get account by name
            public AccountsRow GetByName(string name)
            {
                return this.SingleOrDefault(acc => acc.Name == name);
            }

            // Get banking accounts
            public AccountsRow[] GetBankingAccounts()
            {
                var lquery =
                    from acc in this
                    where acc.Type == EAccountType.Cash || acc.Type == EAccountType.Bank || acc.Type == EAccountType.CreditCard
                    select acc;

                return lquery.ToArray();
            }

            // Get investment accounts
            public AccountsRow[] GetInvestmentAccounts()
            {
                var lquery =
                    from acc in this
                    where acc.Type == EAccountType.Investment
                    select acc;

                return lquery.ToArray();
            }

            // Get asset accounts
            public AccountsRow[] GetAssetAccounts()
            {
                var lquery =
                    from acc in this
                    where acc.Type == EAccountType.OtherAsset || acc.Type == EAccountType.OtherLiability
                    select acc;

                return lquery.ToArray();
            }


            // Adding/updating rows
            public AccountsRow Add(string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind)
            {
                var accRow = NewAccountsRow();

                UpdateAccount(accRow, name, description, type, creditLimit, kind);

                Rows.Add(accRow);

                return accRow;
            }

            private static void UpdateAccount(AccountsRow accRow, string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind)
            {
                accRow.Name = name;
                accRow.Description = description;
                accRow.Type = type;

                // Credit-card specific
                if (type == EAccountType.CreditCard)
                {
                    accRow.CreditLimit = creditLimit;
                }
                else
                {
                    accRow.SetCreditLimitNull();
                }

                // Investment specific
                if (type == EAccountType.Investment)
                {
                    accRow.Kind = kind;
                }
                else
                {
                    accRow.SetKindNull();
                }
            }
        }
    }
}
