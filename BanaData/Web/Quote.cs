using System;
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
            // We support only the yahoo finance API as of today
            return YahooFinanceAPI.GetQuote(symbols);
        }
    }
}
