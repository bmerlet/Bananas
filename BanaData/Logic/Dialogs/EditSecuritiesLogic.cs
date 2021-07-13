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
using BanaData.Collections;

namespace BanaData.Logic.Dialogs
{
    public class EditSecuritiesLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public EditSecuritiesLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            securitySource = new WpfObservableRangeCollection<SecurityItem>();
            securitySource.ReplaceRange(mainWindowLogic.Securities);

            SecuritiesSource = (CollectionView)CollectionViewSource.GetDefaultView(securitySource);
            SecuritiesSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            AddCommand = new CommandBase(OnAddSecurity);
            EditCommand = new CommandBase(OnEditSecurity);
            DeleteCommand = new CommandBase(OnDeleteSecurity);
        }

        #endregion

        #region UI properties

        public SecurityItem SelectedSecurity { get; set; }
        public SecurityItem SecurityToScrollTo { get; private set; }

        private readonly WpfObservableRangeCollection<SecurityItem> securitySource;
        public CollectionView SecuritiesSource { get; }

        public CommandBase AddCommand { get; }
        public CommandBase EditCommand { get; }
        public CommandBase DeleteCommand { get; }

        #endregion

        #region Actions

        private void OnAddSecurity()
        {
            // Create new security
            var security = new SecurityItem(-1, "", "", ESecurityType.Invalid);

            var logic = new EditSecurityLogic(mainWindowLogic, security);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new security
                var newSecurity = logic.NewSecurityItem;

                // Commit change
                newSecurity = AddSecurityToDataSet(newSecurity);

                // Upate main list
                mainWindowLogic.Securities.Add(newSecurity);
                mainWindowLogic.NotifySecurityChange();

                // Update UI
                securitySource.Add(newSecurity);
                SelectedSecurity = newSecurity;
                SecurityToScrollTo = newSecurity;
                OnPropertyChanged(() => SelectedSecurity);
                OnPropertyChanged(() => SecurityToScrollTo);
            }
        }

        private void OnEditSecurity()
        {
            if (SelectedSecurity != null)
            {
                var logic = new EditSecurityLogic(mainWindowLogic, SelectedSecurity);
                if (mainWindowLogic.GuiServices.ShowDialog(logic))
                {
                    // Get modified security
                    var newSecurity = logic.NewSecurityItem;

                    // Commit change
                    UpdateSecurityInDataSet(newSecurity);

                    // Upate main list
                    mainWindowLogic.Securities.Remove(SelectedSecurity);
                    mainWindowLogic.Securities.Add(newSecurity);
                    mainWindowLogic.NotifySecurityChange();

                    // Update UI
                    securitySource.Remove(SelectedSecurity);
                    securitySource.Add(newSecurity);
                    SelectedSecurity = newSecurity;
                    SecurityToScrollTo = newSecurity;
                    OnPropertyChanged(() => SelectedSecurity);
                    OnPropertyChanged(() => SecurityToScrollTo);
                }
            }
        }

        private void OnDeleteSecurity()
        {
            if (SelectedSecurity != null)
            {
                // Delete only if no transactions
                var securityRow = mainWindowLogic.Household.Security.FindByID(SelectedSecurity.ID);
                if (securityRow.HasTransactions)
                {
                    mainWindowLogic.ErrorMessage("This security cannot be deleted because it has transactions associated with it.");
                    return;
                }

                // Commit change
                RemoveSecurityFromDataSet(SelectedSecurity);

                // Upate main list
                mainWindowLogic.Securities.Remove(SelectedSecurity);
                mainWindowLogic.NotifySecurityChange();

                // Update UI
                securitySource.Remove(SelectedSecurity);
            }
        }

        private SecurityItem AddSecurityToDataSet(SecurityItem newSecurity)
        {
            var household = mainWindowLogic.Household;

            // Create and commit new security
            var newSecurityRow = household.Security.Add(newSecurity.Name, newSecurity.Symbol, newSecurity.Type);

            mainWindowLogic.CommitChanges();

            // Note that a new ID is created automatically, so we need to update the security item with it
            return new SecurityItem(newSecurity, newSecurityRow.ID);
        }

        private void UpdateSecurityInDataSet(SecurityItem newSecurity)
        {
            var household = mainWindowLogic.Household;

            // Update the row
            household.Security.Update(newSecurity.ID, newSecurity.Name, newSecurity.Symbol, newSecurity.Type);

            // Commit
            mainWindowLogic.CommitChanges();
        }

        private void RemoveSecurityFromDataSet(SecurityItem security)
        {
            var household = mainWindowLogic.Household;

            // Remove the security
            household.Security.FindByID(security.ID).Delete();

            mainWindowLogic.CommitChanges();
        }

        #endregion
    }
}
