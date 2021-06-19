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
        private readonly LineItem[] oldLineItems;
        private readonly List<CategoryItem> categories;

        private const string PAYMENT = "Payment";
        private const string DEPOSIT = "Deposit";

        #endregion

        #region Constructor

        public EditSplitLogic(MainWindowLogic _mainWindowLogic, LineItem[] _lineItems)
        {
            (mainWindowLogic, oldLineItems) = (_mainWindowLogic, _lineItems);

            // Create our own copy of the category list, to avoid filter issue on nested dialogs
            categories = new List<CategoryItem>(mainWindowLogic.Categories);

            // Deposit or payment?
            decimal total = oldLineItems.Sum(li => li.Amount);
            bool isDeposit = total > 0;
            Type = isDeposit ? DEPOSIT : PAYMENT;
            Total = Math.Abs(total).ToString("N");

            // Build line item collection
            gridViewLineItems = new ObservableCollection<GridViewLineItem>();
            foreach (var li in oldLineItems)
            {
                gridViewLineItems.Add(new GridViewLineItem(this, li, isDeposit, categories));
            }

            // Update total wjhen a line is deleted
            gridViewLineItems.CollectionChanged += (o, e) => UpdateTotal();

            // Give the default view to the UI
            GridViewLineItems = (CollectionView)CollectionViewSource.GetDefaultView(gridViewLineItems);
        }

        #endregion

        #region UI properties

        // Collection of line items
        private readonly ObservableCollection<GridViewLineItem> gridViewLineItems;
        public CollectionView GridViewLineItems { get; }

        // Deposit/payment Combobox
        public string Type { get; set; }
        public string[] TypeSource { get; } = new string[] { DEPOSIT, PAYMENT };

        // Total
        public string Total { get; private set; }

        #endregion

        #region Result

        public LineItem[] NewLineItems { get; private set; }

        #endregion

        #region Actions

        public GridViewLineItem BuildNewLineItem()
        {
            int id = gridViewLineItems.Max(gvli => gvli.ID) + 1;
            var lineItem = new LineItem(mainWindowLogic, id, "", -1, -1, "", 0, true);
            var result = new GridViewLineItem(this, lineItem, false, categories);

            return result;
        }

        // Called by line item when amount changes and also when the collection of line items changes
        public void UpdateTotal()
        {
            Total = gridViewLineItems.Sum(gvli => gvli.Amount).ToString("N");
            OnPropertyChanged(() => Total);
        }

        protected override bool? Commit()
        {
            // Build the new line items
            var newLineItems = new List<LineItem>();

            foreach (var gvli in gridViewLineItems)
            {
                decimal amount = Type == DEPOSIT ? gvli.Amount : -gvli.Amount;
                LineItem li;
                try
                {
                    li = LineItem.GetLineItem(mainWindowLogic, gvli.ID, gvli.Category, gvli.Memo, amount, true);
                }
                catch(ArgumentException e)
                {
                    mainWindowLogic.ErrorMessage(e.Message);
                    return null; // Stay on dialog
                }

                newLineItems.Add(li);
            }

            // Publish them
            NewLineItems = newLineItems.ToArray();

            // Any change?
            bool change = oldLineItems.Length != NewLineItems.Length;
            if (!change)
            {
                for(int i = 0; i < oldLineItems.Length; i++)
                {
                    change |=
                        oldLineItems[i].Category != NewLineItems[i].Category ||
                        oldLineItems[i].Memo != NewLineItems[i].Memo ||
                        oldLineItems[i].Amount != NewLineItems[i].Amount;
                }
            }

            return change;
        }

        #endregion

        #region Supporting classes

        // A line item, as presented in the edit split dialog
        public class GridViewLineItem : LogicBase, IEditableObject
        {
            private struct GridViewLineItemData
            {
                public string Memo;
                public string Category;
                public decimal Amount;
            }

            private GridViewLineItemData data;
            private GridViewLineItemData backup;
            private bool editing;
            private readonly EditSplitLogic logic;

            // Default constructor
            // It is needed because when UserCanAddRows is set, the datagrid verifies that there is
            // a default constructor. But it is actually not used since we hijack the AddingNewItem
            // datagrid event and create the object ourself using the explicit constructor.
            public GridViewLineItem() => throw new NotImplementedException();

            // Explicit constructor
            public GridViewLineItem(EditSplitLogic _logic, LineItem lineItem, bool deposit, IEnumerable<CategoryItem> categories) =>
                (logic, ID, data.Memo, data.Category, Categories, data.Amount) = 
                (_logic, lineItem.ID, lineItem.Memo, lineItem.Category, categories, deposit ? lineItem.Amount : -lineItem.Amount);

            public readonly int ID;

            //
            // UI properties
            //

            // Memo
            public string Memo
            { 
                get => data.Memo;
                set => data.Memo = value;
            }

            // Category and the supporting categories
            public string Category 
            {
                get => data.Category;
                set => data.Category = value;
            }
            public IEnumerable<CategoryItem> Categories { get; }

            // Amount, and its string representation
            public decimal Amount
            {
                get => data.Amount;
                set
                {
                    data.Amount = value;
                    OnPropertyChanged(() => AmountString);
                    logic.UpdateTotal();
                }
            }
            public string AmountString => data.Amount.ToString("N");

            public void BeginEdit()
            {
                if (!editing)
                {
                    // Backup the data
                    backup = data;
                    editing = true;
                }
            }

            public void EndEdit()
            {
                if (editing)
                {
                    // Publish data (not sure it's needed)
                    editing = false;
                    OnPropertyChanged(() => Category);
                    OnPropertyChanged(() => Memo);
                    OnPropertyChanged(() => Amount);
                    OnPropertyChanged(() => AmountString);
                }
            }

            public void CancelEdit()
            {
                if (editing)
                {
                    // Recover from backup data
                    editing = false;
                    data = backup;
                    OnPropertyChanged(() => Category);
                    OnPropertyChanged(() => Memo);
                    OnPropertyChanged(() => Amount);
                    OnPropertyChanged(() => AmountString);
                }
            }
        }

        #endregion
    }
}
