using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowQuoteUpdateLogic : LogicBase
    {
        public ShowQuoteUpdateLogic(int numUpdates, decimal oldNetWorth, decimal newNetWorth)
            => (NumUpdatesText, OldNetWorth, NewNetWorth) = ($"Updated {numUpdates} quotes", oldNetWorth, newNetWorth);

        public string NumUpdatesText { get; }
        public decimal OldNetWorth { get; }
        public decimal NewNetWorth { get; }
        public decimal Difference => NewNetWorth - OldNetWorth;
    }
}
