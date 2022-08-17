using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Main;
using BanaData.Serializations;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Pickers
{
    public class QIFExportPickerLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public QIFExportPickerLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Create commands
            BrowseDifferentialCommand = new CommandBase(OnBrowseDifferentialCommand);
            BrowseRegularCommand = new CommandBase(OnBrowseRegularCommand);

            // Create account list
            AccountListLogic = new AccountListLogic(mainWindowLogic);

            // Init
            exportType = EExportType.Differential;
            contents = QIFWriter.EContents.All;
            AccountListLogic.SelectAllCommand.Execute();

            string lastFile = mainWindowLogic.UserSettings.LastFileOpened;
            DifferentialExportPath =
                System.IO.Path.Combine(
                    lastFile == null ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : System.IO.Path.GetDirectoryName(lastFile),
                    "Differential.QIF");

            RegularExportPath =
                lastFile == null ?
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Differential.QIF") :
                lastFile.Substring(0, lastFile.LastIndexOf('.')) + ".QIF";
        }

        #endregion

        #region UI properties

        //
        // Radio button full/differential export
        //
        private enum EExportType { Regular, Differential, None };
        private EExportType exportType;
        public bool RegularExport { get => exportType == EExportType.Regular ; set { if (value) exportType = EExportType.Regular; UpdateEnabled(); } }
        public bool DifferentialExport { get => exportType == EExportType.Differential; set { if (value) exportType = EExportType.Differential; UpdateEnabled(); } }

        //
        // Differential export filename
        //
        public string DifferentialExportPath { get; set; }
        public CommandBase BrowseDifferentialCommand { get; }
        public bool DifferentialEnabled => exportType == EExportType.Differential;

        //
        // Regular export filename
        //
        public string RegularExportPath { get; set; }
        public CommandBase BrowseRegularCommand { get; }
        public bool RegularEnabled => exportType == EExportType.Regular;

        //
        // What to export
        //
        private QIFWriter.EContents contents;

        public bool? ExportCategories 
        { 
            get => contents.HasFlag(QIFWriter.EContents.Categories);
            set => contents = value == true ? contents | QIFWriter.EContents.Categories : contents &~QIFWriter.EContents.Categories;
        }

        public bool? ExportAccounts
        {
            get => contents.HasFlag(QIFWriter.EContents.Accounts);
            set => contents = value == true ? contents | QIFWriter.EContents.Accounts : contents & ~QIFWriter.EContents.Accounts;
        }

        public bool? ExportSecurities
        {
            get => contents.HasFlag(QIFWriter.EContents.Securities);
            set => contents = value == true ? contents | QIFWriter.EContents.Securities : contents & ~QIFWriter.EContents.Securities;
        }

        public bool? ExportMemorizedPayees
        {
            get => contents.HasFlag(QIFWriter.EContents.MemorizedPayees);
            set => contents = value == true ? contents | QIFWriter.EContents.MemorizedPayees : contents & ~QIFWriter.EContents.MemorizedPayees;
        }

        public bool? ExportTransactions
        {
            get => contents.HasFlag(QIFWriter.EContents.Transactions);
            set { contents = value == true ? contents | QIFWriter.EContents.Transactions : contents & ~QIFWriter.EContents.Transactions; UpdateEnabled(); }
        }

        //
        // List of accounts with checkboxes
        //
        public AccountListLogic AccountListLogic { get; }
        public bool AccountListEnabled { get; private set; }

        #endregion

        #region Actions

        private void UpdateEnabled()
        {
            bool enableAccountList = RegularEnabled && contents.HasFlag(QIFWriter.EContents.Transactions);
            if (AccountListEnabled != enableAccountList)
            {
                AccountListEnabled = enableAccountList;
                OnPropertyChanged(() => AccountListEnabled);
            }

            OnPropertyChanged(() => DifferentialEnabled);
            OnPropertyChanged(() => RegularEnabled);
        }

        private void OnBrowseDifferentialCommand()
        {
            var logic = new Basics.SaveFileLogic(DifferentialExportPath, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Import QIF file");
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                DifferentialExportPath = logic.File;
                OnPropertyChanged(() => DifferentialExportPath);
            }
        }

        private void OnBrowseRegularCommand()
        {
            var logic = new Basics.SaveFileLogic(RegularExportPath, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Import QIF file");
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                RegularExportPath = logic.File;
                OnPropertyChanged(() => RegularExportPath);
            }
        }

        // Result
        public class ExportSpec
        {
            public readonly bool Differential;
            public string Filename;
            public QIFWriter.EContents Contents;
            public IEnumerable<Household.AccountRow> TransactionAccounts;

            public ExportSpec(bool differential, string filename, QIFWriter.EContents contents, IEnumerable<Household.AccountRow> transactionAccounts) =>
                (Differential, Filename, Contents, TransactionAccounts) = (differential, filename, contents, transactionAccounts);
        }

        public ExportSpec Export { get; private set; }

        protected override bool? Commit()
        {
            var household = mainWindowLogic.Household;
            bool? result = false;

            if (DifferentialExport)
            {
                Export = new ExportSpec(true, DifferentialExportPath, QIFWriter.EContents.All, household.Account.Rows.Cast<Household.AccountRow>());
                result = true;
            }
            else if (RegularExport)
            {
                var pickedAccounts = new List<Household.AccountRow>();
                foreach (AccountListLogic.AccountPickerItem accountItem in AccountListLogic.Accounts)
                {
                    if (accountItem.IsSelected == true)
                    {
                        pickedAccounts.Add(accountItem.AccountRow);
                    }
                }

                if (pickedAccounts.Count() == 0 && contents == QIFWriter.EContents.Transactions)
                {
                    mainWindowLogic.ErrorMessage("Select at least an account when exporting transactions");
                    result = null;
                }
                else
                {
                    Export = new ExportSpec(false, RegularExportPath, contents, pickedAccounts);
                    result = true;
                }
            }

            return result;
        }

        #endregion
    }
}
