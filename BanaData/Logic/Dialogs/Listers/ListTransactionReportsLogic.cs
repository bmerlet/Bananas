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
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Listers
{
    public class ListTransactionReportsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public ListTransactionReportsLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            foreach (Household.TransactionReportRow reportRow in mainWindowLogic.Household.TransactionReport.Rows)
            {
                reportsSource.Add(TransactionReportItem.CreateFromDB(reportRow));
            }

            ReportsSource = (CollectionView)CollectionViewSource.GetDefaultView(reportsSource);
            ReportsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            AddCommand = new CommandBase(OnAddReport);
            EditCommand = new CommandBase(OnEditReport);
            DeleteCommand = new CommandBase(OnDeleteReport);
        }

        #endregion

        #region UI properties

        public TransactionReportItem SelectedReport { get; set; }
        public TransactionReportItem ReportToScrollTo { get; private set; }

        private readonly ObservableCollection<TransactionReportItem> reportsSource = new ObservableCollection<TransactionReportItem>();
        public CollectionView ReportsSource { get; }

        public CommandBase AddCommand { get; }
        public CommandBase EditCommand { get; }
        public CommandBase DeleteCommand { get; }

        #endregion

        #region Actions

        private void OnAddReport()
        {
            // Create new report
            var report = TransactionReportItem.CreateEmpty();

            var logic = new EditTransactionReportLogic(mainWindowLogic, report);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new account
                var newReport = logic.NewTransactionReportItem;

                // Commit change
                newReport = AddReportToDataSet(newReport);

                // Update UI
                reportsSource.Add(newReport);
                SelectedReport = newReport;
                ReportToScrollTo = newReport;
                OnPropertyChanged(() => SelectedReport);
                OnPropertyChanged(() => ReportToScrollTo);
            }
        }

        private void OnEditReport()
        {
            if (SelectedReport != null)
            {
                var logic = new EditTransactionReportLogic(mainWindowLogic, SelectedReport);
                if (mainWindowLogic.GuiServices.ShowDialog(logic))
                {
                    // Get modified account
                    var newReport = logic.NewTransactionReportItem;

                    // Commit change
                    UpdateReportInDataSet(newReport);

                    // Update UI
                    reportsSource.Remove(SelectedReport);
                    reportsSource.Add(newReport);
                    SelectedReport = newReport;
                    ReportToScrollTo = newReport;
                    OnPropertyChanged(() => SelectedReport);
                    OnPropertyChanged(() => ReportToScrollTo);
                }
            }
        }

        private void OnDeleteReport()
        {
            if (SelectedReport != null)
            {
                // Commit change
                RemoveReportFromDataSet(SelectedReport);

                reportsSource.Remove(SelectedReport);
            }
        }

        private TransactionReportItem AddReportToDataSet(TransactionReportItem newReport)
        {
            var household = mainWindowLogic.Household;

            // Create and commit new report
            var reportRow = household.TransactionReport.NewTransactionReportRow();

            reportRow.Name = newReport.Name;
            if (!string.IsNullOrWhiteSpace(newReport.Description))
            {
                reportRow.Description = newReport.Description;
            }
            reportRow.StartDate = newReport.StartDate;
            reportRow.EndDate = newReport.EndDate;
            reportRow.IsFilteringOnAccounts = newReport.IsFilteringOnAccounts;
            reportRow.IsFilteringOnPayees = newReport.IsFilteringOnPayees;
            reportRow.IsFilteringOnCategories = newReport.IsFilteringOnCategories;

            household.TransactionReport.AddTransactionReportRow(reportRow);

            foreach (var account in newReport.Accounts)
            {
                var acctRow = household.TransactionReportAccount.NewTransactionReportAccountRow();
                acctRow.TransactionReportID = reportRow.ID;
                acctRow.AccountID = account.ID;
                household.TransactionReportAccount.AddTransactionReportAccountRow(acctRow);
            }

            foreach (var payee in newReport.Payees)
            {
                var payeeRow = household.TransactionReportPayee.NewTransactionReportPayeeRow();
                payeeRow.TransactionReportID = reportRow.ID;
                payeeRow.Payee = payee;
                household.TransactionReportPayee.AddTransactionReportPayeeRow(payeeRow);
            }

            foreach (var category in newReport.Categories)
            {
                var catRow = household.TransactionReportCategory.NewTransactionReportCategoryRow();
                catRow.TransactionReportID = reportRow.ID;
                catRow.CategoryID = category.ID;
                household.TransactionReportCategory.AddTransactionReportCategoryRow(catRow);
            }

            mainWindowLogic.CommitChanges();

            // Note that a new ID is created automatically, so we need to update the account item with it
            return TransactionReportItem.CreateFromDB(reportRow);
        }

        private void UpdateReportInDataSet(TransactionReportItem updatedReport)
        {
            // Update the row
            var reportRow = updatedReport.TransactionReportRow;
            reportRow.Name = updatedReport.Name;
            if (string.IsNullOrWhiteSpace(updatedReport.Description))
            {
                reportRow.SetDescriptionNull();
            }
            else
            {
                reportRow.Description = updatedReport.Description;
            }
            reportRow.StartDate = updatedReport.StartDate;
            reportRow.EndDate = updatedReport.EndDate;
            reportRow.IsFilteringOnAccounts = updatedReport.IsFilteringOnAccounts;
            reportRow.IsFilteringOnPayees = updatedReport.IsFilteringOnPayees;
            reportRow.IsFilteringOnCategories = updatedReport.IsFilteringOnCategories;

            // Update existing report account rows
            foreach (var existingReportAccountRow in reportRow.GetTransactionReportAccountRows())
            {
                // Remove accounts that are not present anymore
                if (!updatedReport.Accounts.Contains(existingReportAccountRow.AccountRow))
                {
                    existingReportAccountRow.Delete();
                }
            }
            foreach (var account in updatedReport.Accounts)
            {
                // Add accounts that are not present
                if (reportRow.GetTransactionReportAccountRows().FirstOrDefault(trar => trar.AccountRow == account) == null)
                {
                    var household = mainWindowLogic.Household;
                    var acctRow = household.TransactionReportAccount.NewTransactionReportAccountRow();
                    acctRow.TransactionReportID = reportRow.ID;
                    acctRow.AccountID = account.ID;
                    household.TransactionReportAccount.AddTransactionReportAccountRow(acctRow);
                }
            }

            // Update existing report payees
            foreach (var existingReportPayeeRow in reportRow.GetTransactionReportPayeeRows())
            {
                // Remove payees that are not present anymore
                if (!updatedReport.Payees.Contains(existingReportPayeeRow.Payee))
                {
                    existingReportPayeeRow.Delete();
                }
            }
            foreach (var payee in updatedReport.Payees)
            {
                // Add payees that are not present
                if (reportRow.GetTransactionReportPayeeRows().FirstOrDefault(trpr => trpr.Payee == payee) == null)
                {
                    var household = mainWindowLogic.Household;
                    var payeeRow = household.TransactionReportPayee.NewTransactionReportPayeeRow();
                    payeeRow.TransactionReportID = reportRow.ID;
                    payeeRow.Payee = payee;
                    household.TransactionReportPayee.AddTransactionReportPayeeRow(payeeRow);
                }
            }

            // Update existing report category rows
            foreach (var existingReportCategoryRow in reportRow.GetTransactionReportCategoryRows())
            {
                // Remove categories that are not present anymore
                if (!updatedReport.Categories.Contains(existingReportCategoryRow.CategoryRow))
                {
                    existingReportCategoryRow.Delete();
                }
            }
            foreach (var category in updatedReport.Categories)
            {
                // Add categories that are not present
                if (reportRow.GetTransactionReportCategoryRows().FirstOrDefault(trcr => trcr.CategoryRow == category) == null)
                {
                    var household = mainWindowLogic.Household;
                    var catRow = household.TransactionReportCategory.NewTransactionReportCategoryRow();
                    catRow.TransactionReportID = reportRow.ID;
                    catRow.CategoryID = category.ID;
                    household.TransactionReportCategory.AddTransactionReportCategoryRow(catRow);
                }
            }

            // Commit
            mainWindowLogic.CommitChanges();
        }

        private void RemoveReportFromDataSet(TransactionReportItem report)
        {
            // Remove the report
            report.TransactionReportRow.Delete();

            mainWindowLogic.CommitChanges();
        }

        #endregion
    }
}
