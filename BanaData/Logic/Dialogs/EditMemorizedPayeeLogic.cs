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
    /// <summary>
    /// Logic to edit a specific memorized payees
    /// </summary>
    public class EditMemorizedPayeeLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly int id;
        private readonly bool add;

        #endregion

        #region Constructor

        public EditMemorizedPayeeLogic(MainWindowLogic _mainWindowLogic, int _id, bool _add)
        {
            mainWindowLogic = _mainWindowLogic;
            id = _id;
            add = _add;

            var household = mainWindowLogic.Household;
            Household.MemorizedPayeesRow payee = null;
            Household.MemorizedLineItemsRow[] lineItems = new Household.MemorizedLineItemsRow[0];
            if (!add)
            {
                payee = household.MemorizedPayees.FindByID(id);
                lineItems = household.MemorizedLineItems.GetByMemorizedPayee(payee);
            }

            Name = add ? "" : payee.Payee;
            Status = add ? ETransactionStatus.Pending : payee.Status;
        }

        #endregion

        #region UI properties

        // Name of payee
        public string Name { get; set; }

        // Status of the transaction
        public ETransactionStatus Status { get; set; }

        // Line items

        // MemorizedLineItemsRow
        // Per line item:
        // AccountID (for tx)
        // Amount
        // Category
        // Memo

        #endregion

        #region Actions

        private void BuildMemorizedPayeesList()
        {
            var household = mainWindowLogic.Household;

            foreach (var mpr in household.MemorizedPayees)
            {
                // Get memorized line item(s)
                var lineItems = household.MemorizedLineItems.GetByMemorizedPayee(mpr);
                decimal amount = lineItems.Sum(li => li.Amount);

                string category = "";

                if (lineItems.Length > 1)
                {
                    category = "<Split>";
                }
                else if (!lineItems[0].IsCategoryIDNull())
                {
                    var destCategory = household.Categories.FindByID(lineItems[0].CategoryID);
                    category = destCategory.FullName;
                }

                //var mpi = new MemorizedPayeeItem(mpr.ID, mpr.Payee, amount, category);
                //memorizedPayeesSource.Add(mpi);
            }
        }

        protected override bool? Commit()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Line item class

        public class LineItem
        {

            // UI properties
            public string CategoryStr { get; set; }
            public string Memo { get; set; }
            public decimal Amount { get; set; }

        }

        #endregion
    }
}
