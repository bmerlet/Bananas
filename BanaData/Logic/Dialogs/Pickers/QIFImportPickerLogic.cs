using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using BanaData.Serializations;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Pickers
{
    public class QIFImportPickerLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public QIFImportPickerLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Create commands
            BrowseImportDBCommand = new CommandBase(OnBrowseImportDBCommand);
            BrowseImportTransactionsCommand = new CommandBase(OnBrowseImportTransactionsCommand);

            // Create account list for autocomplete text box
            foreach (Household.AccountRow accountRow in mainWindowLogic.Household.Account)
            {
                var accountItem = AccountItem.CreateFromDB(accountRow);
                accounts.Add(accountItem);
            }

            // Init
            importType = EImportType.Transactions;

            // Init file names
            string lastFile = mainWindowLogic.UserSettings.LastFileOpened;
            string ImportDBPath = mainWindowLogic.UserSettings.LastImportDBFile;

            if (ImportDBPath == null)
            {
                ImportDBPath = System.IO.Path.Combine(
                    lastFile == null ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : System.IO.Path.GetDirectoryName(lastFile),
                    "DB.QIF");
                mainWindowLogic.SaveUserSettings();
            }

            ImportTransactionsPath = mainWindowLogic.UserSettings.LastImportTransactionsFile;
            if (ImportTransactionsPath == null)
            {
                ImportTransactionsPath =
                    lastFile == null ?
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Transactions.QIF") :
                    lastFile.Substring(0, lastFile.LastIndexOf('.')) + ".QIF";
                mainWindowLogic.SaveUserSettings();
            }

            SpecificAccount = mainWindowLogic.UserSettings.LastImportAccountName;

            UpdateEnabled();
        }

        #endregion

        #region UI properties

        //
        // Radio button import DB/transactions
        //
        private enum EImportType { Full, Transactions, None };
        private EImportType importType;
        public bool ImportDB { get => importType == EImportType.Full ; set { if (value) importType = EImportType.Full; UpdateEnabled(); } }
        public bool ImportTransactions { get => importType == EImportType.Transactions; set { if (value) importType = EImportType.Transactions; UpdateEnabled(); } }

        //
        // Full import filename
        //
        public string ImportDBPath { get; set; }
        public CommandBase BrowseImportDBCommand { get; }
        public bool ImportDBPathEnabled => importType == EImportType.Full;

        //
        // Transaction import filename
        //
        public string ImportTransactionsPath { get; set; }
        public CommandBase BrowseImportTransactionsCommand { get; }
        public bool ImportTransactionsPathEnabled => importType == EImportType.Transactions;

        //
        // If importing for a specific account
        //
        private bool? importToSpecificAccount = false;
        public bool? ImportToSpecificAccount { get => importToSpecificAccount; set { importToSpecificAccount = value; UpdateEnabled(); } }
        public string SpecificAccount { get; set; }
        private readonly List<AccountItem> accounts = new List<AccountItem>();
        public IEnumerable<AccountItem> Accounts => accounts;
        public bool ImportToSpecificAccountEnabled => importType == EImportType.Transactions && ImportToSpecificAccount == true;

        //
        // Options when importing transactions
        //
        public bool? ImportComments { get; set; } = false;
        public bool? LowerCasePayees { get; set; } = true;

        #endregion

        #region Actions

        private void UpdateEnabled()
        {
            OnPropertyChanged(() => ImportDBPathEnabled);
            OnPropertyChanged(() => ImportTransactionsPathEnabled);
            OnPropertyChanged(() => ImportToSpecificAccountEnabled);
        }

        private void OnBrowseImportDBCommand()
        {
            var logic = new Basics.OpenFileLogic(ImportDBPath, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Import QIF file");
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                ImportDBPath = logic.File;
                OnPropertyChanged(() => ImportDBPath);
                mainWindowLogic.UserSettings.LastImportDBFile = ImportDBPath;
                mainWindowLogic.SaveUserSettings();
            }
        }

        private void OnBrowseImportTransactionsCommand()
        {
            var logic = new Basics.OpenFileLogic(ImportTransactionsPath, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Import QIF file");
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                ImportTransactionsPath = logic.File;
                OnPropertyChanged(() => ImportTransactionsPath);
                mainWindowLogic.UserSettings.LastImportTransactionsFile = ImportTransactionsPath;
                mainWindowLogic.SaveUserSettings();
            }
        }

        // Result
        public QIFImportSpecification ImportSpecification { get; private set; }

        protected override bool? Commit()
        {
            if (ImportDB)
            {
                ImportSpecification = new QIFImportSpecification(true, ImportDBPath, null, true, false);
            }
            else
            {
                Household.AccountRow account = null;
                if (ImportToSpecificAccount == true)
                {
                    var accountItem = accounts.Find(a => a.Name == SpecificAccount);
                    if (accountItem == null)
                    {
                        mainWindowLogic.ErrorMessage("Please specify the account to import the transactions into.");
                        return null;
                    }
                    account = accountItem.AccountRow;
                    if (SpecificAccount != mainWindowLogic.UserSettings.LastImportAccountName)
                    {
                        mainWindowLogic.UserSettings.LastImportAccountName = SpecificAccount;
                        mainWindowLogic.SaveUserSettings();
                    }
                }
                ImportSpecification = new QIFImportSpecification(false, ImportTransactionsPath, account, ImportComments == true, LowerCasePayees == true);
            }

            return true;
        }

        #endregion
    }
}
