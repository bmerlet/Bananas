using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    public class OfxDocument
    {
        public readonly OfxHeader OfxHeader;
        public readonly SgmlAggregate Sgml;

        public OfxDocument(OfxHeader ofxHeader, SgmlAggregate sgml)
        {
            OfxHeader = ofxHeader;
            Sgml = sgml;
        }
    }

    public class OfxHeader
    {
        public readonly int Version;

        public OfxHeader(int version)
        {
            Version = version;
        }
    }
}
