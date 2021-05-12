using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs
{
    public class OpenFileLogic : LogicBase
    {
        public OpenFileLogic(string lastFile, string extensions)
        {
            File = lastFile;
            Extensions = extensions;
        }

        public string File { get; set; }
        public string Extensions { get; }
    }
}
