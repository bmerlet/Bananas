using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Dialogs;
using BanaData.Web;

namespace BanaData.Logic.Main
{
    public class MainMenuLogic
    {
        #region Private members

        private readonly MainWindowLogic mainWindow;

        #endregion

        #region Constructor

        public MainMenuLogic(MainWindowLogic _mainWindow)
        {
            mainWindow = _mainWindow;

            Open = new CommandBase(OnOpen);
            Import = new CommandBase(OnImport);
            Merge = new CommandBase(OnMerge);
            Export = new CommandBase(OnExport);
            DifferentialExport = new CommandBase(OnDifferentialExport);
            Save = new CommandBase(OnSave);
            Exit = new CommandBase(OnExit);

            EditAccounts = new CommandBase(OnEditAccounts);
            EditCategories = new CommandBase(OnEditCategories);
            EditMemorizedPayees = new CommandBase(OnEditMemorizedPayees);
            EditSecurities = new CommandBase(OnEditSecurities);

            Reconcile = new CommandBase(OnReconcile);
            Reconcile.SetCanExecute(false);
            UpdateStockPrices = new CommandBase(OnUpdateStockPrices);

            ShowYearlyCapGainsAndDividends = new CommandBase(OnShowYearlyCapGainsAndDividends);

            Test = new CommandBase(OnTest);
        }

        #endregion

        #region File memu

        //
        // Open
        //
        public CommandBase Open { get; }

        private void OnOpen()
        {
            OpenFileLogic logic = new OpenFileLogic(mainWindow.UserSettings.LastFileOpened, "Banana files (*.xban)|*.xban|Any file (*.*)|*.*", "Open file");
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.OpenFile(logic.File);
            }
        }

        //
        // Import
        //
        public CommandBase Import { get; }

        private void OnImport()
        {
            string ZZZfile = @"C:\Users\bmerlet\Documents\Lab\Projects\C#\Bananas\sgbjm.qif";
            OpenFileLogic logic = new OpenFileLogic(ZZZfile, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Import QIF file");
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                if (mainWindow.YesNoQuestion($"Are you sure you want to delete the current database data and replace it with the contents of {logic.File}?"))
                {
                    mainWindow.ImportQIF(logic.File);
                }
            }
        }

        //
        // Merge
        //
        public CommandBase Merge { get; }

        private void OnMerge()
        {
            string ZZZfile = @"C:\Users\bmerlet\Documents\Lab\Projects\C#\Bananas\sgbjm.qif";
            OpenFileLogic logic = new OpenFileLogic(ZZZfile, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Merge QIF file");
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.MergeQIF(logic.File);
            }
        }

        //
        // Export
        //
        public CommandBase Export { get; }

        private void OnExport()
        {
            string ZZZfile = @"C:\Users\bmerlet\Documents\Lab\Projects\C#\Bananas\out.qif";
            var logic = new SaveFileLogic(ZZZfile, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Export to file");
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.ExportQIF(logic.File);
            }
        }

        //
        // Differential Export
        //
        public CommandBase DifferentialExport { get; }

        private void OnDifferentialExport()
        {
            string ZZZfile = @"C:\Users\bmerlet\Documents\Lab\Projects\C#\Bananas\out.qif";
            var logic = new SaveFileLogic(ZZZfile, "Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*", "Export to file");
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.DifferentialExportQIF(logic.File);
            }
        }

        //
        // Save
        //
        public CommandBase Save { get; }

        private void OnSave()
        {
            mainWindow.Save();
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
            var logic = new EditAccountsLogic(mainWindow);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit categories
        //
        public CommandBase EditCategories { get; }

        private void OnEditCategories()
        {
            var logic = new EditCategoriesLogic(mainWindow);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit memorized payees
        //
        public CommandBase EditMemorizedPayees { get; }

        private void OnEditMemorizedPayees()
        {
            var logic = new EditMemorizedPayeesLogic(mainWindow);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit securities
        //
        public CommandBase EditSecurities { get; }

        private void OnEditSecurities()
        {
            var logic = new EditSecuritiesLogic(mainWindow);
            mainWindow.GuiServices.ShowDialog(logic);
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

            // Create logic and show reconcile info dialog
            var infoLogic = new ReconcileInfoLogic(mainWindow, accountID);
            if (mainWindow.GuiServices.ShowDialog(infoLogic))
            {
                // Show reconcile dialog
                var reconcileLogic = new ReconcileLogic(mainWindow, accountID);
                if (mainWindow.GuiServices.ShowDialog(reconcileLogic))
                {
                    // Update the cleared status in the register
                    mainWindow.BankRegister.UpdateAllTransactionStatus();

                    // Update the register if an interest transaction was created
                    if (reconcileLogic.InterestTransactionID >= 0)
                    {
                        mainWindow.BankRegister.AddTransaction(reconcileLogic.InterestTransactionID);
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
            var household = mainWindow.Household;
            var securities = new List<int>();
            var quoter = new Quote();

            // Go through all investment accounts
            foreach(Household.AccountsRow account in household.Accounts.Rows)
            {
                if (account.Type == EAccountType.Investment)
                {
                    // Find the securities held
                    foreach(int security in account.GetInvestmentSecurities())
                    {
                        if (!securities.Contains(security))
                        {
                            securities.Add(security);
                        }
                    }
                }
            }

            // Now ask quote for all securities held
            foreach (int security in securities)
            {
                var secRow = household.Securities.FindByID(security);
                if (!secRow.IsSymbolNull())
                {
                    var price = quoter.GetQuote(secRow.Symbol);

                    Console.WriteLine($"{secRow.Symbol}:\t{price}");

                    if (price >= 0)
                    {
                        household.SecurityPrices.Add(secRow, DateTime.Now, price);
                    }
                }
            }

            mainWindow.CommitChanges();
            mainWindow.UpdateAll();
        }

        #endregion

        #region View menu

        //
        // Show closed accounts toggle
        //
        public bool ShowClosedAccounts
        {
            get => !mainWindow.UserSettings.HideClosedAccounts;
            set { mainWindow.UserSettings.HideClosedAccounts = !value; mainWindow.UpdateAll(); }
        }

        //
        // Show yearly capital gains and dividends
        //
        public CommandBase ShowYearlyCapGainsAndDividends { get; }

        private void OnShowYearlyCapGainsAndDividends()
        {
            mainWindow.GuiServices.ShowDialog(new ShowYearlyCapGainsAndDividendsLogic(mainWindow));
        }

        #endregion

        #region Test button

        public CommandBase Test { get; }

        private void OnTest()
        {
            var q = new Web.Quote();
            var price = q.GetQuote("XOM");
            Console.WriteLine("XOM price: " + price);
        }

        #endregion
    }
}
