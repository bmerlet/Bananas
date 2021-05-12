using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Database;
using System.Collections.ObjectModel;

namespace BanaData.Logic.Main
{
    /// <summary>
    /// Main window logic
    /// </summary>
    public class MainWindow : LogicBase
    {
        public MainWindow(IGuiServices guiServices)
        {
            // Memorize UI service provider
            GuiServices = guiServices;

            // Create empty database
            Household = new Household();

            // Create logic for main window subcomponents
            MainMenu = new MainMenu(this);
            BankAccountGroup = new AccountGroup(this, AccountGroup.EType.Banking);
            InvestmentAccountGroup = new AccountGroup(this, AccountGroup.EType.Investment);
            AssetAccountGroup = new AccountGroup(this, AccountGroup.EType.Asset);
        }

        // Gui services
        public readonly IGuiServices GuiServices;

        // The database
        public readonly Household Household;

        // Main form sub-logics
        public readonly MainMenu MainMenu;
        // public readonly StatusBar StatusBar;

        #region UI properties

        // The accounts and balances displayed on the left side
        public AccountGroup BankAccountGroup { get; private set; }
        public AccountGroup InvestmentAccountGroup { get; private set; }
        public AccountGroup AssetAccountGroup { get; private set; }

        public string NetWorth { get; private set; }

        #endregion

        public void UpdateAll()
        {
            decimal netWorth = 0;

            netWorth += BankAccountGroup.UpdateAccountsAndBalances();
            netWorth += InvestmentAccountGroup.UpdateAccountsAndBalances();
            netWorth += AssetAccountGroup.UpdateAccountsAndBalances();

            NetWorth = netWorth.ToString("N");
            OnPropertyChanged(() => NetWorth);
        }
    }
}
