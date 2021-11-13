using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Editors
{
    public class RenamePayeeLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public RenamePayeeLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            DateRange = new DateRangeLogic(DateRangeLogic.ERange.AllAvailable,
                () => mainWindowLogic.Household.RegularTransactions.Select(tr => tr.Date).Min());

            // Copy to avoid filter interaction issues with the open register
            MemorizedPayees = new List<MemorizedPayeeItem>(mainWindowLogic.MemorizedPayees);
        }

        #endregion

        #region UI properties

        // Existing payee 
        public string ExistingPayeeName { get; set; } = "";

        // Known payees
        public List<MemorizedPayeeItem> MemorizedPayees { get; }

        // New payee name
        public string NewPayeeName { get; set; } = "";

        // Date range
        public DateRangeLogic DateRange { get; }

        // Also rename in memorized payees
        public bool? ChangeMemorizedPayees { get; set; } = true;

        #endregion

        #region Actions

        protected override bool? Commit()
        {
            if (string.IsNullOrWhiteSpace(ExistingPayeeName))
            {
                mainWindowLogic.ErrorMessage("Existing payee name cannot be blank");
                return null;
            }

            if (string.IsNullOrWhiteSpace(NewPayeeName))
            {
                mainWindowLogic.ErrorMessage("New payee name cannot be blank");
                return null;
            }

            var household = mainWindowLogic.Household;
            bool change = false;

            foreach (var transactionRow in household.RegularTransactions.Where(tr =>
                tr.Date >= DateRange.StartDate && tr.Date <= DateRange.EndDate && 
                !tr.IsPayeeNull() && tr.Payee.Equals(ExistingPayeeName, StringComparison.InvariantCultureIgnoreCase)))
            {
                change = true;
                transactionRow.Payee = NewPayeeName;
            }

            if (ChangeMemorizedPayees == true)
            {
                foreach (var memorizedPayeeRow in household.MemorizedPayees.Where(mp =>
                    !mp.IsPayeeNull() && mp.Payee.Equals(ExistingPayeeName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    change = true;
                    memorizedPayeeRow.Payee = NewPayeeName;
                }
            }

            if (change)
            {
                mainWindowLogic.CommitChanges();

                // ZZZZZ Update registers

                if (ChangeMemorizedPayees == true)
                {
                    mainWindowLogic.UpdateMemorizedPayees();
                }
            }

            return change;
        }

        #endregion
    }
}
