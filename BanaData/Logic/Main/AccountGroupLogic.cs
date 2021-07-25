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

        // Selected account
        private AccountAndBalance selectedAccount;
        public AccountAndBalance SelectedAccount 
        { 
            get => selectedAccount;
            set
            {
                if (selectedAccount != value)
                {
                    selectedAccount = value;
                    if (value != null)
                    {
                        AccountClicked?.Invoke(this, new AccountClickedEventArgs(selectedAccount.AccountID));
                    }
                    OnPropertyChanged(() => SelectedAccount);
                }
            }
        }

        // Combined balance
        public decimal Balance { get; private set; }

        #endregion

        #region Actions

        public decimal UpdateAccountsAndBalances(IEnumerable<int> accountIDs)
        {
            var household = mainWindow.Household;
            decimal totalBalance = 0;

            if (accountIDs == null)
            {
                // Recreate the list from scratch
                AccountsAndBalances.Clear();
                Household.AccountRow[] accounts = null;

                switch (type)
                {
                    case EType.Banking:
                        accounts = household.Account.GetBankingAccounts();
                        break;
                    case EType.Investment:
                        accounts = household.Account.GetInvestmentAccounts();
                        break;
                    case EType.Asset:
                        accounts = household.Account.GetAssetAccounts();
                        break;
                }

                foreach (var account in accounts)
                {
                    // Skip closed accounts if required
                    if (account.Hidden && mainWindow.UserSettings.HideClosedAccounts)
                    {
                        continue;
                    }

                    decimal balance = type == EType.Investment ? account.GetInvestmentValue() : account.GetBalance();

                    AccountsAndBalances.Add(new AccountAndBalance(account.ID, account.Name, balance));

                    totalBalance += balance;
                }
            }
            else
            {
                // Update balance for given IDs
                foreach (var id in accountIDs)
                {
                    var accountAndBalance = AccountsAndBalances.FirstOrDefault(aab => aab.AccountID == id);
                    if (accountAndBalance != null)
                    {
                        var account = household.Account.FindByID(id);
                        decimal balance = type == EType.Investment ? account.GetInvestmentValue() : account.GetBalance();
                        if (accountAndBalance.Balance != balance)
                        {
                            accountAndBalance.UpdateBalance(balance);
                        }
                    }
                }

                totalBalance = AccountsAndBalances.Sum(aab => aab.Balance);
            }

            if (totalBalance != Balance)
            {
                Balance = totalBalance;
                OnPropertyChanged(() => Balance);
            }

            return totalBalance;
        }

        #endregion

        #region support classes

        public class AccountAndBalance : LogicBase
        {
            public AccountAndBalance(int accountID, string accountName, decimal balance) =>
                (AccountID, AccountName, Balance) = (accountID, accountName, balance);

            // Properties for logic
            public readonly int AccountID;

            // UI Properties

            // Account name
            public string AccountName { get; }

            // Account balance
            public decimal Balance { get; private set; }

            public void UpdateBalance(decimal balance)
            {
                Balance = balance;
                OnPropertyChanged(() => Balance);
            }
        }

        #endregion
    }
}
