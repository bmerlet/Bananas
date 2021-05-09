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
                foreach (var securityPriceRow in GetChildRows(securityToSecurityPrice))
                {
                    var securityPrice = securityPriceRow as SecurityPricesRow;

                    if (securityPrice.Date.CompareTo(mostRecent) > 0)
                    {
                        mostRecent = securityPrice.Date;
                        price = securityPrice.Value;
                    }
                }

                return price;
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

            private static SecuritiesRow UpdateSecurity(SecuritiesRow secRow, string name, string symbol, ESecurityType type)
            {
                secRow.Name = name;
                secRow.Symbol = symbol;
                secRow.Type = type;

                return secRow;
            }
        }
    }
}
