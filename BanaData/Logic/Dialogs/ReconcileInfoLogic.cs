using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic.Dialogs;
using BanaData.Database;
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
        private readonly int accountID;
        private Household.ReconcileInfoRow reconcileInfoRow;

        #endregion

        #region Constructor

        public ReconcileInfoLogic(MainWindowLogic _mainWindowLogic, int _accountID)
        {
            (mainWindowLogic, accountID) = (_mainWindowLogic, _accountID);

            // Get DB
            var household = mainWindowLogic.Household;

            // Get account info
            var accountRow = household.Accounts.FindByID(accountID);

            // Fill out properties with account info
            BasicInfo = $"Information to reconcile account {accountRow.Name}";

            // Get last statement end date
            PriorStatementEndDate = accountRow.IsLastStatementDateNull() ? new DateTime(2021, 01, 01) : accountRow.LastStatementDate;

            // Compute last reconciled balance
            PriorStatementBalance = accountRow.GetReconciledBalance();

            // Guess the statement end date
            StatementEndDate = PriorStatementEndDate.AddMonths(1);

            // Is interest info visible?
            IsInterestInfoVisible = accountRow.Type == Database.EAccountType.Bank;

            // Default interest
            InterestAmount = 0;
            InterestDate = StatementEndDate;
            InterestCategory = "Interest Inc";

            // Find if there is a reconcile info available for this account
            var reconcileInfos = accountRow.GetReconcileInfoRows();
            if (reconcileInfos.Length > 1)
            {
                throw new ArgumentOutOfRangeException($"Multiple ReconcileInfo rows for account {accountRow.Name}");
            }

            if (reconcileInfos.Length == 1)
            {
                reconcileInfoRow = reconcileInfos[0];

                // Copy info from reconcile info item
                StatementEndDate = reconcileInfoRow.StatementDate;
                StatementBalance = reconcileInfoRow.StatementBalance;
                InterestAmount = reconcileInfoRow.IsInterestAmountNull() ? InterestAmount : reconcileInfoRow.InterestAmount;
                InterestDate = reconcileInfoRow.IsInterestDateNull() ? InterestDate : reconcileInfoRow.InterestDate;
                if (!reconcileInfoRow.IsInterestCategoryIDNull())
                {
                    InterestCategory = household.Categories.FindByID(reconcileInfoRow.InterestCategoryID).FullName;
                }
            }

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

        #region Actions

        protected override bool? Commit()
        {
            var household = mainWindowLogic.Household;
            bool adding = reconcileInfoRow == null;

            int categoryID = -1;
            if (IsInterestInfoVisible)
            {
                var category = mainWindowLogic.Categories.FirstOrDefault(c => c.FullName == InterestCategory);
                if (category == null)
                {
                    mainWindowLogic.ErrorMessage($"Invalid category: {InterestCategory}");
                    return null;
                }
                categoryID = category.ID;
            }

            if (adding)
            {
                reconcileInfoRow = household.ReconcileInfo.NewReconcileInfoRow();
                reconcileInfoRow.AccountID = accountID;
            }

            reconcileInfoRow.StatementDate = StatementEndDate;
            reconcileInfoRow.StatementBalance = StatementBalance;

            if (IsInterestInfoVisible)
            {
                reconcileInfoRow.InterestAmount = InterestAmount;
                reconcileInfoRow.InterestDate = InterestDate;
                reconcileInfoRow.InterestCategoryID = categoryID;
            }
            else
            {
                reconcileInfoRow.SetInterestAmountNull();
                reconcileInfoRow.SetInterestDateNull();
                reconcileInfoRow.SetInterestCategoryIDNull();
            }

            if (adding)
            {
                household.ReconcileInfo.Rows.Add(reconcileInfoRow);
            }

            mainWindowLogic.CommitChanges();

            return true;
        }

        #endregion
    }
}
