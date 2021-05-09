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
        public Lot(DateTime date, Household.SecuritiesRow security, decimal quantity)
        {
            Date = date;
            Security = security;
            Quantity = quantity;
        }

        public DateTime Date;

        public Household.SecuritiesRow Security { get; private set; }

        public decimal Quantity { get; private set; }

        public decimal GetValuation()
        {
            decimal price = Security.GetMostRecentPrice();
            return price * Quantity;
        }

        public override bool Equals(object obj)
        {
            return
                obj is Lot o &&
                o.Date == Date &&
                o.Security == Security &&
                o.Quantity == Quantity;
        }

        public override int GetHashCode()
        {
            int hashCode = 1531903380;
            hashCode = hashCode * -1521134295 + Date.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Household.SecuritiesRow>.Default.GetHashCode(Security);
            hashCode = hashCode * -1521134295 + Quantity.GetHashCode();
            return hashCode;
        }
    }
}
