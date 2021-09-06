using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic.Dialogs;
using BanaData.Database;
using BanaData.Logic.Main;
using BanaData.Logic.Items;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.ComponentModel;

namespace BanaData.Logic.Dialogs.Editors
{
    /// <summary>
    /// Logic for the dialog to query reconcilation information
    /// </summary>
    public class ReconcileInfoLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household.AccountRow accountRow;
        private Household.ReconcileInfoRow reconcileInfoRow;

        #endregion

        #region Constructor

        public ReconcileInfoLogic(MainWindowLogic _mainWindowLogic, int _accountID)
        {
            (mainWindowLogic, accountRow) = (_mainWindowLogic, _mainWindowLogic.Household.Account.FindByID(_accountID));

            // Get DB
            var household = mainWindowLogic.Household;

            // Fill out properties with account info
            BasicInfo = $"Information to reconcile account {accountRow.Name}";

            // Get prior statement end date
            PriorStatementEndDate = accountRow.IsLastStatementDateNull() ? new DateTime(2021, 01, 01) : accountRow.LastStatementDate;

            // Compute last reconciled balance
            PriorStatementBalance = accountRow.GetReconciledBalance();

            // Guess the statement end date
            statementEndDate = PriorStatementEndDate.AddMonths(1);

            // Is interest info visible?
            IsInterestInfoVisible = accountRow.Type == EAccountType.Bank;

            // Default interest
            InterestAmount = 0;
            InterestDate = StatementEndDate;
            InterestCategory = "Interest Inc";

            // Are security info visible?
            IsSecurityInfoVisible = accountRow.Type == EAccountType.Investment;

            // Setup view of security
            SecurityInfoSource = (CollectionView)CollectionViewSource.GetDefaultView(securityInfos);
            SecurityInfoSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            // Guess the securities available in this account by applying all the reconciled transactions
            // and also looking at the portfolio at the last statement date
            GuessSecurities(StatementEndDate);

            // Find if there is a reconcile info available for this account
            var reconcileInfos = accountRow.GetReconcileInfoRows();
            if (reconcileInfos.Length > 1)
            {
                throw new ArgumentOutOfRangeException($"Multiple ReconcileInfo rows for account {accountRow.Name}");
            }

            if (reconcileInfos.Length == 1)
            {
                reconcileInfoRow = reconcileInfos[0];

                // Copy basic info from reconcile info item
                StatementEndDate = reconcileInfoRow.StatementDate;
                StatementBalance = reconcileInfoRow.StatementBalance;

                // Copy interest info if available
                if (accountRow.Type == EAccountType.Bank)
                {
                    InterestAmount = reconcileInfoRow.InterestAmount;
                    InterestDate = reconcileInfoRow.InterestDate;
                    InterestCategory = household.Category.FindByID(reconcileInfoRow.InterestCategoryID).FullName;
                }

                // Copy security info if available
                if (accountRow.Type == EAccountType.Investment)
                {
                    foreach (var sri in reconcileInfoRow.GetSecurityReconcileInfoRows())
                    {
                        var securityInfoItem = securityInfos.FirstOrDefault(sii => sii.Symbol == sri.SecurityRow.Symbol);
                        if (securityInfoItem != null)
                        {
                            securityInfoItem.Quantity = sri.SecurityQuantity;
                        }
                        else
                        {
                            securityInfos.Add(new SecurityInfoItem(sri.SecurityRow, sri.SecurityQuantity));
                        }
                    }

                    // Adjust tab index in collection order
                    int tabIndex = 2;
                    foreach (SecurityInfoItem sii in SecurityInfoSource)
                    {
                        sii.TabIndex = tabIndex++;
                    }
                }
            }
        }

        #endregion

        #region UI properties

        //
        // Basic info
        //
        public string BasicInfo { get; }

        //
        // Statement dates
        //
        public DateTime PriorStatementEndDate { get; set; }

        private DateTime statementEndDate;
        public DateTime StatementEndDate
        {
            get => statementEndDate;
            set
            {
                if (statementEndDate != value)
                {
                    statementEndDate = value;
                    GuessSecurities(statementEndDate);
                }
            }
        }

        //
        // Statement balances
        //
        public decimal PriorStatementBalance { get; }
        public decimal StatementBalance { get; set; }

        //
        // Interest info (for bank accounts)
        //
        public bool IsInterestInfoVisible { get; }

        public decimal InterestAmount { get; set; }
        public DateTime InterestDate { get; set; }
        public string InterestCategory { get; set; }
        public IEnumerable<CategoryItem> Categories => mainWindowLogic.Categories;

        //
        // Securities info (for investment accounts)
        //
        public bool IsSecurityInfoVisible { get; }
        private readonly ObservableCollection<SecurityInfoItem> securityInfos = new ObservableCollection<SecurityInfoItem>();
        public CollectionView SecurityInfoSource { get; }

        #endregion

        #region Actions

        public void GuessSecurities(DateTime end)
        {
            if (accountRow.Type != EAccountType.Investment)
            {
                return;
            }

            securityInfos.Clear();

            // Get the protfolio at the statement date
            var portfolio = accountRow.GetPortfolio(end);

            // Create all the corresponding security info items
            foreach (var security in portfolio.GetSecuritiesRows())
            {
                var quantity = portfolio.Lots.Where(l => l.Security == security).Sum(l => l.Quantity);
                securityInfos.Add(new SecurityInfoItem(security, quantity));
            }

            // Get the reconciled protfolio state
            portfolio = accountRow.GetReconciledPortfolio();

            // Add any missing security with a presumed quantity of zero
            foreach (var security in portfolio.GetSecuritiesRows())
            {
                if (securityInfos.FirstOrDefault(si => si.Symbol == security.Symbol) == null)
                {
                    securityInfos.Add(new SecurityInfoItem(security, 0));
                }
            }
        }


        protected override bool? Commit()
        {
            var household = mainWindowLogic.Household;
            bool adding = reconcileInfoRow == null;

            accountRow.LastStatementDate = PriorStatementEndDate;

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
                reconcileInfoRow.AccountID = accountRow.ID;
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

            foreach (var si in securityInfos)
            {
                var existingRow = reconcileInfoRow.GetSecurityReconcileInfoRows().FirstOrDefault(sri => sri.SecurityRow == si.SecurityRow);
                if (existingRow == null)
                {
                    var reconcileSecurityInfoRow = household.SecurityReconcileInfo.NewSecurityReconcileInfoRow();
                    reconcileSecurityInfoRow.ReconcileInfoID = reconcileInfoRow.ID;
                    reconcileSecurityInfoRow.SecurityID = si.SecurityRow.ID;
                    reconcileSecurityInfoRow.SecurityQuantity = si.Quantity;
                    household.SecurityReconcileInfo.Rows.Add(reconcileSecurityInfoRow);
                }
                else
                {
                    existingRow.SecurityQuantity = si.Quantity;
                }
            }

            mainWindowLogic.CommitChanges();

            return true;
        }

        #endregion

        #region SecurityInfoItem class

        public class SecurityInfoItem
        {
            public SecurityInfoItem(Household.SecurityRow securitiesRow, decimal quantity) => (SecurityRow, Quantity) = (securitiesRow, quantity);

            public readonly Household.SecurityRow SecurityRow;
            public string Name => SecurityRow.Name;
            public string Symbol => SecurityRow.Symbol == Household.SecurityRow.SYMBOL_NONE ? SecurityRow.Name : SecurityRow.Symbol;
            public decimal Quantity { get; set; }
            public int TabIndex { get; set; }
        }

        #endregion
    }
}
