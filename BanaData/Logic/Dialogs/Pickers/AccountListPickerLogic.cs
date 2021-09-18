using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using BanaData.Database;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Pickers
{
    public class AccountListPickerLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly IEnumerable<Household.AccountRow> oldPickedAccounts;

        #endregion

        #region Constructor

        public AccountListPickerLogic(MainWindowLogic _mainWindowLogic, IEnumerable<Household.AccountRow> pickedAccounts)
        {
            (mainWindowLogic, oldPickedAccounts) = (_mainWindowLogic, pickedAccounts);

            // Create account list
            AccountListLogic = new AccountListLogic(mainWindowLogic);

            // Select passed in accounts
            foreach (AccountListLogic.AccountPickerItem accountItem in AccountListLogic.Accounts)
            {
                accountItem.IsSelected = oldPickedAccounts.Contains(accountItem.AccountRow);
            }
        }

        #endregion

        #region UI properties

        //
        // List of accounts with checkboxes
        //
        public AccountListLogic AccountListLogic { get; }

        #endregion

        #region Actions

        // Result
        public IEnumerable<Household.AccountRow> PickedAccounts;

        protected override bool? Commit()
        {
            var pickedAccounts = new List<Household.AccountRow>();

            foreach (AccountListLogic.AccountPickerItem accountItem in AccountListLogic.Accounts)
            {
                if (accountItem.IsSelected == true)
                {
                    pickedAccounts.Add(accountItem.AccountRow);
                }
            }

            PickedAccounts = pickedAccounts;

            // Err on the side of caution
            return true;
        }

        #endregion
    }
}
