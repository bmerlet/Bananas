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

namespace BanaData.Logic.Dialogs.Pickers
{
    public class PayeeListPickerLogic : LogicDialogBase
    {
        #region Private memebers

        private readonly IEnumerable<string> oldPickedPayees;

        #endregion

        #region Constructor

        public PayeeListPickerLogic(Household household, IEnumerable<string> pickedPayees)
        {
            oldPickedPayees = pickedPayees;

            foreach (Household.TransactionRow transRow in household.RegularTransactions)
            {
                if (!transRow.IsPayeeNull() && payees.FirstOrDefault(p => p.Payee == transRow.Payee) == null)
                {
                    payees.Add(new PayeePickerItem(transRow.Payee, oldPickedPayees.Contains(transRow.Payee)));
                }
            }

            // Setup payee view
            Payees = (CollectionView)CollectionViewSource.GetDefaultView(payees);
            Payees.SortDescriptions.Add(new SortDescription("Payee", ListSortDirection.Ascending));

            // Setup commands
            ClearAllCommand = new CommandBase(OnClearAllCommand);
            SelectAllCommand = new CommandBase(OnSelectAllCommand);
        }

        #endregion

        #region UI properties

        //
        // List of payees
        //
        private readonly ObservableCollection<PayeePickerItem> payees = new ObservableCollection<PayeePickerItem>();
        public CollectionView Payees { get; }

        //
        // Buttons
        //
        public CommandBase ClearAllCommand { get; }
        public CommandBase SelectAllCommand { get; }

        #endregion

        #region Actions

        // Result
        public IEnumerable<string> PickedPayees;

        protected override bool? Commit()
        {
            var pickedPayees = new List<string>();

            foreach (var payee in payees)
            {
                if (payee.IsSelected == true)
                {
                    pickedPayees.Add(payee.Payee);
                }
            }

            PickedPayees = pickedPayees;

            // Err on the side of caution
            return true;
        }

        private void OnClearAllCommand()
        {
            foreach (var payee in payees)
            {
                payee.IsSelected = false;
            }
        }

        private void OnSelectAllCommand()
        {
            foreach (var payeet in payees)
            {
                payeet.IsSelected = true;
            }
        }

        #endregion

        #region PayeePickerItem class

        public class PayeePickerItem : LogicBase
        {
            public PayeePickerItem(string payee, bool selected) =>
                (Payee, isSelected) = (payee, selected);

            public string Payee { get; }

            private bool? isSelected;
            public bool? IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                        InvokePropertyChanged(nameof(IsSelected));
                    }
                }
            }
        }

        #endregion
    }
}
