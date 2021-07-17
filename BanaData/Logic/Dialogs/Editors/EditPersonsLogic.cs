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
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Editors
{
    public class EditPersonsLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public EditPersonsLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Add all the persons
            foreach (Household.PersonRow personRow in mainWindowLogic.Household.Person.Rows)
            {
                var item = new PersonItem(personRow.ID, personRow.Name);
                item.NameChanged += OnNameChanged;
                persons.Add(item);
            }

            // Add an empty one at the end
            var emptyItem = new PersonItem(-1, "");
            emptyItem.NameChanged += OnNameChanged;
            persons.Add(emptyItem);

            PersonsSource = (CollectionView)CollectionViewSource.GetDefaultView(persons);
            PersonsSource.SortDescriptions.Add(new SortDescription("Grouper", ListSortDirection.Ascending));
            PersonsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        #endregion

        #region UI properties

        private readonly ObservableCollection<PersonItem> persons = new ObservableCollection<PersonItem>();
        public CollectionView PersonsSource { get; }

        #endregion

        #region Actions

        private void OnNameChanged(object sender, EventArgs e)
        {
            if (sender is PersonItem item)
            {
                // Verify no duplicate name
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    foreach (var person in persons)
                    {
                        if (person != item && person.Name == item.Name)
                        {
                            mainWindowLogic.ErrorMessage("A household member with this name already exists");
                        }
                    }
                }

                // If the user populated an empty name, create another one
                if (persons.All(p => !string.IsNullOrWhiteSpace(p.Name)))
                {
                    var emptyItem = new PersonItem(-1, "");
                    emptyItem.NameChanged += OnNameChanged;
                    persons.Add(emptyItem);
                    PersonsSource.Refresh();
                }
            }
        }

        protected override bool? Commit()
        {
            bool change = false;
            var household = mainWindowLogic.Household;

            foreach (var item1 in persons.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
            {
                foreach (var item2 in persons.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
                {
                    if (item1 != item2 && item1.Name == item2.Name)
                    {
                        mainWindowLogic.ErrorMessage("Duplicate name: " + item1.Name);
                        return null;
                    }
                }
            }

            foreach (var item in persons.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
            {
                if (item.ID < 0)
                {
                    var row = household.Person.NewPersonRow();
                    row.Name = item.Name;
                    household.Person.AddPersonRow(row);
                    change = true;
                }
                else
                {
                    var row = household.Person.FindByID(item.ID);
                    if (row.Name != item.Name)
                    {
                        row.Name = item.Name;
                        change = true;
                    }
                }
            }

            if (change)
            {
                mainWindowLogic.CommitChanges();
            }

            return change;
        }

        #endregion

        #region PersonItem class

        public class PersonItem : LogicBase
        {
            public PersonItem(int id, string _name) => (ID, name) = (id, _name);

            public event EventHandler NameChanged;

            public readonly int ID;

            private string name;
            public string Name
            {
                get => name;
                set { name = value; NameChanged?.Invoke(this, EventArgs.Empty); }
            }

            public string Grouper => (ID < 0 && string.IsNullOrWhiteSpace(name)) ? "Z" : "A";
        }

        #endregion
    }
}
