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
        partial class SecurityPricesDataTable
        {
            public SecurityPricesRow Add(SecuritiesRow security, DateTime date, decimal price)
            {
                var priceRow = NewSecurityPricesRow();

                UpdateSecurityPrice(priceRow, security, date, price);

                Rows.Add(priceRow);

                return priceRow;
            }

            private static SecurityPricesRow UpdateSecurityPrice(SecurityPricesRow priceRow, SecuritiesRow security, DateTime date, decimal price)
            {
                priceRow.SecurityID = security.ID;
                priceRow.Date = date;
                priceRow.Value = price;

                return priceRow;
            }
        }
    }
}
