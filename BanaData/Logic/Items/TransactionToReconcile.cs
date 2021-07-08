using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.UILogic;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// A transaction as it appears in the reconcile dialogs
    /// </summary>
    public class TransactionToReconcile : LogicBase
    {
        // Constructor
        public TransactionToReconcile(int id, bool _isCleared, DateTime date, string medium, string description, string symbol, decimal amount, bool isTransferFillIn) =>
            (ID, isCleared, Date, Medium, Description, Symbol, Amount, IsTransferFillIn) =
            (id, _isCleared, date, medium, description, symbol, amount, isTransferFillIn);

        // Identifier - transaction ID
        public readonly int ID;
        public bool IsTransferFillIn;

        // Event
        public event EventHandler TransactionCleared;

        // Only modifiable UI property: If the item is checked
        private bool isCleared;
        public bool? IsCleared
        {
            get => isCleared;
            set
            {
                if (isCleared != value)
                {
                    isCleared = value == true;
                    OnPropertyChanged(() => IsCleared);
                    TransactionCleared?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        //
        // Read-only UI properties
        //

        // Date
        public DateTime Date { get; }

        // Medium (for bank accounts only)
        public string Medium { get; }

        // Description (payee for banks, generated description for investments)
        public string Description { get; }

        // Security quantity (for investments)
        public string Symbol { get; }

        // dollar amount
        public decimal Amount { get; }
    }

}
