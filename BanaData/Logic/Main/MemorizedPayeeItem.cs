using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Logic.Main
{
    /// <summary>
    /// Class representing memorized payees, as viewed in the autocomplete payee textbox
    /// </summary>
    public class MemorizedPayeeItem : IComparable<MemorizedPayeeItem>
    {
        public MemorizedPayeeItem(string payee, decimal amount, string category, string memo)
        {
            Payee = payee;
            Amount = amount.ToString("N");
            Category = category;
            Memo = memo;
        }

        public string Payee { get; }
        public string Amount { get; }
        public string Category { get; }
        public string Memo { get; }

        public int CompareTo(MemorizedPayeeItem other)
        {
            return Payee.CompareTo(other.Payee);
        }

        // string that is used to filter on (and to return) 
        public override string ToString()
        {
            return Payee;
        }
    }
}
