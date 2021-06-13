using BanaData.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;

namespace BanaData.Logic.Main
{
    public class AccountGroupLogic : LogicBase
    {
        public enum EType { Banking, Investment, Asset };

        #region Private members

        private readonly MainWindowLogic mainWindow;
        private readonly EType type;

        #endregion

        #region Constructor

        public AccountGroupLogic(MainWindowLogic mainWindow, EType type)
        {
            this.mainWindow = mainWindow;
            this.type = type;

            switch(type)
            {
                case EType.Banking:
                    Header = "Banking Accounts:";
                    Footer = "Banking total:";
                    break;
                case EType.Investment:
                    Header = "Investment Accounts:";
                    Footer = "Investments total:";
                    break;
                case EType.Asset:
                    Header = "Asset Accounts:";
                    Footer = "Assets total:";
                    break;
            }
        }

        #endregion

        #region Events

        public class AccountClickedEventArgs : EventArgs
        {
            public AccountClickedEventArgs(int id)
            {
                AccountID = id;
            }

            public readonly int AccountID;
        }

        // Invoked when an account is clicked
        public EventHandler<AccountClickedEventArgs> AccountClicked;

        #endregion

        #region UI properties

        // Name
        public string Header { get; private set; }
        public string Footer { get; private set; }

        // List of accounts and balances
        public ObservableCollection<AccountAndBalance> AccountsAndBalances { get; } = new ObservableCollection<AccountAndBalance>();

        // Combined balance
        public string Balance { get; private set; }

        #endregion

        #region Actions

        public decimal UpdateAccountsAndBalances()
        {
            // Get list of accounts from database
            IEnumerable<Household.AccountsRow> accounts =  Enumerable.Empty<Household.AccountsRow>();
            switch(type)
            {
                case EType.Banking:
                    accounts = mainWindow.Household.Accounts.GetBankingAccounts();
                    break;
                case EType.Investment:
                    accounts = mainWindow.Household.Accounts.GetInvestmentAccounts();
                    break;
                case EType.Asset:
                    accounts = mainWindow.Household.Accounts.GetAssetAccounts();
                    break;
            }

            int ix = 0;
            decimal totalBalance = 0;
            foreach(var acct in accounts)
            {
                // Skip closed accounts if required
                if (acct.Hidden && mainWindow.UserSettings.HideClosedAccounts)
                {
                    continue;
                }

                decimal balance =
                    type == EType.Investment ?
                    acct.GetInvestmentValue() :
                    acct.GetBankingBalance();

                if (AccountsAndBalances.Count <= ix ||
                    AccountsAndBalances[ix].AccountID != acct.ID ||
                    AccountsAndBalances[ix].AccountName != acct.Name ||
                    AccountsAndBalances[ix].DecimalBalance != balance)
                {
                    var aab = new AccountAndBalance(this, acct.ID, acct.Name, balance);
                    if (ix < AccountsAndBalances.Count)
                    {
                        AccountsAndBalances[ix] = aab;
                    }
                    else
                    {
                        AccountsAndBalances.Add(aab);
                    }
                }

                totalBalance += balance;
                ix++;
            }

            // Truncate
            while(AccountsAndBalances.Count > ix)
            {
                AccountsAndBalances.RemoveAt(ix);
            }

            var totalBalanceStr = totalBalance.ToString("N");
            if (totalBalanceStr != Balance)
            {
                Balance = totalBalanceStr;
            }

            // Force refresh (for winforms)
            OnPropertyChanged(() => Balance);

            return totalBalance;
        }

        //  Called from AccountAndBalance
        public void OnAccountClicked(int accountID)
        {
            AccountClicked?.Invoke(this, new AccountClickedEventArgs(accountID));
        }


        #endregion

        #region support classes

        public class AccountAndBalance
        {
            public AccountAndBalance(AccountGroupLogic accountGroup, int accountID, string accountName, decimal balance)
            {
                AccountID = accountID;
                AccountName = accountName;
                Balance = balance.ToString("N");
                DecimalBalance = balance;
                GoToAccount = new CommandBase(() => accountGroup.OnAccountClicked(accountID));
            }

            // Properties for logic
            public readonly decimal DecimalBalance;
            public readonly int AccountID;

            // UI Properties
            // Account name
            public string AccountName { get; }

            // Account balance
            public string Balance { get; }

            // Command to execute when clicking on the account
            public CommandBase GoToAccount { get; }
        }

        #endregion
    }
}
