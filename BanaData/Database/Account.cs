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
        #region Extensions to the Account Row

        partial class AccountRow
        {
            // Bridges to local enum types
            public EAccountType Type
            {
                get => (EAccountType)IType;
                set => IType = (int)value;
            }

            public EInvestmentKind Kind
            {
                get => (EInvestmentKind)IKind;
                set => IKind = (int)value;
            }

            public void SetKindNull()
            {
                SetIKindNull();
            }

            public bool IsKindNull()
            {
                return IsIKindNull();
            }

            // Safe versions of columns that may be DB null
            public string SDescription
            {
                get => IsDescriptionNull() ? "" : Description;
                set
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        SetDescriptionNull();
                    }
                    else
                    {
                        Description = value;
                    }
                }
            }

            public EInvestmentKind SKind => IsIKindNull() ? EInvestmentKind.Invalid : Kind;

            public string Owner => IsPersonIDNull() ? null : PersonRow.Name; 

            // Are there transactions associated with this account?
            public bool HasTransactions => GetTransactionRows().Length > 0;

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
                foreach (var transaction in GetTransactionRows())
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
                foreach (var lineItemRow in GetLineItemRows())
                {
                    var transRow = lineItemRow.TransactionRow;
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
            public IEnumerable<TransactionRow> GetUnreconciledTransactions()
            {
                return GetTransactionRows().Where(tr => tr.Status != ETransactionStatus.Reconciled);
            }

            // Get the unreconciled transfer to a banking account
            public IEnumerable<LineItemRow> GetUnreconciledTransfers()
            {
                return GetLineItemRows()
                    .Where(li => li.TransactionRow.AccountID != ID)
                    .Where(li => li.TransferStatus != ETransactionStatus.Reconciled);
            }

            // Get the balance of an investment account
            public decimal GetInvestmentValue()
            {
                // Compute the portfolio
                var portfolio = new Portfolio();
                foreach (var transRow in GetTransactionRows())
                {
                    portfolio.ApplyTransaction(transRow);
                }

                // Add tansfers to/from that account
                foreach (var lineItemRow in GetLineItemRows())
                {
                    if (lineItemRow.TransactionRow.AccountID != ID)
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
                foreach (var transRow in GetTransactionRows())
                {
                    portfolio.ApplyTransaction(transRow);
                }

                // Get latest price for the securities in the portfolio
                return portfolio.GetSecurities();
            }

            // Get the portfolio at a specific date
            public Portfolio GetPortfolio(DateTime? date, Household.TransactionRow excludedTransaction = null, ETransactionStatus? status = null)
            {
                // Compute the portfolio at the specified date
                var portfolio = new Portfolio();
                foreach (TransactionRow transRow in GetTransactionRows())
                {
                    if (transRow == excludedTransaction)
                    {
                        continue;
                    }

                    if (date.HasValue && transRow.Date.CompareTo(date.Value) > 0)
                    {
                        continue;
                    }

                    if (status.HasValue && transRow.Status != status.Value)
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

        #endregion

        #region Extensions to the Account table

        partial class AccountDataTable
        {
            // Get account by name
            public AccountRow GetByName(string name)
            {
                return this.SingleOrDefault(acc => acc.Name == name);
            }

            // Get banking accounts
            public AccountRow[] GetBankingAccounts()
            {
                var lquery =
                    from acc in this
                    where acc.Type == EAccountType.Cash || acc.Type == EAccountType.Bank || acc.Type == EAccountType.CreditCard
                    select acc;

                return lquery.ToArray();
            }

            // Get investment accounts
            public AccountRow[] GetInvestmentAccounts()
            {
                var lquery =
                    from acc in this
                    where acc.Type == EAccountType.Investment
                    select acc;

                return lquery.ToArray();
            }

            // Get asset accounts
            public AccountRow[] GetAssetAccounts()
            {
                var lquery =
                    from acc in this
                    where acc.Type == EAccountType.OtherAsset || acc.Type == EAccountType.OtherLiability
                    select acc;

                return lquery.ToArray();
            }


            // Adding/updating rows
            public AccountRow Add(string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind, bool hidden, PersonRow personRow)
            {
                var accRow = NewAccountRow();

                UpdateAccount(accRow, name, description, type, creditLimit, kind, hidden, personRow);

                Rows.Add(accRow);

                return accRow;
            }

            public AccountRow Update(AccountRow accRow, string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind, bool hidden, PersonRow personRow)
            {
                UpdateAccount(accRow, name, description, type, creditLimit, kind, hidden, personRow);

                return accRow;
            }

            private static void UpdateAccount(AccountRow accRow, string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind, bool hidden, PersonRow personRow)
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

                if (personRow == null)
                {
                    accRow.SetPersonIDNull();
                }
                else
                {
                    accRow.PersonID = personRow.ID;
                }
            }
        }

        #endregion
    }
}
