using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Listers
{
    public class ListSecurityPricesLogic : LogicDialogBase
    {
        private readonly MainWindowLogic mainWindowLogic;
        private readonly SecurityItem securityItem;

        public ListSecurityPricesLogic(MainWindowLogic mainWindowLogic, SecurityItem securityItem)
        {
            (this.mainWindowLogic, this.securityItem) = (mainWindowLogic, securityItem);

        }

        public string Title => $"{securityItem.Symbol} price history";

        protected override bool? Commit()
        {
            throw new NotImplementedException();
        }
    }
}
