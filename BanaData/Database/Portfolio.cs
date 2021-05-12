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

        public decimal CashBalance
        {
            get { return cashBalance; }
        }

        public List<Lot> Lots
        {
            get { return lots; }
        }

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

            switch (investmentTransaction.Type)
            {
                case EInvestmentTransactionType.Cash:
                case EInvestmentTransactionType.InterestIncome:
                case EInvestmentTransactionType.Dividends:
                case EInvestmentTransactionType.ShortTermCapitalGains:
                case EInvestmentTransactionType.LongTermCapitalGains:
                case EInvestmentTransactionType.TransferCash:
                case EInvestmentTransactionType.TransferCashIn:
                case EInvestmentTransactionType.TransferMiscellaneousIncomeIn:
                case EInvestmentTransactionType.ReturnOnCapital:
                case EInvestmentTransactionType.Exercise:
                    cashBalance += transaction.GetAmount();
                    break;

                case EInvestmentTransactionType.TransferCashOut:
                    cashBalance -= transaction.GetAmount();
                    break;

                case EInvestmentTransactionType.SharesIn:
                case EInvestmentTransactionType.BuyFromTransferredCash:
                case EInvestmentTransactionType.ReinvestDividends:
                case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                    AddShares(transaction.Date, security, investmentTransaction.SecurityQuantity);
                    break;
                case EInvestmentTransactionType.Buy:
                    cashBalance -= transaction.GetAmount();
                    AddShares(transaction.Date, security, investmentTransaction.SecurityQuantity);
                    break;

                case EInvestmentTransactionType.SharesOut:
                case EInvestmentTransactionType.SellAndTransferCash:
                    RemoveShares(transaction.Date, security, investmentTransaction.SecurityQuantity);
                    break;
                case EInvestmentTransactionType.Sell:
                    cashBalance += transaction.GetAmount();
                    RemoveShares(transaction.Date, security, investmentTransaction.SecurityQuantity);
                    break;

                case EInvestmentTransactionType.Grant:
                case EInvestmentTransactionType.Vest:
                case EInvestmentTransactionType.Expire:
                    break;

                // ZZZ
                case EInvestmentTransactionType.TransferDividends:
                case EInvestmentTransactionType.TransferLongTermCapitalGains:
                case EInvestmentTransactionType.TransferShortTermCapitalGains:
                    break;

                default:
                    throw new InvalidOperationException("Unknown operation " + investmentTransaction.Type);
            }
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
    }
}
