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

            Description = $"Sale of {investmentTransactionRow.SecurityQuantity:N4} shares of {securityRow.Symbol} at ${investmentTransactionRow.SecurityPrice:N4}";
            
            // Figure out the portfolio for this account at the transaction date
            var portfolio = accountRow.GetPortfolio(transactionRow.Date);

            // Get the used lots
            var usedLots = portfolio.GetLotsUsedForSale(securityRow, investmentTransactionRow.SecurityQuantity);
            decimal quantity = investmentTransactionRow.SecurityQuantity;
            foreach(var lot in usedLots)
            {
                decimal quantityUsed = Math.Min(lot.Quantity, quantity);
                decimal gain = (investmentTransactionRow.SecurityPrice - lot.SecurityPrice) * quantityUsed;

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

        private List<UsedLot> longTermLots = new List<UsedLot>();
        public IEnumerable<UsedLot> LongTermLots => longTermLots;

        public decimal ShortTermGain { get; private set; }

        private List<UsedLot> shortTermLots = new List<UsedLot>();
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
