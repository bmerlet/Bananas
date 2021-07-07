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
            public bool HasTransactions => GetTransactionsRows().Length > 0;

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
                foreach (var transaction in GetTransactionsRows())
                {
                    if (!filter || transaction.Status == statusToFilterOn)
                    {
                        if (Type == EAccountType.Investment)
                        {
                            var investmentTransaction = Type == EAccountType.Investment ? transaction.GetInvestmentTransaction() : null;
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
                foreach (var lineItemRow in GetLineItemsRows())
                {
                    var transRow = lineItemRow.TransactionsRow;
                    if (transRow.AccountID != ID)
                    {
                        if (!filter || lineItemRow.TransferStatus == statusToFilterOn)
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
                return GetTransactionsRows().Where(tr => tr.Status != ETransactionStatus.Reconciled);
            }

            // Get the unreconciled transfer to a banking account
            public IEnumerable<LineItemsRow> GetUnreconciledTransfers()
            {
                return GetLineItemsRows()
                    .Where(li => li.TransactionsRow.AccountID != ID)
                    .Where(li => li.TransferStatus != ETransactionStatus.Reconciled);
            }

            // Get the balance of an investment account
            public decimal GetInvestmentValue()
            {
                // Compute the portfolio
                var portfolio = new Portfolio();
                foreach (var transRow in GetTransactionsRows())
                {
                    portfolio.ApplyTransaction(transRow);
                }

                // Add tansfers to/from that account
                foreach (var lineItemRow in GetLineItemsRows())
                {
                    if (lineItemRow.TransactionsRow.AccountID != ID)
                    {
                        portfolio.ApplyTransfer(lineItemRow);
                    }
                }

                // Get latest price for the securities in the portfolio
                return portfolio.GetValuation();
            }

            // Get the securities held in an investment account
            public IEnumerable<int> GetInvestmentSecurities()
            {
                // Compute the portfolio
                var portfolio = new Portfolio();
                foreach (var transRow in GetTransactionsRows())
                {
                    portfolio.ApplyTransaction(transRow);
                }

                // Get latest price for the securities in the portfolio
                return portfolio.GetSecurities();
            }

            // Get the portfolio at a specific date
            public Portfolio GetPortfolio(DateTime? date)
            {
                // Compute the portfolio at the specified date
                var portfolio = new Portfolio();
                foreach (TransactionsRow transRow in GetTransactionsRows())
                {
                    if (date.HasValue && transRow.Date.CompareTo(date.Value) > 0)
                    {
                        continue;
                    }

                    portfolio.ApplyTransaction(transRow);
                }

                return portfolio;
            }

            public bool HasSame(string description, EAccountType type, decimal creditLimit, EInvestmentKind kind)
            {
                if (IsDescriptionNull())
                {
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        return false;
                    }
                }
                else if (Description != description)
                {
                    return false;
                }

                if (Type != type)
                {
                    return false;
                }

                if (CreditLimit != creditLimit)
                {
                    return false;
                }

                if (type == EAccountType.Investment)
                {
                    if (Kind != kind &&
                        !(Kind == EInvestmentKind.TraditionalIRA && kind == EInvestmentKind.Brokerage))
                    {
                        return false;
                    }
                }

                return true;
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
                accRow.CreditLimit = creditLimit;

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
