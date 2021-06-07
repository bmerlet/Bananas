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

namespace BanaData.Logic.Main
{
    /// <summary>
    /// Main window logic
    /// </summary>
    public class MainWindowLogic : LogicBase
    {
        // User settings manager
        private readonly SettingsManager<UserSettings> settingsManager = new SettingsManager<UserSettings>("Sarabande Inc.", "Bananas");

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
        }

        // Gui services
        public readonly IGuiServices GuiServices;

        // The database
        public readonly Household Household;

        // Main form sub-logics
        public MainMenuLogic MainMenuLogic { get; }
        // public readonly StatusBar StatusBar;

        // User settings
        public readonly UserSettings UserSettings;

        // List of memorized payees
        public readonly List<MemorizedPayeeItem> MemorizedPayees = new List<MemorizedPayeeItem>();

        // List of categories, as displayed in the UI
        public readonly List<CategoryItem> Categories = new List<CategoryItem>();

        #region UI properties

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

        public void OpenFile(string file)
        {
            if (file.EndsWith(".QIF", StringComparison.InvariantCultureIgnoreCase))
            {
                Converter.ConvertFromQIF(file, Household);
                UpdateAll();
                UserSettings.LastFileOpened = file;
            }
        }

        public void SaveUserSettings()
        {
            settingsManager.Save(UserSettings);
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
                    if (!dbli.IsCategoryIDNull())
                    {
                        var destCategory = household.Categories.FindByID(dbLineItems[0].CategoryID);
                        category = destCategory.FullName;
                    }

                    string memo = dbli.IsMemoNull() ? "" : dbli.Memo;

                    lineItems.Add(new LineItem(dbli.ID, category, memo, dbli.Amount));
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

            foreach (var category in Household.Categories)
            {
                Categories.Add(new CategoryItem(category.FullName));
            }

            MemorizedPayees.Sort();
        }
    }
}
