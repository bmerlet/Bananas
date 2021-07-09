//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public class Portfolio
    {
        decimal cashBalance;
        private readonly List<Lot> lots;

        public Portfolio()
        {
            this.cashBalance = 0;
            this.lots = new List<Lot>();
        }

        public decimal CashBalance => cashBalance;

        public IEnumerable<Lot> Lots => lots;

        public void ApplyTransaction(Household.TransactionsRow transaction)
        {
            // Get investment transaction
            var investmentTransaction = transaction.GetInvestmentTransaction();

            // Get security if any
            Household.SecurityRow security = null;
            if (!investmentTransaction.IsSecurityIDNull())
            {
                security = investmentTransaction.SecuritiesRow;
            }

            // Don't! The commission is taken into account in the transaction's amount
            //cashBalance -= investmentTransaction.Commission;

            if (investmentTransaction.IsCashIn || investmentTransaction.IsCashOut)
            {
                cashBalance += transaction.GetAmount();
            }

            if (investmentTransaction.IsSecurityIn)
            {
                AddShares(transaction.Date, security, investmentTransaction.SecurityQuantity, investmentTransaction.SecurityPrice);
            }
            else if (investmentTransaction.IsSecurityOut)
            {
                RemoveShares(security, investmentTransaction.SecurityQuantity);
            }
        }

        public void ApplyTransfer(Household.LineItemsRow lineItem)
        {
            cashBalance -= lineItem.Amount;
        }

        private void AddShares(DateTime date, Household.SecurityRow security, decimal quantity, decimal securityPrice)
        {
            lots.Add(new Lot(date, security, quantity, securityPrice));
        }

        private void RemoveShares(Household.SecurityRow security, decimal quantity)
        {
            // Only FIFO supported
            while (quantity > 0)
            {
                // Find most ancient lot for this security
                var lot = lots.FirstOrDefault(l => l.Security.ID == security.ID);
                if (lot == null)
                {
                    throw new InvalidOperationException("Cannot find lot to remove shares from");
                }

                if (quantity >= lot.Quantity)
                {
                    // Delete the lot
                    quantity -= lot.Quantity;
                    lots.Remove(lot);
                }
                else
                {
                    // Remove shares from the lot
                    var ix = lots.IndexOf(lot);
                    decimal newLotQuantity = lot.Quantity - quantity;
                    lots[ix] = new Lot(lot.Date, lot.Security, newLotQuantity, lot.SecurityPrice);
                    quantity = 0;
                }
            }
        }

        public IEnumerable<Lot> GetLotsUsedForSale(Household.SecurityRow security, decimal quantity)
        {
            // List of used lots
            var usedLots = new List<Lot>();

            // List of lots for this security
            var availableLots = lots.FindAll(l => l.Security.ID == security.ID);

            // Only FIFO supported
            while (quantity > 0)
            {
                var lot = availableLots.First();

                if (quantity >= availableLots[0].Quantity)
                {
                    // This lot is completely used up
                    usedLots.Add(lot);
                    availableLots.RemoveAt(0);
                    quantity -= lot.Quantity;
                }
                else
                {
                    // This lot is partially used up
                    usedLots.Add(lot);
                    quantity = 0;
                }
            }

            return usedLots;
        }

        public decimal GetValuation()
        {
            decimal val = cashBalance;

            foreach (var lot in lots)
            {
                val += lot.GetValuation();
            }

            return val;
        }

        // Get the securities held in this portfolio
        public IEnumerable<int> GetSecurities()
        {
            return lots.Select(l => l.Security.ID).Distinct();
        }

        public IEnumerable<Household.SecurityRow> GetSecuritiesRows()
        {
            return lots.Select(l => l.Security).Distinct();
        }

        // Computes cap gains from a transaction in the DB
        static public ComputeSaleCapitalGainsResult ComputeSaleCapitalGains(Household household, int transactionID, bool reportUsedLots)
        {
            // Get transaction info
            var transactionRow = household.Transactions.FindByID(transactionID);
            var investmentTransactionRow = transactionRow.GetInvestmentTransaction();
            var securityRow = investmentTransactionRow.SecuritiesRow;
            var accountRow = transactionRow.AccountsRow;

            decimal quantity = investmentTransactionRow.SecurityQuantity;
            decimal price = investmentTransactionRow.SecurityPrice;

            string description = $"Sale of {quantity:N4} shares of {securityRow.Symbol} at ${price:N4}";
            if (investmentTransactionRow.Commission != 0)
            {
                description += $" with a {investmentTransactionRow.Commission:C2} commision";

                // Recompute the price taking commission into account
                price = transactionRow.GetAmount() / quantity;
            }

            return ComputeSaleCapitalGains(accountRow, transactionRow.Date, transactionRow, securityRow, quantity, price, description, reportUsedLots);
        }

        // Computes cap gains from a hypothetical transaction (taking place today)
        static public ComputeSaleCapitalGainsResult ComputeSaleHypotheticalCapitalGains(
            Household.AccountRow accountRow,
            Household.SecurityRow securityRow,
            decimal securityQuantity,
            decimal securityPrice)
        {
            return ComputeSaleCapitalGains(accountRow, DateTime.Today, null, securityRow, securityQuantity, securityPrice, null, false);
        }

        private static ComputeSaleCapitalGainsResult ComputeSaleCapitalGains(
            Household.AccountRow accountRow, 
            DateTime date,
            Household.TransactionsRow transactionToExclude,
            Household.SecurityRow securityRow,
            decimal securityQuantity,
            decimal securityPrice,
            string description,
            bool reportUsedLots)
        {
            // init  results
            decimal longTermGain = 0;
            var longTermLots = new List<UsedLot>();
            decimal shortTermGain = 0;
            var shortTermLots = new List<UsedLot>();


            // Figure out the portfolio for this account at the transaction date
            var portfolio = accountRow.GetPortfolio(date, transactionToExclude);

            // Get the used lots
            var usedLots = portfolio.GetLotsUsedForSale(securityRow, securityQuantity);
            foreach (var lot in usedLots)
            {
                decimal quantityUsed = Math.Min(lot.Quantity, securityQuantity);
                decimal gain = (securityPrice - lot.SecurityPrice) * quantityUsed;

                var usedLot = reportUsedLots ? new UsedLot(lot.Date, lot.Quantity, quantityUsed, lot.SecurityPrice, gain) : null;
                if (date.Subtract(lot.Date).TotalDays > 365) // ZZZ
                {
                    if (reportUsedLots)
                    {
                        longTermLots.Add(usedLot);
                    }
                    longTermGain += gain;
                }
                else
                {
                    if (reportUsedLots)
                    {
                        shortTermLots.Add(usedLot);
                    }
                    shortTermGain += gain;
                }

                securityQuantity -= quantityUsed;
            }

            return new ComputeSaleCapitalGainsResult(description, longTermGain, longTermLots, shortTermGain, shortTermLots);
        }

        // Result from method above
        public class ComputeSaleCapitalGainsResult
        {
            public ComputeSaleCapitalGainsResult(string description, decimal longTermGain, IEnumerable<UsedLot> longTermLots, decimal shortTermGain, IEnumerable<UsedLot> shortTermLots) =>
                (Description, LongTermGain, LongTermLots, ShortTermGain, ShortTermLots) = (description, longTermGain, longTermLots, shortTermGain, shortTermLots);

            public readonly string Description;

            public readonly decimal LongTermGain;
            public readonly IEnumerable<UsedLot> LongTermLots;

            public readonly decimal ShortTermGain;
            public readonly IEnumerable<UsedLot> ShortTermLots;
        }

    }
}
