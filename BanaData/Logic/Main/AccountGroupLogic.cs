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

        private MainWindowLogic mainWindow;
        private EType type;

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
                decimal balance =
                    type == EType.Investment ?
                    acct.GetInvestmentValue() :
                    acct.GetBankingBalance();

                if (AccountsAndBalances.Count <= ix ||
                    AccountsAndBalances[ix].AccountID != acct.ID ||
                    AccountsAndBalances[ix].AccountName != acct.Name ||
                    AccountsAndBalances[ix].DecimalBalance != balance)
                {
                    var aab = new AccountAndBalance(acct.ID, acct.Name, balance);
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
                AccountsAndBalances.RemoveAt(ix - 1);
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


        #endregion

        #region support classes

        public class AccountAndBalance
        {
            public AccountAndBalance(int accountID, string accountName, decimal balance)
            {
                AccountID = accountID;
                AccountName = accountName;
                Balance = balance.ToString("N");
                DecimalBalance = balance;
            }

            public int AccountID { get; }
            public string AccountName { get; }
            public string Balance { get; }

            public readonly decimal DecimalBalance; 
        }

        #endregion
    }
}
