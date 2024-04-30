using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Dialogs.Editors;
using BanaData.Logic.Dialogs.Listers;
using BanaData.Logic.Dialogs.Reports;
using BanaData.Web;
using BanaData.Logic.Items;
using BanaData.Collections;
using BanaData.Logic.Dialogs.Reports.Accounting;
using BanaData.Logic.Dialogs.Pickers;
using System.Windows;

namespace BanaData.Logic.Main
{
    public class MainMenuLogic
    {
        #region Private members

        private readonly MainWindowLogic mainWindow;
        private readonly Household household;

        #endregion

        #region Constructor

        public MainMenuLogic(MainWindowLogic _mainWindow, Household _household)
        {
            mainWindow = _mainWindow;
            household = _household;

            New = new CommandBase(OnNew);
            Open = new CommandBase(OnOpen);
            Save = new CommandBase(OnSave);
            SaveAs = new CommandBase(OnSaveAs);
            Backup = new CommandBase(OnBackup);
            SetPassword = new CommandBase(OnSetPassword);
            Import = new CommandBase(OnImport);
            Export = new CommandBase(OnExport);
            Exit = new CommandBase(OnExit);

            EditAccounts = new CommandBase(OnEditAccounts);
            EditCategories = new CommandBase(OnEditCategories);
            EditPersons = new CommandBase(OnEditPersons);
            EditMemorizedPayees = new CommandBase(OnEditMemorizedPayees);
            EditSecurities = new CommandBase(OnEditSecurities);
            EditTransactionReports = new CommandBase(OnEditTransactionReports);
            EditScheduledTransactions = new CommandBase(OnEditScheduledTransactions);
            RenamePayee = new CommandBase(OnRenamePayee);

            Reconcile = new CommandBase(OnReconcile);
            Reconcile.SetCanExecute(false);
            UpdateStockPrices = new CommandBase(OnUpdateStockPrices);

            ShowYearlyCapGainsAndDividends = new CommandBase(OnShowYearlyCapGainsAndDividends);
            ShowHoldings = new CommandBase(OnShowHoldings);
            ShowHoldingsPerPerson = new CommandBase(OnShowHoldingsPerPerson);
            ShowHoldings.SetCanExecute(false);
            ShowRebalance = new CommandBase(OnShowRebalance);
            ShowRebalance.SetCanExecute(false);
            ShowWealthOverTime = new CommandBase(OnShowWealthOverTime);
            ShowCashFlowBetweenPersons = new CommandBase(OnShowCashFlowBetweenPersons);
            ShowBalanceSheet = new CommandBase(OnShowBalanceSheet);
            ShowIncomeStatement = new CommandBase(OnShowIncomeStatement);
            ShowJournal = new CommandBase(OnShowJournal);

            TransactionReportsSource = (CollectionView)CollectionViewSource.GetDefaultView(transactionReports);
            TransactionReportsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            UpdateTransactionReports();
        }

        #endregion

        #region File memu

        //
        // New
        //
        public CommandBase New { get; }

        private void OnNew()
        {
            mainWindow.NewFile();
        }

        //
        // Open
        //
        public CommandBase Open { get; }

        private void OnOpen()
        {
            OpenFileLogic logic = new OpenFileLogic(
                mainWindow.UserSettings.LastFileOpened, 
                "Banana files (*.ban)|*.ban|Banana XML files (*.xban)|*.xban|Any file (*.*)|*.*", "Open file");
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.OpenFile(logic.File);
            }
        }

        //
        // Save
        //
        public CommandBase Save { get; }

        private void OnSave()
        {
            mainWindow.SaveFile();
        }

        //
        // Save as
        //
        public CommandBase SaveAs { get; }

        private void OnSaveAs()
        {
            mainWindow.SaveAsFile();
        }

        //
        // Backup
        //
        public CommandBase Backup { get; }

        private void OnBackup()
        {
            SaveFileLogic logic = new SaveFileLogic(
                mainWindow.UserSettings.LastBackupFile,
                "Banana files (*.ban)|*.ban|Banana XML files (*.xban)|*.xban|Any file (*.*)|*.*", "Backup to file");
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.BackupFile(logic.File);
            }
        }

        //
        // Set password
        //
        public CommandBase SetPassword { get; }

        private void OnSetPassword()
        {
            mainWindow.SetPassword();
        }

        //
        // Import DB or transactions
        //
        public CommandBase Import { get; }

        private void OnImport()
        {
            var logic = new QIFImportPickerLogic(mainWindow);
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.ImportQIF(logic.ImportType, logic.ImportPath);
            }
        }

        //
        // Export
        //
        public CommandBase Export { get; }

        private void OnExport()
        {
            var logic = new QIFExportPickerLogic(mainWindow, household);
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                var exportSpec = logic.Export;
                if (exportSpec.Differential)
                {
                    mainWindow.DifferentialExportQIF(exportSpec.Filename);
                }
                else
                {
                    mainWindow.ExportQIF(exportSpec.Filename, exportSpec.Contents, exportSpec.TransactionAccounts);
                }
            }
        }

        //
        // Exit
        //
        public CommandBase Exit { get; }

        private void OnExit()
        {
            mainWindow.GuiServices.Exit();
        }

        #endregion

        #region Edit menu

        //
        // Edit accounts
        //
        public CommandBase EditAccounts { get; }

        private void OnEditAccounts()
        {
            var logic = new ListAccountsLogic(mainWindow, household);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit categories
        //
        public CommandBase EditCategories { get; }

        private void OnEditCategories()
        {
            var logic = new ListCategoriesLogic(mainWindow, household);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit persons
        //
        public CommandBase EditPersons { get; }

        private void OnEditPersons()
        {
            var logic = new EditPersonsLogic(mainWindow, household);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit memorized payees
        //
        public CommandBase EditMemorizedPayees { get; }

        private void OnEditMemorizedPayees()
        {
            var logic = new ListMemorizedPayeesLogic(mainWindow, household);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Rename payee
        //
        public CommandBase RenamePayee { get; }

        private void OnRenamePayee()
        {
            var logic = new RenamePayeeLogic(mainWindow, household);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit securities
        //
        public CommandBase EditSecurities { get; }

        private void OnEditSecurities()
        {
            var logic = new ListSecuritiesLogic(mainWindow, household);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit transaction reports
        //
        public CommandBase EditTransactionReports { get; }

        private void OnEditTransactionReports(object arg)
        {
            var logic = new ListTransactionReportsLogic(mainWindow, household, arg as TransactionReportItem);
            mainWindow.GuiServices.ShowDialog(logic);
            UpdateTransactionReports();
        }

        //
        // Edit scheduled transactions
        //
        public CommandBase EditScheduledTransactions { get; }

        private void OnEditScheduledTransactions()
        {
            var logic = new ListSchedulesLogic(mainWindow, household);
            mainWindow.GuiServices.ShowDialog(logic);
            mainWindow.CheckForScheduledTransactions();
        }

        #endregion

        #region Actions menu

        //
        // Reconcile
        //
        public CommandBase Reconcile { get; }

        private void OnReconcile()
        {
            // Retreive account info
            int accountID = mainWindow.DisplayedAccountID;
            bool isInvestment = household.Account.FindByID(accountID).Type == EAccountType.Investment;

            // Create logic and show reconcile info dialog
            var infoLogic = new ReconcileInfoLogic(mainWindow, household, accountID);
            if (mainWindow.GuiServices.ShowDialog(infoLogic))
            {
                // Show reconcile dialog
                if (isInvestment)
                {
                    var logic = new ReconcileInvestmentsLogic(mainWindow, household, accountID);
                    if (mainWindow.GuiServices.ShowDialog(logic))
                    {
                        mainWindow.InvestmentRegister.UpdateAllTransactionStatus();
                    }
                }
                else
                {
                    var logic = new ReconcileLogic(mainWindow, household, accountID);

                    if (mainWindow.GuiServices.ShowDialog(logic))
                    {
                        // Update the cleared status in the register
                        mainWindow.BankRegister.UpdateAllTransactionStatus();

                        // Update the register if an interest transaction was created
                        if (logic.InterestTransactionID >= 0)
                        {
                            mainWindow.BankRegister.AddTransaction(logic.InterestTransactionID);
                            mainWindow.UpdateAccountNamesAndBalances(new int[] { accountID });
                        }
                    }
                }
            }
        }

        //
        // Update stock prices
        //
        public CommandBase UpdateStockPrices { get; }

        private void OnUpdateStockPrices()
        {
            decimal oldNetWorth = mainWindow.NetWorth;
            var securities = new List<Household.SecurityRow>();
            var investmentIDs = new List<int>();

            // Go through all investment accounts
            foreach(Household.AccountRow account in household.Account.Rows)
            {
                if (account.Type == EAccountType.Investment && account.Kind != EInvestmentKind.Asset)
                {
                    // Find the securities held
                    foreach(int security in account.GetPortfolio().GetSecurities())
                    {
                        var secRow = household.Security.FindByID(security);
                        if (secRow.Symbol != Household.SecurityRow.SYMBOL_NONE)
                        {
                            if (!securities.Contains(secRow))
                            {
                                securities.Add(secRow);
                            }
                        }
                    }

                    // Remember to update balance for this account
                    investmentIDs.Add(account.ID);
                }
            }

            // Ask quote for symbols of all securities held
            var quotes = Quote.GetQuote(securities.Select(s => s.Symbol).ToArray());

            if (quotes.Any(q => q < 0))
            {
                string str = "Could not get quotes for the following symbols:\n";
                for (int i = 0; i < securities.Count; i++)
                {
                    if (quotes[i] < 0)
                    {
                        str += securities[i].Symbol + " ";
                    }
                }
                mainWindow.ErrorMessage(str, "Quotes");
            }

            // Now update quotes
            var today = DateTime.Today;
            for(int sIx = 0; sIx < securities.Count; sIx++) 
            {
                var secRow = securities[sIx];
                var price = quotes[sIx];

                //Console.WriteLine($"{secRow.Symbol}:\t{price}");
                if (price >= 0)
                {
                    // Keep at most one price per day
                    var secPriceRow = household.SecurityPrice.Where(sp => sp.SecurityRow == secRow && sp.Date == today).SingleOrDefault();
                    if (secPriceRow == null)
                    {
                        household.SecurityPrice.Add(secRow, DateTime.Today, price);
                    }
                    else
                    {
                        secPriceRow.Value = price;
                    }
                }
            }

            mainWindow.CommitChanges(household);
            mainWindow.UpdateAccountNamesAndBalances(investmentIDs);

            mainWindow.GuiServices.ShowDialog(new ShowQuoteUpdateLogic(securities.Count, oldNetWorth, mainWindow.NetWorth));
        }

        #endregion

        #region View menu

        //
        // Show closed accounts toggle
        //
        public bool ShowClosedAccounts
        {
            get => !mainWindow.UserSettings.HideClosedAccounts;
            set { mainWindow.UserSettings.HideClosedAccounts = !value; mainWindow.UpdateAccountNamesAndBalances(null); }
        }

        //
        // Play Ka-Ching sound toggle
        //
        public bool PlayKaChingSound
        {
            get => mainWindow.UserSettings.PlayKaChingSound;
            set { mainWindow.UserSettings.PlayKaChingSound = value; mainWindow.SaveUserSettings(); }
        }

        //
        // Transaction reports
        //

        private readonly WpfObservableRangeCollection<TransactionReportMenuItem> transactionReports = new WpfObservableRangeCollection<TransactionReportMenuItem>();
        public CollectionView TransactionReportsSource { get; }

        public void UpdateTransactionReports()
        {
            transactionReports.ReplaceRange(
                household.TransactionReport.Rows
                .Cast<Household.TransactionReportRow>()
                .Select(tr => TransactionReportItem.CreateFromDB(tr))
                .Select(tr => new TransactionReportMenuItem(mainWindow, household, tr)));
        }

        public class TransactionReportMenuItem : LogicBase
        {
            public TransactionReportMenuItem(MainWindowLogic mainWindowLogic, Household household, TransactionReportItem transactionReportItem)
            {
                TransactionReportItem = transactionReportItem;
                GenerateReport = new CommandBase(arg =>
                {
                    mainWindowLogic.GuiServices.ShowDialog(new TransactionReportLogic(mainWindowLogic, household, arg as TransactionReportItem, true));
                });
            }

            public string Name => TransactionReportItem.Name;
            public CommandBase GenerateReport { get; }
            public TransactionReportItem TransactionReportItem { get; }

            // Needed by WpfObservableRangeCollection
            public override bool Equals(object obj)
            {
                return
                    obj is TransactionReportMenuItem o &&
                    Name == o.Name &&
                    TransactionReportItem.Equals(o.TransactionReportItem);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        //
        // Show yearly capital gains and dividends
        //
        public CommandBase ShowYearlyCapGainsAndDividends { get; }

        private void OnShowYearlyCapGainsAndDividends()
        {
            mainWindow.GuiServices.ShowDialog(new ShowYearlyCGDivIntLogic(household, mainWindow.UserSettings, mainWindow.GuiServices));
        }

        //
        // Show holdings
        //
        public CommandBase ShowHoldings { get; }

        private void OnShowHoldings()
        {
            var logic = new ShowHoldingsLogic(household, mainWindow.DisplayedAccountID);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Show per-prson holdings
        //
        public CommandBase ShowHoldingsPerPerson { get; }

        private void OnShowHoldingsPerPerson()
        {
            var logic = new ShowHoldingsPerPersonLogic(mainWindow, household);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Show rebalance dashboard
        //
        public CommandBase ShowRebalance { get; }

        private void OnShowRebalance()
        {
            mainWindow.GuiServices.ShowDialog(new ShowRebalanceLogic(mainWindow, household, mainWindow.DisplayedAccountID));
        }

        //
        // Show wealth over time
        //
        public CommandBase ShowWealthOverTime { get; }

        private void OnShowWealthOverTime()
        {
            mainWindow.GuiServices.ShowDialog(new ShowWealthOverTimeLogic(household, mainWindow.GuiServices));
        }

        //
        // Show cash flow between household members
        //
        public CommandBase ShowCashFlowBetweenPersons { get; }

        private void OnShowCashFlowBetweenPersons()
        {
            mainWindow.GuiServices.ShowDialog(new ShowCashFlowBetweenPersonsLogic(mainWindow, household, mainWindow.UserSettings));
        }

        //
        // Balance sheet
        //
        public CommandBase ShowBalanceSheet { get; }

        private void OnShowBalanceSheet()
        {
            mainWindow.GuiServices.ShowDialog(new BalanceSheetLogic(household));
        }

        //
        // Income statement
        //
        public CommandBase ShowIncomeStatement { get; }

        private void OnShowIncomeStatement()
        {
            mainWindow.GuiServices.ShowDialog(new IncomeStatementLogic(household));
        }

        //
        // Journal
        //
        public CommandBase ShowJournal { get; }

        private void OnShowJournal()
        {
            mainWindow.GuiServices.ShowDialog(new JournalLogic(household));
        }

        #endregion
    }
}
