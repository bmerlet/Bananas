using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BanaData.Web
{
    //
    // Get quotes based on FinnHub API (finnhub.io)
    // Registered as b@y !LP
    // My free key: d5kge0hr01qitje4i2ggd5kge0hr01qitje4i2h0
    // My secret: d5kge0hr01qitje4i2i0
    //
    // Sample query and answer:
    //https://finnhub.io/api/v1/quote?symbol=IBM&token=d5kge0hr01qitje4i2ggd5kge0hr01qitje4i2h0
    //{
    //  "c": 261.74, <- Current price
    //  "h": 263.31,
    //  "l": 260.68,
    //  "o": 261.07,
    //  "pc": 259.45,
    //  "t": 1582641000 
    //}
    //

    static internal class FinnHubAPI
    {

        const string KEY = "d5kge0hr01qitje4i2ggd5kge0hr01qitje4i2h0";
        // XXX "X-Finnhub-Secret": "d5kge0hr01qitje4i2i0" 
        const string QuoteQuery =
             "https://finnhub.io/api/v1/quote?symbol={0}&token={1}";

        static public void GetQuote(string[] symbols, decimal[] result)
        {
            for (int i = 0; i < symbols.Length; i++)
            {
                if (result[i] >= 0)
                {
                    continue;
                }

                result[i] = GetOneQuote(symbols[i], KEY);
            }
        }

        static public decimal GetOneQuote(string symbol, string key)
        {
            // Form URL
            var url = string.Format(QuoteQuery, symbol, key);

            // Create web request
            HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(url);
            webreq.Method = "GET";
            HttpWebResponse webresp = null;

            // Get the response from the finance API.
            try
            {
                webresp = (HttpWebResponse)webreq.GetResponse();
            }
            catch (System.Net.WebException e)
            {
                return -1;
            }

            // Read the body of the response from the server.
            var strm = new StreamReader(webresp.GetResponseStream(), Encoding.ASCII);
            var completeAnswer = strm.ReadToEnd();

            // Parse the JSON answer
            string PRICE_KEYWORD = "\"c\":";
            int ix = completeAnswer.IndexOf(PRICE_KEYWORD);
            if (ix >= 0)
            {
                string str = completeAnswer.Substring(ix + PRICE_KEYWORD.Length);
                str.Trim();
                int quoteiX = str.IndexOf(",");
                if (quoteiX >= 0)
                {
                    str = str.Substring(0, quoteiX);
                    decimal result = decimal.Parse(str);
                    return result;
                }
                throw new InvalidDataException("Can't parse FinnHub answer: " + completeAnswer);
            }
            return -1;
        }
    }
}
