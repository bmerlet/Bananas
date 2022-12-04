using BanaData.Database;
using BanaData.Serializations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Toolbox.UILogic;

namespace BanaData.Logic.Main
{
    public class AccountGroupLogic : LogicBase
    {
        public enum EType { Banking, Investment, Asset };

        #region Private members

        private readonly Household household;
        private readonly UserSettings userSettings;
        private readonly EType type;

        #endregion

        #region Constructor

        public AccountGroupLogic(Household _household, UserSettings _userSettings, EType _type)
        {
            household = _household;
            userSettings = _userSettings;
            type = _type;

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
                    Header = "Assets:";
                    Footer = "Assets total:";
                    break;
            }

            AccountsAndBalances = (CollectionView)CollectionViewSource.GetDefaultView(accountsAndBalances);
            AccountsAndBalances.SortDescriptions.Add(new SortDescription("SubType", ListSortDirection.Ascending));
            AccountsAndBalances.SortDescriptions.Add(new SortDescription("AccountName", ListSortDirection.Ascending));
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
        private ObservableCollection<AccountAndBalance> accountsAndBalances { get; } = new ObservableCollection<AccountAndBalance>();
        public CollectionView AccountsAndBalances { get; }

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
            decimal totalBalance = 0;

            if (accountIDs == null)
            {
                // Recreate the list from scratch
                accountsAndBalances.Clear();
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
                    if (account.Hidden && userSettings.HideClosedAccounts)
                    {
                        continue;
                    }

                    // To group bank accounts, cash and credit cards together
                    int subType = 0;
                    if (type == EType.Banking)
                    {
                        // Display bank accounts first, then cash accounts, then credit cards
                        switch(account.Type)
                        {
                            case EAccountType.Bank:
                                subType = 1;
                                break;
                            case EAccountType.Cash:
                                subType = 2;
                                break;
                            case EAccountType.CreditCard:
                                subType = 3;
                                break;
                        }
                    }
                    else if (type == EType.Investment)
                    {
                        // Display brokerage accounts then IRAs
                        subType = account.Kind == EInvestmentKind.TraditionalIRA ? 2 : 1;
                    }

                    decimal balance = type == EType.Banking ? account.GetBalance() : account.GetInvestmentValue();

                    accountsAndBalances.Add(new AccountAndBalance(account.ID, account.Name, subType, balance));

                    totalBalance += balance;
                }
            }
            else
            {
                // Update balance for given IDs
                foreach (var id in accountIDs)
                {
                    var accountAndBalance = accountsAndBalances.FirstOrDefault(aab => aab.AccountID == id);
                    if (accountAndBalance != null)
                    {
                        var account = household.Account.FindByID(id);
                        decimal balance = type == EType.Banking ? account.GetBalance() : account.GetInvestmentValue();
                        if (accountAndBalance.Balance != balance)
                        {
                            accountAndBalance.UpdateBalance(balance);
                        }
                        if (accountAndBalance.AccountName != account.Name)
                        {
                            accountAndBalance.UpdateName(account.Name);
                        }
                    }
                }

                totalBalance = accountsAndBalances.Sum(aab => aab.Balance);
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
            public AccountAndBalance(int accountID, string accountName, int subType, decimal balance) =>
                (AccountID, AccountName, SubType, Balance) = (accountID, accountName, subType, balance);

            // Properties for logic
            public readonly int AccountID;

            // UI Properties

            // Account name
            public string AccountName { get; private set;}

            // Subtype for sorting
            public int SubType { get; }

            // Account balance
            public decimal Balance { get; private set; }

            public void UpdateBalance(decimal balance)
            {
                Balance = balance;
                OnPropertyChanged(() => Balance);
            }

            public void UpdateName(string name)
            {
                AccountName = name;
                OnPropertyChanged(() => AccountName);
            }
        }

        #endregion
    }
}
