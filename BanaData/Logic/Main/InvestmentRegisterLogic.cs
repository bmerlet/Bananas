using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;

namespace BanaData.Logic.Main
{
    public class InvestmentRegisterLogic : LogicBase
    {
        private MainWindowLogic mainWindowLogic;

        public InvestmentRegisterLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;
        }

        public CollectionView Transactions { get; }

        public void SetAccount(int id)
        {
            //throw new NotImplementedException();
        }

        public void RecomputeBalances()
        {
            //throw new NotImplementedException();
        }
    }
}
