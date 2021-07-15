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
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs
{
    public class AccountListPickerLogic : LogicDialogBase
    {
        #region Private memebers

        private readonly MainWindowLogic mainWindowLogic;
        private readonly IEnumerable<Household.AccountRow> oldPickedAccounts;

        #endregion

        #region Constructor

        public AccountListPickerLogic(MainWindowLogic _mainWindowLogic, IEnumerable<Household.AccountRow> pickedAccounts)
        {
            (mainWindowLogic, oldPickedAccounts) = (_mainWindowLogic, pickedAccounts);

            foreach (Household.AccountRow accountRow in mainWindowLogic.Household.Account)
            {
                accounts.Add(new AccountPickerItem(accountRow, oldPickedAccounts.Contains(accountRow)));
            }

            // Setup account view
            Accounts = (CollectionView)CollectionViewSource.GetDefaultView(accounts);
            Accounts.SortDescriptions.Add(new SortDescription("AccountItem.Name", ListSortDirection.Ascending));

            // Setup commands
            ClearAllCommand = new CommandBase(OnClearAllCommand);
            SelectAllCommand = new CommandBase(OnSelectAllCommand);
            SelectInvestmentCommand = new CommandBase(OnSelectInvestmentCommand);
            SelectBankingCommand = new CommandBase(OnSelectBankingCommand);
    }

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

        // Result
        public IEnumerable<Household.AccountRow> PickedAccounts;

        protected override bool? Commit()
        {
            var pickedAccounts = new List<Household.AccountRow>();
            
            foreach(var acct in accounts)
            {
                if (acct.IsSelected == true)
                {
                    pickedAccounts.Add(acct.AccountRow);
                }
            }

            PickedAccounts = pickedAccounts;

            // Err on the side of caution
            return true;
        }

        private void OnClearAllCommand()
        {
            foreach(var acct in accounts)
            {
                acct.IsSelected = false;
            }
        }

        private void OnSelectAllCommand()
        {
            foreach (var acct in accounts)
            {
                acct.IsSelected = true;
            }
        }

        private void OnSelectInvestmentCommand()
        {
            foreach (var acct in accounts)
            {
                acct.IsSelected = acct.AccountRow.Type == EAccountType.Investment;
            }
        }

        private void OnSelectBankingCommand()
        {
            foreach (var acct in accounts)
            {
                acct.IsSelected = acct.AccountRow.Type == EAccountType.Bank;
            }
        }

        #endregion

        #region Supporting class

        public class AccountPickerItem : LogicBase
        {
            public AccountPickerItem(Household.AccountRow accountRow, bool selected) =>
                (AccountRow, AccountItem, isSelected) = (accountRow, AccountItem.CreateFromDB(accountRow), selected);

            public readonly Household.AccountRow AccountRow;

            public AccountItem AccountItem { get; }

            private bool ?isSelected;
            public bool? IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                    }
                    OnPropertyChanged(() => IsSelected);
                }
            }
        }

        #endregion
    }
}
