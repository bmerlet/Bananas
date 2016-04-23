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
    class Lot
    {
        DateTime date;
        Household.SecuritiesRow security;
        decimal quantity;

        public Lot(DateTime date, Household.SecuritiesRow security, decimal quantity)
        {
            this.date = date;
            this.security = security;
            this.quantity = quantity;
        }

        public DateTime Date
        {
            get { return date; }
        }

        public Household.SecuritiesRow Security
        {
            get { return security; }
        }

        public decimal Quantity
        {
            get { return quantity; }
        }

        public decimal GetValuation()
        {
            decimal price = security.GetMostRecentPrice();
            return price * quantity;
        }
    }
}
