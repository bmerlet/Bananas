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

            // Get the balance of an account
            public decimal GetBalance()
            {
                return GetBalance(false);
            }

            // Get the reconciled balance of an account
            public decimal GetReconciledBalance()
            {
                return GetBalance(true, ETransactionStatus.Reconciled);
            }

            // Get the balance of a banking account
            private decimal GetBalance(bool filter, ETransactionStatus statusToFilterOn = ETransactionStatus.Pending)
            {
                decimal balance = 0;

                // Find balance from all transactions
                var accountToTransactions = Table.ChildRelations["FK_Accounts_Transactions"];
                foreach (Household.TransactionsRow transaction in GetChildRows(accountToTransactions))
                {
                    if (!filter || transaction.Status == statusToFilterOn)
                    {
                        if (Type == EAccountType.Investment)
                        {
                            var investmentTransaction = Type == EAccountType.Investment ? ((Household)Table.DataSet).InvestmentTransactions.GetByTransaction(transaction) : null;
                            if (investmentTransaction.IsCashIn || investmentTransaction.IsCashOut)
                            {
                                balance += transaction.GetAmount();
                            }
                        }
                        else
                        {
                            balance += transaction.GetAmount();
                        }
                    }
                }

                // Add tansfers to/from that account
                var accountToLineItem = Table.ChildRelations["Accounts_LineItems"];
                foreach (Household.LineItemsRow lineItemRow in GetChildRows(accountToLineItem))
                {
                    var transRow = ((Household)Table.DataSet).Transactions.FindByID(lineItemRow.TransactionID);
                    if (transRow.AccountID != ID)
                    {
                        if (!filter ||
                            (((Household)Table.DataSet).Transactions.FindByID(lineItemRow.TransactionID) is TransactionsRow trans && trans.Status == statusToFilterOn))
                        {
                            balance -= lineItemRow.Amount;
                        }
                    }
                }

                return balance;
            }

            // Get the unreconciled transactions of a banking account
            public IEnumerable<TransactionsRow> GetUnreconciledTransactions()
            {
                var accountToTransactions = Table.ChildRelations["FK_Accounts_Transactions"];

                return GetChildRows(accountToTransactions)
                    .Cast<TransactionsRow>()
                    .Where(tr => tr.Status != ETransactionStatus.Reconciled);
            }

            // Get the unreconciled transfer to a banking account
            public IEnumerable<LineItemsRow> GetUnreconciledTransfers()
            {
                var houshold = (Household)Table.DataSet;

                // Add tansfers to/from that account
                var accountToLineItem = Table.ChildRelations["Accounts_LineItems"];

                return GetChildRows(accountToLineItem)
                    .Cast<LineItemsRow>()
                    .Where(li => houshold.Transactions.FindByID(li.TransactionID).AccountID != ID)
                    .Where(li => li.TransferStatus != ETransactionStatus.Reconciled);
            }

            // Get the balance of an investment account
            public decimal GetInvestmentValue()
            {
                // Handle to the dataset
                var household = Table.DataSet as Household;

                // Compute the portfolio
                var portfolio = new Portfolio();
                var accountToTransaction = Table.ChildRelations["FK_Accounts_Transactions"];
                foreach (Household.TransactionsRow transRow in GetChildRows(accountToTransaction))
                {
                    portfolio.ApplyTransaction(household, transRow);
                }

                // Add tansfers to/from that account
                var accountToLineItem = Table.ChildRelations["Accounts_LineItems"];
                foreach (Household.LineItemsRow lineItemRow in GetChildRows(accountToLineItem))
                {
                    var transRow = household.Transactions.FindByID(lineItemRow.TransactionID);
                    if (transRow.AccountID != ID)
                    {
                        portfolio.ApplyTransfer(household, lineItemRow);
                    }
                }

                // Get latest price for the securities in the portfolio
                return portfolio.GetValuation();
            }

            // Get the securities held in an investment account
            public IEnumerable<int> GetInvestmentSecurities()
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
                return portfolio.GetSecurities();
            }

            // Get the portfolio at a specific date
            public Portfolio GetPortfolio(DateTime? date)
            {
                // Handle to the dataset
                var household = Table.DataSet as Household;

                // Compute the portfolio
                var portfolio = new Portfolio();
                var accountToTransaction = Table.ChildRelations["FK_Accounts_Transactions"];
                foreach (TransactionsRow transRow in GetChildRows(accountToTransaction))
                {
                    if (date.HasValue && transRow.Date.CompareTo(date.Value) >= 0)
                    {
                        continue;
                    }

                    portfolio.ApplyTransaction(household, transRow);
                }

                // Get latest price for the securities in the portfolio
                return portfolio;
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
