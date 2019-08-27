using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfxClient.Data
{
    public class OfxDocument
    {
        public readonly string Error;
        public readonly OfxHeader OfxHeader;
        public readonly SgmlAggregate Sgml;

        public OfxDocument(string error, OfxHeader ofxHeader, SgmlAggregate sgml)
        {
            Error = error;
            OfxHeader = ofxHeader;
            Sgml = sgml;
        }
    }
}
