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
    public class QIFWriter
    {
        #region private member

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;

        #endregion

        #region Constructor

        public QIFWriter(MainWindowLogic _mainWindowLogic, Household _household) =>
            (mainWindowLogic, household) = (_mainWindowLogic, _household);

        #endregion

        #region Entry points

        //
        // Export bits and pieces
        //
        [Flags]
        public enum EContents
        {
            Categories = 0x01,
            Accounts = 0x02,
            Securities = 0x04,
            Transactions = 0x08,
            MemorizedPayees = 0x10,

            All = Categories | Accounts | Securities | Transactions | MemorizedPayees
        }

        public string Export(string filename, EContents contents, IEnumerable<Household.AccountRow> transactionAccounts)
        {
            string result = "Export completed.";

            using (var sw = new StreamWriter(filename, false))
            {
                if (contents.HasFlag(EContents.Categories))
                {
                    ExportCategories(sw);
                }

                if (contents.HasFlag(EContents.Accounts))
                {
                    ExportAccounts(sw);
                }

                if (contents.HasFlag(EContents.Securities))
                {
                    ExportSecurities(sw);
                }

                if (contents.HasFlag(EContents.Transactions))
                {
                    (int numAccounts, int numTransactions)  = ExportTransactions(sw, transactionAccounts);
                    result = $"Exported {numTransactions} transactions in {numAccounts} accounts.";
                }

                if (contents.HasFlag(EContents.MemorizedPayees))
                {
                    ExportMemorizedPayees(sw);
                }

                if (contents.HasFlag(EContents.Securities))
                {
                    ExportSecurityPrices(sw);
                }
            }

            return result;
        }

        //
        // Only write out the transactions associated with the current checkpoint
        // then create a new checkpoint
        //
        public (int numAccounts, int numTransactions) DifferentialExportToQIF(string filename)
        {
            var checkpointRow = household.Checkpoint.GetCurrentCheckpoint();
            int numAccounts;
            int numTransactions;

            using (var sw = new StreamWriter(filename, false))
            {
                (numAccounts, numTransactions) = ExportTransactions(sw, household.Account.Rows.Cast<Household.AccountRow>(), checkpointRow);
            }

            // Create a new checkpoint
            household.Checkpoint.CreateNewCheckpoint();
            mainWindowLogic.CommitChanges(household);

            return (numAccounts, numTransactions);
        }

        #endregion

        #region Write QIF categories

        private void ExportCategories(StreamWriter sw)
        {
            sw.WriteLine("!Type:Cat");
            foreach(Household.CategoryRow categoryRow in household.Category.Rows)
            {
                sw.WriteLine($"N{categoryRow.FullName}");
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
            foreach (Household.AccountRow accountRow in household.Account.Rows)
            {
                ExportOneAccount(sw, accountRow, GetAccountType(accountRow, false));
            }
            sw.WriteLine("!Clear:AutoSwitch");
        }

        private void ExportOneAccount(StreamWriter sw, Household.AccountRow accountRow, string accountType)
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

        private string GetAccountType(Household.AccountRow accountRow, bool forTransactions)
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
                        case EInvestmentKind.Asset:
                            return "Invst";
                    }
                    break;
            }

            throw new InvalidDataException("Trouble parsing account type");
        }

        #endregion

        #region Write QIF securities

        private void ExportSecurities(StreamWriter sw)
        {
            foreach (Household.SecurityRow securityRow in household.Security.Rows)
            {
                sw.WriteLine("!Type:Security");
                sw.WriteLine($"N{securityRow.Name}");
                if (securityRow.Symbol != Household.SecurityRow.SYMBOL_NONE)
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

        private (int numAccounts, int numTransactions) ExportTransactions(StreamWriter sw, IEnumerable<Household.AccountRow> accounts, Household.CheckpointRow checkpointRow = null)
        {
            sw.WriteLine("!Option:AutoSwitch");
            int numAccounts = 0;
            int numTransactions = 0;

            foreach (Household.AccountRow accountRow in accounts)
            {
                bool accountWritten = false;

                // Get all transactions on this account
                foreach (var transactionRow in accountRow.GetRegularTransactionRows())
                {
                    // If filtering on checkpoint, skip transactions that do not match
                    if (checkpointRow != null && transactionRow.CheckpointRow != checkpointRow)
                    {
                        continue;
                    }

                    // Write the account as a header first time around
                    if (!accountWritten)
                    {
                        sw.WriteLine("!Account");
                        string accountType = GetAccountType(accountRow, true);
                        ExportOneAccount(sw, accountRow, accountType);

                        sw.WriteLine($"!Type:{accountType}");
                        accountWritten = true;

                        numAccounts += 1;
                    }

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
                    numTransactions += 1;
                }
            }

            return (numAccounts, numTransactions);
        }

        private void ExportBankingTransaction(StreamWriter sw, Household.AccountRow accountRow, Household.TransactionRow transactionRow)
        {
            var liRows = transactionRow.GetLineItemRows();
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

        private void ExportInvestmentTransaction(StreamWriter sw, Household.TransactionRow transactionRow)
        {
            var liRow = transactionRow.GetLineItemRows()[0];
            decimal amount = Math.Abs(liRow.Amount);

            var investmentTransactionRow = transactionRow.GetInvestmentTransaction();

            if (investmentTransactionRow.Type == EInvestmentTransactionType.ReinvestDividends &&
                investmentTransactionRow.Commission != 0)
            {
                // Sepcial case of ReinvDiv with fees - not handled gracefully by quicken.
                // See below at commission processing
                amount -= investmentTransactionRow.Commission;
            }


            switch (investmentTransactionRow.Type)
            {
                case EInvestmentTransactionType.CashIn:
                    sw.WriteLine("NCash");
                    break;
                case EInvestmentTransactionType.CashOut:
                    sw.WriteLine("NCash");
                    amount = -amount;
                    break;
                case EInvestmentTransactionType.TransferCashIn:
                    sw.WriteLine("NXIn");
                    break;
                case EInvestmentTransactionType.TransferCashOut:
                    sw.WriteLine("NXOut");
                    break;
                case EInvestmentTransactionType.InterestIncome:
                    sw.WriteLine("NIntInc");
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
                sw.WriteLine($"Y{investmentTransactionRow.SecurityRow.Name}");
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
                if (investmentTransactionRow.Type == EInvestmentTransactionType.ReinvestDividends)
                {
                    // Special handling for fees on ReinvDiv (not supported gracefully by quicken)
                    // Create a separate transaction for the fee
                    sw.WriteLine("^");
                    ExportDate(sw, transactionRow.Date);
                    sw.WriteLine("NMiscIncX");
                    sw.WriteLine($"Y{investmentTransactionRow.SecurityRow.Name}");
                    ExportTransactionStatus(sw, transactionRow.Status, true);
                    sw.WriteLine($"U{investmentTransactionRow.Commission:N2}");
                    sw.WriteLine($"T{investmentTransactionRow.Commission:N2}");
                    sw.WriteLine($"M{investmentTransactionRow.Commission:N2} as a fee");
                    sw.WriteLine($"L_DivInc|[{transactionRow.AccountRow.Name}]");
                }
                else
                {
                    sw.WriteLine($"O{investmentTransactionRow.Commission:N2}");
                }
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

        private void ExportCategory(StreamWriter sw, Household.LineItemRow lineItemRow, bool forSplit)
        {
            string letter = forSplit ? "S" : "L";
            if (lineItemRow.GetLineItemCategoryRow() is Household.LineItemCategoryRow lineItemCategoryRow)
            {
                sw.WriteLine(letter + mainWindowLogic.Categories.Find(c => c.ID == lineItemCategoryRow.CategoryID).FullName);
            }
            else if (lineItemRow.GetLineItemTransferRow() is Household.LineItemTransferRow lineItemTransferRow)
            {
                var tx = mainWindowLogic.Transfers.Find(t => t.AccountID == lineItemTransferRow.AccountID);
                if (tx == null)
                {
                    tx = mainWindowLogic.HiddenTransfers.Find(t => t.AccountID == lineItemTransferRow.AccountID);
                }
                sw.WriteLine(letter + tx.FullName);
            }
        }

        #endregion

        #region Write QIF memorized payees

        private void ExportMemorizedPayees(StreamWriter sw)
        {
            sw.WriteLine("!Type:Memorized");

            foreach(var mpr in household.MemorizedPayees)
            {
                var liRows = mpr.GetLineItemRows();
                decimal amount = liRows.Sum(li => li.Amount);

                sw.WriteLine(amount > 0 ? "KD" : "KP");
                sw.WriteLine($"U{amount:N2}");
                sw.WriteLine($"T{amount:N2}");

                sw.WriteLine($"P{mpr.Payee}");
                if (!mpr.IsMemoNull())
                {
                    sw.WriteLine($"M{mpr.Memo}");
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
                sw.WriteLine("^");
            }
        }

        #endregion

        #region Export security prices

        private void ExportSecurityPrices(StreamWriter sw)
        {
            foreach (Household.SecurityPriceRow spr in household.SecurityPrice.Rows)
            {

                var securityRow = spr.SecurityRow;
                if (securityRow.Symbol != Household.SecurityRow.SYMBOL_NONE)
                {
                    sw.WriteLine("!Type:Prices");
                    sw.WriteLine($"\"{securityRow.Symbol}\",{spr.Value:N2},\"{spr.Date:MM/dd/yyyy}\"");
                    sw.WriteLine("^");
                }

            }
        }

        #endregion

        #region Shared utilities

        private void ExportDate(StreamWriter sw, DateTime date)
        {
            sw.WriteLine($"D{date:MM/dd/yyyy}");
        }

        #endregion`
    }
}
