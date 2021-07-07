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
        public TransactionToReconcile(int id, bool _isCleared, DateTime date, string medium, string description, string descriptionColumnName, decimal amount, bool isTransferFillIn) =>
            (ID, isCleared, Date, Medium, Description, DescriptionColumnName, Amount, IsTransferFillIn) =
            (id, _isCleared, date, medium, description, descriptionColumnName, amount, isTransferFillIn);

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

        // Read-only UI properties
        public DateTime Date { get; }
        public string Medium { get; }
        public string Description { get; }
        public string DescriptionColumnName { get; }
        public decimal Amount { get; }
    }

}
