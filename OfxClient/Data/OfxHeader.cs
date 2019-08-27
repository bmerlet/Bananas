using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfxClient.Data
{
    /// <summary>
    /// Immutable class representing an OFX header
    /// </summary>
    public class OfxHeader
    {
        public readonly int Version;

        public OfxHeader(int version)
        {
            Version = version;
        }
    }
}
