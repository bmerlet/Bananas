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
        public OpenFileLogic(string lastFile, string filter, string title, bool checkFileExists)
        {
            InitialDirectory = Path.GetDirectoryName(lastFile);
            File = Path.GetFileName(lastFile);
            Filter = filter;
            Title = title;
            CheckFileExists = checkFileExists;
        }

        public string InitialDirectory { get; }
        public string Filter { get; }
        public string File { get; set; }
        public string Title { get; }
        public bool CheckFileExists { get; }
    }
}
