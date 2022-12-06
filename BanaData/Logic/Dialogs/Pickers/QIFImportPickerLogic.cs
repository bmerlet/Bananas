using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            BrowseParsePDFCommand = new CommandBase(OnBrowseParsePDFCommand);

            // Init
            importType = EImportType.PDFTransactions;

            // Init file names
            string lastFile = mainWindowLogic.UserSettings.LastFileOpened;

            ImportDBPath = mainWindowLogic.UserSettings.LastImportDBFile;
            if (ImportDBPath == null)
            {
                ImportDBPath = System.IO.Path.Combine(
                    lastFile == null ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : System.IO.Path.GetDirectoryName(lastFile),
                    "DB.QIF");

                mainWindowLogic.UserSettings.LastImportDBFile = ImportDBPath;
                mainWindowLogic.SaveUserSettings();
            }

            ImportTransactionsPath = mainWindowLogic.UserSettings.LastImportTransactionsFile;
            if (ImportTransactionsPath == null)
            {
                ImportTransactionsPath =
                    lastFile == null ?
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Transactions.QIF") :
                    lastFile.Substring(0, lastFile.LastIndexOf('.')) + ".QIF";

                mainWindowLogic.UserSettings.LastImportTransactionsFile = ImportTransactionsPath;
                mainWindowLogic.SaveUserSettings();
            }

            ParsePDFPath = mainWindowLogic.UserSettings.LastImportPDFFile;
            if (ParsePDFPath == null)
            {
                ParsePDFPath =
                    lastFile == null ?
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Statement.pdf") :
                    lastFile.Substring(0, lastFile.LastIndexOf('.')) + ".pdf";

                mainWindowLogic.UserSettings.LastImportPDFFile = ImportTransactionsPath;
                mainWindowLogic.SaveUserSettings();
            }

            InvokePropertyChanged(nameof(ImportDBPath));
            InvokePropertyChanged(nameof(ImportTransactionsPath));
            InvokePropertyChanged(nameof(ParsePDFPath));
            UpdateEnabled();
        }

        #endregion

        #region UI properties

        //
        // Radio button import DB/transactions
        //
        private EImportType importType;
        public bool ImportDB
        {
            get => importType == EImportType.FullQIF;
            set { if (value) { importType = EImportType.FullQIF; UpdateEnabled(); } } 
        }
        
        public bool ImportTransactions 
        { 
            get => importType == EImportType.QIFTransactions;
            set { if (value) { importType = EImportType.QIFTransactions; UpdateEnabled(); } }
        }
        
        public bool ParsePDF
        {
            get => importType == EImportType.PDFTransactions;
            set { if (value) { importType = EImportType.PDFTransactions; UpdateEnabled(); } }
        }

        //
        // Full import filename
        //
        public string ImportDBPath { get; set; }
        public CommandBase BrowseImportDBCommand { get; }
        public bool ImportDBPathEnabled => importType == EImportType.FullQIF;

        //
        // Transaction import filename
        //
        public string ImportTransactionsPath { get; set; }
        public CommandBase BrowseImportTransactionsCommand { get; }
        public bool ImportTransactionsPathEnabled => importType == EImportType.QIFTransactions;

        //
        // Parse PDF filename
        //
        public string ParsePDFPath { get; set; }
        public CommandBase BrowseParsePDFCommand { get; }
        public bool ParsePDFPathEnabled => importType == EImportType.PDFTransactions;

        #endregion

        #region Actions

        private void UpdateEnabled()
        {
            InvokePropertyChanged(nameof(ImportDBPathEnabled));
            InvokePropertyChanged(nameof(ImportTransactionsPathEnabled));
            InvokePropertyChanged(nameof(ParsePDFPathEnabled));
        }

        private void OnBrowseImportDBCommand()
        {
            var logic = new Basics.OpenFileLogic(ImportDBPath, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Import QIF file");
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                ImportDBPath = logic.File;
                InvokePropertyChanged(nameof(ImportDBPath));
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
                InvokePropertyChanged(nameof(ImportTransactionsPath));
                mainWindowLogic.UserSettings.LastImportTransactionsFile = ImportTransactionsPath;
                mainWindowLogic.SaveUserSettings();
            }
        }

        private void OnBrowseParsePDFCommand()
        {
            var logic = new Basics.OpenFileLogic(ParsePDFPath, "Portable Document Files (*.PDF)|*.PDF|Any file (*.*)|*.*", "Parse PDF file");
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                ParsePDFPath = logic.File;
                InvokePropertyChanged(nameof(ParsePDFPath));
                mainWindowLogic.UserSettings.LastImportPDFFile = ParsePDFPath;
                mainWindowLogic.SaveUserSettings();
            }
        }

        // Result
        public EImportType ImportType => importType;
        public string ImportPath { get; private set; }

        protected override bool? Commit()
        {
            switch (importType)
            {
                case EImportType.FullQIF:
                    ImportPath = ImportDBPath;
                    break;
                case EImportType.QIFTransactions:
                    ImportPath = ImportTransactionsPath;
                    break;
                case EImportType.PDFTransactions:
                    ImportPath = ParsePDFPath;
                    break;
            }

            if (String.IsNullOrWhiteSpace(ImportPath))
            {
                mainWindowLogic.ErrorMessage("Please select a path", "Import");
                return null;
            }

            return true;
        }

        #endregion
    }
}
