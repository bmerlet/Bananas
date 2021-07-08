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

namespace BanaData.Logic.Dialogs
{
    /// <summary>
    /// Shows a rebalance dashboard
    /// </summary>
    public class ShowRebalanceLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household.AccountsRow accountRow;

        #endregion

        #region Constructor

        public ShowRebalanceLogic(MainWindowLogic _mainWindowLogic, int accountID)
        {
            mainWindowLogic = _mainWindowLogic;
            accountRow = mainWindowLogic.Household.Accounts.FindByID(accountID);

            var portfolio = accountRow.GetPortfolio(null);
            foreach(var securityRow in portfolio.GetSecuritiesRows())
            {
                decimal securityQuantity = portfolio.Lots.Where(l => l.Security == securityRow).Sum(l => l.Quantity);
                decimal securityPrice = securityRow.GetMostRecentPrice();
                var securityItem = new SecurityItem(securityRow.Symbol, securityQuantity, securityPrice);
                securityItems.Add(securityItem);
                securityItem.TargetChanged += OnSecurityItemTargetChanged;
            }

            SecuritiesSource = (CollectionView)CollectionViewSource.GetDefaultView(securityItems);
            SecuritiesSource.SortDescriptions.Add(new SortDescription("Symbol", ListSortDirection.Ascending));

            UpdateQuotes = new CommandBase(OnUpdateQuotes);

            Recompute();
        }

        #endregion

        #region UI properties

        // The securities list
        private readonly ObservableCollection<SecurityItem> securityItems = new ObservableCollection<SecurityItem>();
        public CollectionView SecuritiesSource { get; }

        // Command to update quotes
        public CommandBase UpdateQuotes { get; }

        // Error string
        public string Error { get; private set; }

        // Total target %
        public decimal TotalTarget { get; private set; }

        // Total value
        public decimal TotalValue { get; private set; }

        #endregion

        #region Actions

        private void OnUpdateQuotes()
        {
            // ZZZZ
        }

        private void OnSecurityItemTargetChanged(object sender, EventArgs e)
        {
            Recompute();
        }

        private void Recompute()
        {
            // Recompute values
            foreach(var si in securityItems)
            {
                si.Valuation = si.SecurityQuantity * si.SecurityPrice;
            }

            TotalValue = securityItems.Sum(si => si.SecurityQuantity * si.SecurityPrice);
            OnPropertyChanged(() => TotalValue);

            // Recompute actual percentages
            foreach (var si in securityItems)
            {
                si.Actual = si.Valuation / TotalValue;
            }

            // Recompute total target percentage
            TotalTarget = securityItems.Sum(si => si.Target);
            OnPropertyChanged(() => TotalTarget);
            if (TotalTarget != 1)
            {
                Error = "The total target percentage must be 100%";
            }
            else
            {
                Error = "";

                // Compute action
                foreach (var si in securityItems)
                {
                    decimal diff = (si.Actual - si.Target) * TotalValue;
                    decimal numShares = diff / si.SecurityPrice;

                    if (si.Actual > si.Target)
                    {
                        si.Action = $"Too high by {diff:C2}; Sell {numShares:N1} shares of {si.Symbol}";
                    }
                    else
                    {
                        decimal toSell = diff / si.SecurityPrice;
                        si.Action = $"Too low by {-diff:C2}; Buy {-numShares:N1} shares of {si.Symbol}";
                    };
                }

            }

            OnPropertyChanged(() => Error);
        }

        protected override bool? Commit()
        {
            //throw new NotImplementedException();
            return false;
        }

        #endregion

        #region Support classes

        public class SecurityItem : LogicBase
        {
            public SecurityItem(string symbol, decimal securityQuantity, decimal _securityPrice) =>
                (Symbol, SecurityQuantity, securityPrice) = (symbol, securityQuantity, _securityPrice);


            // Invoked when target changes
            public event EventHandler TargetChanged;

            // Security symbol
            public string Symbol { get; }

            // Target %
            private decimal target;
            public decimal Target
            {
                get => target;
                set { target = value; TargetChanged?.Invoke(this, EventArgs.Empty); }
            }

            // How much of the security we own
            public decimal SecurityQuantity { get; }

            // Current price (set by the logic, not the UI)
            private decimal securityPrice;
            public decimal SecurityPrice
            {
                get => securityPrice;
                set { securityPrice = value; OnPropertyChanged(() => SecurityPrice); }
            }

            // Value (set by the logic, not the UI)
            private decimal valuation;
            public decimal Valuation
            {
                get => valuation;
                set { valuation = value; OnPropertyChanged(() => Valuation); }
            }

            // Actual %
            private decimal actual;
            public decimal Actual
            {
                get => actual;
                set { actual = value; OnPropertyChanged(() => Actual); }
            }

            // Action to take
            private string action;
            public string Action
            {
                get => action;
                set { action = value; OnPropertyChanged(() => Action); }
            }

            // ZZZZ should have STCG and LTCG resulting from the action
        }

        #endregion
    }
}
