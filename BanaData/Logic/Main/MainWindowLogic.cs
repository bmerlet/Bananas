using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Data;

using Toolbox.UILogic;
using Toolbox.Models;
using BanaData.Database;
using BanaData.Serializations;
using BanaData.Logic.Items;
using BanaData.Logic.Dialogs;
using BanaData.Logic.Dialogs.Reports;
using BanaData.Logic.Dialogs.Basics;
using System.IO;

namespace BanaData.Logic.Main
{
    /// <summary>
    /// Main window logic
    /// </summary>
    public class MainWindowLogic : LogicBase
    {
        #region Private members

        // Application name
        private const string APPNAME = "Bananas"; 

        // User settings manager
        private readonly SettingsManager<UserSettings> settingsManager = new SettingsManager<UserSettings>("Sarabande Inc.", APPNAME);

        // Save timer and lock
        private readonly Timer saveTimer = new Timer(300 * 1000);
        private readonly object householdLock = new object();

        // Currently open file
        private FileStream fileStream;
        private enum EFileFormat { Ban, XBan};
        private EFileFormat fileFormat;

        // Password used for the current file
        private string password;

        #endregion

        #region Constructor 

        public MainWindowLogic(IGuiServices guiServices)
        {
            // Memorize UI service provider
            GuiServices = guiServices;

            // Create empty database
            Household = new Household();

            // Get settings
            UserSettings = settingsManager.Load() ?? new UserSettings();

            // Main menu
            MainMenuLogic = new MainMenuLogic(this);

            // Search command
            Search = new CommandBase(OnSearch);

            // Account groups
            BankAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Banking);
            InvestmentAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Investment);
            AssetAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Asset);

            // Registers
            BankRegister = new BankRegisterLogic(this);
            InvestmentRegister = new InvestmentRegisterLogic(this);

            BankAccountGroup.AccountClicked += (o, e) => OnBankAccountClicked(o as AccountGroupLogic, e.AccountID);
            InvestmentAccountGroup.AccountClicked += (o, e) => OnInvestmentAccountClicked(e.AccountID);
            AssetAccountGroup.AccountClicked += (o, e) => OnBankAccountClicked(o as AccountGroupLogic, e.AccountID);

            // Create list of investment transaction type
            foreach (EInvestmentTransactionType itt in Enum.GetValues(typeof(EInvestmentTransactionType)))
            {
                investmentTransactionTypes.Add(new InvestmentTransactionTypeItem(itt));
            }
            InvestmentTransactionTypesView = (CollectionView)CollectionViewSource.GetDefaultView(investmentTransactionTypes);
            InvestmentTransactionTypesView.SortDescriptions.Add(new SortDescription("TypeString", ListSortDirection.Ascending));

            // Setup timer to save to disk every 5 minutes
            saveTimer.Elapsed += OnSaveTimerElapsed;
            saveTimer.Enabled = true;

            GuiServices.ExecuteAsync((Action)delegate ()
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length == 2 && args[1].EndsWith(".BAN", StringComparison.InvariantCultureIgnoreCase))
                {
                    OpenFile(args[1]);
                }
                else if (UserSettings.LastFileOpened != null)
                {
                    OpenFile(UserSettings.LastFileOpened);
                }
                else
                {
                    UpdateAll();
                }

            });

        }

        #endregion

        #region Events

        // The security list has changed
        public event EventHandler SecuritiesChanged;

        // The memorized payees have changed
        public event EventHandler MemorizedPayeesChanged;

        // The category list has changed
        public event EventHandler CategoriesChanged;

        #endregion

        #region Public properties

        // Gui services
        public readonly IGuiServices GuiServices;

        // The database
        public readonly Household Household;

        // If household needs to be saved to disk
        public bool Dirty { get; private set; }

        // Main form sub-logics
        public MainMenuLogic MainMenuLogic { get; }
        // public readonly StatusBar StatusBar;

        // User settings
        public readonly UserSettings UserSettings;

        // List of memorized payees
        public readonly List<MemorizedPayeeItem> MemorizedPayees = new List<MemorizedPayeeItem>();

        // List of categories, as displayed in the UI
        // Categories only (non-hidden)
        public readonly List<CategoryItem> Categories = new List<CategoryItem>();
        // Transfers only
        public readonly List<CategoryItem> Transfers = new List<CategoryItem>();
        // Categories and transfers
        public readonly List<CategoryItem> CategoriesAndTransfers = new List<CategoryItem>();
        // Hidden (system) categories
        public readonly List<CategoryItem> HiddenCategories = new List<CategoryItem>();

        // List of securities
        public readonly ObservableCollection<SecurityItem> Securities = new ObservableCollection<SecurityItem>();

        // List of investment transaction types
        private readonly List<InvestmentTransactionTypeItem> investmentTransactionTypes = new List<InvestmentTransactionTypeItem>();
        public CollectionView InvestmentTransactionTypesView { get; }

        // Account currently displayed
        public int DisplayedAccountID { get; private set; } = -1;

        #endregion

        #region UI properties

        // Title
        public string Title { get; private set; } = APPNAME;

        // Main window position and size
        public int LeftX
        {
            get => UserSettings.LeftX;
            set => UserSettings.LeftX = value;
        }

        public int TopY
        {
            get => UserSettings.TopY;
            set => UserSettings.TopY = value;
        }

        public int Width
        {
            get => UserSettings.Width;
            set => UserSettings.Width = value;
        }

        public int Height
        {
            get => UserSettings.Height;
            set => UserSettings.Height = value;
        }

        public int SplitterX
        {
            get => UserSettings.AccountWidth;
            set => UserSettings.AccountWidth = value;
        }

        // Search box
        private string searchText;
        public string SearchText
        {
            get => searchText;
            set { searchText = value; /* Search.Execute(); ZZZ */}
        }
        public CommandBase Search { get; }

        // The accounts and balances displayed on the left side
        public AccountGroupLogic BankAccountGroup { get; private set; }
        public AccountGroupLogic InvestmentAccountGroup { get; private set; }
        public AccountGroupLogic AssetAccountGroup { get; private set; }

        public decimal NetWorth { get; private set; }

        // The bank register
        public BankRegisterLogic BankRegister { get; private set; }
        public bool IsBankRegisterVisible { get; private set; }

        // The investment register
        public InvestmentRegisterLogic InvestmentRegister { get; private set; }
        public bool IsInvestmentRegisterVisible { get; private set; }

        #endregion

        #region File services

        //
        // Open a new file
        //
        public void NewFile()
        {
            // Save the existing file
            SaveFile();

            // Close the existing file
            if (fileStream != null)
            {
                fileStream.Close();
                fileStream = null;
            }

            // Zap the DB
            Household.Clear();
            Household.AcceptChanges();

            // Setup new file
            UserSettings.LastFileOpened = null;
            Dirty = false;
            UpdateTitle();
            UpdateAll();
        }

        //
        // Open a bananas file
        //
        public void OpenFile(string file)
        {
            // Try to open the file
            try
            {
                fileStream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
            catch (Exception e)
            {
                ErrorMessage("Cannot open file: " + e.Message);
                return;
            }

            for (bool retry = true; retry;)
            {
                retry = false;

                // Ask for password if the file is encrypted
                if (file.EndsWith(".BAN", StringComparison.InvariantCultureIgnoreCase))
                {
                    var logic = new PasswordPromptLogic(password, "Open file");
                    if (!GuiServices.ShowDialog(logic))
                    {
                        return;
                    }
                    password = logic.Password;
                }

                // Read the file
                try
                {
                    GuiServices.SetCursor(true);

                    if (file.EndsWith(".BAN", StringComparison.InvariantCultureIgnoreCase))
                    {
                        fileFormat = EFileFormat.Ban;
                        var serializer = new BANSerializer(Household);
                        serializer.Read(fileStream, password);
                    }
                    else if (file.EndsWith(".XBAN", StringComparison.InvariantCultureIgnoreCase))
                    {
                        fileFormat = EFileFormat.XBan;
                        Household.Clear();
                        Household.AcceptChanges();
                        Household.ReadXml(fileStream);
                    }
                    else
                    {
                        ErrorMessage("File extension not supported");
                        return;
                    }
                }
                catch (Exception e)
                {
                    string message = e.Message;

                    if (e is System.Security.Cryptography.CryptographicException && e.Message.StartsWith("Padding is invalid"))
                    {
                        ErrorMessage($"Error reading file {Path.GetFileName(file)}: Bad password");
                        retry = true;
                        continue;
                    }
                    else
                    {
                        ErrorMessage($"Error reading file {Path.GetFileName(file)}: {message}");
                    }
                }
                finally
                {
                    GuiServices.SetCursor(false);
                }

            }

            UserSettings.LastFileOpened = file;
            Dirty = false;

            UpdateTitle();
            UpdateAll();
        }

        public void SaveFile()
        {
            if (fileStream == null)
            {
                SaveAsFile();
            }
            else
            {
                SaveToFile();
            }
        }

        public void SaveAsFile()
        {
            SaveFileLogic logic = new SaveFileLogic(
                UserSettings.LastFileOpened,
                "Banana files (*.ban)|*.ban|Banana XML files (*.xban)|*.xban|Any file (*.*)|*.*", "Save file");
                if (GuiServices.ShowDialog(logic))
            {
                var file = logic.File;
                try
                {
                    fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                }
                catch(Exception e)
                {
                    ErrorMessage($"Cannot open file {Path.GetFileName(file)}: {e.Message}");
                    return;
                }

                fileFormat = file.EndsWith(".BAN", StringComparison.InvariantCultureIgnoreCase) ? EFileFormat.Ban : EFileFormat.XBan;

                SaveToFile();
                UserSettings.LastFileOpened = file;
                UpdateTitle();
            }
        }

        public void BackupFile(string file)
        {
            FileStream backupStream;
            try
            {
                using (backupStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    lock (householdLock)
                    {
                        GuiServices.SetCursor(true);

                        if (file.EndsWith(".BAN", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var serializer = new BANSerializer(Household);
                            serializer.Write(backupStream, password);
                        }
                        else
                        {
                            backupStream.SetLength(0);
                            Household.WriteXml(backupStream);
                        }

                        GuiServices.SetCursor(false);

                        UserSettings.LastBackupFile = file;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorMessage($"Error while backing up to file {Path.GetFileName(file)}: {e.Message}");
                return;
            }
        }

        public void SetPassword()
        {
            var logic = new PasswordPromptLogic(password, "Change password");
            if (GuiServices.ShowDialog(logic))
            {
                password = logic.Password;
                SaveToFile();
                UpdateTitle();
            }
        }

        public void ImportQIF(string file)
        {
            GuiServices.SetCursor(true);

            var parser = new QIFParser(Household);
            parser.ImportFromQIF(file, true);

            GuiServices.SetCursor(false);

            if (!string.IsNullOrEmpty(parser.Log))
            {
                ErrorMessage(parser.Log, "Import results");
            }

            UpdateAll();
            UpdateTitle();
        }

        public void ExportQIF(string file)
        {
            GuiServices.SetCursor(true);

            var exporter = new QIFWriter(this);
            exporter.ExportToQIF(file);
            ErrorMessage("Export completed", "Export results");

            GuiServices.SetCursor(false);
        }

        public void DifferentialExportQIF(string file)
        {
            var exporter = new QIFWriter(this);
            exporter.DifferentialExportToQIF(file);
            ErrorMessage("Differential export completed; new checkpoint created", "Export results");
        }

        public void SaveIfDirty()
        {
            if (Dirty)
            {
                SaveFile();
            }
        }

        public void SaveUserSettings()
        {
            settingsManager.Save(UserSettings);
        }

        #endregion

        #region UI Services

        public void ErrorMessage(string message, string title = "Error")
        {
            var logic = new ErrorLogic(message, title);
            GuiServices.ShowDialog(logic);
        }

        // returns true if answer is yes
        public bool YesNoQuestion(string question)
        {
            var logic = new QuestionLogic(question);
            return GuiServices.ShowDialog(logic);
        }

        #endregion

        #region DB services

        // Commit changes to household data set
        public void CommitChanges()
        {
            lock (householdLock)
            {
                if (Household.SanityCheck() is string error)
                {
                    ErrorMessage("DB sanity check error: " + error);
                    Household.RejectChanges();
                    return;
                }

                Household.AcceptChanges();
                if (Household.HasErrors)
                {
                    ErrorMessage("Error in dataset");
                }

                Dirty = true;
                UpdateTitle();
            }
        }

        #endregion

        #region Updates and notifications

        public void UpdateAll()
        {
            // Close registers
            BankAccountGroup.SelectedAccount = null;
            AssetAccountGroup.SelectedAccount = null;
            InvestmentAccountGroup.SelectedAccount = null;
            IsInvestmentRegisterVisible = false;
            IsBankRegisterVisible = false;
            DisplayedAccountID = -1;
            MainMenuLogic.Reconcile.SetCanExecute(false);
            MainMenuLogic.ShowHoldings.SetCanExecute(false);
            MainMenuLogic.ShowRebalance.SetCanExecute(false);
            OnPropertyChanged(() => IsInvestmentRegisterVisible);
            OnPropertyChanged(() => IsBankRegisterVisible);

            // Update all accounts
            UpdateAccountNamesAndBalances(null);

            OnPropertyChanged(() => NetWorth);

            // Compute the memorized payees
            BuildMemorizedPayeeList();

            // Compute the known categories
            BuildCategoriesList();

            // Compute the known securities
            BuildSecuritiesList();

            // Update transaction reports in menu
            MainMenuLogic.UpdateTransactionReports();
        }

        // Action after securities are changed
        public void NotifySecurityChange()
        {
            SecuritiesChanged?.Invoke(this, EventArgs.Empty);
        }

        // Action after categories are changed
        public void NotifyCategoriesChanged()
        {
            CategoriesChanged?.Invoke(this, EventArgs.Empty);
        }

        // Action after memorized payees are changed
        public void UpdateMemorizedPayees()
        {
            // Rebuild list
            BuildMemorizedPayeeList();
        }

        // Close a specific register
        public void CloseRegisterIfOpen(int accountID)
        {
            if (DisplayedAccountID == accountID)
            {
                IsInvestmentRegisterVisible = false;
                IsBankRegisterVisible = false;
                OnPropertyChanged(() => IsInvestmentRegisterVisible);
                OnPropertyChanged(() => IsBankRegisterVisible);
                DisplayedAccountID = -1;
            }
        }

        public void OnBankAccountClicked(AccountGroupLogic sender, int accountID)
        {
            InvestmentAccountGroup.SelectedAccount = null;
            if (sender == AssetAccountGroup)
            {
                BankAccountGroup.SelectedAccount = null;
            }
            else
            {
                AssetAccountGroup.SelectedAccount = null;
            }

            OnBankAccountClicked(accountID);
        }

        private void OnBankAccountClicked(int accountID, int transactionID = int.MinValue)
        { 
            BankRegister.SetAccount(accountID, transactionID);
            DisplayedAccountID = accountID;
            MainMenuLogic.Reconcile.SetCanExecute(accountID >= 0);
            MainMenuLogic.ShowHoldings.SetCanExecute(false);
            MainMenuLogic.ShowRebalance.SetCanExecute(false);
            IsInvestmentRegisterVisible = false;
            IsBankRegisterVisible = true;
            OnPropertyChanged(() => IsInvestmentRegisterVisible);
            OnPropertyChanged(() => IsBankRegisterVisible);
        }

        public void OnInvestmentAccountClicked(int accountID, int transactionID = int.MinValue)
        {
            BankAccountGroup.SelectedAccount = null;
            AssetAccountGroup.SelectedAccount = null;

            InvestmentRegister.SetAccount(accountID, transactionID);
            DisplayedAccountID = accountID;
            MainMenuLogic.Reconcile.SetCanExecute(accountID >= 0);
            MainMenuLogic.ShowHoldings.SetCanExecute(accountID >= 0);
            MainMenuLogic.ShowRebalance.SetCanExecute(accountID >= 0);
            IsInvestmentRegisterVisible = true;
            IsBankRegisterVisible = false;
            OnPropertyChanged(() => IsInvestmentRegisterVisible);
            OnPropertyChanged(() => IsBankRegisterVisible);
        }

        public void GotoTransaction(int accountID, int transactionID)
        {
            BankAccountGroup.SelectedAccount = null;
            AssetAccountGroup.SelectedAccount = null;
            InvestmentAccountGroup.SelectedAccount = null;

            var accountRow = Household.Account.FindByID(accountID);
            if (accountRow.Type == EAccountType.Investment)
            {
                OnInvestmentAccountClicked(accountID, transactionID);
            }
            else
            {
                OnBankAccountClicked(accountID, transactionID);
            }
        }

        // Update balances and net worth after a transaction is modified
        public void UpdateAccountNamesAndBalances(IEnumerable<int> accountIDs)
        {
            NetWorth = 0;

            NetWorth += BankAccountGroup.UpdateAccountsAndBalances(accountIDs);
            NetWorth += InvestmentAccountGroup.UpdateAccountsAndBalances(accountIDs);
            NetWorth += AssetAccountGroup.UpdateAccountsAndBalances(accountIDs);

            OnPropertyChanged(() => NetWorth);
        }

        #endregion

        #region Utilities

        // Builds title
        private void UpdateTitle()
        {
            Title = APPNAME;

            if (!string.IsNullOrWhiteSpace(UserSettings.LastFileOpened))
            {
                Title += " " + System.IO.Path.GetFileName(UserSettings.LastFileOpened);
            }
            if (Dirty)
            {
                Title += " *";
            }

            OnPropertyChanged(() => Title);
        }

        // Save dataset to file in XML, encrypted or not
        private void SaveToFile()
        {
            if (fileStream != null)
            {
                lock (householdLock)
                {
                    GuiServices.SetCursor(true);

                    if (fileFormat == EFileFormat.Ban)
                    {
                        var serializer = new BANSerializer(Household);
                        serializer.Write(fileStream, password);
                    }
                    else
                    {
                        fileStream.SetLength(0);
                        Household.WriteXml(fileStream);
                        fileStream.Flush();
                    }

                    GuiServices.SetCursor(false);
                    Dirty = false;
                }

                UpdateTitle();
            }
        }

        // Save to disk every 5 minutes
        private void OnSaveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            SaveIfDirty();
        }

        // Builds or rebuilds the list of memorized payees
        private void BuildMemorizedPayeeList()
        {
            var household = Household;

            MemorizedPayees.Clear();

            foreach (var mpr in Household.MemorizedPayee)
            {
                // Get memorized line item(s)
                var dbLineItems = mpr.GetMemorizedLineItemRows();
                var lineItems = new List<LineItem>(dbLineItems.Length);
                foreach (var dbli in dbLineItems)
                {
                    string category = "";
                    int categoryID = -1;
                    int categoryAccountID = -1;
                    if (!dbli.IsCategoryIDNull())
                    {
                        var destCategory = household.Category.FindByID(dbli.CategoryID);
                        category = destCategory.FullName;
                        categoryID = destCategory.ID;
                    }
                    else if (!dbli.IsAccountIDNull())
                    {
                        categoryAccountID = dbli.AccountID;
                    }

                    string memo = dbli.IsMemoNull() ? "" : dbli.Memo;

                    lineItems.Add(new LineItem(this, dbli.ID, category, categoryID, categoryAccountID, memo, dbli.Amount, true));
                }

                var mp = new MemorizedPayeeItem(mpr.ID, mpr.Payee, mpr.IsMemoNull() ? "" : mpr.Memo, lineItems.ToArray());

                MemorizedPayees.Add(mp);
            }

            MemorizedPayees.Sort();
            MemorizedPayeesChanged?.Invoke(this, EventArgs.Empty);
        }

        // Builds or rebuilds the list of categories
        private void BuildCategoriesList()
        {
            Categories.Clear();
            Transfers.Clear();
            CategoriesAndTransfers.Clear();
            HiddenCategories.Clear();

            // Add all top-level categories
            foreach (var category in Household.Category)
            {
                // Exclude internal categories (not used)
                if (category.IsParentIDNull())
                {
                    var categoryItem = CategoryItem.CreateFromDB(category, Enumerable.Empty<CategoryItem>());
                    if (category.Name.StartsWith("_"))
                    {
                        HiddenCategories.Add(categoryItem);
                    }
                    else
                    {
                        Categories.Add(categoryItem);
                        CategoriesAndTransfers.Add(categoryItem);
                    }
                }
            }

            // Add children as long as there are children to be added
            bool categoryNotFound;
            do
            {
                categoryNotFound = false;

                foreach (var category in Household.Category)
                {
                    // If category has a parent
                    if (!category.IsParentIDNull())
                    {
                        // If this category is not in the list yet
                        if (Categories.FirstOrDefault(c => c.ID == category.ID) == null)
                        {
                            categoryNotFound = true;

                            // Create category
                            var categoryItem = CategoryItem.CreateFromDB(category, Categories);

                            Categories.Add(categoryItem);
                            CategoriesAndTransfers.Add(categoryItem);
                        }                        
                    }
                }
            } while (categoryNotFound);

            // Add all possible transfers
            foreach (var account in Household.Account)
            {
                if (!account.Hidden)
                {
                    var transferItem = new CategoryItem(account.ID, account.Name);
                    Transfers.Add(transferItem);
                    CategoriesAndTransfers.Add(transferItem);
                }
            }

            Categories.Sort();
            Transfers.Sort();
            CategoriesAndTransfers.Sort();
            HiddenCategories.Sort();

            CategoriesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void BuildSecuritiesList()
        {
            Securities.Clear();

            foreach (Household.SecurityRow security in Household.Security.Rows)
            {
                Securities.Add(new SecurityItem(security.ID, security.Name, security.Symbol, security.Type));
            }

            NotifySecurityChange();
        }

        private void OnSearch(object arg)
        {
            if (arg is string text && !string.IsNullOrWhiteSpace(text))
            {
                GuiServices.ShowDialog(new SearchResultLogic(this, text));
            }
        }

        #endregion
    }
}
