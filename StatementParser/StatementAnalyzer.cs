using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PdfParser;

namespace StatementParser
{
    class StatementAnalyzer
    {
        #region Account descriptions

        static AccountSpec[] accountSpecsVanguard = new AccountSpec[]
        {
            new AccountSpec("SVanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Susan", "brokerage" }) }),
            new AccountSpec("BVanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Benoit", "brokerage" }) }),
        };

        // Account
        class AccountSpec
        {
            public AccountSpec(string name, AccountHint[] hints) => (BananaAccountName, AccountHints) = (name, hints);
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

        static string DetermineAccount(PdfData data)
        {
            string result = null;

            foreach (var spec in accountSpecsVanguard)
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
                    result = spec.BananaAccountName;
                    break;
                }
            }

            return result;
        }

        #endregion

        #region Holdings description

        static HoldingHint[] vanguardBrokerageHodingHints = new HoldingHint[]
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

        public void AnalyzeStatement(PdfData data, string directory)
        {
            string bananaAccountName = DetermineAccount(data);
            Console.WriteLine($"Banana account name: {bananaAccountName}");

            Console.WriteLine("\n=== Holdings:\n");
            foreach (var hint in vanguardBrokerageHodingHints)
            {
                var holding = DetermineHolding(data, hint);
                if (holding != null)
                {
                    Console.WriteLine($"{hint.Ticker}  \t{holding}");
                }
            }

            Console.WriteLine("\n=== Transactions:\n");
            AnalizeVanguardTransactions(data, bananaAccountName, directory);
        }

        private void AnalizeVanguardTransactions(PdfData data, string accountName, string directory)
        {
            var trans = new List<VanguardTransaction>();

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
                        var vg = new VanguardTransaction()
                        {
                            Date = strs[i - 3],
                            Ticker = "VFMXX",
                            Type = strs[i + 1],
                            Quantity = -decimal.Parse(strs[i + 6]),
                            Price = 1,
                            Amount = -decimal.Parse(strs[i + 6])
                        };
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
                                var amount = RemoveDollarSign(strs[i + 7]);
                                var vg = new VanguardTransaction()
                                {
                                    Date = strs[i - 2],
                                    Ticker = ticker,
                                    Type = strs[i + 2],
                                    Quantity = quantity == "-" ? 0 : decimal.Parse(quantity),
                                    Price = price == "-" ? 0 : decimal.Parse(price),
                                    Amount = amount == "-" ? 0 : decimal.Parse(amount)
                                };
                                trans.Add(vg);
                            }
                        }
                    }
                }
            }

            foreach(var vg in trans)
            {
                // ZZZ
                Console.WriteLine($"{vg.Date}\t{vg.Ticker}\t{vg.Type}\t{vg.Quantity}\t{vg.Price}\t{vg.Amount}");
            }
        }

        private string RemoveDollarSign(string str)
        {
            return str[0] == '$' ? str.Substring(1) : str;
        }

        class VanguardTransaction
        {
            public string Date;
            public string Ticker;
            public string Type;
            public decimal Quantity;
            public decimal Price;
            public decimal Amount;
        }

        #endregion
    }
}
