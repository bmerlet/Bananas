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

            // Get the unreconciled transactions of an account
            public IEnumerable<TransactionRow> GetUnreconciledTransactions()
            {
                return GetRegularTransactionRows().Where(tr => tr.Status != ETransactionStatus.Reconciled);
            }

            #region Banking utilities

            // Are there transactions associated with this account?
            public bool HasTransactions => GetTransactionRows().Length > 0;

            // Get all the regular transactions on this account
            public IEnumerable<TransactionRow> GetRegularTransactionRows()
            {
                return GetTransactionRows().Where(tr => tr.Type == ETransactionType.Regular);
            }

            // Get the current balance of a bank account
            public decimal GetBalance()
            {
                return GetBalance(null, null, null);
            }

            // Get the reconciled balance of a bank account
            public decimal GetReconciledBalance()
            {
                return GetBalance(ETransactionStatus.Reconciled, null, null);
            }

            // Get the balance of a banking account
            private decimal GetBalance(ETransactionStatus? status, DateTime? fromDate, DateTime? toDate)
            {
                decimal balance = 0;

                // Find balance from all transactions
                foreach (var transaction in GetRegularTransactionRows())
                {
                    if (fromDate.HasValue && transaction.Date.CompareTo(fromDate.Value) <= 0)
                    {
                        continue;
                    }

                    if (toDate.HasValue && transaction.Date.CompareTo(toDate.Value) > 0)
                    {
                        continue;
                    }

                    if (!status.HasValue || transaction.Status == status.Value)
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

                return balance;
            }

            #endregion

            #region Investment utilties

            //
            // Value
            //

            // Get the current value of an investment account
            public decimal GetInvestmentValue()
            {
                // Compute the portfolio
                var portfolio = GetPortfolio();

                // Get latest price for the securities in the portfolio
                return portfolio.GetValuation(null);
            }

            // Get the value of an investment account at a specified time ZZZZ toDel
            public decimal GetInvestmentValue(DateTime date)
            {
                // Compute the portfolio at the specified date
                var portfolio = GetPortfolio(date);

                // Get latest price for the securities in the portfolio
                return portfolio.GetValuation(date);
            }

            //
            // Portfolio
            //

            // Get current portfolio
            public Portfolio GetPortfolio()
            {
                return GetPortfolio(null, null, null, null);
            }

            // Get portfolio atr a specific time
            public Portfolio GetPortfolio(DateTime date, TransactionRow excludedTransaction = null)
            {
                return GetPortfolio(null, null, null, date, excludedTransaction);
            }

            // Get reconciled portfolio
            public Portfolio GetReconciledPortfolio()
            {
                return GetPortfolio(null, ETransactionStatus.Reconciled, null, null);
            }

            // Internal portfolio-building workhorse
            private Portfolio GetPortfolio(Portfolio portfolio, ETransactionStatus? status, DateTime? fromDate, DateTime? toDate, Household.TransactionRow excludedTransaction = null)
            {
                // Create portfolio if none supplied
                if (portfolio == null)
                {
                    portfolio = new Portfolio();
                }

                // Shift the portfolio to the specified time
                foreach (TransactionRow transRow in GetRegularTransactionRows())
                {
                    if (transRow == excludedTransaction)
                    {
                        continue;
                    }

                    if (fromDate.HasValue && transRow.Date.CompareTo(fromDate.Value) <= 0)
                    {
                        continue;
                    }

                    if (toDate.HasValue && transRow.Date.CompareTo(toDate.Value) > 0)
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

            #endregion
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
