using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Items;
using BanaData.Logic.Main;

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
            mainWindowLogic.MemorizedPayees.ForEach(mpi => memorizedPayeesSource.Add(mpi));
            MemorizedPayeesSource = (CollectionView)CollectionViewSource.GetDefaultView(memorizedPayeesSource);
            MemorizedPayeesSource.SortDescriptions.Add(new SortDescription("Payee", ListSortDirection.Ascending));

            AddMemorizedPayee = new CommandBase(OnAddMemorizedPayee);
            EditMemorizedPayee = new CommandBase(OnEditMemorizedPayee);
            DeleteMemorizedPayee = new CommandBase(OnDeleteMemorizedPayee);
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
            // Create new memorized payee
            int mpid = memorizedPayeesSource.Max(mpi => mpi.ID) + 1;
            int liid = memorizedPayeesSource.Max(mpi => mpi.LineItems.Max(liiid => liiid.ID)) + 1;
            var newMemorizedPayee = new MemorizedPayeeItem(mpid, "", new LineItem[1] { new LineItem(liid, "", "", 0) });

            var logic = new EditMemorizedPayeeLogic(mainWindowLogic, newMemorizedPayee, true);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new memorized payee
                var newPayee = logic.NewMemorizedPayeeItem;

                // Commit change
                // ZZZZZ

                // Update UI
                memorizedPayeesSource.Add(newPayee);
                SelectedMemorizedPayee = newPayee;
            }
        }

        private void OnEditMemorizedPayee()
        {
            var logic = new EditMemorizedPayeeLogic(mainWindowLogic, SelectedMemorizedPayee, false);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get modified payee
                var newPayee = logic.NewMemorizedPayeeItem;

                // Commit change
                // ZZZZ

                // Update UI
                memorizedPayeesSource.Remove(SelectedMemorizedPayee);
                memorizedPayeesSource.Add(newPayee);
                SelectedMemorizedPayee = newPayee;
            }
        }

        private void OnDeleteMemorizedPayee()
        {
            if (SelectedMemorizedPayee != null)
            {
                // Commit change
                // ZZZZ

                memorizedPayeesSource.Remove(SelectedMemorizedPayee);
                MemorizedPayeesSource.Refresh();
            }
        }

        protected override bool? Commit()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
