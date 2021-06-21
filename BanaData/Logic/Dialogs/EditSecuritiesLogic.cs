using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Logic.Main;
using BanaData.Logic.Items;
using BanaData.Database;
using System.ComponentModel;

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

            securitySource = new ObservableCollection<SecurityItem>();

            foreach (Household.SecuritiesRow security in mainWindowLogic.Household.Securities.Rows)
            {
                var symbol = security.IsSymbolNull() ? "" : security.Symbol;
                securitySource.Add(new SecurityItem(security.ID, security.Name, symbol, security.Type));
            }

            SecuritiesSource = (CollectionView)CollectionViewSource.GetDefaultView(securitySource);
            SecuritiesSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            AddSecurity = new CommandBase(OnAddSecurity);
            EditSecurity = new CommandBase(OnEditSecurity);
            DeleteSecurity = new CommandBase(OnDeleteSecurity);
        }

        #endregion

        #region UI properties

        public SecurityItem SelectedSecurity { get; set; }
        public SecurityItem SecurityToScrollTo { get; private set; }

        private readonly ObservableCollection<SecurityItem> securitySource;
        public CollectionView SecuritiesSource { get; }

        public CommandBase AddSecurity { get; }
        public CommandBase EditSecurity { get; }
        public CommandBase DeleteSecurity { get; }

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
                var securityRow = mainWindowLogic.Household.Securities.FindByID(SelectedSecurity.ID);
                if (securityRow.HasTransactions)
                {
                    mainWindowLogic.ErrorMessage("This security cannot be deleted because it has transactions associated with it.");
                    return;
                }

                // Commit change
                RemoveSecurityFromDataSet(SelectedSecurity);

                securitySource.Remove(SelectedSecurity);
            }
        }

        private SecurityItem AddSecurityToDataSet(SecurityItem newSecurity)
        {
            var household = mainWindowLogic.Household;

            // Create and commit new security
            var newSecurityRow = household.Securities.Add(newSecurity.Name, newSecurity.Symbol, newSecurity.Type);

            mainWindowLogic.CommitChanges();

            // Note that a new ID is created automatically, so we need to update the security item with it
            return new SecurityItem(newSecurity, newSecurityRow.ID);
        }

        private void UpdateSecurityInDataSet(SecurityItem newSecurity)
        {
            var household = mainWindowLogic.Household;

            // Update the row
            household.Securities.Update(newSecurity.ID, newSecurity.Name, newSecurity.Symbol, newSecurity.Type);

            // Commit
            mainWindowLogic.CommitChanges();
        }

        private void RemoveSecurityFromDataSet(SecurityItem security)
        {
            var household = mainWindowLogic.Household;

            // Remove the security
            household.Securities.FindByID(security.ID).Delete();

            mainWindowLogic.CommitChanges();
        }

        #endregion
    }
}
