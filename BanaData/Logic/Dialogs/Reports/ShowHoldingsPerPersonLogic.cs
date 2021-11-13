using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowHoldingsPerPersonLogic : LogicBase
    {
        public ShowHoldingsPerPersonLogic(MainWindowLogic mainWindowLogic)
        {
            foreach (Household.PersonRow personRow in mainWindowLogic.Household.Person.Rows)
            {
                personItems.Add(new PersonItem(mainWindowLogic, personRow));
            }

            // For unowned accounts
            var unowned = new PersonItem(mainWindowLogic, null);
            if (unowned.Value != 0)
            {
                personItems.Add(unowned);
            }

            TotalValue = personItems.Sum(pi => pi.Value);

            PersonsSource = (CollectionView)CollectionViewSource.GetDefaultView(personItems);
            PersonsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private readonly ObservableCollection<PersonItem> personItems = new ObservableCollection<PersonItem>();
        public CollectionView PersonsSource { get; }

        public decimal TotalValue { get; private set; }

        public class PersonItem
        {
            public PersonItem(MainWindowLogic mainWindowLogic, Household.PersonRow personRow)
            {
                var household = mainWindowLogic.Household;

                IEnumerable<Household.AccountRow> accounts =
                    personRow == null ?
                    household.Account.Rows.Cast<Household.AccountRow>().Where(a => a.IsPersonIDNull()) :
                    personRow.GetAccountRows();

                foreach (Household.AccountRow accountRow in accounts)
                {
                    decimal value = accountRow.Type == EAccountType.Investment ? accountRow.GetInvestmentValue() : accountRow.GetBalance();
                    if (value != 0 || !mainWindowLogic.UserSettings.HideClosedAccounts || !accountRow.Hidden)
                    {
                        accountItems.Add(new AccountItem(accountRow.Name, value));
                    }
                }

                Name = personRow == null ? "<Unowned>" : personRow.Name;
                Value = accountItems.Sum(ai => ai.Value);

                AccountsSource = (CollectionView)CollectionViewSource.GetDefaultView(accountItems);
                AccountsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }

            public string Name { get; }
            public decimal Value { get; }

            private readonly ObservableCollection<AccountItem> accountItems = new ObservableCollection<AccountItem>();
            public CollectionView AccountsSource { get; }

            // One account
            public class AccountItem
            {
                public AccountItem(string name, decimal value) =>
                    (Name, Value) = (name, value);

                public string Name { get; }
                public decimal Value { get; }
            }

        }
    }
}
