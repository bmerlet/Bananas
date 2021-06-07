using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Database;
using BanaData.Logic.Items;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs
{
    /// <summary>
    /// Logic to edit a specific memorized payees
    /// </summary>
    public class EditMemorizedPayeeLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly MemorizedPayeeItem item;
        private readonly bool add;

        private const string DEPOSIT = "Deposit";
        private const string PAYMENT = "Payment";

        #endregion

        #region Constructor

        public EditMemorizedPayeeLogic(MainWindowLogic _mainWindowLogic, MemorizedPayeeItem _item, bool _add)
        {
            (mainWindowLogic, item, add) = (_mainWindowLogic, _item, _add);

            // Setup UI properties
            Payee = item.Payee;
            IsSplit = item.IsSplit;
            
            if (IsSplit == true)
            {
                Category = "<Split>";
                CategoryEnabled = false;
                Memo = "";
                MemoEnabled = false;
                TypeEnabled = false;
                AbsoluteAmountEnabled = false;
            }
            else
            {
                Category = item.Category;
                CategoryEnabled = true;
                Memo = item.Memo;
                MemoEnabled = true;
                TypeEnabled = true;
                AbsoluteAmountEnabled = true;
            }

            AbsoluteAmount = Math.Abs(item.Amount);
            Type = item.Amount > 0 ? DEPOSIT: PAYMENT;

            EditSplit = new CommandBase(OnEditSplit);
        }

        #endregion

        #region UI properties

        // Name of payee
        public string Payee { get; set; }

        // Memo (when not split)
        public string Memo { get; set; }
        public bool? MemoEnabled { get; private set; }

        // Category (when not split)
        public string Category { get; set; }
        public bool? CategoryEnabled { get; private set; }

        // Type (when not split)
        public string[] TypeSource { get; } = new string[] { DEPOSIT, PAYMENT };
        public bool? TypeEnabled { get; private set; }
        public string Type { get; set; }

        // Amount (when not split)
        public decimal AbsoluteAmount { get; set; }
        public bool? AbsoluteAmountEnabled { get; private set; }

        // If split
        public bool? IsSplit { get; private set; }

        // Split button
        public CommandBase EditSplit { get; }

        #endregion

        #region Actions

        private void OnEditSplit()
        {
            throw new NotImplementedException();
        }

        protected override bool? Commit()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
