using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Web
{
    class Quote
    {
        const string QuoteQuery =
             "https://query1.finance.yahoo.com/v7/finance/quote?lang=en-US&region=US&corsDomain=finance.yahoo.com&symbols={0}&fields=regularMarketPrice";
        const string AnswerMarker = "regularMarketPrice\":";

        public decimal GetQuote(string symbol)
        {
            decimal result = -1;

            var url = string.Format(QuoteQuery, symbol);

            // Create web request
            HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(url);

            // Get the response from the Internet resource.
            HttpWebResponse webresp = (HttpWebResponse)webreq.GetResponse();

            // Read the body of the response from the server.
            var strm = new StreamReader(webresp.GetResponseStream(), Encoding.ASCII);

            var completeAnswer = strm.ReadToEnd();

            int start = completeAnswer.IndexOf(AnswerMarker);
            if (start >= 0)
            {
                start += AnswerMarker.Length;
                int end = completeAnswer.IndexOf(',', start);
                if (end > start)
                {
                    var num = completeAnswer.Substring(start, end - start);
                    decimal.TryParse(num, out result);
                }
            }

            return result;
        }
    }
}
