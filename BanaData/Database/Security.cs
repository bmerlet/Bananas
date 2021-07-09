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
        partial class SecurityRow
        {
            // Bridges to local enum types
            public ESecurityType Type
            {
                get { return (ESecurityType)IType; }
                set { IType = (int)value; }
            }

            public decimal GetMostRecentPrice(DateTime? limit = null)
            {
                decimal price = 0;
                DateTime mostRecent = DateTime.MinValue;

                foreach (SecurityPriceRow securityPriceRow in GetSecurityPriceRows())
                {
                    if (limit.HasValue && securityPriceRow.Date.CompareTo(limit.Value) > 0)
                    {
                        continue;
                    }

                    if (securityPriceRow.Date.CompareTo(mostRecent) > 0)
                    {
                        mostRecent = securityPriceRow.Date;
                        price = securityPriceRow.Value;
                    }
                }

                return price;
            }

            // Are there transactions associated with this security
            public bool HasTransactions => GetInvestmentTransactionsRows().Length > 0;

            public bool HasSame(string symbol, ESecurityType type)
            {
                if (IsSymbolNull())
                {
                    if (!string.IsNullOrWhiteSpace(symbol))
                    {
                        return false;
                    }
                }
                else if (Symbol != symbol)
                {
                    return false;
                }

                return Type == type;
            }

        }

        partial class SecurityDataTable
        {
            public SecurityRow GetByName(string name)
            {
                try
                {
                    return this.Single(s => s.Name == name);
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }

            public SecurityRow GetBySymbol(string symbol)
            {
                return this.First(sec => !sec.IsSymbolNull() && sec.Symbol == symbol);
            }

            public SecurityRow Add(string name, string symbol, ESecurityType type)
            {
                var secRow = NewSecurityRow();

                UpdateSecurity(secRow, name, symbol, type);

                Rows.Add(secRow);

                return secRow;
            }

            public SecurityRow Update(int id, string name, string symbol, ESecurityType type)
            {
                var secRow = FindByID(id);

                return UpdateSecurity(secRow, name, symbol, type);
            }

            private static SecurityRow UpdateSecurity(SecurityRow secRow, string name, string symbol, ESecurityType type)
            {
                secRow.Name = name;

                if (string.IsNullOrWhiteSpace(symbol))
                {
                    secRow.SetSymbolNull();
                }
                else
                {
                    secRow.Symbol = symbol;
                }

                secRow.Type = type;

                return secRow;
            }
        }
    }
}
