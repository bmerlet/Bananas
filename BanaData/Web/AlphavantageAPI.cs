using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using static BanaData.Web.YahooFinanceAPI;

namespace BanaData.Web
{
    //
    // Get quotes based on Alphavantage API (www.alphavantage.co)
    // My free key: F0LIOHHMLTUHPC5K
    // My alternate free key: AU82WV5G96O55250
    // Sample query and answer:
    //https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=IBM&apikey=F0LIOHHMLTUHPC5K
    //{
    //    "Global Quote": {
    //        "01. symbol": "IBM",
    //        "02. open": "167.4000",
    //        "03. high": "168.2200",
    //        "04. low": "166.2250",
    //        "05. price": "167.4300",
    //        "06. volume": "5263342",
    //        "07. latest trading day": "2024-04-29",
    //        "08. previous close": "167.1300",
    //        "09. change": "0.3000",
    //        "10. change percent": "0.1795%"
    //    }
    //}
    //
    static internal class AlphavantageAPI
    {
        const string KEY1 = "F0LIOHHMLTUHPC5K";
        const string KEY2 = "AU82WV5G96O55250";
        const string QuoteQuery =
             "https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={0}&apikey={1}";

        static public void GetQuote(string[] symbols, decimal[] result)
        {
            string curKey = KEY1;

            for(int i = 0; i < symbols.Length; i++)
            {
                if (result[i] >= 0)
                {
                    continue;
                }

                decimal st = GetOneQuote(symbols[i], curKey);
                if (st < 0 && curKey == KEY1)
                {
                    curKey = KEY2;
                    st = GetOneQuote(symbols[i], curKey);
                }
                result[i] = st;
                System.Threading.Thread.Sleep(1100); // ZZZ No more than 1 queries a second says support
                // System.Threading.Thread.Sleep(6100); // ZZZ No more than 10 queries a minute says support
            }
        }

        static public decimal GetOneQuote(string symbol, string key)
        {
            // Form URL
            var url = string.Format(QuoteQuery, symbol, key);

            // Create web request
            HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(url);
            webreq.Method = "GET";

            // Get the response from the finance API.
            HttpWebResponse webresp = (HttpWebResponse)webreq.GetResponse();

            // Read the body of the response from the server.
            var strm = new StreamReader(webresp.GetResponseStream(), Encoding.ASCII);
            var completeAnswer = strm.ReadToEnd();

            // Parse the JSON answer
            string PRICE_KEYWORD = "\"05. price\":";
            int ix = completeAnswer.IndexOf(PRICE_KEYWORD);
            if (ix >= 0)
            {
                string str = completeAnswer.Substring(ix + PRICE_KEYWORD.Length + 1);
                str.Trim();
                str = str.Substring(1);
                int quoteiX = str.IndexOf("\"");
                if (quoteiX >= 0)
                {
                    str = str.Substring(0, quoteiX);
                    decimal result = decimal.Parse(str);
                    return result;
                }
                throw new InvalidDataException("Can't parse Alphavantage answer: " + completeAnswer);
            }
            return -1;
        }
    }
}
