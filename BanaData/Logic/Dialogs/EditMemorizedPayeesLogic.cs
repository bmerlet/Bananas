using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using BanaData.Database;

namespace BanaData.Logic.Dialogs
{
    /// <summary>
    /// Logic to edit the list of memorized payees
    /// </summary>
    public class EditMemorizedPayeesLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public EditMemorizedPayeesLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            memorizedPayeesSource = new ObservableCollection<MemorizedPayeeItem>();
            mainWindowLogic.MemorizedPayees.ForEach(mpi => memorizedPayeesSource.Add(mpi));
            MemorizedPayeesSource = (CollectionView)CollectionViewSource.GetDefaultView(memorizedPayeesSource);
            MemorizedPayeesSource.SortDescriptions.Add(new SortDescription("Payee", ListSortDirection.Ascending));

            AddMemorizedPayee = new CommandBase(OnAddMemorizedPayee);
            EditMemorizedPayee = new CommandBase(OnEditMemorizedPayee);
            DeleteMemorizedPayee = new CommandBase(OnDeleteMemorizedPayee);
        }

        #endregion

        #region UI properties

        public MemorizedPayeeItem SelectedMemorizedPayee { get; set; }
        public MemorizedPayeeItem MemorizedPayeeToScrollTo { get; private set; }

        private readonly ObservableCollection<MemorizedPayeeItem> memorizedPayeesSource;
        public CollectionView MemorizedPayeesSource { get; }

        public CommandBase AddMemorizedPayee { get; }
        public CommandBase EditMemorizedPayee { get; }
        public CommandBase DeleteMemorizedPayee { get; }

        #endregion

        #region Actions

        private void OnAddMemorizedPayee()
        {
            // Create new memorized payee
            int mpid = memorizedPayeesSource.Max(mpi => mpi.ID) + 1;
            int liid = memorizedPayeesSource.Max(mpi => mpi.LineItems.Max(liiid => liiid.ID)) + 1;
            var newMemorizedPayee = new MemorizedPayeeItem(mpid, "", new LineItem[1] { new LineItem(liid, "", -1, -1, "", 0) });

            var logic = new EditMemorizedPayeeLogic(mainWindowLogic, newMemorizedPayee, true);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new memorized payee
                var newPayee = logic.NewMemorizedPayeeItem;

                // Commit change
                AddMemorizedPayeeToDataSet(newPayee);

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
            var logic = new EditMemorizedPayeeLogic(mainWindowLogic, SelectedMemorizedPayee, false);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get modified payee
                var newPayee = logic.NewMemorizedPayeeItem;

                // Commit change
                UpdateMemorizedPayeeInDataSet(newPayee);

                // Update UI
                memorizedPayeesSource.Remove(SelectedMemorizedPayee);
                memorizedPayeesSource.Add(newPayee);
                SelectedMemorizedPayee = newPayee;
                MemorizedPayeeToScrollTo = newPayee;
                OnPropertyChanged(() => SelectedMemorizedPayee);
                OnPropertyChanged(() => MemorizedPayeeToScrollTo);
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

        private void AddMemorizedPayeeToDataSet(MemorizedPayeeItem newPayee)
        {
            var household = mainWindowLogic.Household;

            var newPayeeRow = mainWindowLogic.Household.MemorizedPayees.NewRow() as Household.MemorizedPayeesRow;

            // Commit new payee
            newPayeeRow.ID = newPayee.ID;
            newPayeeRow.Payee = newPayee.Payee;
            newPayeeRow.Status = ETransactionStatus.Pending;

            household.MemorizedPayees.Rows.Add(newPayeeRow);

            // Commit all line items
            foreach (var lineItem in newPayee.LineItems)
            {
                var newRow = household.MemorizedLineItems.NewRow() as Household.MemorizedLineItemsRow;

                newRow.ID = lineItem.ID;
                newRow.MemorizedPayeeID = newPayee.ID;

                UpdateOneDataSetLineItem(lineItem, newRow);

                household.MemorizedLineItems.Rows.Add(newRow);
            }

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateMemorizedPayees();
        }

        private void UpdateMemorizedPayeeInDataSet(MemorizedPayeeItem newPayee)
        {
            var household = mainWindowLogic.Household;

            // Update the memorized payee
            var payeeRow = household.MemorizedPayees.FindByID(newPayee.ID);
            if (payeeRow.Payee != newPayee.Payee)
            {
                payeeRow.Payee = newPayee.Payee;
            }

            // Get existing line items
            var oldLineItems = household.MemorizedLineItems.GetByMemorizedPayee(payeeRow);

            // Delete line items that don't exist in the new payee
            // Modify the other ones
            foreach (var oldLineItem in oldLineItems)
            {
                var newLineItem = newPayee.LineItems.FirstOrDefault(li => li.ID == oldLineItem.ID);
                if (newLineItem == null)
                {
                    oldLineItem.Delete();
                }
                else
                {
                    UpdateOneDataSetLineItem(newLineItem, oldLineItem);
                }
            }
            mainWindowLogic.CommitChanges();

            // Create the line items that don't exist
            foreach (var newLineItem in newPayee.LineItems)
            {
                if (oldLineItems.FirstOrDefault(oli => oli.ID == newLineItem.ID) == null)
                {
                    var newRow = household.MemorizedLineItems.NewRow() as Household.MemorizedLineItemsRow;

                    newRow.ID = newLineItem.ID;

                    newRow.MemorizedPayeeID = newPayee.ID;

                    UpdateOneDataSetLineItem(newLineItem, newRow);

                    household.MemorizedLineItems.Rows.Add(newRow);
                }
            }

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateMemorizedPayees();
        }

        private void UpdateOneDataSetLineItem(LineItem lineItem, Household.MemorizedLineItemsRow row)
        {
            if (string.IsNullOrWhiteSpace(lineItem.Memo))
            {
                row.SetMemoNull();
            }
            else
            {
                row.Memo = lineItem.Memo;
            }

            row.IsTransfer = false;
            if (lineItem.CategoryID >= 0)
            {
                row.CategoryID = lineItem.CategoryID;
                row.SetAccountIDNull();
            }
            else if (lineItem.CategoryAccountID >= 0)
            {
                row.SetCategoryIDNull();
                row.AccountID = lineItem.CategoryAccountID;
                row.IsTransfer = true;
            }
            else
            {
                row.SetCategoryIDNull();
                row.SetAccountIDNull();
            }

            row.Amount = lineItem.Amount;
        }

        private void RemoveMemorizedPayeeFromDataSet(MemorizedPayeeItem payee)
        {
            var household = mainWindowLogic.Household;

            // Remove the corresponding memorized line items
            foreach (var lineItem in payee.LineItems)
            {
                household.MemorizedLineItems.FindByID(lineItem.ID).Delete();
            }

            // Remove the memorized payee
            household.MemorizedPayees.FindByID(payee.ID).Delete();

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateMemorizedPayees();
        }

        #endregion
    }
}
