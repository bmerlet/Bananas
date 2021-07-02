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
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs
{
    public class AccountPickerLogic : LogicDialogBase
    {
        #region Private memebers

        private readonly MainWindowLogic mainWindowLogic;
        private readonly IEnumerable<Household.AccountsRow> oldPickedAccounts;

        #endregion

        #region Constructor

        public AccountPickerLogic(MainWindowLogic _mainWindowLogic, IEnumerable<Household.AccountsRow> pickedAccounts)
        {
            (mainWindowLogic, oldPickedAccounts) = (_mainWindowLogic, pickedAccounts);

            foreach (Household.AccountsRow accountRow in mainWindowLogic.Household.Accounts)
            {
                accounts.Add(new AccountPickerItem(accountRow, oldPickedAccounts.Contains(accountRow)));
            }

            // Setup account view
            Accounts = (CollectionView)CollectionViewSource.GetDefaultView(accounts);
            Accounts.SortDescriptions.Add(new SortDescription("AccountItem.Name", ListSortDirection.Ascending));
        }

        #endregion

        #region UI properties

        //
        // List of accounts
        private readonly ObservableCollection<AccountPickerItem> accounts = new ObservableCollection<AccountPickerItem>();
        public CollectionView Accounts;

        #endregion

        #region Actions

        // Result
        public IEnumerable<Household.AccountsRow> PickedAccounts;

        protected override bool? Commit()
        {
            var pickedAccounts = new List<Household.AccountsRow>();
            
            foreach(var acct in accounts)
            {
                if (acct.IsSelected == true)
                {
                    pickedAccounts.Add(acct.AccountRow);
                }
            }

            // Err on the side of caution
            return true;
        }

        #endregion

        #region Supporting class

        public class AccountPickerItem
        {
            public AccountPickerItem(Household.AccountsRow accountRow, bool selected) =>
                (AccountRow, AccountItem, IsSelected) = (accountRow, AccountItem.CreateFromDB(accountRow), selected);

            public readonly Household.AccountsRow AccountRow;

            public AccountItem AccountItem { get; }

            public bool? IsSelected { get; set; } 
        }

        #endregion
    }
}
