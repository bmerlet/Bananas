using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Database;

namespace BanaData.Logic.Dialogs
{
    public class ReconcileLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly int accountID;

        #endregion

        #region Constructor

        public ReconcileLogic(MainWindowLogic _mainWindowLogic, int _accountID)
        {
            (mainWindowLogic, accountID) = (_mainWindowLogic, _accountID);

            // Get DB
            var household = mainWindowLogic.Household;

            // Get account info
            var accountRow = household.Accounts.FindByID(accountID);

            // ZZZZZZZ
        }

        #endregion

        #region UI properties

        #endregion

        #region Actions

        protected override bool? Commit()
        {
            return true;
        }

        #endregion
    }
}
