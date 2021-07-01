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

        public void ApplyTransaction(Household household, Household.TransactionsRow transaction)
        {
            // Get investment transaction
            var investmentTransaction = household.InvestmentTransactions.GetByTransaction(transaction);

            // Get security if any
            Household.SecuritiesRow security = null;
            if (!investmentTransaction.IsSecurityIDNull())
            {
                security = household.Securities.FindByID(investmentTransaction.SecurityID);
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

#pragma warning disable IDE0060 // Remove unused parameter
        public void ApplyTransfer(Household household, Household.LineItemsRow lineItem)
        {
            cashBalance -= lineItem.Amount;
        }
#pragma warning restore IDE0060 // Remove unused parameter

        private void AddShares(DateTime date, Household.SecuritiesRow security, decimal quantity, decimal securityPrice)
        {
            lots.Add(new Lot(date, security, quantity, securityPrice));
        }

        private void RemoveShares(Household.SecuritiesRow security, decimal quantity)
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

        public IEnumerable<Lot> GetLotsUsedForSale(Household.SecuritiesRow security, decimal quantity)
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
    }
}
