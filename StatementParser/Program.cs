using System;
using System.Collections.Generic;
using PdfParser;

namespace StatementParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new PdfParser.PdfParser();
            var data = parser.Parse(@"C:\Users\bmerlet\Downloads\statement.pdf");

            for(int i = 0; i < data.NumberOfPages; i++)
            {
                Console.WriteLine($"================================= Page {i + 1} ==================================");
                var strs = data.ExtractTextFromPage(i);

                foreach(var str in strs)
                {
                    Console.WriteLine(str);
                }
            }

            Console.WriteLine($"Banana account name: {DetermineAccount(data)}");

            Console.WriteLine("\n=== Holdings:\n");
            foreach(var hint in vanguardBrokerageHodingHints)
            {
                var holding = DetermineHolding(data, hint);
                if (holding != null)
                {
                    Console.WriteLine($"{hint.Ticker}  \t{holding}");
                }
            }

            Console.WriteLine("\n=== Transactions:\n");

        }

        static string DetermineAccount(PdfData data)
        {
            string result = null;

            foreach (var spec in accountSpecsVanguard)
            {
                bool pass = true;
                foreach(var hint in spec.AccountHints)
                {
                    for(int page = hint.MinPage - 1; page <= hint.MaxPage - 1; page++)
                    {
                        var strs = data.ExtractTextFromPage(page);
                        foreach(var required in hint.Required)
                        {
                            bool found = false;
                            foreach(var str in strs)
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

        static AccountSpec[] accountSpecsVanguard = new AccountSpec[]
        { 
            new AccountSpec("SVanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Susan", "brokerage" }) }),
            new AccountSpec("BVanguard Brokerage", new AccountHint[] { new AccountHint(2, 2, new string[] { "Vanguard", "Benoit", "brokerage" }) }),
        };

        static HoldingHint[] vanguardBrokerageHodingHints = new HoldingHint[]
        {
            new HoldingHint(4, 5, "VANGUARD FEDERAL MONEY", -4, "VMFXX"),
            new HoldingHint(4, 5, "VNQ", 2, "VNQ"),
            new HoldingHint(4, 5, "VGIT", 2, "VGIT"),
            new HoldingHint(4, 5, "BND", 2, "BND"),
            new HoldingHint(4, 5, "VXUS", 2, "VXUS"),
            new HoldingHint(4, 5, "VTI", 2, "VTI"),
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

        static string DetermineHolding(PdfData data, HoldingHint hint)
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
    }
}
