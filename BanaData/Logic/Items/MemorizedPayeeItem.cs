using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Immutable class representing a memorized payee for the UI
    /// </summary>
    public class MemorizedPayeeItem : IComparable<MemorizedPayeeItem>
    {
        #region Constructor

        public MemorizedPayeeItem(int id, string payee, string memo, LineItem[] lineItems)
        {
            (ID, Payee, LineItems) = (id, payee, lineItems);

            if (lineItems.Length == 1)
            {
                (Category, Memo, Amount, IsSplit) = 
                    (lineItems[0].Category, memo, lineItems[0].Amount, false);
            }
            else
            {
                decimal sum = lineItems.Sum(li => li.Amount);
                (Category, Memo, Amount, IsSplit) = 
                    ("<Split>", memo, sum, true);
            }
        }

        #endregion

        #region Logic properties

        public readonly int ID;
        public readonly LineItem[] LineItems;

        #endregion

        #region UI properties

        public string Payee { get; }
        public string Category { get; }
        public bool? IsSplit { get; }
        public string Memo { get; }
        public decimal Amount { get; }

        #endregion

        #region IComparable implementation

        public int CompareTo(MemorizedPayeeItem other)
        {
            return Payee.CompareTo(other.Payee);
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"{Payee} cat {Category} memo {Memo} split {IsSplit} Amount {Amount}";
        }

        public override bool Equals(object obj)
        {
            return obj is MemorizedPayeeItem o && o.ID == ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        #endregion
    }
}
