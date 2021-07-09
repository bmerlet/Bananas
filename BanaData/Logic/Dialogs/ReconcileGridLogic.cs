using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Items;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs
{
    /// <summary>
    /// Logic for one reconcile grid
    /// </summary>
    public class ReconcileGridLogic : LogicBase
    {
        #region Private members

        private readonly Household.AccountRow accountRow;

        #endregion

        #region Constructor

        public ReconcileGridLogic(
            Household.AccountRow _accountRow,
            string title,
            IEnumerable<TransactionToReconcile> _transactions)
        {
            accountRow = _accountRow;

            // Init title
            Title = title;

            // Give the transactions to the UI
            foreach(var transaction in _transactions)
            {
                transactions.Add(transaction);
                transaction.TransactionCleared += OnTransactionCleared;

            }
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));

            UpdateClearedTotal();
        }

        #endregion

        #region Events

        // Activated on transaction status change
        public event EventHandler ClearedBalanceChanged;

        #endregion

        #region UI properties

        // Title
        public string Title { get; }

        // Transactions
        private readonly ObservableCollection<TransactionToReconcile> transactions = new ObservableCollection<TransactionToReconcile>();
        public CollectionView Transactions { get; }

        // Total cleared
        public decimal TotalCleared { get; private set; }
        public string NumberOfCheckedItems { get; private set; }

        // What we use for description
        public string DescriptionColumnName => accountRow.Type == EAccountType.Investment ? "description" : "Payee";
        public double DescriptionColumnWidth => accountRow.Type == EAccountType.Investment ? 250 : 150;

        // If this is a bank
        public bool IsMediumVisible => accountRow.Type == EAccountType.Bank;
        public double MediumColumnWidth => IsMediumVisible ? 80 : 0;

        // If we have a symbol column
        public double SymbolColumnWidth => IsSymbolVisible ? 80 : 0;
        public bool IsSymbolVisible => accountRow.Type == EAccountType.Investment;

        #endregion

        #region Actions

        // Activated when the cleared status changes for a transaction
        private void OnTransactionCleared(object sender, EventArgs e)
        {
            UpdateClearedTotal();
        }

        private void UpdateClearedTotal()
        {
            TotalCleared = transactions.Sum(tr => tr.IsCleared == true ? tr.Amount : 0);
            OnPropertyChanged(() => TotalCleared);

            NumberOfCheckedItems = "Cleared transactions: " + transactions.Count(tr => tr.IsCleared == true);
            OnPropertyChanged(() => NumberOfCheckedItems);

            ClearedBalanceChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
