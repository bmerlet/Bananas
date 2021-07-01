using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Logic.Main;
using BanaData.Database;

namespace BanaData.Logic.Dialogs
{
    public class ShowCapitalGainsLogic : LogicBase
    {
        private readonly MainWindowLogic mainWindowLogic;

        public ShowCapitalGainsLogic(MainWindowLogic _mainWindowLogic, int transactionID)
        {
            mainWindowLogic = _mainWindowLogic;

            BuildCapitalGainsInfo(transactionID);
        }

        private void BuildCapitalGainsInfo(int transactionID)
        {
            // Get transaction info
            var household = mainWindowLogic.Household;
            var transactionRow = household.Transactions.FindByID(transactionID);
            var investmentTransactionRow = household.InvestmentTransactions.GetByTransaction(transactionRow);
            var securityRow = household.Securities.FindByID(investmentTransactionRow.SecurityID);
            var accountRow = household.Accounts.FindByID(transactionRow.AccountID);

            decimal quantity = investmentTransactionRow.SecurityQuantity;
            decimal price = investmentTransactionRow.SecurityPrice;

            Description = $"Sale of {quantity:N4} shares of {securityRow.Symbol} at ${price:N4}";
            if (investmentTransactionRow.Commission != 0)
            {
                Description += $" with a {investmentTransactionRow.Commission:C2} commision";

                // Recompute the price taking commission into account
                price = transactionRow.GetAmount() / quantity;
            }
            
            // Figure out the portfolio for this account at the transaction date
            var portfolio = accountRow.GetPortfolio(transactionRow.Date);

            // Get the used lots
            var usedLots = portfolio.GetLotsUsedForSale(securityRow, investmentTransactionRow.SecurityQuantity);
            foreach(var lot in usedLots)
            {
                decimal quantityUsed = Math.Min(lot.Quantity, quantity);
                decimal gain = (price - lot.SecurityPrice) * quantityUsed;

                var usedLot = new UsedLot(lot.Date, lot.Quantity, quantityUsed, lot.SecurityPrice, gain);
                if (transactionRow.Date.Subtract(lot.Date).TotalDays > 365) // ZZZ
                {
                    longTermLots.Add(usedLot);
                    LongTermGain += gain;
                }
                else
                {
                    shortTermLots.Add(usedLot);
                    ShortTermGain += gain;
                }

                quantity -= quantityUsed;
            }
        }

        public string Description { get; private set; }

        public decimal LongTermGain { get; private set; }

        private readonly List<UsedLot> longTermLots = new List<UsedLot>();
        public IEnumerable<UsedLot> LongTermLots => longTermLots;

        public decimal ShortTermGain { get; private set; }

        private readonly List<UsedLot> shortTermLots = new List<UsedLot>();
        public IEnumerable<UsedLot> ShortTermLots => shortTermLots;

        public class UsedLot
        {
            public UsedLot(DateTime date, decimal totalQuantity, decimal quantityUsed, decimal costBasis, decimal gain) =>
                (Date, TotalQuantity, QuantityUsed, CostBasis, Gain) = (date, totalQuantity, quantityUsed, costBasis, gain);

            public DateTime Date { get; }
            public decimal TotalQuantity { get; }
            public decimal QuantityUsed { get; }
            public decimal CostBasis { get; }
            public decimal Gain { get; }
        }
    }
}
