using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BanaData.Database;
using BanaData.Logic.Main;

namespace BanaData.Serializations
{
    /// <summary>
    /// Writes out the database in QIF format
    /// </summary>
    class QIFWriter
    {
        #region private member

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;

        #endregion

        #region Constructor

        public QIFWriter(MainWindowLogic _mainWindowLogic) =>
            (mainWindowLogic, household) = (_mainWindowLogic, _mainWindowLogic.Household);

        #endregion

        #region Entry points

        public void ExportToQIF(string filename)
        {
            using (var sw = new StreamWriter(filename, false))
            {
                ExportCategories(sw);
                ExportAccounts(sw);
                ExportSecurities(sw);
                ExportTransactions(sw);
                ExportMemorizedPayees(sw);
            }
        }

        #endregion

        #region Write QIF categories

        private void ExportCategories(StreamWriter sw)
        {
            sw.WriteLine("!Type:Cat");
            foreach(Household.CategoriesRow categoryRow in household.Categories.Rows)
            {
                sw.WriteLine($"N{mainWindowLogic.Categories.Find(c => c.ID == categoryRow.ID).FullName}");
                if (!categoryRow.IsDescriptionNull())
                {
                    sw.WriteLine($"D{categoryRow.Description}");
                }
                if (!categoryRow.IsTaxInfoNull())
                {
                    sw.WriteLine("T");
                    sw.WriteLine($"R{categoryRow.TaxInfo}");
                }
                sw.WriteLine(categoryRow.IsIncome ? "I" : "E");
                sw.WriteLine("^");
            }
        }

        #endregion

        #region Write QIF accounts

        private void ExportAccounts(StreamWriter sw)
        {
            sw.WriteLine("!Option:AutoSwitch");
            sw.WriteLine("!Account");
            foreach (Household.AccountsRow accountRow in household.Accounts.Rows)
            {
                ExportOneAccount(sw, accountRow, GetAccountType(accountRow, false));
            }
            sw.WriteLine("!Clear:AutoSwitch");
        }

        private void ExportOneAccount(StreamWriter sw, Household.AccountsRow accountRow, string accountType)
        {
            sw.WriteLine($"N{accountRow.Name}");
            sw.WriteLine($"T{accountType}");
            if (accountRow.Type == EAccountType.CreditCard)
            {
                sw.WriteLine($"L{accountRow.CreditLimit:N2}");
            }

            if (!accountRow.IsDescriptionNull())
            {
                sw.WriteLine($"D{accountRow.Description}");
            }

            sw.WriteLine("^");
        }

        private string GetAccountType(Household.AccountsRow accountRow, bool forTransactions)
        {
            switch (accountRow.Type)
            {
                case EAccountType.Bank:
                    return "Bank";
                case EAccountType.CreditCard:
                    return "CCard";
                case EAccountType.Cash:
                    return "Cash";
                case EAccountType.Investment:
                    if (forTransactions)
                    {
                        return "Invst";
                    }
                    switch (accountRow.Kind)
                    {
                        case EInvestmentKind.Brokerage:
                        case EInvestmentKind.TraditionalIRA:
                            return "Port";
                        case EInvestmentKind.SingleMutualFund:
                            return "Mutual";
                        case EInvestmentKind._401k:
                            return "401(k)/403(b)";
                        case EInvestmentKind.Invalid:
                            return "Invst";
                    }
                    break;
                case EAccountType.OtherAsset:
                    return "Oth A";
                case EAccountType.OtherLiability:
                    return "Oth L";
            }

            throw new InvalidDataException("Trouble parsing account type");
        }

        #endregion

        #region Write QIF securities

        private void ExportSecurities(StreamWriter sw)
        {
            foreach (Household.SecuritiesRow securityRow in household.Securities.Rows)
            {
                sw.WriteLine("!Type:Security");
                sw.WriteLine($"N{securityRow.Name}");
                if (!securityRow.IsSymbolNull())
                {
                    sw.WriteLine($"S{securityRow.Symbol}");
                }
                switch(securityRow.Type)
                {
                    case ESecurityType.Stock:
                        sw.WriteLine("TStock");
                        break;
                    case ESecurityType.MutualFund:
                        sw.WriteLine("TMutual Fund");
                        break;
                    case ESecurityType.MarketIndex:
                        sw.WriteLine("TMarket Index");
                        break;
                    case ESecurityType.EmployeeStockOption:
                        sw.WriteLine("TEmp. Stock Opt.");
                        break;
                }
                sw.WriteLine("^");
            }
        }

        #endregion

        #region Write QIF transactions

        private void ExportTransactions(StreamWriter sw)
        {
            sw.WriteLine("!Option:AutoSwitch");

            foreach (Household.AccountsRow accountRow in household.Accounts.Rows)
            {
                sw.WriteLine("!Account");
                string accountType = GetAccountType(accountRow, true);
                ExportOneAccount(sw, accountRow, accountType);

                sw.WriteLine($"!Type:{accountType}");

                foreach (var transactionRow in accountRow.GetTransactions())
                {
                    ExportDate(sw, transactionRow.Date);

                    if (accountRow.Type == EAccountType.Investment)
                    {
                        ExportInvestmentTransaction(sw, transactionRow);
                    }
                    else
                    {
                        ExportBankingTransaction(sw, accountRow, transactionRow);
                    }

                    sw.WriteLine("^");
                }
            }
        }

        private void ExportBankingTransaction(StreamWriter sw, Household.AccountsRow accountRow, Household.TransactionsRow transactionRow)
        {
            var liRows = transactionRow.GetLineItemsRows();
            decimal amount = liRows.Sum(li => li.Amount);

            sw.WriteLine($"U{amount:N2}");
            sw.WriteLine($"T{amount:N2}");
            ExportTransactionStatus(sw, transactionRow.Status, false);

            if (accountRow.Type == EAccountType.Bank)
            {
                var bankingTrans = transactionRow.GetBankingTransaction();
                switch(bankingTrans.Medium)
                {
                    case ETransactionMedium.Check:
                        sw.WriteLine($"N{bankingTrans.CheckNumber}");
                        break;
                    case ETransactionMedium.ATM:
                        sw.WriteLine("NATM");
                        break;
                    case ETransactionMedium.Deposit:
                        sw.WriteLine("NDEP");
                        break;
                    case ETransactionMedium.Dividend:
                        sw.WriteLine("NDIV");
                        break;
                    case ETransactionMedium.EFT:
                        sw.WriteLine("NEFT");
                        break;
                    case ETransactionMedium.PrintCheck:
                        sw.WriteLine("NPrint");
                        break;
                    case ETransactionMedium.Transfer:
                        sw.WriteLine("NTXFR");
                        break;
                }
            }

            if (!transactionRow.IsPayeeNull())
            {
                sw.WriteLine($"P{transactionRow.Payee}");
            }
            if (!transactionRow.IsMemoNull())
            {
                sw.WriteLine($"M{transactionRow.Memo}");
            }
            ExportCategory(sw, liRows[0], false);

            if (liRows.Length > 1)
            {
                foreach(var liRow in liRows)
                {
                    ExportCategory(sw, liRow, true);
                    if (!liRow.IsMemoNull())
                    {
                        sw.WriteLine($"E{liRow.Memo}");
                    }
                    sw.WriteLine($"${liRow.Amount:N2}");
                }
            }
        }

        private void ExportInvestmentTransaction(StreamWriter sw, Household.TransactionsRow transactionRow)
        {
            var liRow = transactionRow.GetLineItemsRows()[0];
            decimal amount = Math.Abs(liRow.Amount);

            var investmentTransactionRow = transactionRow.GetInvestmentTransaction();

            switch(investmentTransactionRow.Type)
            {
                case EInvestmentTransactionType.Cash:
                    sw.WriteLine("NCash");
                    break;
                case EInvestmentTransactionType.InterestIncome:
                    sw.WriteLine("NIntInc");
                    break;
                case EInvestmentTransactionType.TransferCash:
                    sw.WriteLine("NContribX");
                    break;
                case EInvestmentTransactionType.TransferCashIn:
                    sw.WriteLine("NXIn");
                    break;
                case EInvestmentTransactionType.TransferCashOut:
                    sw.WriteLine("NXOut");
                    break;
                case EInvestmentTransactionType.TransferMiscellaneousIncomeIn:
                    sw.WriteLine("NMiscIncX");
                    break;
                case EInvestmentTransactionType.SharesIn:
                    sw.WriteLine("NShrsIn");
                    break;
                case EInvestmentTransactionType.SharesOut:
                    sw.WriteLine("NShrsOut");
                    break;
                case EInvestmentTransactionType.Buy:
                    sw.WriteLine("NBuy");
                    break;
                case EInvestmentTransactionType.BuyFromTransferredCash:
                    sw.WriteLine("NBuyX");
                    break;
                case EInvestmentTransactionType.Sell:
                    sw.WriteLine("NSell");
                    break;
                case EInvestmentTransactionType.SellAndTransferCash:
                    sw.WriteLine("NSellX");
                    break;
                case EInvestmentTransactionType.Dividends:
                    sw.WriteLine("NDiv");
                    break;
                case EInvestmentTransactionType.TransferDividends:
                    sw.WriteLine("NDivX");
                    break;
                case EInvestmentTransactionType.ReinvestDividends:
                    sw.WriteLine("NReinvDiv");
                    break;
                case EInvestmentTransactionType.ShortTermCapitalGains:
                    sw.WriteLine("NCGShort");
                    break;
                case EInvestmentTransactionType.TransferShortTermCapitalGains:
                    sw.WriteLine("NCGShortX");
                    break;
                case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                    sw.WriteLine("NReinvSh");
                    break;
                case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                    sw.WriteLine("NReinvMd");
                    break;
                case EInvestmentTransactionType.LongTermCapitalGains:
                    sw.WriteLine("NCGLong");
                    break;
                case EInvestmentTransactionType.TransferLongTermCapitalGains:
                    sw.WriteLine("NCGLongX");
                    break;
                case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                    sw.WriteLine("NReinvLg");
                    break;
                case EInvestmentTransactionType.ReturnOnCapital:
                    sw.WriteLine("NRtrnCap");
                    break;
                case EInvestmentTransactionType.Grant:
                    sw.WriteLine("NGrant");
                    break;
                case EInvestmentTransactionType.Vest:
                    sw.WriteLine("NVest");
                    break;
                case EInvestmentTransactionType.Exercise:
                    sw.WriteLine("NExercise");
                    break;
                case EInvestmentTransactionType.Expire:
                    sw.WriteLine("NExpire");
                    break;
            }

            if (!transactionRow.IsPayeeNull())
            {
                sw.WriteLine($"P{transactionRow.Payee}");
            }

            if (!investmentTransactionRow.IsSecurityIDNull())
            {
                sw.WriteLine($"Y{investmentTransactionRow.SecuritiesRow.Name}");
            }

            if (!investmentTransactionRow.IsSecurityPriceNull())
            {
                sw.WriteLine($"I{investmentTransactionRow.SecurityPrice:0.######}");
            }

            if (!investmentTransactionRow.IsSecurityQuantityNull())
            {
                sw.WriteLine($"Q{investmentTransactionRow.SecurityQuantity:0.######}");
            }

            ExportTransactionStatus(sw, transactionRow.Status, true);

            if (!transactionRow.IsMemoNull())
            {
                sw.WriteLine($"M{transactionRow.Memo}");
            }

            if (amount != 0)
            {
                sw.WriteLine($"U{amount:N2}");
                sw.WriteLine($"T{amount:N2}");
            }

            if (!transactionRow.IsMemoNull())
            {
                sw.WriteLine($"M{transactionRow.Memo}");
            }

            ExportCategory(sw, liRow, false);

            if (amount != 0)
            {
                sw.WriteLine($"${amount:N2}");
            }

            if (investmentTransactionRow.Commission != 0)
            {
                sw.WriteLine($"O{investmentTransactionRow.Commission:N2}");
            }
        }

        private void ExportTransactionStatus(StreamWriter sw, ETransactionStatus status, bool investment)
        {
            switch(status)
            {
                case ETransactionStatus.Pending:
                    sw.WriteLine("C");
                    break;
                case ETransactionStatus.Cleared:
                    sw.WriteLine("Cc");
                    break;
                case ETransactionStatus.Reconciled:
                    sw.WriteLine(investment ? "CR": "CX");
                    break;
            }
        }

        private void ExportCategory(StreamWriter sw, Household.LineItemsRow lineItemRow, bool forSplit)
        {
            string letter = forSplit ? "S" : "L";
            if (!lineItemRow.IsCategoryIDNull())
            {
                sw.WriteLine(letter + mainWindowLogic.Categories.Find(c => c.ID == lineItemRow.CategoryID).FullName);
            }
            else if (!lineItemRow.IsAccountIDNull())
            {
                sw.WriteLine(letter + mainWindowLogic.Categories.Find(c => c.AccountID == lineItemRow.AccountID).FullName);
            }
        }

        #endregion

        #region Write QIF memorized payees

        private void ExportMemorizedPayees(StreamWriter sw)
        {
            sw.WriteLine("!Type:Memorized");

            foreach(Household.MemorizedPayeesRow mpr in household.MemorizedPayees.Rows)
            {
                var liRows = mpr.GetMemorizedLineItemsRows();
                decimal amount = liRows.Sum(li => li.Amount);

                sw.WriteLine(amount > 0 ? "KD" : "KP");
                sw.WriteLine($"U{amount:N2}");
                sw.WriteLine($"T{amount:N2}");

                sw.WriteLine($"P{mpr.Payee}");
                if (!mpr.IsMemoNull())
                {
                    sw.WriteLine($"M{mpr.Memo}");
                }

                ExportCategoryForMemorizedPayee(sw, liRows[0], false);

                if (liRows.Length > 1)
                {
                    foreach(var liRow in liRows)
                    {
                        ExportCategoryForMemorizedPayee(sw, liRow, true);
                        if (!liRow.IsMemoNull())
                        {
                            sw.WriteLine($"E{liRow.Memo}");
                        }
                        sw.WriteLine($"${liRow.Amount:N2}");
                    }
                }
            }
        }

        private void ExportCategoryForMemorizedPayee(StreamWriter sw, Household.MemorizedLineItemsRow lineItemRow, bool forSplit)
        {
            string letter = forSplit ? "S" : "L";
            if (!lineItemRow.IsCategoryIDNull())
            {
                sw.WriteLine(letter + mainWindowLogic.Categories.Find(c => c.ID == lineItemRow.CategoryID).FullName);
            }
            else if (!lineItemRow.IsAccountIDNull())
            {
                sw.WriteLine(letter + mainWindowLogic.Categories.Find(c => c.AccountID == lineItemRow.AccountID).FullName);
            }
        }

        #endregion

        #region Shared utilities

        private void ExportDate(StreamWriter sw, DateTime date)
        {
            var dayStr = string.Format("{0,2:##}", date.Day);
            var yearStr = string.Format("{0,2:##}", date.Year - 2000);
            sw.WriteLine($"D{date.Month}/{dayStr}'{yearStr}");
        }

        #endregion`
    }
}
