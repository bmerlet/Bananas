using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

using BanaData.Database;
using BanaData.Logic.Dialogs.Editors;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Listers
{
    /// <summary>
    /// Logic to edit the list of memorized payees
    /// </summary>
    public class ListMemorizedPayeesLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public ListMemorizedPayeesLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            memorizedPayeesSource = new ObservableCollection<MemorizedPayeeItem>();
            mainWindowLogic.MemorizedPayees.ForEach(mpi => memorizedPayeesSource.Add(mpi));
            MemorizedPayeesSource = (CollectionView)CollectionViewSource.GetDefaultView(memorizedPayeesSource);
            MemorizedPayeesSource.SortDescriptions.Add(new SortDescription("Payee", ListSortDirection.Ascending));
            MemorizedPayeesSource.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));

            AddCommand = new CommandBase(OnAddMemorizedPayee);
            EditCommand = new CommandBase(OnEditMemorizedPayee);
            DeleteCommand = new CommandBase(OnDeleteMemorizedPayee);
        }

        #endregion

        #region UI properties

        public MemorizedPayeeItem SelectedMemorizedPayee { get; set; }
        public MemorizedPayeeItem MemorizedPayeeToScrollTo { get; private set; }

        private readonly ObservableCollection<MemorizedPayeeItem> memorizedPayeesSource;
        public CollectionView MemorizedPayeesSource { get; }

        public CommandBase AddCommand { get; }
        public CommandBase EditCommand { get; }
        public CommandBase DeleteCommand { get; }

        #endregion

        #region Actions

        private void OnAddMemorizedPayee()
        {
            // Create new memorized payee
            var newMemorizedPayee = new MemorizedPayeeItem(-1, "", "", new LineItem[1] { new LineItem(mainWindowLogic, -1, "", -1, -1, "", 0, true) });

            var logic = new EditMemorizedPayeeLogic(mainWindowLogic, newMemorizedPayee, true);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new memorized payee
                var newPayee = logic.NewMemorizedPayeeItem;

                // Commit change
                newPayee = AddMemorizedPayeeToDataSet(newPayee);

                // Update UI
                memorizedPayeesSource.Add(newPayee);
                SelectedMemorizedPayee = newPayee;
                MemorizedPayeeToScrollTo = newPayee;
                OnPropertyChanged(() => SelectedMemorizedPayee);
                OnPropertyChanged(() => MemorizedPayeeToScrollTo);
            }
        }

        private void OnEditMemorizedPayee()
        {
            if (SelectedMemorizedPayee != null)
            {
                var logic = new EditMemorizedPayeeLogic(mainWindowLogic, SelectedMemorizedPayee, false);
                if (mainWindowLogic.GuiServices.ShowDialog(logic))
                {
                    // Get modified payee
                    var newPayee = logic.NewMemorizedPayeeItem;

                    // Commit change
                    newPayee = UpdateMemorizedPayeeInDataSet(newPayee);

                    // Update UI
                    memorizedPayeesSource.Remove(SelectedMemorizedPayee);
                    memorizedPayeesSource.Add(newPayee);
                    SelectedMemorizedPayee = newPayee;
                    MemorizedPayeeToScrollTo = newPayee;
                    OnPropertyChanged(() => SelectedMemorizedPayee);
                    OnPropertyChanged(() => MemorizedPayeeToScrollTo);
                }
            }
        }

        private void OnDeleteMemorizedPayee()
        {
            if (SelectedMemorizedPayee != null)
            {
                // Commit change
                RemoveMemorizedPayeeFromDataSet(SelectedMemorizedPayee);

                memorizedPayeesSource.Remove(SelectedMemorizedPayee);
                MemorizedPayeesSource.Refresh();
            }
        }

        private MemorizedPayeeItem AddMemorizedPayeeToDataSet(MemorizedPayeeItem newPayee)
        {
            var household = mainWindowLogic.Household;

            // Commit new payee
            var newPayeeRow = household.MemorizedPayee.Add(newPayee.Payee, ETransactionStatus.Pending, newPayee.Memo);

            // Commit all line items
            var newLineItems = new List<LineItem>();
            foreach (var lineItem in newPayee.LineItems)
            {
                var newRow = household.MemorizedLineItem.Add(newPayeeRow, lineItem.CategoryID, lineItem.CategoryAccountID, lineItem.Memo, lineItem.Amount);

                // Recreate line item with correct ID
                newLineItems.Add(new LineItem(lineItem, newRow.ID));
            }

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateMemorizedPayees();

            // Recreate memorized payee with correct ID
            return new MemorizedPayeeItem(newPayeeRow.ID, newPayeeRow.Payee, newPayee.Memo, newLineItems.ToArray());
        }

        private MemorizedPayeeItem UpdateMemorizedPayeeInDataSet(MemorizedPayeeItem newPayee)
        {
            var household = mainWindowLogic.Household;

            // Update the memorized payee
            var payeeRow = household.MemorizedPayee.FindByID(newPayee.ID);
            household.MemorizedPayee.Update(payeeRow, newPayee.Payee, ETransactionStatus.Pending, newPayee.Memo);

            // Get existing line items
            var oldLineItems = payeeRow.GetMemorizedLineItemRows();

            // Delete line items that don't exist in the new payee
            // Modify the other ones
            var newMemorizedLineItems = new List<LineItem>();
            foreach (var oldLineItem in oldLineItems)
            {
                var newLineItem = newPayee.LineItems.FirstOrDefault(li => li.ID == oldLineItem.ID);
                if (newLineItem == null)
                {
                    oldLineItem.Delete();
                }
                else
                {
                    household.MemorizedLineItem.Update(oldLineItem, payeeRow, newLineItem.CategoryID, newLineItem.CategoryAccountID, newLineItem.Memo, newLineItem.Amount);
                    newMemorizedLineItems.Add(newLineItem);
                }
            }
            mainWindowLogic.CommitChanges();

            // Create the line items that don't exist
            oldLineItems = payeeRow.GetMemorizedLineItemRows();
            foreach (var newLineItem in newPayee.LineItems)
            {
                if (oldLineItems.FirstOrDefault(oli => oli.ID == newLineItem.ID) == null)
                {
                    var newRow = household.MemorizedLineItem.Add(payeeRow, newLineItem.CategoryID, newLineItem.CategoryAccountID, newLineItem.Memo, newLineItem.Amount);

                    // Recreate line item with correct ID
                    newMemorizedLineItems.Add(new LineItem(newLineItem, newRow.ID));
                }
            }

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateMemorizedPayees();

            // Recreate memorized payee with correct line item IDs
            return new MemorizedPayeeItem(payeeRow.ID, payeeRow.Payee, newPayee.Memo, newMemorizedLineItems.ToArray());
        }

        private void RemoveMemorizedPayeeFromDataSet(MemorizedPayeeItem payee)
        {
            var household = mainWindowLogic.Household;

            // Remove the corresponding memorized line items
            foreach (var lineItem in payee.LineItems)
            {
                household.MemorizedLineItem.FindByID(lineItem.ID).Delete();
            }

            // Remove the memorized payee
            household.MemorizedPayee.FindByID(payee.ID).Delete();

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateMemorizedPayees();
        }

        #endregion
    }
}
