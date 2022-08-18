using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PdfParser;

namespace StatementParser
{
    class StatementAnalyzer
    {
        #region Account descriptions

        public enum EInstitution { None, Vanguard, Chase }

        static readonly AccountSpec[] accountSpecs = new AccountSpec[]
        {
            new AccountSpec(EInstitution.Vanguard, "SVanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Susan", "brokerage" }) }),
            new AccountSpec(EInstitution.Vanguard, "BVanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Benoit", "brokerage" }) }),
            new AccountSpec(EInstitution.Chase, "SAmazon XX6555", new AccountHint[] { new AccountHint(1, 1, new string[] { "Amazon Customer Service" }) }),
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

        static (EInstitution, string) DetermineAccount(PdfData data)
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

        #region Statement analyzer

        public void AnalyzeStatement(PdfData data, string qifFileName)
        {
            (EInstitution institution, string bananaAccountName) = DetermineAccount(data);

            Console.WriteLine($"Institution: {institution}");
            Console.WriteLine($"Banana account name: {bananaAccountName}");

            if (institution == EInstitution.Vanguard)
            {
                Console.WriteLine("\n=== Holdings:\n");
                foreach (var hint in vanguardBrokerageHodingHints)
                {
                    var holding = DetermineHolding(data, hint);
                    if (holding != null)
                    {
                        Console.WriteLine($"{hint.Ticker}  \t{holding}");
                    }
                }
            }

            Console.WriteLine("\n=== Transactions:\n");
            if (institution == EInstitution.Vanguard)
            {
                AnalyzeVanguardTransactions(data, bananaAccountName, qifFileName);
            }
            else if (institution == EInstitution.Chase)
            {
                AnalyzeChaseTransactions(data, bananaAccountName, qifFileName);
            }
        }

        #endregion
        
        #region Vanguard analyzer

        private void AnalyzeVanguardTransactions(PdfData data, string accountName, string qifFileName)
        {
            var trans = new List<VanguardTransaction>();

            // Find the year - First page: July 31, 2022, monthly transaction statement
            string year = "/2100";
            {
                var strs = data.ExtractTextFromPage(0);
                foreach(var str in strs)
                {
                    int ix = str.IndexOf(", monthly transaction statement");
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

                for(int i = 0; i < strs.Length; i++)
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
                    if (strs[i] == "VANGUARD FEDERAL MONEY" && strs[i + 1] == "Reinvestment")
                    {
                        var vg = new VanguardTransaction(
                            strs[i - 3] + year,
                            "VMFXX", 
                            strs[i + 1],
                            -decimal.Parse(strs[i + 6]),
                            1,
                            -decimal.Parse(strs[i + 6]));
                        trans.Add(vg);
                    }
                    else
                    {
                        foreach (var ticker in new string[] { "BND", "BNDX", "VGIT", "VXUS", "VTI" })
                        {
                            if (strs[i] == ticker)
                            {
                                // Found transaction
                                var quantity = strs[i + 4];
                                var price = RemoveDollarSign(strs[i + 5]);
                                var amount = RemoveNegativeSign(RemoveDollarSign(strs[i + 7]));
                                var vg = new VanguardTransaction(
                                    strs[i - 2] + year,
                                    ticker,
                                    strs[i + 2],
                                    quantity == "-" ? 0 : decimal.Parse(quantity),
                                    price == "-" ? 0 : decimal.Parse(price),
                                    amount == "-" ? 0 : decimal.Parse(amount)
                                );
                                trans.Add(vg);
                            }
                        }
                    }
                }
            }

            // Remove extraneous transactions
            var tmpTrans = trans;
            trans = new List<VanguardTransaction>();
            for(int i = 0; i < tmpTrans.Count;i++)
            {
                if (i < tmpTrans.Count - 1 &&
                    tmpTrans[i].Ticker == tmpTrans[i+1].Ticker &&
                    tmpTrans[i].Type == "Dividend" &&
                    tmpTrans[i+1].Type == "Reinvestment" &&
                    tmpTrans[i].Amount == tmpTrans[i+1].Amount)
                {
                    continue;
                }
                trans.Add(tmpTrans[i]);
            }

            // ZZZ Debug
            foreach (var vg in trans)
            {
                Console.WriteLine($"{vg.Date}\t{vg.Ticker}\t{vg.Type}\t{vg.Quantity}\t{vg.Price}\t{vg.Amount}");
            }

            // Produce QIF output
            var eol = Environment.NewLine;
            string qif =
                "!Option:AutoSwitch" + eol +
                "!Account" + eol +
                "N" + accountName + eol +
                "TInvst" + eol +
                "^" + eol +
                "!Type:Invst" + eol;

            foreach (var vg in trans)
            {
                var amount = vg.Amount.ToString();
                qif +=
                    "D" + vg.Date + eol +
                    "N" + GetVanguardQIFType(vg.Type) + eol +
                    "Y" + GetNameForTicker(vg.Ticker) + eol +
                    $"I{vg.Price}" + eol +
                    $"Q{vg.Quantity}" + eol +
                    "C" + eol +
                    "U" + amount + eol +
                    "T" + amount + eol +
                    "$" + amount + eol +
                    "^" + eol;
            }

            File.AppendAllText(qifFileName, qif);
        }

        private string GetVanguardQIFType(string vgType)
        {
            switch(vgType)
            {
                case "Dividend": return "Div";
                case "Reinvestment": return "ReinvDiv";
            }
            throw new FormatException("Unknown vanguard type " + vgType);
        }

        private string GetNameForTicker(string ticker)
        {
            switch (ticker)
            {
                case "VMFXX": return "Vanguard Federal Money Market Fund Investor Shares";
                case "BND": return "VANGUARD TOTAL BOND MARKET ETF";
                case "BNDX": return "VANGUARD TOTAL INTL BOND INDEX ETF";
                case "VNQ": return "VANGUARD REIT INDEX ETF";
                case "VXUS": return "VANGUARD TOTAL INTL STOCK INDEX FUND ETF";
                case "VTI": return "VANGUARD TOTAL STOCK MARKET ETF";
                case "VGIT": return "Vanguard Intermediate-Term Treasury Index Fund ETF Shares";
            }
            throw new FormatException("Unknown ticker " + ticker);
        }

        private string RemoveDollarSign(string str) => str[0] == '$' ? str.Substring(1) : str;

        private string RemoveNegativeSign(string str) => str[0] == '-' ? str.Substring(1) : str;

        class VanguardTransaction
        {
            public VanguardTransaction(string date, string ticker, string type, decimal quantity, decimal price, decimal amount) =>
                (Date, Ticker, Type, Quantity, Price, Amount) = (date, ticker, type, quantity, price, amount);
            public readonly string Date;
            public readonly string Ticker;
            public readonly string Type;
            public readonly decimal Quantity;
            public readonly decimal Price;
            public readonly decimal Amount;
        }

        #endregion

        #region Chase CC analyzer

        private void AnalyzeChaseTransactions(PdfData data, string accountName, string qifFileName)
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
                foreach(var str in strs)
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
                        var cct = new CCTransaction(strs[i - 1].Trim() + year, "Payment", null, null, decimal.Parse(strs[i + 1]));
                        trans.Add(cct);
                    }
                    else if (strs[i].StartsWith("Order Number"))
                    {
                        var split = strs[i].Split(new char[] { ' ', '\t' });
                        var orderNumber = split[split.Length - 1];
                        var cct = new CCTransaction(strs[i - 3].Trim() + year, "Amazon", orderNumber, null, decimal.Parse(strs[i + -1]));
                        trans.Add(cct);
                    }
                }
            }

            // ZZZ Debug
            foreach (var cct in trans)
            {
                Console.WriteLine($"{cct.Date}\t{cct.Vendor}\t{cct.Comment}\t{cct.Category}\t{cct.Amount}");
            }
            Console.WriteLine();

            // Produce QIF output
            var eol = Environment.NewLine;
            string qif =
                "!Option:AutoSwitch" + eol +
                "!Account" + eol +
                "N" + accountName + eol +
                "TCCard" + eol +
                "^" + eol +
                "!Type:CCard" + eol;

            foreach (var cct in trans)
            {
                var amount = (-cct.Amount).ToString("N2");
                var transqif =
                    "D" + cct.Date + eol +
                    "U" + amount + eol +
                    "T" + amount + eol +
                    "C" + eol +
                    "P" + (cct.Vendor == null ? "" : cct.Vendor) + eol +
                    "M" + (cct.Comment == null ? "" : cct.Comment) + eol +
                    "L" + (cct.Category == null ? "" : cct.Category) + eol +
                    "^" + eol;

                qif += transqif;
            }

            File.AppendAllText(qifFileName, qif);

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
    }
}
