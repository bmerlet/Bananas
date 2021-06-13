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
        // Explicit constructor
        public LineItem(int id, string category, int categoryID, int categoryAccountID, string memo, decimal amount) =>
            (ID, Category, CategoryID, CategoryAccountID, Memo, Amount, AmountString) =
            (id, category, categoryID, categoryAccountID, memo, amount, amount.ToString("N"));

        // Clone with a new ID
        public LineItem(LineItem src, int id) =>
            (ID, Category, CategoryID, CategoryAccountID, Memo, Amount, AmountString) =
            (id, src.Category, src.CategoryID, src.CategoryAccountID, src.Memo, src.Amount, src.Amount.ToString("N"));
        public readonly int ID;
        public readonly int CategoryID;
        public readonly int CategoryAccountID;

        public string Category { get; }
        public string Memo { get; }
        public decimal Amount { get; }
        public string AmountString { get; }

        public bool Payment => Amount < 0;
        public bool Deposit => Amount >= 0;
        public decimal AbsoluteAmount => Math.Abs(Amount);
        public string AbsoluteAmountString => Math.Abs(Amount).ToString("N");
    }
}
