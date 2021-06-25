using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Collections;

namespace BanaData.Logic.Main
{
    public class InvestmentRegisterLogic : AbstractRegisterLogic
    {
        #region Private members

        // Actual collection of transactions backing the Transactions collection view property
        private readonly WpfObservableRangeCollection<InvestmentTransactionLogic> transactions = new WpfObservableRangeCollection<InvestmentTransactionLogic>();

        // Temporary list used when bulk-adding transactions
        private readonly List<InvestmentTransactionLogic> temporaryTransactionList = new List<InvestmentTransactionLogic>();

        #endregion

        #region Constructor

        public InvestmentRegisterLogic(MainWindowLogic mainWindowLogic)
            : base(mainWindowLogic)
        {
            // Create transaction collection view, and sort by date
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            Transactions.GroupDescriptions.Add(new PropertyGroupDescription("GroupSorter"));

            DeleteTransaction = new CommandBase(OnDeleteTransaction);

            // Create column width manager
            Widths = new ColumnWidths(mainWindowLogic);
        }

        #endregion

        #region UI properties

        // Selected transaction
        private InvestmentTransactionLogic selectedTransaction;
        private bool logicIsChangingSelection;
        public InvestmentTransactionLogic SelectedTransaction
        {
            get => selectedTransaction;
            set
            {
                if (value != selectedTransaction)
                {
                    if (logicIsChangingSelection)
                    {
                        // This logic is changing the selection (e.g. processing of return key)
                        selectedTransaction = value;
                        editedTransaction = value;
                        OnPropertyChanged(() => SelectedTransaction);
                    }
                    else
                    {
                        // User changed selection (e.g. by clicking on a row)
                        if (editedTransaction != null && transactions.Contains(editedTransaction))
                        {
                            editedTransaction.CancelEdit();
                        }
                        selectedTransaction = value;
                        editedTransaction = value;
                    }

                    if (selectedTransaction != null)
                    {
                        selectedTransaction.BeginEdit();
                    }
                    OnPropertyChanged(() => EditedTransaction);
                    OnPropertyChanged("UpdateOverlayPosition");
                }
            }
        }

        // Transaction to show
        public InvestmentTransactionLogic TransactionToScrollTo { get; private set; }

        // Transaction being edited
        private InvestmentTransactionLogic editedTransaction;
        public InvestmentTransactionLogic EditedTransaction
        {
            get => editedTransaction;
            set { editedTransaction = value; OnPropertyChanged(() => EditedTransaction); }
        }

        // Contect menu commands
        public CommandBase DeleteTransaction { get; }

        // Column widths
        public ColumnWidths Widths { get; }

        #endregion

        #region Actions & Hooks for abstract base class

        protected override void ClearTransactionList() => temporaryTransactionList.Clear();
        protected override void PublishTransactionList() => transactions.ReplaceRange(temporaryTransactionList);
        protected override IEnumerable<AbstractTransactionLogic> AbstractTransactions => transactions.Cast<AbstractTransactionLogic>();

        // Routine to add a transaction from the DB into the list
        protected override void AddDBTransactionToList(Household.AccountsRow accountRow, Household.TransactionsRow transRow, List<LineItem> lineItems, bool bulk)
        {
            var household = mainWindowLogic.Household;

            // Get investment transaction info
            var investmentTransRow = household.InvestmentTransactions.GetByTransaction(transRow);

            // Create data
            var transactionData = new InvestmentTransactionLogic.InvestmentTransactionData(
                transRow.Date,
                transRow.IsPayeeNull() ? "" : transRow.Payee,
                transRow.Status,
                lineItems,
                investmentTransRow.Type,
                investmentTransRow .IsSecurityIDNull() ? - 1 : investmentTransRow.SecurityID,
                investmentTransRow.IsSecurityPriceNull() ?  0 : investmentTransRow.SecurityPrice,
                investmentTransRow.IsSecurityQuantityNull() ? 0 : investmentTransRow.SecurityQuantity,
                investmentTransRow.Commission);

            var investmentTransaction = new InvestmentTransactionLogic(mainWindowLogic, this, accountID, transRow.ID, transactionData);

            if (bulk)
            {
                temporaryTransactionList.Add(investmentTransaction);
            }
            else
            {
                transactions.Add(investmentTransaction);
            }
        }

        public void MoveDownOneTransaction(bool wasEmptyTransaction)
        {
            if (wasEmptyTransaction)
            {
                // Create an empty transaction if we consumed the previous one
                AddEmptyTransactionAtBottom();
            }
            else
            {
                // Move the selection down one row otherwise
                var nextTransaction = GetNextTransaction(SelectedTransaction);

                logicIsChangingSelection = true;
                SelectedTransaction = nextTransaction as InvestmentTransactionLogic;
                logicIsChangingSelection = false;
            }
        }

        public void RemoveTransactionFromList(InvestmentTransactionLogic itl)
        {
            if (SelectedTransaction == itl)
            {
                logicIsChangingSelection = true;
                SelectedTransaction = null;
                logicIsChangingSelection = false;
            }
            transactions.Remove(itl);
        }

        public void AddTransactionBackToList(InvestmentTransactionLogic itl)
        {
            transactions.Add(itl);
        }

        protected override void AddEmptyTransactionAtBottom()
        {
            // Add new empty transaction at the bottom
            var emptyTransaction = new InvestmentTransactionLogic(mainWindowLogic, this, accountID);
            transactions.Add(emptyTransaction);
            //Transactions.Refresh();

            // Select it
            mainWindowLogic.GuiServices.ExecuteAsync((Action)delegate ()
            {
                logicIsChangingSelection = true;
                SelectedTransaction = emptyTransaction;
                logicIsChangingSelection = false;

                // Go to the bottom
                //TransactionToScrollTo = bankingTransaction;
                //OnPropertyChanged(() => TransactionToScrollTo);
                OnPropertyChanged("ScrollToBottom");
            });
        }

        private void OnDeleteTransaction(object arg)
        {
            InvestmentTransactionLogic itl = arg == null ? EditedTransaction : arg as InvestmentTransactionLogic;
            if (itl == null)
            {
                return;
            }

            if (itl.TransID < 0)
            {
                // Can't remove the empty transaction
                return;
            }

            // Cancel all changes
            itl.CancelEdit();

            // Delete from dataset
            var household = mainWindowLogic.Household;
            var transactionRow = household.Transactions.FindByID(itl.TransID);

            // Delete all line items
            var lineItems = household.LineItems.GetByTransaction(transactionRow);
            foreach (var lineItem in lineItems)
            {
                lineItem.Delete();
            }

            // Delete investment transaction
            household.InvestmentTransactions.GetByTransaction(transactionRow).Delete();

            // Finally delete the transaction
            transactionRow.Delete();
            mainWindowLogic.CommitChanges();

            // Delete from list
            transactions.Remove(itl);
            Transactions.Refresh();

            // Move away
            //MoveDownOneTransaction(false);

            // Compute balances
            RecomputeBalances();
        }

        // Override to compute share balances in addition to cash balance
        public override void RecomputeBalances()
        {
            // Compute cash balance
            base.RecomputeBalances();

            // Compute share balances

            // securityId -> share balance
            var dico = new Dictionary<int, decimal>();

            foreach (InvestmentTransactionLogic itl in Transactions)
            {
                decimal balance = decimal.MinValue;
                var secuID = itl.SecurityID;
 
                if (itl.IsSecurityIn)
                {
                    balance = itl.SecurityQuantityDecimal;
                    if (dico.ContainsKey(secuID))
                    {
                        balance += dico[secuID];
                        dico.Remove(secuID);
                    }
                    dico.Add(itl.SecurityID, balance);
                }
                else if (itl.IsSecurityOut)
                {
                    balance = -itl.SecurityQuantityDecimal;
                    if (dico.ContainsKey(secuID))
                    {
                        balance += dico[secuID];
                        dico.Remove(secuID);
                    }
                    dico.Add(itl.SecurityID, balance);
                }

                itl.ShareBalance = balance;
            }
        }

        #endregion

        #region Supporting classes

        public class ColumnWidths : LogicBase
        {
            private readonly MainWindowLogic mainWindowLogic;

            public ColumnWidths(MainWindowLogic _mainWindowLogic) => mainWindowLogic = _mainWindowLogic;

            public double WidthOfStatusColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfStatusColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfStatusColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfStatusColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfStatusColumn);
                    }
                }
            }

            public double WidthOfDateColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfDateColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfDateColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfDateColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDateColumn);
                    }
                }
            }

            public double WidthOfTypeColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfTypeColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfTypeColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfTypeColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfTypeColumn);
                    }
                }
            }

            public double WidthOfDescriptionColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfDescriptionColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfDescriptionColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfDescriptionColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDescriptionColumn);
                    }
                }
            }

            public double WidthOfMemoColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfMemoColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfMemoColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfMemoColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfMemoColumn);
                    }
                }
            }

            public double WidthOfCategoryColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfCategoryColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfCategoryColumn);
                    }
                }
            }

            public double WidthOfSecuritySymbolColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfSecuritySymbolColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfSecuritySymbolColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfSecuritySymbolColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfSecuritySymbolColumn);
                    }
                }
            }

            public double WidthOfSecurityQuantityColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfSecurityQuantityColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfSecurityQuantityColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfSecurityQuantityColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfSecurityQuantityColumn);
                    }
                }
            }

            public double WidthOfSecurityPriceColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfSecurityPriceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfSecurityPriceColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfSecurityPriceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfSecurityPriceColumn);
                    }
                }
            }

            public double WidthOfSecurityBalanceColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfSecurityBalanceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfSecurityBalanceColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfSecurityBalanceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfSecurityBalanceColumn);
                    }
                }
            }

            public double WidthOfAmountColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfAmountColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfAmountColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfAmountColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfAmountColumn);
                    }
                }
            }

            public double WidthOfBalanceColumn
            {
                get => mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn != value)
                    {
                        mainWindowLogic.UserSettings.InvstWidthOfBalanceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfBalanceColumn);
                    }
                }
            }

        }

        #endregion
    }
}
