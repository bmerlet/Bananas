using System;
using System.Collections.Generic;
using System.IO;
using PdfParser;

namespace StatementParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            directory = Path.Combine(directory, "StatementParser");

            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Error: Directory {directory} does not exist");
                Explain(directory);
            }
            else
            {
                var files = new List<string>();
                files.AddRange(Directory.GetFiles(directory, "*.pdf"));
                if (files.Count == 0)
                {
                    Console.WriteLine($"Error: Directory {directory} does not contain any .pdf file");
                    Explain(directory);
                }
                else
                {
                    var analyzer = new StatementAnalyzer();
                    var targetFile = Path.Combine(directory, "StatementParser.QIF");
                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                    }

                    foreach (var f in files)
                    {
                        Console.WriteLine($"=== Processing {f}");
                        var data = new PdfData(f);
                        data.Parse();

                        // Debug
                        for (int i = 0; i < data.NumberOfPages; i++)
                        {
                            Console.WriteLine($"================================= Page {i + 1} ==================================");
                            var strs = data.ExtractTextFromPage(i);

                            //foreach (var str in strs)
                            //{
                            //    Console.WriteLine(str);
                            //}
                        }

                        analyzer.AnalyzeStatement(data, targetFile);
                    }
                }
            }
        }

        private static void Explain(string directory)
        {
            Console.WriteLine("");
            Console.WriteLine($"Put the statement(s) you want to parse in {directory}.");
            Console.WriteLine("They must be in pdf format.");
            Console.WriteLine("The program will parse all of them and produce a StatementParser.QIF file");
            Console.WriteLine("that can then be imported into Bananas (or Quicken)");
            Console.WriteLine("");
        }
    }
}
