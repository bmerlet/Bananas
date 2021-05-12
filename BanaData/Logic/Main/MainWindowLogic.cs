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
            MainMenu = new MainMenuLogic(this);
            BankAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Banking);
            InvestmentAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Investment);
            AssetAccountGroup = new AccountGroupLogic(this, AccountGroupLogic.EType.Asset);

            UpdateAll();
        }

        // Gui services
        public readonly IGuiServices GuiServices;

        // The database
        public readonly Household Household;

        // Main form sub-logics
        public readonly MainMenuLogic MainMenu;
        // public readonly StatusBar StatusBar;

        // User settings
        public readonly UserSettings UserSettings;

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

        #endregion

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
        }
    }
}
