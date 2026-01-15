using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Web
{
    static class Quote
    {
        static public decimal[] GetQuote(string[] symbols)
        {
            decimal[] results = new decimal[symbols.Length];
            for(int i = 0; i < results.Length; i++)
            {
                results[i] = -1;
            }

            //
            // State of the code:
            //   The yahoo finance API does not work anymnore,
            //   the Alphavantage API works but is slow,
            //   and the FinnHub API does not resolve the Capital Group fund names for some reason.
            //
            // Strategy:
            //   Get quotes through finnhub API. Then use Alphavantage to get the missing ones
            //

            // result = YahooFinanceAPI.GetQuotes(symbols);
            FinnHubAPI.GetQuote(symbols, results);
            AlphavantageAPI.GetQuote(symbols, results);

            return results;
        }
    }
}
