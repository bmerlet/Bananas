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
    class Portfolio
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
                AddShares(transaction.Date, security, investmentTransaction.SecurityQuantity);
            }
            else if (investmentTransaction.IsSecurityOut)
            {
                RemoveShares(transaction.Date, security, investmentTransaction.SecurityQuantity);
            }
        }

        public void ApplyTransfer(Household household, Household.LineItemsRow lineItem)
        {
            cashBalance -= lineItem.Amount;
        }

        private void AddShares(DateTime date, Household.SecuritiesRow security, decimal quantity)
        {
            lots.Add(new Lot(date, security, quantity));
        }

        private void RemoveShares(DateTime date, Household.SecuritiesRow security, decimal quantity)
        {
            // ZZZ FIFO for now
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
                    lots[ix] = new Lot(lot.Date, lot.Security, newLotQuantity);
                    quantity = 0;
                }
            }

            // Negative lots fake:
            // lots.Add(new Lot(date, security, -quantity));
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
