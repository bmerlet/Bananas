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

using Toolbox.Attributes;
using Toolbox.UILogic;
using Toolbox.Models;
using BanaData.Database;
using BanaData.Serializations;
using BanaData.Logic.Items;
using BanaData.Logic.Dialogs;

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
                if (UserSettings.LastFileOpened != null)
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

        public void OpenFile(string file)
        {
            if (file.EndsWith(".BAN", StringComparison.InvariantCultureIgnoreCase))
            {
                // Ask for password
                var logic = new PasswordPromptLogic(password, "Open file");
                if (!GuiServices.ShowDialog(logic))
                {
                    return;
                }
                password = logic.Password;
            }

            ReadFromFile(file);
            UpdateAll();
        }

        public void SaveFile(string file)
        {
            SaveToFile(file);

            UserSettings.LastFileOpened = file;
            UpdateTitle();
        }

        public void SetPassword()
        {
            var logic = new PasswordPromptLogic(password, "Change password");
            if (GuiServices.ShowDialog(logic))
            {
                password = logic.Password;
                SaveToFile(UserSettings.LastFileOpened);
                UpdateTitle();
            }
        }

        public void ImportQIF(string file)
        {
            GuiServices.SetCursor(true);

            var parser = new QIFParser(this);
            parser.ImportFromQIF(file, true);

            GuiServices.SetCursor(false);

            if (!string.IsNullOrEmpty(parser.Log))
            {
                ErrorMessage(parser.Log, "Import results");
            }

            // Save to a .BAN (ZZZ Revisit later)
            file = file.Substring(0, file.Length - 3) + "BAN";
            UserSettings.LastFileOpened = file;

            SaveToFile(file);
            UpdateAll();
        }

        public void MergeQIF(string file)
        {
            GuiServices.SetCursor(true);

            var parser = new QIFParser(this);
            bool change = parser.MergeFromQIF(file);

            GuiServices.SetCursor(false);

            if (!string.IsNullOrEmpty(parser.Log))
            {
                ErrorMessage(parser.Log, "Merge results");
            }

            if (change)
            {
                Dirty = true;
                UpdateAll();
            }
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
                Save();
            }
        }

        public void Save()
        {
            SaveToFile(UserSettings.LastFileOpened);
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

            // Update accounts
            NetWorth = 0;
            NetWorth += BankAccountGroup.UpdateAccountsAndBalances();
            NetWorth += InvestmentAccountGroup.UpdateAccountsAndBalances();
            NetWorth += AssetAccountGroup.UpdateAccountsAndBalances();

            OnPropertyChanged(() => NetWorth);

            // Compute the memorized payees
            BuildMemorizedPayeeList();

            // Compute the known categories
            BuildCategoriesList();

            // Compute the known securities
            BuildSecuritiesList();
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

        private void OnBankAccountClicked(int accountID, int transactionID = int.MinValue, int lineItemID = int.MinValue)
        { 
            BankRegister.SetAccount(accountID, transactionID, lineItemID);
            DisplayedAccountID = accountID;
            MainMenuLogic.Reconcile.SetCanExecute(accountID >= 0);
            MainMenuLogic.ShowHoldings.SetCanExecute(false);
            MainMenuLogic.ShowRebalance.SetCanExecute(false);
            IsInvestmentRegisterVisible = false;
            IsBankRegisterVisible = true;
            OnPropertyChanged(() => IsInvestmentRegisterVisible);
            OnPropertyChanged(() => IsBankRegisterVisible);
        }

        public void OnInvestmentAccountClicked(int accountID, int transactionID = int.MinValue, int lineItemID = int.MinValue)
        {
            BankAccountGroup.SelectedAccount = null;
            AssetAccountGroup.SelectedAccount = null;

            InvestmentRegister.SetAccount(accountID, transactionID, lineItemID);
            DisplayedAccountID = accountID;
            MainMenuLogic.Reconcile.SetCanExecute(accountID >= 0);
            MainMenuLogic.ShowHoldings.SetCanExecute(accountID >= 0);
            MainMenuLogic.ShowRebalance.SetCanExecute(accountID >= 0);
            IsInvestmentRegisterVisible = true;
            IsBankRegisterVisible = false;
            OnPropertyChanged(() => IsInvestmentRegisterVisible);
            OnPropertyChanged(() => IsBankRegisterVisible);
        }

        public void GotoTransaction(int accountID, int transactionID, int lineItemID)
        {
            BankAccountGroup.SelectedAccount = null;
            AssetAccountGroup.SelectedAccount = null;
            InvestmentAccountGroup.SelectedAccount = null;

            var accountRow = Household.Account.FindByID(accountID);
            if (accountRow.Type == EAccountType.Investment)
            {
                OnInvestmentAccountClicked(accountID, transactionID, lineItemID);
            }
            else
            {
                OnBankAccountClicked(accountID, transactionID, lineItemID);
            }
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
        private void SaveToFile(string file)
        {
            lock (householdLock)
            {
                GuiServices.SetCursor(true);

                if (file.EndsWith(".BAN", StringComparison.InvariantCultureIgnoreCase))
                {
                    var serializer = new BANSerializer(Household);
                    serializer.Write(file, password);
                }
                else if (file.EndsWith(".XBAN", StringComparison.InvariantCultureIgnoreCase))
                {
                    Household.WriteXml(file);
                }

                GuiServices.SetCursor(false);
                Dirty = false;
            }

            UpdateTitle();
        }

        private void ReadFromFile(string file)
        {
            try
            {
                GuiServices.SetCursor(true);

                if (file.EndsWith(".BAN", StringComparison.InvariantCultureIgnoreCase))
                {
                    var serializer = new BANSerializer(Household);
                    serializer.Read(file, password);
                }
                else if (file.EndsWith(".XBAN", StringComparison.InvariantCultureIgnoreCase))
                {
                    Household.Clear();
                    Household.AcceptChanges();
                    Household.ReadXml(file);
                }
                else
                {
                    throw new InvalidOperationException("File extension not supported");
                }
            }
            catch (Exception e)
            {
                //DataRow[] rowErrors = Household.Accounts.GetErrors();
                ErrorMessage($"Error reading file {System.IO.Path.GetFileName(file)}: {e.Message}");
            }
            finally
            {
                GuiServices.SetCursor(false);
            }

            UserSettings.LastFileOpened = file;
            Dirty = false;
            UpdateTitle();
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
                    var description = category.IsDescriptionNull() ? "" : category.Description;
                    var taxInfo = category.IsTaxInfoNull() ? "" : category.TaxInfo;
                    var categoryItem = new CategoryItem(category.ID, category.Name, description, category.IsIncome, taxInfo, null);
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
                        if (CategoriesAndTransfers.FirstOrDefault(c => c.ID == category.ID) == null)
                        {
                            categoryNotFound = true;

                            // Find parent in list
                            var parent = CategoriesAndTransfers.FirstOrDefault(c => c.ID == category.ParentID);
                            if (parent != null)
                            {
                                // Create category
                                var description = category.IsDescriptionNull() ? "" : category.Description;
                                var taxInfo = category.IsTaxInfoNull() ? "" : category.TaxInfo;
                                var categoryItem = new CategoryItem(category.ID, category.Name, description, category.IsIncome, taxInfo, parent);

                                Categories.Add(categoryItem);
                                CategoriesAndTransfers.Add(categoryItem);
                            }
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

        #endregion
    }
}
