using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            Name = reportItem.Name;
            Description = reportItem.Description;
            StartDate = reportItem.StartDate;
            EndDate = reportItem.EndDate;
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

        // Sources
        public ObservableCollection<string> AccountsSource { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> PayeesSource { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> CategoriesSource { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ColumnsSource { get; } = new ObservableCollection<string>();

        // Pickers
        public CommandBase PickAccounts { get; }
        public CommandBase PickPayees { get; }
        public CommandBase PickCategories { get; }
        public CommandBase PickColumns { get; }

        #endregion

        #region Result

        public TransactionReportItem NewTransactionReportItem { get; private set; }

        #endregion

        #region Actions

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

            NewTransactionReportItem = new TransactionReportItem(oldReportItem.TransactionReportRow, Name, Description, StartDate, EndDate);

            bool change =
                oldReportItem.Name != Name ||
                oldReportItem.Description != Description;

            return change;
        }

        #endregion
    }
}
