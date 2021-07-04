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
        partial class SecuritiesRow
        {
            // Bridges to local enum types
            public ESecurityType Type
            {
                get { return (ESecurityType)IType; }
                set { IType = (int)value; }
            }

            public decimal GetMostRecentPrice()
            {
                decimal price = 0;
                DateTime mostRecent = DateTime.MinValue;

                var securityToSecurityPrice = Table.ChildRelations["FK_Securities_SecurityPrices"];
                foreach (SecurityPricesRow securityPriceRow in GetChildRows(securityToSecurityPrice))
                {
                    if (securityPriceRow.Date.CompareTo(mostRecent) > 0)
                    {
                        mostRecent = securityPriceRow.Date;
                        price = securityPriceRow.Value;
                    }
                }

                return price;
            }

            public IEnumerable<SecurityPricesRow> GetSecurityPrices()
            {
                var securityToSecurityPrice = Table.ChildRelations["FK_Securities_SecurityPrices"];
                return GetChildRows(securityToSecurityPrice).Cast<SecurityPricesRow>();
            }

            // Are there transactions associated with this security
            public bool HasTransactions
            {
                get
                {
                    var securityToTransactions = Table.ChildRelations["FK_Securities_InvestmentTransactions"];
                    return GetChildRows(securityToTransactions).Length > 0;
                }
            }

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

        partial class SecuritiesDataTable
        {
            public SecuritiesRow GetByName(string name)
            {
                return this.Single(s => s.Name == name);
            }

            public SecuritiesRow GetBySymbol(string symbol)
            {
                return this.First(sec => !sec.IsSymbolNull() && sec.Symbol == symbol);
            }

            public SecuritiesRow Add(string name, string symbol, ESecurityType type)
            {
                var secRow = NewSecuritiesRow();

                UpdateSecurity(secRow, name, symbol, type);

                Rows.Add(secRow);

                return secRow;
            }

            public SecuritiesRow Update(int id, string name, string symbol, ESecurityType type)
            {
                var secRow = FindByID(id);

                return UpdateSecurity(secRow, name, symbol, type);
            }

            private static SecuritiesRow UpdateSecurity(SecuritiesRow secRow, string name, string symbol, ESecurityType type)
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
