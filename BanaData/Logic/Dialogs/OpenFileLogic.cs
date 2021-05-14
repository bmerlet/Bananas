using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs
{
    public class OpenFileLogic : LogicBase
    {
        public OpenFileLogic(string lastFile, string filter)
        {
            InitialDirectory = Path.GetDirectoryName(lastFile);
            File = Path.GetFileName(lastFile);
            Filter = filter;
        }

        public string InitialDirectory { get; }
        public string File { get; set; }
        public string Filter { get; }
    }
}
