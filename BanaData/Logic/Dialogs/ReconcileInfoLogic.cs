using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Logic.Items;

namespace BanaData.Logic.Dialogs
{
    /// <summary>
    /// Logic for the dialog to query reconcilation information
    /// </summary>
    public class ReconcileInfoLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly ReconcileInfoItem reconcileInfoItem;
        private ReconcileInfoItem newReconcileInfoItem;

        #endregion

        #region Constructor

        public ReconcileInfoLogic(MainWindowLogic _mainWindowLogic, ReconcileInfoItem _reconcileInfoItem)
        {
            (mainWindowLogic, reconcileInfoItem) = (_mainWindowLogic, _reconcileInfoItem);

            // Get account info
            var accountRow = mainWindowLogic.Household.Accounts.FindByID(reconcileInfoItem.AccountID);

            BasicInfo = $"Information to reconcile account {accountRow.Name}";

            // Get last statement end date
            // PriorStatementEndDate = accountRow.LastStatementEndDate; ZZZZ TODO

            // Compute last reconciled balance
            PriorStatementBalance = accountRow.GetBankingReconciledBalance();

            // Guess the statement end date ZZZZ TODO
            StatementEndDate = DateTime.Today;

            // Is interest info visible?
            IsInterestInfoVisible = accountRow.Type == Database.EAccountType.Bank;

            // Copy info from reconcile info item
            StatementEndDate = reconcileInfoItem.StatementEndDate;
            StatementBalance = reconcileInfoItem.StatementBalance;
            InterestAmount = reconcileInfoItem.InterestAmount;
            InterestDate = reconcileInfoItem.InterestDate;
            InterestCategory = reconcileInfoItem.InterestCategory;
        }

        #endregion

        #region UI properties

        // Basic info
        public string BasicInfo { get; }

        // Statement dates
        public DateTime PriorStatementEndDate { get; }
        public DateTime StatementEndDate { get; set; }

        // Statement balances
        public decimal PriorStatementBalance { get; }
        public decimal StatementBalance { get; set; }

        // Interest info (for bank accounts)
        public bool IsInterestInfoVisible { get; }

        public decimal InterestAmount { get; set; }
        public DateTime InterestDate { get; set; }
        public string InterestCategory { get; set; }
        public IEnumerable<CategoryItem> Categories => mainWindowLogic.Categories;

        #endregion

        #region Result

        public ReconcileInfoItem NewReconcileInfoItem => newReconcileInfoItem;

        #endregion

        #region Actions

        protected override bool? Commit()
        {
            newReconcileInfoItem = new ReconcileInfoItem(
                reconcileInfoItem.AccountID, StatementEndDate, StatementBalance, InterestAmount, InterestDate, InterestCategory);

            return !newReconcileInfoItem.Equals(reconcileInfoItem);
        }

        #endregion
    }
}
