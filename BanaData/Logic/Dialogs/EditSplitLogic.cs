using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Logic.Items;

namespace BanaData.Logic.Dialogs
{
    /// <summary>
    /// Logic to edit a split
    /// </summary>
    public class EditSplitLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly LineItem[] lineItems;

        #endregion

        #region Constructor

        public EditSplitLogic(MainWindowLogic _mainWindowLogic, LineItem[] _lineItems)
        {
            (mainWindowLogic, lineItems) = (_mainWindowLogic, _lineItems);

            AdjustAmount = new CommandBase(OnAdjustAmount);
        }

        #endregion

        #region UI properties

        // Adjust button
        public CommandBase AdjustAmount { get; }

        #endregion

        #region Result

        public LineItem[] NewLineItems => lineItems; // ZZZ For now

        #endregion

        #region Actions

        private void OnAdjustAmount()
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
