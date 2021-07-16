using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Collections;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs
{
    public class EditTransactionReportLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly TransactionReportItem oldReportItem;

        #endregion

        #region Constructor

        public EditTransactionReportLogic(MainWindowLogic _mainWindowLogic, TransactionReportItem reportItem)
        {
            (mainWindowLogic, oldReportItem) = (_mainWindowLogic, reportItem);

            PickAccounts = new CommandBase(OnPickAccounts);
            PickPayees = new CommandBase(OnPickPayees);
            PickCategories = new CommandBase(OnPickCategories);
            PickColumns = new CommandBase(OnPickColumns);
            GenerateReport = new CommandBase(OnGenerateReport);

            Name = reportItem.Name;
            Description = reportItem.Description;
            StartDate = reportItem.StartDate;
            EndDate = reportItem.EndDate;
            IsFilteringOnAccounts = reportItem.IsFilteringOnAccounts;
            IsFilteringOnPayees = reportItem.IsFilteringOnPayees;
            IsFilteringOnCategories = reportItem.IsFilteringOnCategories;
        }

        #endregion

        #region UI properties

        // Name
        public string Name { get; set; }

        // Description
        public string Description { get; set; }

        // Start and end time (inclusive)
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Generate the report
        public CommandBase GenerateReport { get; }

        // Accounts
        private readonly List<Household.AccountRow> accounts = new List<Household.AccountRow>();
        public WpfObservableRangeCollection<string> AccountsSource { get; } = new WpfObservableRangeCollection<string>();
        private bool isFilteringOnAccounts;
        public bool? IsFilteringOnAccounts 
        {
            get => isFilteringOnAccounts;
            set { isFilteringOnAccounts = value == true; PickAccounts.SetCanExecute(isFilteringOnAccounts); }
        }
        public CommandBase PickAccounts { get; }

        // Payees
        public WpfObservableRangeCollection<string> PayeesSource { get; } = new WpfObservableRangeCollection<string>();
        private bool isFilteringOnPayees;
        public bool? IsFilteringOnPayees 
        {
            get => isFilteringOnPayees;
            set { isFilteringOnPayees = value == true; PickPayees.SetCanExecute(isFilteringOnPayees); }
        }
        public CommandBase PickPayees { get; }

        // Categories
        private readonly List<Household.CategoryRow> categories = new List<Household.CategoryRow>();
        public WpfObservableRangeCollection<string> CategoriesSource { get; } = new WpfObservableRangeCollection<string>();
        private bool isFilteringOnCategories;
        public bool? IsFilteringOnCategories 
        {
            get => isFilteringOnCategories;
            set { isFilteringOnCategories = value == true; PickCategories.SetCanExecute(isFilteringOnCategories); }
        }
        public CommandBase PickCategories { get; }

        // Columns
        public WpfObservableRangeCollection<string> ColumnsSource { get; } = new WpfObservableRangeCollection<string>();
        public CommandBase PickColumns { get; }

        #endregion

        #region Result

        public TransactionReportItem NewTransactionReportItem { get; private set; }

        #endregion

        #region Actions

        private void OnPickAccounts()
        {
            var logic = new AccountListPickerLogic(mainWindowLogic, accounts);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                accounts.Clear();
                accounts.AddRange(logic.PickedAccounts);
                AccountsSource.ReplaceRange(logic.PickedAccounts.Select(a => a.Name));
            }
        }

        private void OnPickPayees()
        {

        }

        private void OnPickCategories()
        {
            var logic = new CategoryListPickerLogic(mainWindowLogic, categories);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                categories.Clear();
                categories.AddRange(logic.PickedCategories);
                CategoriesSource.ReplaceRange(logic.PickedCategories.Select(a => a.FullName));
            }
        }

        private void OnPickColumns()
        {

        }

        private void OnGenerateReport()
        {

        }

        protected override bool? Commit()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                mainWindowLogic.ErrorMessage("Report name cannot be blank");
                return null;
            }

            foreach (Household.TransactionReportRow report in mainWindowLogic.Household.TransactionReport.Rows)
            {
                if (report != oldReportItem.TransactionReportRow && report.Name == Name)
                {
                    mainWindowLogic.ErrorMessage("There is already a report with this name");
                    return null;
                }
            }

            // Verify start date before end date ZZZZZZ

            NewTransactionReportItem = new TransactionReportItem(
                oldReportItem.TransactionReportRow,
                Name, 
                Description,
                StartDate,
                EndDate,
                isFilteringOnAccounts,
                isFilteringOnPayees,
                isFilteringOnCategories);

            bool change =
                oldReportItem.Name != Name ||
                oldReportItem.Description != Description ||
                oldReportItem.IsFilteringOnAccounts != isFilteringOnAccounts ||
                oldReportItem.IsFilteringOnPayees != isFilteringOnPayees ||
                oldReportItem.IsFilteringOnCategories != isFilteringOnCategories;

            return change;
        }

        #endregion
    }
}
