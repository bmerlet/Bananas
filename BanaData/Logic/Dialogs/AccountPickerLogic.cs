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
    public class AccountPickerLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household.AccountRow accountFrom;

        #endregion

        #region Constructor

        public AccountPickerLogic(MainWindowLogic _mainWindowLogic, Household.AccountRow _accountFrom)
        {
            (mainWindowLogic, accountFrom) = (_mainWindowLogic, _accountFrom);

            accountsSource = new ObservableCollection<AccountItem>();

            foreach (Household.AccountRow acct in mainWindowLogic.Household.Account.Rows)
            {
                // Skip hidden accounts if required
                if (acct.Hidden && mainWindowLogic.UserSettings.HideClosedAccounts)
                {
                    continue;
                }

                // Banking to banking and investment to investment
                if (accountFrom != null)
                {
                    if (acct == accountFrom ||
                        (accountFrom.Type == EAccountType.Investment && acct.Type != EAccountType.Investment) ||
                        (accountFrom.Type != EAccountType.Investment && acct.Type == EAccountType.Investment))
                    {
                        continue;
                    }
                }

                accountsSource.Add(AccountItem.CreateFromDB(acct));
            }

            AccountsSource = (CollectionView)CollectionViewSource.GetDefaultView(accountsSource);
            AccountsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        #endregion

        #region UI properties

        public AccountItem SelectedAccount { get; set; }

        private readonly ObservableCollection<AccountItem> accountsSource;
        public CollectionView AccountsSource { get; }


        #endregion

        #region Actions

        public Household.AccountRow PickedAccount { get; private set; }

        protected override bool? Commit()
        {
            if (SelectedAccount == null)
            {
                mainWindowLogic.ErrorMessage("Please select an account.");
                return null;
            }

            PickedAccount = mainWindowLogic.Household.Account.FindByID(SelectedAccount.ID);

            return true;
        }

        #endregion
    }
}
