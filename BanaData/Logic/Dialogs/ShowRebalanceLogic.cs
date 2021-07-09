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
using BanaData.Web;
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
        private readonly Household.AccountRow accountRow;

        #endregion

        #region Constructor

        public ShowRebalanceLogic(MainWindowLogic _mainWindowLogic, int accountID)
        {
            mainWindowLogic = _mainWindowLogic;
            accountRow = mainWindowLogic.Household.Account.FindByID(accountID);

            var portfolio = accountRow.GetPortfolio(null);
            foreach(var securityRow in portfolio.GetSecuritiesRows())
            {
                decimal securityQuantity = portfolio.Lots.Where(l => l.Security == securityRow).Sum(l => l.Quantity);
                decimal securityPrice = securityRow.GetMostRecentPrice();
                var securityItem = new SecurityItem(securityRow, securityRow.Symbol, securityQuantity, securityPrice);
                securityItems.Add(securityItem);
                securityItem.TargetChanged += OnSecurityItemTargetChanged;
            }

            SecuritiesSource = (CollectionView)CollectionViewSource.GetDefaultView(securityItems);
            SecuritiesSource.SortDescriptions.Add(new SortDescription("Symbol", ListSortDirection.Ascending));

            UpdateQuotes = new CommandBase(OnUpdateQuotes);

            SetupPercentages();
            Recompute();
        }

        #endregion

        #region UI properties

        // The securities list
        private readonly ObservableCollection<SecurityItem> securityItems = new ObservableCollection<SecurityItem>();
        public CollectionView SecuritiesSource { get; }

        // Deviation percent threshold
        private decimal threshold = 0.01M;
        public decimal Threshold
        {
            get => threshold;
            set { threshold = value; Recompute(); }
        }

        // Command to update quotes
        public CommandBase UpdateQuotes { get; }

        // Error string
        public string Error { get; private set; }

        // Total target %
        public decimal TotalTarget { get; private set; }

        // Total value
        public decimal TotalValue { get; private set; }

        // Widths
        private double widthOfSecurityColumn = 90;
        public double WidthOfSecurityColumn
        {
            get => widthOfSecurityColumn;
            set { widthOfSecurityColumn = value; OnPropertyChanged(() => WidthOfSecurityColumn); }
        }

        private double widthOfTargetColumn = 60;
        public double WidthOfTargetColumn
        {
            get => widthOfTargetColumn;
            set { widthOfTargetColumn = value; OnPropertyChanged(() => WidthOfTargetColumn); }
        }

        private double widthOfQuantityColumn = 90;
        public double WidthOfQuantityColumn
        {
            get => widthOfQuantityColumn;
            set { widthOfQuantityColumn = value; OnPropertyChanged(() => WidthOfQuantityColumn); }
        }

        private double widthOfPriceColumn = 60;
        public double WidthOfPriceColumn
        {
            get => widthOfPriceColumn;
            set { widthOfPriceColumn = value; OnPropertyChanged(() => WidthOfPriceColumn); }
        }

        private double widthOValueColumn = 100;
        public double WidthOValueColumn
        {
            get => widthOValueColumn;
            set { widthOValueColumn = value; OnPropertyChanged(() => WidthOValueColumn); }
        }

        #endregion

        #region Actions

        private void OnUpdateQuotes()
        {
            var quote = new Quote();
            foreach(var si in securityItems)
            {
                si.SecurityPrice = quote.GetQuote(si.Symbol);
            }

            Recompute();
        }

        private void SetupPercentages()
        {
            // Look up in DB
            bool found = false;
            foreach(var targetRow in accountRow.GetRebalanceTargetRows())
            {
                foreach (var si in securityItems)
                {
                    if (si.SecurityRow == targetRow.SecurityRow)
                    {
                        si.Target = targetRow.Target;
                        found = true;
                    }
                }
            }

            // Not in DB: do quick and dirty computation based on current values
            if (!found)
            {
                TotalValue = securityItems.Sum(si => si.SecurityQuantity * si.SecurityPrice);
                foreach (var si in securityItems)
                {
                    si.Target = Math.Round(si.SecurityQuantity * si.SecurityPrice / TotalValue, 3);
                }
                TotalTarget = securityItems.Sum(si => si.Target);
                securityItems.Last().Target = 1 + securityItems.Last().Target - TotalTarget;
            }
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

                    if (Math.Abs(si.Actual - si.Target) >= Threshold)
                    {
                        if (si.Actual > si.Target)
                        {
                            si.Status = $"Too high by {diff:C2}";
                            si.Action = $"Sell {numShares:N1} shares of {si.Symbol}";

                            var cg = Portfolio.ComputeSaleHypotheticalCapitalGains(accountRow, si.SecurityRow, numShares, si.SecurityPrice);
                            si.Consequences =
                                (cg.ShortTermGain != 0 ? $"STCG: {cg.ShortTermGain:C2}" : "") +
                                (cg.ShortTermGain != 0 && cg.LongTermGain != 0 ? ", " : "") +
                                (cg.LongTermGain != 0 ? $"LTCG: {cg.LongTermGain:C2}" : "");
                        }
                        else
                        {
                            si.Status = $"Too low by {-diff:C2}";
                            si.Action = $"Buy {-numShares:N1} shares of {si.Symbol}";
                            si.Consequences = "";
                        }
                    }
                    else
                    {
                        si.Status = "OK";
                        si.Action = "";
                        si.Consequences = "";
                    }
                }

            }

            OnPropertyChanged(() => Error);
        }

        protected override bool? Commit()
        {
            // Commit the current targets to DB
            bool change = false;

            foreach (var si in securityItems)
            {
                bool found = false;

                foreach (var targetRow in accountRow.GetRebalanceTargetRows())
                {
                    if (si.SecurityRow == targetRow.SecurityRow)
                    {
                        if (targetRow.Target != si.Target)
                        {
                            // Update DB entry
                            targetRow.Target = si.Target;
                            change = true;
                        }
                        found = true;
                    }
                }

                if (!found)
                {
                    // Create new entry in DB
                    var newTarget = mainWindowLogic.Household.RebalanceTarget.NewRebalanceTargetRow();
                    newTarget.AccountID = accountRow.ID;
                    newTarget.SecurityID = si.SecurityRow.ID;
                    newTarget.Target = si.Target;
                    mainWindowLogic.Household.RebalanceTarget.AddRebalanceTargetRow(newTarget);

                    change = true;
                }
            }

            if (change)
            {
                mainWindowLogic.CommitChanges();
            }

            return change;
        }

        #endregion

        #region Support classes

        public class SecurityItem : LogicBase
        {
            public SecurityItem(Household.SecurityRow securityRow, string symbol, decimal securityQuantity, decimal _securityPrice) =>
                (SecurityRow, Symbol, SecurityQuantity, securityPrice) = (securityRow, symbol, securityQuantity, _securityPrice);


            // For logic
            public readonly Household.SecurityRow SecurityRow;
                
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

            // Result: too high/too low
            private string status;
            public string Status
            {
                get => status;
                set { status = value; OnPropertyChanged(() => Status); }
            }

            // Action to take
            private string action;
            public string Action
            {
                get => action;
                set { action = value; OnPropertyChanged(() => Action); }
            }

            // Tax consequence
            private string consequences;
            public string Consequences
            {
                get => consequences;
                set { consequences = value; OnPropertyChanged(() => Consequences); }
            }
        }

        #endregion
    }
}
