using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Web
{
    static internal class YahooFinanceAPI
    {
        const string QuoteQuery =
             "https://query1.finance.yahoo.com/v7/finance/quote?lang=en-US&region=US&corsDomain=finance.yahoo.com&symbols={0}&fields=regularMarketPrice&crumb={1}";

        // Cookie and crumbs if already been here
        static private Cookie cookie = null;
        static private string crumb = null;

        static public decimal[] GetQuote(string[] symbols)
        {
            // Deal with stupid input
            if (symbols.Length == 0)
            {
                return new decimal[0];
            }

            // Attempt to reuse cookie/crumb, fail silently
            if (cookie != null & crumb != null)
            {
                try
                {
                    return GetQuoteInternal(symbols);
                }
                catch (WebException)
                {
                    cookie = null; 
                    crumb = null;
                }
            }

            GetCookieAndCrumb(symbols[0]);

            return GetQuoteInternal(symbols);
        }
        static private decimal[] GetQuoteInternal(string[] symbols)
        {
            decimal[] result = new decimal[symbols.Length];

            // Form comma-separated list of symbols
            StringBuilder symbol = new StringBuilder();
            foreach (var s in symbols)
            {
                symbol.Append(s);
                symbol.Append(",");
            }
            symbol.Remove(symbol.Length - 1, 1);

            // Form URL
            var url = string.Format(QuoteQuery, symbol, crumb);

            // Create web request
            HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(url);
            webreq.Method = "GET";
            webreq.CookieContainer = new CookieContainer();
            webreq.CookieContainer.Add(cookie);

            // Get the response from the yahoo finance API.
            HttpWebResponse webresp = (HttpWebResponse)webreq.GetResponse();

            // Read the body of the response from the server.
            var strm = new StreamReader(webresp.GetResponseStream(), Encoding.ASCII);
            var completeAnswer = strm.ReadToEnd();

            // Parse the JSON answer
            var parsedAnswer = JSONParser.FromJson<YahooResponse>(completeAnswer);

            for (int i = 0; i < symbols.Length; i++)
            {
                var qu = parsedAnswer.quoteResponse.result.FirstOrDefault(r => r.symbol == symbols[i]);
                result[i] = qu == null ? 0 : qu.regularMarketPrice;
            }

            return result;
        }

        // Scrape output from a regular query to get a cookie and crumb
        static private void GetCookieAndCrumb(string symbol)
        {
            // Setup URL
            string mainURL = "https://finance.yahoo.com/quote/" + symbol + "/history";
            HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(mainURL);
            webreq.CookieContainer = new CookieContainer();

            // Needed otherwise the server does not return any cookies
            webreq.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";

            // Get response
            HttpWebResponse webresp = (HttpWebResponse)webreq.GetResponse();

            // Store first cookie
            cookie = webresp.Cookies[0];

            //foreach (Cookie cook in webresp.Cookies)
            //{
            //    Console.WriteLine($"{cook.Name} = {cook.Value}");
            //}

            // Read complete response to scrape for the crumb
            var strm = new StreamReader(webresp.GetResponseStream(), Encoding.ASCII);
            var completeAnswer = strm.ReadToEnd();

            int contextIx = completeAnswer.IndexOf("window.YAHOO.context =");
            int crumbIx = completeAnswer.IndexOf("\"crumb\"", contextIx);
            int startCrumbIx = completeAnswer.IndexOf('"', crumbIx + 9);
            int endCrumbIx = completeAnswer.IndexOf('"', startCrumbIx + 1);
            crumb = completeAnswer.Substring(startCrumbIx + 1, endCrumbIx - startCrumbIx - 1);

            //Console.WriteLine("Last crumb: " + crumb);
        }

        // JSON object model
        /*
         {"quoteResponse":{"result":
            [{"language":"en-US","region":"US","quoteType":"ETF","typeDisp":"ETF","quoteSourceName":"Nasdaq Real Time Price","triggerable":true,
              "customPriceAlertConfidence":"HIGH","marketState":"REGULAR","exchange":"PCX","exchangeTimezoneName":"America/New_York",
              "exchangeTimezoneShortName":"EDT","gmtOffSetMilliseconds":-14400000,"market":"us_market","esgPopulated":false,
              "regularMarketPrice":81.73,"sourceInterval":15,"exchangeDataDelayedBy":0,"tradeable":false,"cryptoTradeable":false,
              "firstTradeDateMilliseconds":1096464600000,"priceHint":2,"regularMarketTime":1683919417,"fullExchangeName":"NYSEArca","symbol":"VNQ"},
             {"language":"en-US","region":"US","quoteType":"ETF","typeDisp":"ETF","quoteSourceName":"Nasdaq Real Time Price","triggerable":true,
              "customPriceAlertConfidence":"HIGH","marketState":"REGULAR","exchange":"PCX","exchangeTimezoneName":"America/New_York",
              "exchangeTimezoneShortName":"EDT","gmtOffSetMilliseconds":-14400000,"market":"us_market","esgPopulated":false,
              "regularMarketPrice":203.51,"sourceInterval":15,"exchangeDataDelayedBy":0,"tradeable":false,"cryptoTradeable":false,
              "firstTradeDateMilliseconds":992611800000,"priceHint":2,"regularMarketTime":1683919420,"fullExchangeName":"NYSEArca","symbol":"VTI"}]
            ,"error":null}}
         */
#pragma warning disable CS0649 // Suppress filed never assigned to warnings, the JSON parser does it through reflection
        internal class YahooResponse
        {
            public QuoteResponse quoteResponse;
        }

        internal class QuoteResponse
        {
            public List<QuoteResult> result;
            public string error;
        }

        internal class QuoteResult
        {
            public string language;
            public string region;
            public string quoteType;
            public string typeDisp;
            public string quoteSourceName;
            public bool triggerable;
            public string customPriceAlertConfidence;
            public string marketState;
            public string exchange;
            public string exchangeTimezoneName;
            public string exchangeTimezoneShortName;
            public long gmtOffSetMilliseconds;
            public string market;
            public bool esgPopulated;
            public decimal regularMarketPrice;
            public int sourceInterval;
            public int exchangeDataDelayedBy;
            public bool tradeable;
            public bool cryptoTradeable;
            public long firstTradeDateMilliseconds;
            public int priceHint;
            public long regularMarketTime;
            public string fullExchangeName;
            public string symbol;
        }
#pragma warning restore CS0649
    }
}
