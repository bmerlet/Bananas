//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bananas.Data
{
    class Portfolio
    {
        decimal cashBalance;
        private List<Lot> lots;

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

            // Ah, take the commission first!
            cashBalance -= investmentTransaction.Commission;

            switch (investmentTransaction.Type)
            {
                case EInvestmentTransactionType.Cash:
                case EInvestmentTransactionType.InterestIncome:
                case EInvestmentTransactionType.Dividends:
                case EInvestmentTransactionType.TransferCash:
                case EInvestmentTransactionType.TransferCashIn:
                case EInvestmentTransactionType.TransferMiscellaneousIncomeIn:
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
                case EInvestmentTransactionType.Exercise:
                case EInvestmentTransactionType.Expire:
                    break;
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
                if (quantity >= lots[0].Quantity)
                {
                    quantity -= lots[0].Quantity;
                    lots.RemoveAt(0);
                }
                else
                {
                    decimal newLotQuantity = lots[0].Quantity - quantity;
                    lots[0] = new Lot(lots[0].Date, lots[0].Security, newLotQuantity);
                    quantity = 0;
                }
            }

            // Negative lots fake:
            // lots.Add(new Lot(date, security, -quantity));
        }

        public decimal GetValuation(Household household)
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
