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
    /// <summary>
    /// An amount of security bought at one point in time
    /// </summary>
    public class Lot
    {
        public Lot(DateTime date, Household.SecuritiesRow security, decimal quantity, decimal securityPrice) =>
            (Date, Security, Quantity, SecurityPrice) = (date, security, quantity, securityPrice);

        public DateTime Date { get; }

        public Household.SecuritiesRow Security { get; }

        public decimal Quantity { get; }

        public decimal SecurityPrice { get; }

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
                o.Quantity == Quantity &&
                o.SecurityPrice == SecurityPrice;
        }

        public override int GetHashCode()
        {
            int hashCode = 1531903380;
            hashCode = hashCode * -1521134295 + Date.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Household.SecuritiesRow>.Default.GetHashCode(Security);
            hashCode = hashCode * -1521134295 + Quantity.GetHashCode();
            hashCode = hashCode * -1521134295 + SecurityPrice.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// UI-oriented representation of a lot used for a sale
    /// </summary>
    public class UsedLot
    {
        public UsedLot(DateTime date, decimal totalQuantity, decimal quantityUsed, decimal costBasis, decimal gain) =>
            (Date, TotalQuantity, QuantityUsed, CostBasis, Gain) = (date, totalQuantity, quantityUsed, costBasis, gain);

        public DateTime Date { get; }
        public decimal TotalQuantity { get; }
        public decimal QuantityUsed { get; }
        public decimal CostBasis { get; }
        public decimal Gain { get; }
    }

}
