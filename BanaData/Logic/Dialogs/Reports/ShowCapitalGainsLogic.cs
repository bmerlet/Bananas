using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Logic.Main;
using BanaData.Database;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowCapitalGainsLogic : LogicBase
    {
        private readonly Household household;

        public ShowCapitalGainsLogic(Household _household, int transactionID)
        {
            household = _household;

            BuildCapitalGainsInfo(transactionID);
        }

        private void BuildCapitalGainsInfo(int transactionID)
        {
            var sale = Portfolio.ComputeSaleCapitalGains(household, transactionID, true);

            Description = sale.Description;
            LongTermLots = sale.LongTermLots;
            LongTermGain = sale.LongTermGain;
            ShortTermLots = sale.ShortTermLots;
            ShortTermGain = sale.ShortTermGain;
        }

        public string Description { get; private set; }

        public decimal LongTermGain { get; private set; }

        public IEnumerable<UsedLot> LongTermLots { get; private set; }

        public decimal ShortTermGain { get; private set; }

        public IEnumerable<UsedLot> ShortTermLots { get; private set; }

    }
}
