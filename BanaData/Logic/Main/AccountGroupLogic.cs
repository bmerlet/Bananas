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
        public enum ESubType
        {
            // Banking
            BankHeader = 0,
            BankAccount = 1,
            CashHeader = 2,
            CashAccount = 3,
            CreditCardHeader = 4,
            CreditCardAccount = 5,

            // Investment
            BrokerageHeader = 0,
            BrokerageAccount = 1,
            RetirementHeader = 2,
            RetirementAccount = 3,

            // Assets
            AssetAccount = 1
        };

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
                    InvokePropertyChanged(nameof(SelectedAccount));
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

                bool hasBank = false;
                bool hasCash = false;
                bool hasCreditCard = false;
                bool hasBrokerage = false;
                bool hasRetirement= false;

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
                    ESubType subType = ESubType.AssetAccount;

                    if (type == EType.Banking)
                    {
                        // Display bank accounts first, then cash accounts, then credit cards
                        switch(account.Type)
                        {
                            case EAccountType.Bank:
                                subType = ESubType.BankAccount;
                                hasBank= true;
                                break;
                            case EAccountType.Cash:
                                subType = ESubType.CashAccount;
                                hasCash= true;
                                break;
                            case EAccountType.CreditCard:
                                subType = ESubType.CreditCardAccount;
                                hasCreditCard= true;
                                break;
                        }
                    }
                    else if (type == EType.Investment)
                    {
                        // Display brokerage accounts then IRAs
                        switch(account.Kind)
                        {
                            case EInvestmentKind.Asset:
                                subType = ESubType.AssetAccount;
                                break;

                            case EInvestmentKind.Brokerage:
                                subType = ESubType.BrokerageAccount;
                                hasBrokerage= true;
                                break;

                            case EInvestmentKind.TraditionalIRA:
                            case EInvestmentKind._401k:
                                subType = ESubType.RetirementAccount;
                                hasRetirement = true;
                                break;
                        }
                    }

                    decimal balance = type == EType.Banking ? account.GetBalance() : account.GetInvestmentValue();

                    accountsAndBalances.Add(new AccountAndBalance(account.ID, "    " + account.Name, subType, balance));

                    totalBalance += balance;
                }

                // Create headers for present categories
                if (hasBank)
                {
                    accountsAndBalances.Add(new AccountAndBalance(-1, "Bank:", ESubType.BankHeader, 0));
                }
                if (hasCash)
                {
                    accountsAndBalances.Add(new AccountAndBalance(-1, "Cash:", ESubType.CashHeader, 0));
                }
                if (hasCreditCard)
                {
                    accountsAndBalances.Add(new AccountAndBalance(-1, "Credit cards:", ESubType.CreditCardHeader, 0));
                }
                if (hasBrokerage)
                {
                    accountsAndBalances.Add(new AccountAndBalance(-1, "Brokerage accounts:", ESubType.BrokerageHeader, 0));
                }
                if (hasRetirement)
                {
                    accountsAndBalances.Add(new AccountAndBalance(-1, "Retirement accounts:", ESubType.RetirementHeader, 0));
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
                        if (accountAndBalance.AccountName.Trim() != account.Name)
                        {
                            accountAndBalance.UpdateName("    " + account.Name);
                        }
                    }
                }

                totalBalance = accountsAndBalances.Sum(aab => aab.Balance);
            }

            if (totalBalance != Balance)
            {
                Balance = totalBalance;
                InvokePropertyChanged(nameof(Balance));
            }

            return totalBalance;
        }

        #endregion

        #region support classes

        public class AccountAndBalance : LogicBase
        {
            public AccountAndBalance(int accountID, string accountName, ESubType _subType, decimal balance) =>
                (AccountID, AccountName, subType, Balance) = (accountID, accountName, _subType, balance);

            // Properties for logic
            public readonly int AccountID;

            // UI Properties

            // Account name
            public string AccountName { get; private set;}

            // Subtype for sorting
            public readonly ESubType subType;
            public int SubType => (int)subType;

            // Color of text
            public string TextColor =>
                (subType == ESubType.BankHeader || subType == ESubType.CashHeader || subType == ESubType.CreditCardHeader ||
                 subType == ESubType.BrokerageHeader || subType == ESubType.RetirementHeader) ? "Black" : "Blue";

            // Show balance
            public bool ShowBalance => !(subType == ESubType.BankHeader || subType == ESubType.CashHeader || subType == ESubType.CreditCardHeader ||
                 subType == ESubType.BrokerageHeader || subType == ESubType.RetirementHeader);

            // Account balance
            public decimal Balance { get; private set; }

            public void UpdateBalance(decimal balance)
            {
                Balance = balance;
                InvokePropertyChanged(nameof(Balance));
            }

            public void UpdateName(string name)
            {
                AccountName = name;
                InvokePropertyChanged(nameof(AccountName));
            }
        }

        #endregion
    }
}
