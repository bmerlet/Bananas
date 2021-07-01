using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Logic.Main;

namespace BanaData.Logic.Dialogs
{
    public class ShowYearlyCapGainsAndDividendsLogic : LogicBase
    {
        private readonly MainWindowLogic mainWindowLogic;

        public ShowYearlyCapGainsAndDividendsLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;
        }


    }
}
