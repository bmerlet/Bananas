using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public readonly List<CategoryItem> HiddenTransfers = new List<CategoryItem>();
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
            // Zap existing
            PrepareForNewFile();

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
            // Zap existing
            PrepareForNewFile();

            // Assume the open is going to work
            UserSettings.LastFileOpened = file;
            Dirty = false;
            UpdateTitle();
            UpdateAll();

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

        private void PrepareForNewFile()
        {
            // Save and close the existing file
            if (fileStream != null)
            {
                SaveFile();

                fileStream.Close();
                fileStream = null;
            }

            // Zap the DB
            Household.Clear();
            Household.AcceptChanges();

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

            Dirty = true;
            UpdateAll();
            UpdateTitle();
        }

        public void ExportQIF(string file, QIFWriter.EContents contents, IEnumerable<Household.AccountRow> transactionAccounts)
        {
            GuiServices.SetCursor(true);

            var exporter = new QIFWriter(this);
            var result = exporter.Export(file, contents, transactionAccounts);
            ErrorMessage(result, "Export results");

            GuiServices.SetCursor(false);
        }

        public void DifferentialExportQIF(string file)
        {
            var exporter = new QIFWriter(this);
            (int numAccounts, int numTransactions)  = exporter.DifferentialExportToQIF(file);
            ErrorMessage($"Differential export: Exported {numTransactions} transactions in {numAccounts} accounts; new checkpoint created", "Export results");
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

        // Look for scheduled transactions that need to be executed
        public void CheckForScheduledTransactions()
        {
            var now = DateTime.Today;
            bool changesToCommit = false;

            foreach(var scheduleRow in Household.Schedule)
            {
                if (now.CompareTo(scheduleRow.NextDate) >= 0)
                {
                    // Time to enter a scheduled transaction!
                    bool doIt = true;
                    bool updateNextDate = true;

                    // Prompt the user
                    if (scheduleRow.Flags.HasFlag(EScheduleFlag.PromptBefore))
                    {
                        var scheduleItem = new ScheduleItem(this, scheduleRow);
                        doIt = YesNoQuestion($"Do you want to enter the scheduled transaction on account {scheduleItem.Account}, category {scheduleItem.Category} for {scheduleItem.Amount:N2}?");
                        if (!doIt)
                        {
                            updateNextDate = YesNoQuestion("Do you want to skip this iteration?");
                        }
                    }

                    // Enter it
                    if (doIt)
                    {
                        var str = scheduleRow.TransactionRow;

                        // Create transaction
                        var transactionRow = Household.Transaction.Add(
                            str.AccountRow,
                            scheduleRow.NextDate,
                            str.IsPayeeNull() ? null : str.Payee,
                            str.IsMemoNull() ? null : str.Memo,
                            ETransactionStatus.Pending,
                            Household.Checkpoint.GetMostRecentCheckpoint(),
                            ETransactionType.Regular);

                        // Banking transaction if needed
                        if (str.AccountRow.Type == EAccountType.Bank)
                        {
                            var medium = str.GetBankingTransaction().Medium;
                            decimal checkNumber = 0;
                            if (medium == ETransactionMedium.NextCheckNum)
                            {
                                medium = ETransactionMedium.Check;
                                checkNumber = Household.BankingTransactionDataTable.GetNextCheckNumber(str.AccountRow);
                            }
                            Household.BankingTransaction.Add(transactionRow, medium, (uint)checkNumber);
                        }

                        // Line items
                        foreach(var sli in str.GetLineItemRows())
                        {
                            var memo = sli.IsMemoNull() ? null : sli.Memo;
                            var lineItemRow = Household.LineItem.Add(transactionRow, memo, sli.Amount);

                            if (sli.GetLineItemCategoryRow() is Household.LineItemCategoryRow licr)
                            {
                                Household.LineItemCategory.AddLineItemCategoryRow(lineItemRow, licr.CategoryRow);
                            }
                            else if (sli.GetLineItemTransferRow() is Household.LineItemTransferRow litr)
                            {
                                // Create peer transaction
                                transactionRow.CreatePeerTransaction(litr.AccountID, lineItemRow, -lineItemRow.Amount);
                            }
                        }

                        // Update balances
                        UpdateAccountNamesAndBalances(new int[] { str.AccountID });

                        // Update register if open
                        if (DisplayedAccountID == str.AccountID)
                        {
                            BankRegister.AddTransaction(transactionRow.ID);
                        }
                    }

                    // Update next date
                    if (updateNextDate)
                    {
                        switch(scheduleRow.Frequency)
                        {
                            case EScheduleFrequency.Daily:
                                scheduleRow.NextDate = scheduleRow.NextDate.AddDays(1);
                                break;
                            case EScheduleFrequency.Weekly:
                                scheduleRow.NextDate = scheduleRow.NextDate.AddDays(7);
                                break;
                            case EScheduleFrequency.Biweekly:
                                scheduleRow.NextDate = scheduleRow.NextDate.AddDays(14);
                                break;
                            case EScheduleFrequency.SemiMonthly:
                                if (scheduleRow.NextDate.Day >= 15)
                                {
                                    scheduleRow.NextDate = scheduleRow.NextDate.AddDays(-15);
                                    scheduleRow.NextDate = scheduleRow.NextDate.AddMonths(1);
                                }
                                else
                                {
                                    scheduleRow.NextDate = scheduleRow.NextDate.AddDays(14);
                                }
                                break;
                            case EScheduleFrequency.Monthly:
                                scheduleRow.NextDate = scheduleRow.NextDate.AddMonths(1);
                                break;
                            case EScheduleFrequency.Quarterly:
                                scheduleRow.NextDate = scheduleRow.NextDate.AddMonths(3);
                                break;
                            case EScheduleFrequency.Yearly:
                                scheduleRow.NextDate = scheduleRow.NextDate.AddYears(1);
                                break;
                        }
                        CommitChanges();
                    }

                    // Notify 
                    if (scheduleRow.Flags.HasFlag(EScheduleFlag.NotifyAfter))
                    {
                        var scheduleItem = new ScheduleItem(this, scheduleRow);
                        ErrorMessage($"Note: Entered the scheduled transaction on account {scheduleItem.Account}, category {scheduleItem.Category} for {scheduleItem.Amount:N2}.");
                    }
                }

                if (scheduleRow.Flags.HasFlag(EScheduleFlag.Expires) && now.CompareTo(scheduleRow.EndDate) >= 0)
                {
                    var scheduleItem = new ScheduleItem(this, scheduleRow);
                    bool del = YesNoQuestion($"Scheduled transaction on account {scheduleItem.Account}, category {scheduleItem.Category} for {scheduleItem.Amount:N2} has expired - delete it?");
                    if (del)
                    {
                        var str = scheduleRow.TransactionRow;
                        foreach (var li in str.GetLineItemRows())
                        {
                            li.CascadeDelete();
                        }

                        if (str.AccountRow.Type == EAccountType.Bank)
                        {
                            str.GetBankingTransaction().Delete();
                        }

                        scheduleRow.Delete();
                        str.Delete();

                        changesToCommit = true;
                    }
                }
            } // for all schedules

            if (changesToCommit)
            {
                CommitChanges();
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

            // Compute the known categories
            BuildCategoriesList();

            // Compute the memorized payees
            BuildMemorizedPayeeList();

            // Compute the known securities
            BuildSecuritiesList();

            // Update transaction reports in menu
            MainMenuLogic.UpdateTransactionReports();

            // See if scheduled transactions need to happen
            CheckForScheduledTransactions();
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
            GuiServices.ExecuteAsync((Action)delegate ()
            {
                SaveIfDirty();
                CheckForScheduledTransactions();
            });
        }

        // Builds or rebuilds the list of memorized payees
        private void BuildMemorizedPayeeList()
        {
            var household = Household;

            MemorizedPayees.Clear();

            foreach (var mpr in Household.MemorizedPayees)
            {
                // Get memorized line item(s)
                var dbLineItems = mpr.GetLineItemRows();
                var lineItems = new List<LineItem>(dbLineItems.Length);
                foreach (var dbli in dbLineItems)
                {
                    lineItems.Add(new LineItem(this, dbli, true));
                }

                var mp = new MemorizedPayeeItem(mpr.ID, mpr.Payee, mpr.IsMemoNull() ? "" : mpr.Memo, lineItems.ToArray());

                MemorizedPayees.Add(mp);
            }

            MemorizedPayees.Sort();
            MemorizedPayeesChanged?.Invoke(this, EventArgs.Empty);
        }

        // Builds or rebuilds the list of categories
        public void BuildCategoriesList()
        {
            Categories.Clear();
            Transfers.Clear();
            HiddenTransfers.Clear();
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
                var transferItem = new CategoryItem(account.ID, account.Name);
                if (account.Hidden)
                {
                    HiddenTransfers.Add(transferItem);
                }
                else
                {
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
