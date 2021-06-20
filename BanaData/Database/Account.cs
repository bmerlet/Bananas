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

            // Are there transactions associated with this account?
            public bool HasTransactions
            {
                get
                {
                    var accountToTransactions = Table.ChildRelations["FK_Accounts_Transactions"];
                    return GetChildRows(accountToTransactions).Length > 0;
                }
            }

            // Get the balance of a banking account
            public decimal GetBankingBalance()
            {
                decimal balance = 0;

                var accountToTransactions = Table.ChildRelations["FK_Accounts_Transactions"];
                foreach (Household.TransactionsRow transaction in GetChildRows(accountToTransactions))
                {
                    balance += transaction.GetAmount();
                }

                return balance;
            }

            // Get the reconciled balance of a banking account
            public decimal GetBankingReconciledBalance()
            {
                decimal balance = 0;

                var accountToTransactions = Table.ChildRelations["FK_Accounts_Transactions"];
                foreach (Household.TransactionsRow transaction in GetChildRows(accountToTransactions))
                {
                    if (transaction.Status == ETransactionStatus.Reconciled)
                    {
                        balance += transaction.GetAmount();
                    }
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
                return portfolio.GetValuation();
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
            public AccountsRow Add(string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind, bool hidden)
            {
                var accRow = NewAccountsRow();

                UpdateAccount(accRow, name, description, type, creditLimit, kind, hidden);

                Rows.Add(accRow);

                return accRow;
            }

            public AccountsRow Update(int id, string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind, bool hidden)
            {
                var accRow = FindByID(id);

                UpdateAccount(accRow, name, description, type, creditLimit, kind, hidden);

                return accRow;
            }

            private static void UpdateAccount(AccountsRow accRow, string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind, bool hidden)
            {
                accRow.Name = name;
                accRow.Type = type;
                accRow.Hidden = hidden;

                // Description is optional
                if (string.IsNullOrWhiteSpace(description))
                {
                    accRow.SetDescriptionNull();
                }
                else
                {
                    accRow.Description = description;
                }

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
