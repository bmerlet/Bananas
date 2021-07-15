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

namespace BanaData.Logic.Dialogs
{
    public class EditTransactionReportsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public EditTransactionReportsLogic(MainWindowLogic _mainWindowLogic)
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
            var report = new TransactionReportItem(null, "", "", DateTime.Today, DateTime.Today);

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

            household.TransactionReport.AddTransactionReportRow(reportRow);

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
            {
                reportRow.Description = updatedReport.Description;
            }
            reportRow.StartDate = updatedReport.StartDate;
            reportRow.EndDate = updatedReport.EndDate;

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
