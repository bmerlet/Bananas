using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Immutable class representing a line item for the UI
    /// </summary>
    public class LineItem
    {
        public LineItem(int id, string category, int categoryID, int categoryAccountID, string memo, decimal amount) =>
            (ID, Category, CategoryID, CategoryAccountID, Memo, Amount, AmountString, Payment, Deposit, AbsoluteAmount, AbsoluteAmountString) =
            (id, category, categoryID, categoryAccountID, memo, amount, amount.ToString("N"), amount < 0, amount >= 0, Math.Abs(amount), Math.Abs(amount).ToString("N"));

        public readonly int ID;
        public readonly int CategoryID;
        public readonly int CategoryAccountID;

        public string Category { get; }
        public string Memo { get; }
        public decimal Amount { get; }
        public string AmountString { get; }

        public bool Payment { get; }
        public bool Deposit { get; }
        public decimal AbsoluteAmount { get; }
        public string AbsoluteAmountString { get; }
    }
}
