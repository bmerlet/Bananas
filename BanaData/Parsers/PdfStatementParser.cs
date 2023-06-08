using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BanaData.Database;
using PdfParser;

namespace BanaData.Parsers
{
    public class PdfStatementParser
    {
        #region Entry points

        public void Parse(string path, Household db)
        {
            // Parse the PDF file
            var data = new PdfData(path);
            data.Parse();

            // Try to recognize statement and institution
            (EInstitution institution, string account) = DetermineAccount(data);
            LogLine("Institution: " + institution.ToString());
            LogLine("Account: " + account);
            LogLine("");

            var accountRow = db.Account.Where(a => a.Name == account).Single();

            switch (institution)
            {
                case EInstitution.Vanguard:
                    LogLine("\n=== Holdings:" + eol);
                    foreach (var hint in vanguardBrokerageHodingHints)
                    {
                        var holding = DetermineHolding(data, hint);
                        if (holding != null)
                        {
                            LogLine($"{hint.Ticker}  \t{holding}");
                        }
                    }

                    LogLine(eol + "=== Transactions:" + eol);
                    AnalyzeVanguardTransactions(data, accountRow, db);
                    break;

                case EInstitution.Chase:
                    AnalyzeChaseTransactions(data, accountRow, db);
                    break;

                default:
                    throw new InvalidOperationException("Statement institution not recognized");
            }
        }

        #endregion

        #region Public properties

        public string Log { get; private set; } = "";

        #endregion

        #region Account descriptions

        public enum EInstitution { None, Vanguard, Chase }

        static readonly AccountSpec[] accountSpecs = new AccountSpec[]
        {
            new AccountSpec(EInstitution.Vanguard, "NeoG Vanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Susan", "Benoit", "Joint brokerage" }) }),
            new AccountSpec(EInstitution.Vanguard, "SVanguard IRA XX1248", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Susan", "Traditional IRA brokerage" }) }),
            new AccountSpec(EInstitution.Vanguard, "BVanguard Roll IRA", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Benoit", "Rollover IRA brokerage" }) }),
            new AccountSpec(EInstitution.Vanguard, "SVanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Susan", "brokerage" }) }),
            new AccountSpec(EInstitution.Vanguard, "BVanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Benoit", "brokerage" }) }),
            new AccountSpec(EInstitution.Chase, "GAmazon XX6555", new AccountHint[] { new AccountHint(1, 1, new string[] { "Amazon Customer Service" }) }),
        };

        // Account
        class AccountSpec
        {
            public AccountSpec(EInstitution institution, string name, AccountHint[] hints) =>
                (Institution, BananaAccountName, AccountHints) = (institution, name, hints);

            public readonly EInstitution Institution;
            public readonly string BananaAccountName;
            public readonly AccountHint[] AccountHints;
        }

        // Account hint
        class AccountHint
        {
            public AccountHint(int min, int max, string[] required) => (MinPage, MaxPage, Required) = (min, max, required);
            public readonly int MinPage;
            public readonly int MaxPage;
            public readonly string[] Required;
        }

        static private (EInstitution, string) DetermineAccount(PdfData data)
        {
            EInstitution institution = EInstitution.None;
            string accountName = "?";

            foreach (var spec in accountSpecs)
            {
                bool pass = true;
                foreach (var hint in spec.AccountHints)
                {
                    for (int page = hint.MinPage - 1; page <= hint.MaxPage - 1; page++)
                    {
                        var strs = data.ExtractTextFromPage(page);
                        foreach (var required in hint.Required)
                        {
                            bool found = false;
                            foreach (var str in strs)
                            {
                                if (str.Contains(required))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                pass = false;
                                break;
                            }
                        }
                    }

                    if (!pass)
                    {
                        break;
                    }
                }

                if (pass)
                {
                    accountName = spec.BananaAccountName;
                    institution = spec.Institution;
                    break;
                }
            }

            return (institution, accountName);
        }

        #endregion

        #region Holdings description

        static readonly HoldingHint[] vanguardBrokerageHodingHints = new HoldingHint[]
        {
            new HoldingHint(4, 5, "VANGUARD FEDERAL MONEY", -4, "VMFXX"),
            new HoldingHint(4, 5, "VNQ", 2, "VNQ"),
            new HoldingHint(4, 5, "VGIT", 2, "VGIT"),
            new HoldingHint(4, 5, "BND", 2, "BND"),
            new HoldingHint(4, 5, "BNDX", 2, "BNDX"),
            new HoldingHint(4, 5, "VXUS", 2, "VXUS"),
            new HoldingHint(4, 5, "VTI", 2, "VTI"),
        };

        class HoldingHint
        {
            public HoldingHint(int min, int max, string trigger, int offset, string ticker) =>
                (MinPage, MaxPage, Trigger, Offset, Ticker) = (min, max, trigger, offset, ticker);
            public readonly int MinPage;
            public readonly int MaxPage;
            public readonly string Trigger;
            public readonly int Offset;
            public readonly string Ticker;
        }

        string DetermineHolding(PdfData data, HoldingHint hint)
        {
            for (int page = hint.MinPage - 1; page <= hint.MaxPage - 1; page++)
            {
                var strs = data.ExtractTextFromPage(page);
                for (int i = 0; i < strs.Length; i++)
                {
                    if (strs[i] == hint.Trigger)
                    {
                        return strs[i + hint.Offset];
                    }
                }
            }
            return null;
        }

        #endregion

        #region Vanguard analyzer

        private void AnalyzeVanguardTransactions(PdfData data, Household.AccountRow account, Household db)
        {
            var trans = new List<VanguardTransaction>();

            // Find the year - First page: July 31, 2022, monthly transaction statement
            string year = "/2100";
            {
                var strs = data.ExtractTextFromPage(0);
                foreach (var str in strs)
                {
                    int ix = str.IndexOf(", monthly transaction statement");
                    if (ix < 0)
                    {
                        ix = str.IndexOf(", quarter-to-date statement");
                    }
                    if (ix < 0)
                    {
                        ix = str.IndexOf(", year-to-date statement");
                    }
                    if (ix > 4)
                    {
                        year = "/" + str.Substring(ix - 4, 4);
                        break;
                    }
                }
            }

            // Start at page 5 "Completed Transactions", stop at page containing "Disclosures"
            bool foundCompletedTransactions = false;
            for (int page = 4; page < data.NumberOfPages; page++)
            {
                var strs = data.ExtractTextFromPage(page);
                if (strs.FirstOrDefault(s => s == "Disclosures") != null)
                {
                    break;
                }

                for (int i = 0; i < strs.Length; i++)
                {
                    if (!foundCompletedTransactions)
                    {
                        if (strs[i].Contains("Completed transactions"))
                        {
                            foundCompletedTransactions = true;
                        }
                        continue;
                    }

                    // We are after Completed Transactions" and before "Disclosures"
                    if (strs[i] == "VANGUARD FEDERAL MONEY" && (i + 1) < strs.Length && strs[i + 1] == "Reinvestment")
                    {
                        // Settlement fund reinvestment 
                        decimal quantity = -decimal.Parse(strs[i + 6]);
                        var vg = new VanguardTransaction(
                            strs[i - 3] + year,
                            "VMFXX",
                            GetVanguardType(strs[i + 1]),
                            quantity,
                            1,
                            quantity);
                        trans.Add(vg);
                    }
                    else if (strs[i] == "VANGUARD FEDERAL MONEY" && (i + 1) < strs.Length && strs[i + 1] == "Sweep in")
                    {
                        // Sweep in
                        decimal quantity = -decimal.Parse(strs[i + 6]);
                        var vg = new VanguardTransaction(
                            strs[i - 3] + year,
                            "VMFXX",
                            EInvestmentTransactionType.Buy,
                            quantity,
                            1,
                            -quantity,
                            "Sweep in");
                        trans.Add(vg);
                    }
                    else if (strs[i] == "VANGUARD FEDERAL MONEY" && (i + 1) < strs.Length && strs[i + 1] == "Sweep out")
                    {
                        // Sweep out
                        decimal quantity = -decimal.Parse(strs[i + 6]);
                        var vg = new VanguardTransaction(
                            strs[i - 3] + year,
                            "VMFXX",
                            EInvestmentTransactionType.Sell,
                            quantity,
                            1,
                            quantity,
                            "Sweep out");
                        trans.Add(vg);
                    }
                    else
                    {
                        // Transactions on specific securities
                        foreach (var ticker in new string[] { "BND", "BNDX", "VGIT", "VXUS", "VTI", "VNQ" })
                        {
                            if (strs[i] == ticker)
                            {
                                // Found transaction
                                var type = GetVanguardType(strs[i + 2]);
                                var quantityStr = strs[i + 4];
                                var priceStr = RemoveDollarSign(strs[i + 5]);
                                var amountStr = RemoveNegativeSign(RemoveDollarSign(strs[i + 7]));
                                decimal amount = 0;
                                if (amountStr != "-")
                                {
                                    amount = decimal.Parse(amountStr);
                                }
                                if (type == EInvestmentTransactionType.Buy)
                                {
                                    amount = -amount;
                                }

                                var vg = new VanguardTransaction(
                                    strs[i - 2] + year,
                                    ticker,
                                    type,
                                    quantityStr == "-" ? 0 : decimal.Parse(quantityStr),
                                    priceStr == "-" ? 0 : decimal.Parse(priceStr),
                                    amount
                                );
                                trans.Add(vg);
                            }
                        }
                    }
                }
            }

            // Remove extraneous transactions (combine dividend and reinvestment)
            var tmpTrans = trans;
            trans = new List<VanguardTransaction>();
            for (int i = 0; i < tmpTrans.Count; i++)
            {
                if (i < tmpTrans.Count - 1 &&
                    tmpTrans[i].Ticker == tmpTrans[i + 1].Ticker &&
                    tmpTrans[i].Type == EInvestmentTransactionType.Dividends &&
                    tmpTrans[i + 1].Type == EInvestmentTransactionType.ReinvestDividends &&
                    tmpTrans[i].Amount == tmpTrans[i + 1].Amount)
                {
                    continue;
                }
                trans.Add(tmpTrans[i]);
            }

            // ZZZ Debug
            foreach (var vg in trans)
            {
                LogLine($"{vg.Date}\t{vg.Ticker}\t{vg.Quantity}\t{vg.Price}\t{vg.Amount}\t{vg.Type}");
            }
            LogLine("");

            // Add the transactions to the database
            var checkpointRow = db.Checkpoint.GetCurrentCheckpoint();
            foreach (var vg in trans)
            {
                var securityRow = db.Security.First(s => s.Symbol == vg.Ticker);
                var transRow = db.Transaction.Add(account, DateTime.Parse(vg.Date), null, vg.Memo, ETransactionStatus.Pending, checkpointRow, ETransactionType.Regular);
                db.InvestmentTransaction.Add(transRow, vg.Type, securityRow, vg.Price, vg.Quantity, 0);
                db.LineItem.Add(transRow, null, vg.Amount);
            }
        }

        private EInvestmentTransactionType GetVanguardType(string vgType)
        {
            switch (vgType)
            {
                case "Dividend": return EInvestmentTransactionType.Dividends;
                case "Reinvestment": return EInvestmentTransactionType.ReinvestDividends;
                case "Buy": return EInvestmentTransactionType.Buy;
            }
            throw new FormatException("Unknown vanguard type " + vgType);
        }

        //private string GetNameForTicker(string ticker)
        //{
        //    switch (ticker)
        //    {
        //        case "VMFXX": return "Vanguard Federal Money Market Fund Investor Shares";
        //        case "BND": return "VANGUARD TOTAL BOND MARKET ETF";
        //        case "BNDX": return "VANGUARD TOTAL INTL BOND INDEX ETF";
        //        case "VNQ": return "VANGUARD REIT INDEX ETF";
        //        case "VXUS": return "VANGUARD TOTAL INTL STOCK INDEX FUND ETF";
        //        case "VTI": return "VANGUARD TOTAL STOCK MARKET ETF";
        //        case "VGIT": return "Vanguard Intermediate-Term Treasury Index Fund ETF Shares";
        //    }
        //    throw new FormatException("Unknown ticker " + ticker);
        //}

        class VanguardTransaction
        {
            public VanguardTransaction(string date, string ticker, EInvestmentTransactionType type, decimal quantity, decimal price, decimal amount, string memo = null) =>
                (Date, Ticker, Type, Quantity, Price, Amount, Memo) = (date, ticker, type, quantity, price, amount, memo);
            public readonly string Date;
            public readonly string Ticker;
            public readonly EInvestmentTransactionType Type;
            public readonly decimal Quantity;
            public readonly decimal Price;
            public readonly decimal Amount;
            public readonly string Memo;
        }

        #endregion

        #region Chase CC analyzer

        private void AnalyzeChaseTransactions(PdfData data, Household.AccountRow account, Household db)
        {
            var trans = new List<CCTransaction>();

            string year = null; ;
            {
                var strs = data.ExtractTextFromPage(0);
                foreach (var str in strs)
                {
                    int ix = str.IndexOf(", monthly transaction statement");
                    if (ix > 4)
                    {
                        year = "/" + str.Substring(ix - 4, 4);
                        break;
                    }
                }
            }

            // Start at page 3 "Transaction Description", stop at "Annual Percentage Rate"
            bool foundTransactionDescription = false;
            bool foundEnd = false;

            for (int page = 2; page < data.NumberOfPages && !foundEnd; page++)
            {
                // get the strings on page
                var strs = data.ExtractTextFromPage(page);

                // Eliminate empty strings
                var list = new List<string>();
                foreach (var str in strs)
                {
                    if (!String.IsNullOrWhiteSpace(str))
                    {
                        list.Add(str);
                    }
                }
                strs = list.ToArray();

                // Fill in the year the first time around
                if (year == null)
                {
                    for (int i = 0; i < strs.Length; i++)
                    {
                        if (strs[i].StartsWith("Statement Date:"))
                        {
                            string statementDate = strs[i + 1];
                            var split = statementDate.Split(new char[] { '/' });
                            year = "/20" + split[split.Length - 1];
                        }
                    }
                }

                for (int i = 0; i < strs.Length; i++)
                {
                    if (!foundTransactionDescription)
                    {
                        if (strs[i].Contains("Transaction Description"))
                        {
                            foundTransactionDescription = true;
                        }
                        continue;
                    }

                    if (strs[i].Contains("Annual Percentage Rate (APR)"))
                    {
                        // Done
                        foundEnd = true;
                        break;
                    }

                    // We are after "Transaction Description" and before "Annual Percentage Rate (APR)"
                    if (strs[i].StartsWith("Payment Thank You"))
                    {
                        var cct = new CCTransaction(strs[i - 1].Trim() + year, "Payment", null, null, -decimal.Parse(strs[i + 1]));
                        trans.Add(cct);
                    }
                    else if (strs[i].StartsWith("Order Number"))
                    {
                        var split = strs[i].Split(new char[] { ' ', '\t' });
                        var orderNumber = split[split.Length - 1];
                        var cct = new CCTransaction(strs[i - 3].Trim() + year, "Amazon", orderNumber, null, -decimal.Parse(strs[i - 1]));
                        trans.Add(cct);
                    }
                }
            }

            // ZZZ Debug
            foreach (var cct in trans)
            {
                LogLine($"{cct.Date}\t{cct.Vendor}\t{cct.Comment}\t{cct.Category}\t{cct.Amount}");
            }
            LogLine("");

            // Add to db
            var checkpointRow = db.Checkpoint.GetCurrentCheckpoint();
            foreach (var cct in trans)
            {
                var transRow = db.Transaction.Add(account, DateTime.Parse(cct.Date), cct.Vendor, cct.Comment, ETransactionStatus.Pending, checkpointRow, ETransactionType.Regular);
                db.LineItem.Add(transRow, null, cct.Amount);
            }
        }

        private class CCTransaction
        {
            public CCTransaction(string date, string vendor, string comment, string category, decimal amount) =>
                (Date, Vendor, Comment, Category, Amount) = (date, vendor, comment, category, amount);
            public readonly string Date;
            public readonly string Vendor;
            public readonly string Comment;
            public readonly string Category;
            public readonly decimal Amount;
        }

        #endregion

        #region Private utilities

        private static readonly string eol = Environment.NewLine;

        private void LogLine(string str)
        {
            Log += str + eol;
        }

        private string RemoveDollarSign(string str) => str[0] == '$' ? str.Substring(1) : str;

        private string RemoveNegativeSign(string str) => str[0] == '-' ? str.Substring(1) : str;

        #endregion
    }
}
