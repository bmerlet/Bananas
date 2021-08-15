using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Logic.Main;
using BanaData.Logic.Items;
using BanaData.Database;
using BanaData.Logic.Dialogs.Editors;

namespace BanaData.Logic.Dialogs.Listers
{
    public class ListAccountsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public ListAccountsLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            accountsSource = new ObservableCollection<AccountItem>();

            foreach (Household.AccountRow acct in mainWindowLogic.Household.Account.Rows)
            {
                // Skip hidden accounts if required
                if (acct.Hidden && mainWindowLogic.UserSettings.HideClosedAccounts)
                {
                    continue;
                }

                accountsSource.Add(AccountItem.CreateFromDB(acct));
            }

            AccountsSource = (CollectionView)CollectionViewSource.GetDefaultView(accountsSource);
            AccountsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            AddCommand = new CommandBase(OnAddAccount);
            EditCommand = new CommandBase(OnEditAccount);
            DeleteCommand = new CommandBase(OnDeleteAccount);
        }

        #endregion

        #region UI properties

        public AccountItem SelectedAccount { get; set; }
        public AccountItem AccountToScrollTo { get; private set; }

        private readonly ObservableCollection<AccountItem> accountsSource;
        public CollectionView AccountsSource { get; }

        public CommandBase AddCommand { get; }
        public CommandBase EditCommand { get; }
        public CommandBase DeleteCommand { get; }

        #endregion

        #region Actions

        private void OnAddAccount()
        {
            // Create new account
            var account = new AccountItem(null, "", "", EAccountType.Bank, 0, EInvestmentKind.Invalid, false, null);

            var logic = new EditAccountLogic(mainWindowLogic, account, true);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new account
                var newAccount = logic.NewAccountItem;

                // Commit change
                newAccount = AddAccountToDataSet(newAccount);

                // Update UI
                accountsSource.Add(newAccount);
                SelectedAccount = newAccount;
                AccountToScrollTo = newAccount;
                OnPropertyChanged(() => SelectedAccount);
                OnPropertyChanged(() => AccountToScrollTo);
            }
        }

        private void OnEditAccount()
        {
            if (SelectedAccount != null)
            {
                var logic = new EditAccountLogic(mainWindowLogic, SelectedAccount, false);
                if (mainWindowLogic.GuiServices.ShowDialog(logic))
                {
                    // Get modified account
                    var newAccount = logic.NewAccountItem;

                    // Commit change
                    UpdateAccountInDataSet(newAccount);

                    // Update UI
                    accountsSource.Remove(SelectedAccount);
                    accountsSource.Add(newAccount);
                    SelectedAccount = newAccount;
                    AccountToScrollTo = newAccount;
                    OnPropertyChanged(() => SelectedAccount);
                    OnPropertyChanged(() => AccountToScrollTo);
                }
            }
        }

        private void OnDeleteAccount()
        {
            if (SelectedAccount != null)
            {
                // Delete only if no transactions
                if (SelectedAccount.AccountRow.HasTransactions)
                {
                    mainWindowLogic.ErrorMessage("This account cannot be deleted because it has transactions associated with it.");
                    return;
                }

                // Commit change
                RemoveAccountFromDataSet(SelectedAccount);

                accountsSource.Remove(SelectedAccount);
            }
        }

        private AccountItem AddAccountToDataSet(AccountItem newAccount)
        {
            var household = mainWindowLogic.Household;

            // Create and commit new account
            var newAccountRow = household.Account.Add(
                newAccount.Name, newAccount.Description, newAccount.Type, newAccount.CreditLimit, newAccount.InvestmentKind, newAccount.Hidden, FindPersonRow(newAccount.Owner));

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateAccountNamesAndBalances(null);
            mainWindowLogic.BuildCategoriesList();

            // Note that a new ID is created automatically, so we need to update the account item with it
            return new AccountItem(newAccount, newAccountRow);
        }

        private void UpdateAccountInDataSet(AccountItem newAccount)
        {
            var household = mainWindowLogic.Household;

            // Update the row
            household.Account.Update(
                newAccount.AccountRow, newAccount.Name, newAccount.Description, newAccount.Type, newAccount.CreditLimit, newAccount.InvestmentKind, newAccount.Hidden, FindPersonRow(newAccount.Owner));

            // Commit
            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateAccountNamesAndBalances(new int[] { newAccount.AccountRow.ID });
            mainWindowLogic.CloseRegisterIfOpen(newAccount.AccountRow.ID);
            mainWindowLogic.BuildCategoriesList();
        }

        private void RemoveAccountFromDataSet(AccountItem account)
        {
            // Remove the account
            int id = account.AccountRow.ID;
            account.AccountRow.Delete();

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateAccountNamesAndBalances(null);
            mainWindowLogic.CloseRegisterIfOpen(id);
            mainWindowLogic.BuildCategoriesList();
        }

        private Household.PersonRow FindPersonRow(string owner)
        {
            Household.PersonRow personRow = null;

            if (owner != null)
            {
                personRow = mainWindowLogic.Household.Person.Rows.Cast<Household.PersonRow>().Where(pr => pr.Name == owner).SingleOrDefault();
            }

            return personRow;
        }

        #endregion
    }
}
