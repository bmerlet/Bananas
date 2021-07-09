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
        partial class SecurityPriceDataTable
        {
            public SecurityPriceRow Add(SecurityRow security, DateTime date, decimal price)
            {
                var priceRow = NewSecurityPriceRow();

                UpdateSecurityPrice(priceRow, security, date, price);

                Rows.Add(priceRow);

                return priceRow;
            }

            private static SecurityPriceRow UpdateSecurityPrice(SecurityPriceRow priceRow, SecurityRow security, DateTime date, decimal price)
            {
                priceRow.SecurityID = security.ID;
                priceRow.Date = date;
                priceRow.Value = price;

                return priceRow;
            }
        }
    }
}
