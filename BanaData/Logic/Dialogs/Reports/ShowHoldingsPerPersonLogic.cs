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
using BanaData.Serializations;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowHoldingsPerPersonLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;

        #endregion

        #region Constructor

        public ShowHoldingsPerPersonLogic(MainWindowLogic _mainWindowLogic, Household _household)
        {
            (mainWindowLogic, household) = (_mainWindowLogic, _household);
            Date = DateTime.Today;

            PersonsSource = (CollectionView)CollectionViewSource.GetDefaultView(personItems);
            PersonsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        #endregion

        #region UI properties

        // Date to compute the holdings at
        private DateTime date;
        public DateTime Date { get => date; set { date = value; ComputeHoldings(); } }

        // The list of persons
        private readonly ObservableCollection<PersonItem> personItems = new ObservableCollection<PersonItem>();
        public CollectionView PersonsSource { get; }

        // Total value;
        public decimal TotalValue { get; private set; }

        #endregion

        #region Actions

        private void ComputeHoldings()
        {
            personItems.Clear();

            foreach (Household.PersonRow personRow in household.Person.Rows)
            {
                personItems.Add(new PersonItem(household, mainWindowLogic.UserSettings, personRow, date));
            }

            // For unowned accounts
            var unowned = new PersonItem(household, mainWindowLogic.UserSettings, null, date);
            if (unowned.Value != 0)
            {
                personItems.Add(unowned);
            }

            TotalValue = personItems.Sum(pi => pi.Value);
            OnPropertyChanged(() => TotalValue);
        }

        #endregion

        #region Person class

        public class PersonItem
        {
            // Constructor
            public PersonItem(Household household, UserSettings userSettings, Household.PersonRow personRow, DateTime date)
            {
                IEnumerable<Household.AccountRow> accounts =
                    personRow == null ?
                    household.Account.Rows.Cast<Household.AccountRow>().Where(a => a.IsPersonIDNull()) :
                    personRow.GetAccountRows();

                decimal value;
                Portfolio portfolio = null;

                foreach (Household.AccountRow accountRow in accounts)
                {
                    if (accountRow.Type == EAccountType.Investment)
                    {
                        portfolio = accountRow.GetPortfolio(date);
                        value = portfolio.GetValuation(date);
                    }
                    else
                    {
                        value = accountRow.GetBalance(date);
                    }

                    if (value != 0 || !userSettings.HideClosedAccounts || !accountRow.Hidden)
                    {
                        accountItems.Add(new AccountItem(accountRow.Name, value, portfolio, date));
                    }
                }

                Name = personRow == null ? "<Unowned>" : personRow.Name;
                Value = accountItems.Sum(ai => ai.Value);

                AccountsSource = (CollectionView)CollectionViewSource.GetDefaultView(accountItems);
                AccountsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }

            // UI properties
            public string Name { get; }
            public decimal Value { get; }

            private readonly ObservableCollection<AccountItem> accountItems = new ObservableCollection<AccountItem>();
            public CollectionView AccountsSource { get; }

            // One account
            public class AccountItem
            {
                public AccountItem(string name, decimal value) =>
                    (Name, Value) = (name, value);

                public AccountItem(string name, decimal value, Portfolio portfolio, DateTime date)
                {
                    (Name, Value) = (name, value);

                    SecuritiesSource = (CollectionView)CollectionViewSource.GetDefaultView(securities);
                    SecuritiesSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    if (portfolio != null)
                    {
                        foreach(var security in portfolio.GetSecuritiesRows())
                        {
                            var quantity = portfolio.Lots.Where(l => l.Security == security).Sum(l => l.Quantity);
                            securities.Add(new SecurityItem(security.Symbol, quantity * security.GetMostRecentPrice(date)));
                        }
                    }
                }

                public string Name { get; }
                public decimal Value { get; }

                private readonly ObservableCollection<SecurityItem> securities = new ObservableCollection<SecurityItem>();
                public CollectionView SecuritiesSource { get; }

                // One security
                public class SecurityItem
                {
                    public SecurityItem(string name, decimal value) =>
                        (Name, Value) = (name, value);

                    public string Name { get; }
                    public decimal Value { get; }
                }
            }

        }

        #endregion
    }
}
