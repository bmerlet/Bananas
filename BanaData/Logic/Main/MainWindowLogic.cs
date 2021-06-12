using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using Toolbox.Models;
using BanaData.Database;
using BanaData.Serializations;
using BanaData.Logic.Items;
using BanaData.Logic.Dialogs;
using System.Timers;

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

            // Create logic for main window subcomponents
            MainMenuLogic = new MainMenuLogic(this);
            BankAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Banking);
            InvestmentAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Investment);
            AssetAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Asset);
            BankRegister = new BankRegisterLogic(this);

            BankAccountGroup.AccountClicked += (o, e) => OnBankAccountClicked(e.AccountID);

            if (UserSettings.LastFileOpened != null)
            {
                OpenFile(UserSettings.LastFileOpened);
            }
            else
            {
                UpdateAll();
            }

            // Setup timer to save to disk every 5 minutes
            saveTimer.Elapsed += OnSaveTimerElapsed;
            saveTimer.Enabled = true;
        }

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
        public readonly List<CategoryItem> Categories = new List<CategoryItem>();

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

        public string NetWorth { get; private set; }

        // The bank register
        public BankRegisterLogic BankRegister { get; private set; }

        #endregion

        #region Services

        public void ErrorMessage(string message)
        {
            var logic = new ErrorLogic(message);
            GuiServices.ShowDialog(logic);
        }

        public void OpenFile(string file)
        {
            if (file.EndsWith(".QIF", StringComparison.InvariantCultureIgnoreCase))
            {
                Converter.ConvertFromQIF(file, Household);

                // Save to a .XBAN (ZZZ Revisit later)
                file = file.Substring(0, file.Length - 3) + "xban";
                UserSettings.LastFileOpened = file;

                SaveToFile(file);
            }
            else if (file.EndsWith(".XBAN", StringComparison.InvariantCultureIgnoreCase))
            {
                ReadFromFile(file);
            }

            UpdateAll();
        }

        // Commit changes to household data set
        public void CommitChanges()
        {
            lock (householdLock)
            {
                Household.AcceptChanges();
                if (Household.HasErrors)
                {
                    GuiServices.ShowDialog(new ErrorLogic("Error in dataset"));
                }

                Dirty = true;
                UpdateTitle();
            }
        }

        public void SaveUserSettings()
        {
            settingsManager.Save(UserSettings);
        }

        public void Save()
        {
            SaveToFile(UserSettings.LastFileOpened);
        }

        public void UpdateAll()
        {
            // Update accounts
            decimal netWorth = 0;

            netWorth += BankAccountGroup.UpdateAccountsAndBalances();
            netWorth += InvestmentAccountGroup.UpdateAccountsAndBalances();
            netWorth += AssetAccountGroup.UpdateAccountsAndBalances();

            NetWorth = netWorth.ToString("N");
            OnPropertyChanged(() => NetWorth);

            // Compute the memorized payees
            BuildMemorizedPayeeList();

            // Comppute the known categories
            BuildCategoriesList();
        }

        // Action after memorized payees are changed
        public void UpdateMemorizedPayees()
        {
            // Rebuild list
            BuildMemorizedPayeeList();
        }

        public void OnBankAccountClicked(int accountID)
        {
            BankRegister.SetAccount(accountID);
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

        // Save dataset to file in XML
        private void SaveToFile(string file)
        {
            lock (householdLock)
            {
                Household.WriteXml(file);
                Dirty = false;
            }

            UpdateTitle();
        }

        private void ReadFromFile(string file)
        {
            Household.ReadXml(file);

            UserSettings.LastFileOpened = file;
            Dirty = false;
            UpdateTitle();
        }

        // Save to disk every 5 minutes
        private void OnSaveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Dirty)
            {
                SaveToFile(UserSettings.LastFileOpened);
            }
        }

        // Builds or rebuilds the list of memorized payees
        private void BuildMemorizedPayeeList()
        {
            var household = Household;

            MemorizedPayees.Clear();

            foreach (var mpr in Household.MemorizedPayees)
            {
                // Get memorized line item(s)
                var dbLineItems = household.MemorizedLineItems.GetByMemorizedPayee(mpr);
                var lineItems = new List<LineItem>(dbLineItems.Length);
                foreach (var dbli in dbLineItems)
                {
                    string category = "";
                    int categoryID = -1;
                    int categoryAccountID = -1;
                    if (!dbli.IsCategoryIDNull())
                    {
                        var destCategory = household.Categories.FindByID(dbli.CategoryID);
                        category = destCategory.FullName;
                        categoryID = destCategory.ID;
                    }
                    else if (!dbli.IsAccountIDNull())
                    {
                        categoryAccountID = dbli.AccountID;
                    }

                    string memo = dbli.IsMemoNull() ? "" : dbli.Memo;

                    lineItems.Add(new LineItem(dbli.ID, category, categoryID, categoryAccountID, memo, dbli.Amount));
                }

                var mp = new MemorizedPayeeItem(mpr.ID, mpr.Payee, lineItems.ToArray());

                MemorizedPayees.Add(mp);
            }

            MemorizedPayees.Sort();
        }

        // Builds or rebuilds the list of categories
        private void BuildCategoriesList()
        {
            Categories.Clear();

            // Add all top-level categories
            foreach (var category in Household.Categories)
            {
                if (category.IsParentIDNull())
                {
                    Categories.Add(new CategoryItem(category.ID, category.Name, null));
                }
            }

            // Add children as long as there are children to be added
            bool categoryNotFound;
            do
            {
                categoryNotFound = false;

                foreach (var category in Household.Categories)
                {
                    // If category has a parent
                    if (!category.IsParentIDNull())
                    {
                        // If this category is not in the list yet
                        if (Categories.FirstOrDefault(c => c.ID == category.ID) == null)
                        {
                            categoryNotFound = true;

                            // Find parent in list
                            var parent = Categories.FirstOrDefault(c => c.ID == category.ParentID);
                            if (parent != null)
                            {
                                // Find index of parent
                                int indexOfParent = Categories.IndexOf(parent);

                                // Find index of last category with this parent
                                while (Categories[indexOfParent + 1].Parent == parent)
                                {
                                    indexOfParent++;
                                }

                                // Create category
                                var categoryItem = new CategoryItem(category.ID, category.Name, parent);
                                if (indexOfParent == Categories.Count - 1)
                                {
                                    Categories.Add(categoryItem);
                                }
                                else
                                {
                                    Categories.Insert(indexOfParent + 1, categoryItem);
                                }
                            }
                        }                        
                    }
                }
            } while (categoryNotFound);

            // Add all possible transfers
            foreach (var account in Household.Accounts)
            {
                Categories.Add(new CategoryItem(account.ID, account.Name));
            }

            //Categories.Sort();
        }

        #endregion
    }
}
