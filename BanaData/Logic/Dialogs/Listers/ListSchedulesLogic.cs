using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using BanaData.Database;
using BanaData.Logic.Dialogs.Editors;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Listers
{
    /// <summary>
    /// Logic to show and manipulate the list of schedules
    /// </summary>
    public class ListSchedulesLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;

        #endregion

        #region Constructor

        public ListSchedulesLogic(MainWindowLogic _mainWindowLogic, Household _household)
        {
            (mainWindowLogic, household) = (_mainWindowLogic, _household);

            schedulesSource = new ObservableCollection<ScheduleItem>();
            foreach(var scheduleRow in household.Schedule)
            {
                schedulesSource.Add(new ScheduleItem(mainWindowLogic, scheduleRow));
            }
            SchedulesSource = (CollectionView)CollectionViewSource.GetDefaultView(schedulesSource);
            SchedulesSource.SortDescriptions.Add(new SortDescription("Account", ListSortDirection.Ascending));
            SchedulesSource.SortDescriptions.Add(new SortDescription("Payee", ListSortDirection.Ascending));

            AddCommand = new CommandBase(OnAddSchedule);
            EditCommand = new CommandBase(OnEditSchedule);
            DeleteCommand = new CommandBase(OnDeleteSchedule);
        }

        #endregion

        #region UI properties

        public ScheduleItem SelectedSchedule { get; set; }
        public ScheduleItem ScheduleToScrollTo { get; private set; }

        private readonly ObservableCollection<ScheduleItem> schedulesSource;
        public CollectionView SchedulesSource { get; }

        public CommandBase AddCommand { get; }
        public CommandBase EditCommand { get; }
        public CommandBase DeleteCommand { get; }

        #endregion

        #region Actions

        private void OnAddSchedule()
        {
            // Create new schedule
            var schedule = new ScheduleItem(
                -1, DateTime.Today, DateTime.Today, EScheduleFrequency.Monthly, EScheduleFlag.None,
                -1, "", "", "", "", new LineItem[] { new LineItem(mainWindowLogic, -1, "", -1, -1, "", 0, true)});

            var logic = new EditScheduleLogic(mainWindowLogic, household, schedule, true);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new schedule
                var newScheduleItem = logic.NewScheduleItem;

                // Commit change
                newScheduleItem = AddScheduleToDataSet(newScheduleItem);

                // Update UI
                schedulesSource.Add(newScheduleItem);
                SelectedSchedule = newScheduleItem;
                ScheduleToScrollTo = newScheduleItem;
                OnPropertyChanged(() => SelectedSchedule);
                OnPropertyChanged(() => ScheduleToScrollTo);
            }
        }

        private void OnEditSchedule()
        {
            if (SelectedSchedule != null)
            {
                var logic = new EditScheduleLogic(mainWindowLogic, household, SelectedSchedule, false);
                if (mainWindowLogic.GuiServices.ShowDialog(logic))
                {
                    // Get modified payee
                    var updatedScheduleItem = logic.NewScheduleItem;

                    // Commit change
                    updatedScheduleItem = UpdateScheduleInDataSet(updatedScheduleItem);

                    // Update UI
                    schedulesSource.Remove(SelectedSchedule);
                    schedulesSource.Add(updatedScheduleItem);
                    SelectedSchedule = updatedScheduleItem;
                    ScheduleToScrollTo = updatedScheduleItem;
                    OnPropertyChanged(() => SelectedSchedule);
                    OnPropertyChanged(() => ScheduleToScrollTo);
                }
            }
        }

        private void OnDeleteSchedule()
        {
            if (SelectedSchedule != null)
            {
                // Commit change
                RemoveMemorizedPayeeFromDataSet(SelectedSchedule);

                schedulesSource.Remove(SelectedSchedule);
                SchedulesSource.Refresh();
            }
        }

        private ScheduleItem AddScheduleToDataSet(ScheduleItem scheduleItem)
        {
            var accountRow = household.Account.GetByName(scheduleItem.Account);

            // Commit new transaction
            var newTransactionRow = household.Transaction.Add(
                accountRow,
                DateTime.MinValue,
                scheduleItem.Payee,
                scheduleItem.Memo,
                ETransactionStatus.Pending,
                household.Checkpoint.GetCurrentCheckpoint(),
                ETransactionType.ScheduledTransaction);

            // Create banking transaction if needed
            if (accountRow.Type == EAccountType.Bank)
            {
                var medium = Household.BankingTransactionDataTable.ParseMediumString(scheduleItem.Medium);
                household.BankingTransaction.Add(newTransactionRow, medium, 0);
            }

            // Commit all line items
            foreach (var lineItem in scheduleItem.LineItems)
            {
                var newRow = household.LineItem.Add(newTransactionRow, lineItem.Memo, lineItem.Amount);
                if (lineItem.CategoryID != -1)
                {
                    household.LineItemCategory.AddLineItemCategoryRow(newRow, household.Category.FindByID(lineItem.CategoryID));
                }
                else if (lineItem.CategoryAccountID != -1)
                {
                    household.LineItemTransfer.AddLineItemTransferRow(newRow, household.Account.FindByID(lineItem.CategoryAccountID), newTransactionRow);
                }
            }

            // Commit the schedule
            var newScheduleRow = household.Schedule.AddScheduleRow(
                scheduleItem.NextDate,
                scheduleItem.EndDate,
                (int)scheduleItem.Frequency,
                (int)scheduleItem.Flags,
                newTransactionRow);

            mainWindowLogic.CommitChanges(household);

            // Recreate schedule with correct IDs
            return new ScheduleItem(mainWindowLogic, newScheduleRow);
        }

        private ScheduleItem UpdateScheduleInDataSet(ScheduleItem updatedScheduleItem)
        {
            // Update schedule
            var scheduleRow = household.Schedule.FindByID(updatedScheduleItem.ID);
            scheduleRow.NextDate = updatedScheduleItem.NextDate;
            scheduleRow.EndDate = updatedScheduleItem.EndDate;
            scheduleRow.Frequency = updatedScheduleItem.Frequency;
            scheduleRow.Flags = updatedScheduleItem.Flags;

            // Update the transaction
            var transactionRow = household.Transaction.FindByID(scheduleRow.TransactionID);
            var oldAccountRow = transactionRow.AccountRow;
            var newAccountRow = household.Account.GetByName(updatedScheduleItem.Account);
            if (oldAccountRow.Type == EAccountType.Bank)
            {
                if (newAccountRow.Type == EAccountType.Bank)
                {
                    var medium = Household.BankingTransactionDataTable.ParseMediumString(updatedScheduleItem.Medium);
                    transactionRow.GetBankingTransaction().Medium = medium;
                }
                else
                {
                    transactionRow.GetBankingTransaction().Delete();
                }
            }
            else if (newAccountRow.Type == EAccountType.Bank)
            {
                var medium = Household.BankingTransactionDataTable.ParseMediumString(updatedScheduleItem.Medium);
                household.BankingTransaction.Add(transactionRow, medium, 0);
            }

            household.Transaction.Update(
                scheduleRow.TransactionID,
                newAccountRow,
                DateTime.MinValue,
                updatedScheduleItem.Payee,
                updatedScheduleItem.Memo,
                ETransactionStatus.Pending,
                household.Checkpoint.GetCurrentCheckpoint(),
                ETransactionType.ScheduledTransaction);


            // Get existing line items
            var existingLineItems = transactionRow.GetLineItemRows();

            // Delete line items that don't exist in the new transaction
            // Modify the other ones
            foreach (var oldLineItem in existingLineItems)
            {
                var newLineItem = updatedScheduleItem.LineItems.FirstOrDefault(li => li.ID == oldLineItem.ID);
                if (newLineItem == null)
                {
                    oldLineItem.CascadeDelete();
                }
                else
                {
                    bool createCategoryOrTransferLineItem = false;

                    household.LineItem.Update(oldLineItem, transactionRow, newLineItem.Memo, newLineItem.Amount);
                    if (oldLineItem.GetLineItemCategoryRow() is Household.LineItemCategoryRow licr)
                    {
                        if (newLineItem.CategoryID != -1)
                        {
                            licr.CategoryID = newLineItem.CategoryID;
                        }
                        else
                        {
                            licr.Delete();
                            createCategoryOrTransferLineItem = true;
                        }
                    }
                    else if (oldLineItem.GetLineItemTransferRow() is Household.LineItemTransferRow litr)
                    {
                        if (newLineItem.CategoryAccountID != -1)
                        {
                            litr.AccountID = newLineItem.CategoryAccountID;
                        }
                        else
                        {
                            litr.Delete();
                            createCategoryOrTransferLineItem = true;
                        }
                    }
                    else
                    {
                        createCategoryOrTransferLineItem = true;
                    }

                    if (createCategoryOrTransferLineItem)
                    {
                        if (newLineItem.CategoryAccountID != -1)
                        {
                            household.LineItemTransfer.AddLineItemTransferRow(oldLineItem, household.Account.FindByID(newLineItem.CategoryAccountID), transactionRow);
                        }
                        else if (newLineItem.CategoryID != -1)
                        {
                            household.LineItemCategory.AddLineItemCategoryRow(oldLineItem, household.Category.FindByID(newLineItem.CategoryID));
                        }
                    }
                }
            }

            // Create the line items that don't exist
            existingLineItems = transactionRow.GetLineItemRows();
            foreach (var newLineItem in updatedScheduleItem.LineItems)
            {
                if (existingLineItems.FirstOrDefault(oli => oli.ID == newLineItem.ID) == null)
                {
                    var newRow = household.LineItem.Add(transactionRow, newLineItem.Memo, newLineItem.Amount);
                    if (newLineItem.CategoryID != -1)
                    {
                        household.LineItemCategory.AddLineItemCategoryRow(newRow, household.Category.FindByID(newLineItem.CategoryID));
                    }
                    else if (newLineItem.CategoryAccountID != -1)
                    {
                        household.LineItemTransfer.AddLineItemTransferRow(newRow, household.Account.FindByID(newLineItem.CategoryAccountID), transactionRow);
                    }
                }
            }

            mainWindowLogic.CommitChanges(household);

            // Recreate memorized payee with correct IDs
            return new ScheduleItem(mainWindowLogic, scheduleRow);
        }

        private void RemoveMemorizedPayeeFromDataSet(ScheduleItem scheduleItem)
        {
            // Remove the line items
            foreach (var lineItem in scheduleItem.LineItems)
            {
                household.LineItem.FindByID(lineItem.ID).CascadeDelete();
            }

            // Remove the schedule
            household.Schedule.FindByID(scheduleItem.ID).Delete();

            // Remove the transaction
            household.Transaction.FindByID(scheduleItem.TransactionID).Delete();

            mainWindowLogic.CommitChanges(household);
        }

        #endregion
    }
}
