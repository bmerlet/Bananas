using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Basics
{
    public class AccountListLogic : LogicBase
    {
        #region Private members

        private readonly Household household;
        private bool internalUpdate = false;

        #endregion

        #region Constructor

        public AccountListLogic(Household _household)
        {
            household = _household;

            foreach (Household.AccountRow accountRow in household.Account)
            {
                var accountItem = new AccountPickerItem(accountRow);
                accountItem.PropertyChanged += OnAccountItemPropertyChanged;
                accounts.Add(accountItem);
            }

            // Setup account view
            Accounts = (CollectionView)CollectionViewSource.GetDefaultView(accounts);
            Accounts.SortDescriptions.Add(new SortDescription("AccountItem.Hidden", ListSortDirection.Ascending));
            Accounts.SortDescriptions.Add(new SortDescription("AccountItem.Name", ListSortDirection.Ascending));

            // Setup commands
            ClearAllCommand = new CommandBase(OnClearAllCommand);
            SelectAllCommand = new CommandBase(OnSelectAllCommand);
            SelectInvestmentCommand = new CommandBase(OnSelectInvestmentCommand);
            SelectBankingCommand = new CommandBase(OnSelectBankingCommand);
        }

        private void OnAccountItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected" && !internalUpdate)
            {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Events

        public EventHandler SelectionChanged;

        #endregion

        #region UI properties

        //
        // List of accounts
        //
        private readonly ObservableCollection<AccountPickerItem> accounts = new ObservableCollection<AccountPickerItem>();
        public CollectionView Accounts { get; }

        //
        // Buttons
        //
        public CommandBase ClearAllCommand { get; }
        public CommandBase SelectAllCommand { get; }
        public CommandBase SelectInvestmentCommand { get; }
        public CommandBase SelectBankingCommand { get; }

        #endregion

        #region Actions

        private void OnClearAllCommand()
        {
            internalUpdate = true;

            foreach (var acct in accounts)
            {
                acct.IsSelected = false;
            }

            internalUpdate = false;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectAllCommand()
        {
            internalUpdate = true;

            foreach (var acct in accounts)
            {
                acct.IsSelected = true;
            }

            internalUpdate = false;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectInvestmentCommand()
        {
            internalUpdate = true;

            foreach (var acct in accounts)
            {
                acct.IsSelected = acct.AccountRow.Type == EAccountType.Investment;
            }

            internalUpdate = false;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectBankingCommand()
        {
            internalUpdate = true;

            foreach (var acct in accounts)
            {
                acct.IsSelected = acct.AccountRow.Type == EAccountType.Bank;
            }

            internalUpdate = false;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Services

        public bool IsAccountSelected(Household.AccountRow accountRow)
        {
            return Accounts.Cast<AccountPickerItem>().First(a => a.AccountRow == accountRow).IsSelected == true;
        }

        #endregion

        #region Supporting class

        /// <summary>
        /// An account and whether it is selected or not
        /// </summary>
        public class AccountPickerItem : LogicBase
        {
            // Constructor
            public AccountPickerItem(Household.AccountRow accountRow) =>
                (AccountRow, AccountItem) = (accountRow, AccountItem.CreateFromDB(accountRow));

            // Logic property: Account row
            public readonly Household.AccountRow AccountRow;

            // UI properties: Account item and selection
            public AccountItem AccountItem { get; }

            private bool? isSelected;
            public bool? IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                        OnPropertyChanged(() => IsSelected);
                    }
                }
            }
        }

        #endregion
    }
}
