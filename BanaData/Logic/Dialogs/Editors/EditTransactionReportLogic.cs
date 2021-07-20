using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BanaData.Collections;
using BanaData.Database;
using BanaData.Logic.Dialogs.Pickers;
using BanaData.Logic.Dialogs.Reports;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Editors
{
    public class EditTransactionReportLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly TransactionReportItem oldReportItem;
        private ETransactionReportFlag localFlags;

        #endregion

        #region Constructor

        public EditTransactionReportLogic(MainWindowLogic _mainWindowLogic, TransactionReportItem reportItem)
        {
            (mainWindowLogic, oldReportItem) = (_mainWindowLogic, reportItem);

            PickAccounts = new CommandBase(OnPickAccounts);
            PickPayees = new CommandBase(OnPickPayees);
            PickCategories = new CommandBase(OnPickCategories);
            GenerateReport = new CommandBase(OnGenerateReport);

            Name = reportItem.Name;
            Description = reportItem.Description;
            StartDate = reportItem.StartDate;
            EndDate = reportItem.EndDate;

            ShowAccountColumn = reportItem.IsShowingAccountColumn;
            ShowDateColumn = reportItem.IsShowingDateColumn;
            ShowPayeeColumn = reportItem.IsShowingPayeeColumn;
            ShowMemoColumn = reportItem.IsShowingMemoColumn;
            ShowCategoryColumn = reportItem.IsShowingCategoryColumn;

            SetGroup(
                reportItem.IsGroupingByAccount ? GROUP_ACCOUNT :
                (reportItem.IsGroupingByPayee ? GROUP_PAYEE :
                (reportItem.IsGroupingByCategory ? GROUP_CATEGORY : GROUP_NONE)));

            SetShow(
                reportItem.IsShowingTransactions && reportItem.IsShowingSubtotals ? SHOW_SUBTOTAL :
                (reportItem.IsShowingTransactions ? SHOW_TRANS : SHOW_SUBTOTALONLY));

            IsFilteringOnAccounts = reportItem.IsFilteringOnAccounts;
            accounts.AddRange(reportItem.Accounts);
            AccountsSource.ReplaceRange(reportItem.Accounts.Select(a => a.Name));

            IsFilteringOnPayees = reportItem.IsFilteringOnPayees;
            payees.AddRange(reportItem.Payees);
            PayeesSource.ReplaceRange(reportItem.Payees);

            IsFilteringOnCategories = reportItem.IsFilteringOnCategories;
            categories.AddRange(reportItem.Categories);
            CategoriesSource.ReplaceRange(reportItem.Categories.Select(c => c.FullName));
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

        // Group by
        private const string GROUP_NONE = "None";
        private const string GROUP_ACCOUNT = "Account";
        private const string GROUP_PAYEE = "Payee";
        private const string GROUP_CATEGORY = "Category";
        public string[] GroupSource { get; } = new string[] { GROUP_NONE, GROUP_ACCOUNT, GROUP_PAYEE, GROUP_CATEGORY };
        private string group;
        public string Group { get => group; set => SetGroup(value); }

        // Show
        private const string SHOW_TRANS = "Transactions";
        private const string SHOW_SUBTOTAL = "Transactions and subtotals";
        private const string SHOW_SUBTOTALONLY = "Subtotals only";
        public string[] ShowSource { get; } = new string[] { SHOW_TRANS, SHOW_SUBTOTAL, SHOW_SUBTOTALONLY };
        private string show;
        public string Show { get => show; set => SetShow(value); }

        // Columns
        public bool? IsColumnPanelEnabled => Show != SHOW_SUBTOTALONLY;
        public bool? ShowAccountColumn { get; set; }
        public bool? IsAccountColumnEnabled { get; private set; }
        public bool? ShowDateColumn { get; set; }
        public bool? ShowPayeeColumn { get; set; }
        public bool? IsPayeeColumnEnabled { get; private set; }
        public bool? ShowMemoColumn { get; set; }
        public bool? ShowCategoryColumn { get; set; }
        public bool? IsCategoryColumnEnabled { get; private set; }


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
        private readonly List<string> payees = new List<string>();
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

        #endregion

        #region Result

        public TransactionReportItem NewTransactionReportItem { get; private set; }

        #endregion

        #region Actions

        private void SetGroup(string value)
        {
            if (value != group)
            {
                group = value;
                localFlags &= ~(ETransactionReportFlag.GroupByAccount | ETransactionReportFlag.GroupByPayee | ETransactionReportFlag.GroupByCategory);
                
                IsAccountColumnEnabled = true;
                IsPayeeColumnEnabled = true;
                IsCategoryColumnEnabled = true;

                switch (group)
                {
                    case GROUP_ACCOUNT:
                        localFlags |= ETransactionReportFlag.GroupByAccount;
                        ShowAccountColumn = true;
                        OnPropertyChanged(() => ShowAccountColumn);
                        IsAccountColumnEnabled = false;
                        break;
                    case GROUP_PAYEE:
                        localFlags |= ETransactionReportFlag.GroupByPayee;
                        ShowPayeeColumn = true;
                        OnPropertyChanged(() => ShowPayeeColumn);
                        IsPayeeColumnEnabled = false;
                        break;
                    case GROUP_CATEGORY:
                        localFlags |= ETransactionReportFlag.GroupByCategory;
                        ShowCategoryColumn = true;
                        OnPropertyChanged(() => ShowCategoryColumn);
                        IsCategoryColumnEnabled = false;
                        break;
                }

                OnPropertyChanged(() => IsAccountColumnEnabled);
                OnPropertyChanged(() => IsPayeeColumnEnabled);
                OnPropertyChanged(() => IsCategoryColumnEnabled);
            }
        }

        private void SetShow(string value)
        {
            if (value != show)
            {
                show = value;
                localFlags &= ~(ETransactionReportFlag.ShowTransactions | ETransactionReportFlag.ShowSubtotals);

                switch (Show)
                {
                    case SHOW_TRANS:
                        localFlags |= ETransactionReportFlag.ShowTransactions;
                        break;
                    case SHOW_SUBTOTAL:
                        localFlags |= ETransactionReportFlag.ShowTransactions | ETransactionReportFlag.ShowSubtotals;
                        break;
                    case SHOW_SUBTOTALONLY:
                        localFlags |= ETransactionReportFlag.ShowSubtotals;
                        break;
                }

                OnPropertyChanged(() => IsColumnPanelEnabled);
            }
        }

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
            var logic = new PayeeListPickerLogic(mainWindowLogic, payees);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                payees.Clear();
                payees.AddRange(logic.PickedPayees);
                PayeesSource.ReplaceRange(logic.PickedPayees);
            }
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

        private void OnGenerateReport()
        {
            var logic = new TransactionReportLogic(mainWindowLogic, BuildTransactionReportItemFromControls(), false);
            mainWindowLogic.GuiServices.ShowDialog(logic);
        }

        private TransactionReportItem BuildTransactionReportItemFromControls()
        {
            // Build the flags
            ETransactionReportFlag flags = localFlags;

            if (ShowAccountColumn == true)
            {
                flags |= ETransactionReportFlag.ShowAccountColumn;
            }
            if (ShowDateColumn == true)
            {
                flags |= ETransactionReportFlag.ShowDateColumn;
            }
            if (ShowPayeeColumn == true)
            {
                flags |= ETransactionReportFlag.ShowPayeeColumn;
            }
            if (ShowMemoColumn == true)
            {
                flags |= ETransactionReportFlag.ShowMemoColumn;
            }
            if (ShowCategoryColumn == true)
            {
                flags |= ETransactionReportFlag.ShowCategoryColumn;
            }

            if (isFilteringOnAccounts)
            {
                flags |= ETransactionReportFlag.IsFilteringOnAccounts;
            }
            if (isFilteringOnPayees)
            {
                flags |= ETransactionReportFlag.IsFilteringOnPayees;
            }
            if (isFilteringOnCategories)
            {
                flags |= ETransactionReportFlag.IsFilteringOnCategories;
            }

            return new TransactionReportItem(
                oldReportItem.TransactionReportRow,
                Name,
                Description,
                StartDate,
                EndDate,
                flags,
                accounts,
                payees,
                categories);
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

            // Verify start date before end date
            if (StartDate.CompareTo(EndDate) > 0)
            {
                mainWindowLogic.ErrorMessage("Start date must be earlier than end date");
                return null;
            }

            // Build the new report descriptor
            NewTransactionReportItem = BuildTransactionReportItemFromControls();

            bool change =
                oldReportItem.Name != Name ||
                oldReportItem.Description != Description ||
                oldReportItem.StartDate != StartDate ||
                oldReportItem.EndDate != EndDate ||
                oldReportItem.Flags != NewTransactionReportItem.Flags ||
                !oldReportItem.Accounts.SequenceEqual(accounts) ||
                !oldReportItem.Payees.SequenceEqual(payees) ||
                !oldReportItem.Categories.SequenceEqual(categories);

            return change;
        }

        #endregion
    }
}
