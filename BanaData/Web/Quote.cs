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
            // The yahoo finance API does not work anymnore, use the Alphavantage one
            return AlphavantageAPI.GetQuote(symbols);
        }
    }
}
