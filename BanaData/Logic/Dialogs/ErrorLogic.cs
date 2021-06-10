using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs
{
    public class ErrorLogic : LogicBase
    {
        public ErrorLogic(string error) => Error = error;

        public string Error { get; }
    }
}
