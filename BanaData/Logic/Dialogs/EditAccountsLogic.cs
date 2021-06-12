using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Main;
using BanaData.Logic.Items;

namespace BanaData.Logic.Dialogs
{
    public class EditAccountsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public EditAccountsLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            accountsSource = new ObservableCollection<AccountItem>();

            foreach (Household.AccountsRow acct in mainWindowLogic.Household.Accounts.Rows)
            {
                bool investment = acct.Type == EAccountType.Investment;
                decimal balance = investment ? acct.GetInvestmentValue() : acct.GetBankingBalance();

                // Skip closed empty accounts if required
                if (!mainWindowLogic.IsAccountVisible(acct.Name, balance))
                {
                    continue;
                }

                var desc = acct.IsDescriptionNull() ? "" : acct.Description;
                decimal creditLimit = acct.IsCreditLimitNull() ? 0 : acct.CreditLimit;
                EInvestmentKind kind = acct.IsIKindNull() ? EInvestmentKind.Invalid : acct.Kind;

                accountsSource.Add(new AccountItem(acct.ID, acct.Name, desc, acct.Type, creditLimit, kind));
            }

            AccountsSource = (CollectionView)CollectionViewSource.GetDefaultView(accountsSource);
            AccountsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            AddAccount = new CommandBase(OnAddAccount);
            EditAccount = new CommandBase(OnEditAccount);
            DeleteAccount = new CommandBase(OnDeleteAccount);
        }

        #endregion

        #region UI properties

        public AccountItem SelectedAccount { get; set; }
        public AccountItem AccountToScrollTo { get; private set; }

        private readonly ObservableCollection<AccountItem> accountsSource;
        public CollectionView AccountsSource { get; }

        public CommandBase AddAccount { get; }
        public CommandBase EditAccount { get; }
        public CommandBase DeleteAccount { get; }

        #endregion

        #region Actions

        private void OnAddAccount()
        {
            // Create new account
            var account = new AccountItem(-1, "", "", EAccountType.Bank, 0, EInvestmentKind.Invalid);

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

        private void OnDeleteAccount()
        {
            if (SelectedAccount != null)
            {
                // Delete only if no transactions
                var accountRow = mainWindowLogic.Household.Accounts.FindByID(SelectedAccount.ID);
                if (accountRow.HasTransactions)
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

            // Note that a new ID is created automatically, so we need to update
            // the account item with it
            var newAccountRow = household.Accounts.NewRow() as Household.AccountsRow;

            // Create and commit new account
            UpdateAccountRow(newAccount, newAccountRow);
            household.Accounts.Rows.Add(newAccountRow);

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateAll();

            return new AccountItem(newAccountRow.ID, newAccount.Name, newAccount.Description, newAccount.Type, newAccount.CreditLimit, newAccount.InvestmentKind);
        }

        private void UpdateAccountInDataSet(AccountItem newAccount)
        {
            var household = mainWindowLogic.Household;

            // Update the row
            var accountRow = household.Accounts.FindByID(newAccount.ID);
            UpdateAccountRow(newAccount, accountRow);

            // Commit
            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateAll();
        }

        private void UpdateAccountRow(AccountItem newAccount, Household.AccountsRow newAccountRow)
        {
            newAccountRow.Name = newAccount.Name;
            newAccountRow.Type = newAccount.Type;

            if (string.IsNullOrWhiteSpace(newAccount.Description))
            {
                newAccountRow.SetDescriptionNull();
            }
            else
            {
                newAccountRow.Description = newAccount.Description;
            }

            if (newAccount.Type == EAccountType.CreditCard && newAccount.CreditLimit != 0)
            {
                newAccountRow.CreditLimit = newAccount.CreditLimit;
            }
            else
            {
                newAccountRow.SetCreditLimitNull();
            }

            if (newAccount.Type == EAccountType.Investment)
            {
                newAccountRow.Kind = newAccount.InvestmentKind;
            }
            else
            {
                newAccountRow.SetIKindNull();
            }
        }

        private void RemoveAccountFromDataSet(AccountItem account)
        {
            var household = mainWindowLogic.Household;

            // Remove the account
            household.Accounts.FindByID(account.ID).Delete();

            mainWindowLogic.CommitChanges();
            mainWindowLogic.UpdateAll();
        }

        #endregion
    }
}
