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
using BanaData.Logic.Dialogs.Editors;
using BanaData.Logic.Main;
using BanaData.Logic.Items;

namespace BanaData.Logic.Dialogs.Listers
{
    public class ListStatementAccountHintsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;

        #endregion

        #region Constructor

        public ListStatementAccountHintsLogic(MainWindowLogic _mainWindowLogic, Household _household)
        {
            (mainWindowLogic, household) = (_mainWindowLogic, _household);

            statementAccountHintsSource = new ObservableCollection<StatementAccountHintItem>();
            foreach (Household.StatementAccountHintRow hint in household.StatementAccountHint.Rows)
            {
                statementAccountHintsSource.Add(new StatementAccountHintItem(hint));
            }

            StatementAccountHintsSource = (CollectionView)CollectionViewSource.GetDefaultView(statementAccountHintsSource);
            StatementAccountHintsSource.SortDescriptions.Add(new SortDescription("AccountName", ListSortDirection.Ascending));

            AddCommand = new CommandBase(OnAddStatementAccountHint);
            EditCommand = new CommandBase(OnEditStatementAccountHint);
            DeleteCommand = new CommandBase(OnDeleteStatementAccountHint);
        }

        #endregion

        #region UI properties

        public StatementAccountHintItem SelectedStatementAccountHint { get; set; }
        public StatementAccountHintItem StatementAccountHintToScrollTo { get; private set; }

        private readonly ObservableCollection<StatementAccountHintItem> statementAccountHintsSource;
        public CollectionView StatementAccountHintsSource { get; }

        public CommandBase AddCommand { get; }
        public CommandBase EditCommand { get; }
        public CommandBase DeleteCommand { get; }

        #endregion

        #region Actions

        private void OnAddStatementAccountHint()
        {
            // Create new empty statement hint item
            var hint = new StatementAccountHintItem(EInstitution.None, "", 1, 1, new string[] { }, null);

            var logic = new EditStatementAccountHintsLogic(mainWindowLogic, household, hint, true);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new hint
                var newHint = logic.NewStatementAccountHintItem;

                // Commit change
                newHint = AddStatementHintToDataSet(newHint);

                // Update UI
                statementAccountHintsSource.Add(newHint);
                SelectedStatementAccountHint = newHint;
                StatementAccountHintToScrollTo = newHint;
                InvokePropertyChanged(nameof(SelectedStatementAccountHint));
                InvokePropertyChanged(nameof(StatementAccountHintToScrollTo));
            }
        }

        private void OnEditStatementAccountHint()
        {
            if (SelectedStatementAccountHint != null)
            {
                var logic = new EditStatementAccountHintsLogic(mainWindowLogic, household, SelectedStatementAccountHint, false);
                if (mainWindowLogic.GuiServices.ShowDialog(logic))
                {
                    // Get new hint
                    var newHint = logic.NewStatementAccountHintItem;

                    // Commit change
                    UpdateStatementHintInDataSet(newHint);

                    // Update UI
                    statementAccountHintsSource.Remove(SelectedStatementAccountHint);
                    statementAccountHintsSource.Add(newHint);
                    SelectedStatementAccountHint = newHint;
                    StatementAccountHintToScrollTo = newHint;
                    InvokePropertyChanged(nameof(SelectedStatementAccountHint));
                    InvokePropertyChanged(nameof(StatementAccountHintToScrollTo));
                }
            }
        }

        private void OnDeleteStatementAccountHint()
        {
            if (SelectedStatementAccountHint != null)
            {
                // Commit change
                RemoveStatementHintFromDataSet(SelectedStatementAccountHint);

                statementAccountHintsSource.Remove(SelectedStatementAccountHint);
            }
        }

        private StatementAccountHintItem AddStatementHintToDataSet(StatementAccountHintItem newHint)
        {
            // Create new hint
            var accountID = household.Account.Rows.Cast<Household.AccountRow>().Where(a => a.Name == newHint.AccountName).Single().ID;
            var newHintRow = household.StatementAccountHint.Add(newHint.Institution, accountID, newHint.MinPage, newHint.MaxPage);

            // Commit
            mainWindowLogic.CommitChanges(household);

            // Note that a new ID is created automatically, so we need to update the account item with it
            return new StatementAccountHintItem(newHintRow);
        }

        private void UpdateStatementHintInDataSet(StatementAccountHintItem newHint)
        {
            // Update the row
            var accountID = household.Account.Rows.Cast<Household.AccountRow>().Where(a => a.Name == newHint.AccountName).Single().ID;
            household.StatementAccountHint.Update(newHint.StatementAccountHintRow, newHint.Institution, accountID, newHint.MinPage, newHint.MaxPage);

            // Commit
            mainWindowLogic.CommitChanges(household);
        }

        private void RemoveStatementHintFromDataSet(StatementAccountHintItem hint)
        {
            // Remove the hint
            hint.StatementAccountHintRow.Delete();

            // Commit
            mainWindowLogic.CommitChanges(household);
        }

        #endregion
    }
}
