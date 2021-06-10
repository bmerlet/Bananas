using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Logic.Items;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.ComponentModel;

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

            gridViewLineItems = new ObservableCollection<GridViewLineItem>();
            var categories = mainWindowLogic.Categories;
            foreach (var li in lineItems)
            {
                gridViewLineItems.Add(new GridViewLineItem(li, categories));
            }
            GridViewLineItems = (CollectionView)CollectionViewSource.GetDefaultView(gridViewLineItems);

            AdjustAmount = new CommandBase(OnAdjustAmount);
        }

        #endregion

        #region UI properties

        // Collection of line items
        private readonly ObservableCollection<GridViewLineItem> gridViewLineItems;
        public CollectionView GridViewLineItems { get; }

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

        #region Supporting classes

        // A line item, as presented in the edit split dialog
        class GridViewLineItem : LogicBase, IEditableObject
        {
            public GridViewLineItem(LineItem lineItem, IEnumerable<CategoryItem> categories) =>
                (ID, Memo, Category, Categories, Amount) = 
                (lineItem.ID, lineItem.Memo, lineItem.Category, categories, lineItem.Amount);

            public readonly int ID;

            //
            // UI properties
            //

            // Memo
            public string Memo { get; set; }

            // Category and the supporting categories
            public string Category { get; set; }
            public IEnumerable<CategoryItem> Categories { get; }

            // Amount, and its string representation
            private decimal amount;
            public decimal Amount
            {
                get => amount;
                set
                {
                    amount = value;
                    OnPropertyChanged(() => AmountString);
                }
            }
            public string AmountString => amount.ToString("N");

            public void BeginEdit()
            {
                //throw new NotImplementedException();
            }

            public void EndEdit()
            {
                //throw new NotImplementedException();
            }

            public void CancelEdit()
            {
                //throw new NotImplementedException();
            }
        }

        #endregion
    }
}
