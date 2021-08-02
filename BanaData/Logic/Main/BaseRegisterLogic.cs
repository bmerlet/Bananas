using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;

namespace BanaData.Logic.Main
{
    /// <summary>
    /// Minimal register class to use the listview with overlay mechanics
    /// </summary>
    public abstract class BaseRegisterLogic : LogicBase
    {
        // The items making up the register. The CollectionView type enables sorting on columns, and is generic
        public CollectionView RegisterItems { get; protected set; }

        // Transaction to show
        public object TransactionToScrollTo { get; protected set; }

        // Action to follow an update of the overlay position 
        public Action UpdateOverlayPosition { get; protected set; }

        public abstract void ProcessEnter();
        public abstract void MoveUp();
        public abstract void MoveDown();
        public abstract void RecomputeBalances();

        #region Utilities for derived classes

        // Get the next transaction, in the CollectionView order
        protected object GetNextTransaction(object trans)
        {
            var nextTrans = RegisterItems.Cast<object>()
                .SkipWhile(x => !Equals(x, trans)) //skip preceding transactions
                .Skip(1) //skip the transaction itself
                .FirstOrDefault();

            return nextTrans;
        }

        // Get the previous transaction, in the CollectionView order
        protected object GetPreviousTransaction(object trans)
        {
            object lastTrans = null;

            foreach (var curTrans in RegisterItems)
            {
                if (curTrans.Equals(trans))
                {
                    break;
                }

                lastTrans = curTrans;
            }

            return lastTrans;
        }

        #endregion
    }
}
