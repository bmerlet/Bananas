using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs
{
    /// <summary>
    /// Logic to edit the list of memorized payees
    /// </summary>
    public class EditMemorizedPayeesLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public EditMemorizedPayeesLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            memorizedPayeesSource = new ObservableCollection<MemorizedPayeeItem>();
            MemorizedPayeesSource = (CollectionView)CollectionViewSource.GetDefaultView(memorizedPayeesSource);
            MemorizedPayeesSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            AddMemorizedPayee = new CommandBase(OnAddMemorizedPayee);
            EditMemorizedPayee = new CommandBase(OnEditMemorizedPayee);
            DeleteMemorizedPayee = new CommandBase(OnDeleteMemorizedPayee);

            BuildMemorizedPayeesList();
        }

        #endregion

        #region UI properties

        public MemorizedPayeeItem SelectedMemorizedPayee { get; set; }

        private readonly ObservableCollection<MemorizedPayeeItem> memorizedPayeesSource;
        public CollectionView MemorizedPayeesSource { get; }

        public CommandBase AddMemorizedPayee { get; }
        public CommandBase EditMemorizedPayee { get; }
        public CommandBase DeleteMemorizedPayee { get; }

        #endregion

        #region Actions

        private void OnAddMemorizedPayee()
        {
            throw new NotImplementedException();
        }

        private void OnEditMemorizedPayee()
        {
            throw new NotImplementedException();
        }

        private void OnDeleteMemorizedPayee()
        {
            if (SelectedMemorizedPayee != null)
            {
                memorizedPayeesSource.Remove(SelectedMemorizedPayee);
                MemorizedPayeesSource.Refresh();
            }
        }

        private void BuildMemorizedPayeesList()
        {
            var household = mainWindowLogic.Household;

            memorizedPayeesSource.Clear();

            foreach (var mpr in household.MemorizedPayees)
            {
                // Get memorized line item(s)
                var lineItems = household.MemorizedLineItems.GetByMemorizedPayee(mpr);
                decimal amount = lineItems.Sum(li => li.Amount);

                string category = "";

                if (lineItems.Length > 1)
                {
                    category = "<Split>";
                }
                else if (!lineItems[0].IsCategoryIDNull())
                {
                    var destCategory = household.Categories.FindByID(lineItems[0].CategoryID);
                    category = destCategory.FullName;
                }

                var mpi = new MemorizedPayeeItem(mpr.ID, mpr.Payee, amount, category);
                memorizedPayeesSource.Add(mpi);
            }
        }

        protected override bool? Commit()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Item class

        public class MemorizedPayeeItem : IComparable<MemorizedPayeeItem>
        {
            public MemorizedPayeeItem(int id, string name, decimal amount, string category) => (ID, Name, Amount, Category) = (id, name, amount.ToString("N"), category);

            public readonly int ID;

            public string Name { get; }
            public string Amount { get; }
            public string Category { get; }

            public override bool Equals(object obj)
            {
                return obj is MemorizedPayeeItem o && o.ID == ID;
            }

            public override int GetHashCode()
            {
                return ID.GetHashCode();
            }

            public int CompareTo(MemorizedPayeeItem other)
            {
                return Name.CompareTo(other.Name);
            }
        }

        #endregion
    }
}
