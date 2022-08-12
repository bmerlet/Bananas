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

            var analyzer = new StatementAnalyzer();

            analyzer.AnalyzeStatement(data, @"C:\Users\bmerlet\Downloads"); // ZZZ do something about the direectory name
        }
    }
}
