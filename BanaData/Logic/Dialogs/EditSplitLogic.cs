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

            // Update total when a line is deleted
            gridViewLineItems.CollectionChanged += (o, e) => UpdateTotal();

            // Give the default view to the UI
            GridViewLineItems = (CollectionView)CollectionViewSource.GetDefaultView(gridViewLineItems);

            // Delete line item command
            DeleteLineItem = new CommandBase(OnDeleteLineItem);
        }

        // Activated when the window is loaded
        public void OnLoaded()
        {
            // Select first line item
            selectedLineItem = gridViewLineItems[0];
            editedLineItem = gridViewLineItems[0];
            OnPropertyChanged(() => SelectedLineItem);
            OnPropertyChanged(() => EditedLineItem);
            OnPropertyChanged("UpdateOverlayPosition");
        }

        #endregion

        #region UI properties

        // Collection of line items
        private readonly ObservableCollection<GridViewLineItem> gridViewLineItems;
        public CollectionView GridViewLineItems { get; }

        // Selected line item
        private GridViewLineItem selectedLineItem;
        private bool logicIsChangingSelection;
        public GridViewLineItem SelectedLineItem
        {
            get => selectedLineItem;
            set
            {
                if (value != selectedLineItem)
                {
                    if (logicIsChangingSelection)
                    {
                        // This logic is changing the selection (e.g. processing of return key)
                        selectedLineItem = value;
                        editedLineItem = value;
                        OnPropertyChanged(() => SelectedLineItem);
                    }
                    else
                    {
                        // User changed selection (e.g. by clicking on a row)
                        if (editedLineItem != null && gridViewLineItems.Contains(editedLineItem))
                        {
                            editedLineItem.CancelEdit();
                        }
                        selectedLineItem = value;
                        editedLineItem = value;
                    }

                    if (selectedLineItem != null)
                    {
                        selectedLineItem.BeginEdit();
                    }
                    OnPropertyChanged(() => EditedLineItem);
                    OnPropertyChanged("UpdateOverlayPosition");
                }
            }
        }

        // Line item being edited
        private GridViewLineItem editedLineItem;
        public GridViewLineItem EditedLineItem
        {
            get => editedLineItem;
            set { editedLineItem = value; OnPropertyChanged(() => EditedLineItem); }
        }

        // Delete command from context menu
        public CommandBase DeleteLineItem { get; }

        // Deposit/payment Combobox
        public string Type { get; set; }
        public string[] TypeSource { get; } = new string[] { DEPOSIT, PAYMENT };

        // Total
        public string Total { get; private set; }

        // Column widths
        private double widthOfCategoryColumn = 200;
        public double WidthOfCategoryColumn 
        { 
            get => widthOfCategoryColumn; 
            set { widthOfCategoryColumn = value; OnPropertyChanged(() => WidthOfCategoryColumn); }
        }

        private double widthOfMemoColumn = 200;
        public double WidthOfMemoColumn
        {
            get => widthOfMemoColumn;
            set { widthOfMemoColumn = value; OnPropertyChanged(() => WidthOfMemoColumn); }
        }

        private double widthOfAmountColumn = 90;
        public double WidthOfAmountColumn
        {
            get => widthOfAmountColumn;
            set { widthOfAmountColumn = value; OnPropertyChanged(() => WidthOfAmountColumn); }
        }

        #endregion

        #region Result

        public LineItem[] NewLineItems { get; private set; }

        #endregion

        #region Actions

        public void MoveOneLineDown()
        {
            GridViewLineItem gvli;

            int ix = gridViewLineItems.IndexOf(SelectedLineItem);
            if (ix >= gridViewLineItems.Count - 1)
            {
                // Need to add a new item
                var lineItem = new LineItem(mainWindowLogic, -1, "", -1, -1, "", 0, true);
                gvli = new GridViewLineItem(this, lineItem, false, categories);
                gridViewLineItems.Add(gvli);
            }
            else
            {
                // move to next
                gvli = gridViewLineItems[ix + 1];
            }

            // Select it
            logicIsChangingSelection = true;
            SelectedLineItem = gvli;
            logicIsChangingSelection = false;
        }

        private void OnDeleteLineItem(object arg)
        {
            var gvli = arg == null ? editedLineItem : arg as GridViewLineItem;
            if (gvli == null)
            {
                return;
            }

            if (gridViewLineItems.Count == 1)
            {
                mainWindowLogic.ErrorMessage("Cannot remove the last line item");
                return;
            }
            if (gvli == editedLineItem)
            {
                // Select next line item (if it exists) or previous one
                int ix = gridViewLineItems.IndexOf(gvli);
                if (ix < gridViewLineItems.Count - 1)
                {
                    ix += 1;
                }
                else
                {
                    ix -= 1;
                }

                logicIsChangingSelection = true;
                SelectedLineItem = gridViewLineItems[ix];
                logicIsChangingSelection = false;
            }
            gridViewLineItems.Remove(gvli);
        }

        // ZZZ Obsolete
        public GridViewLineItem BuildNewLineItem()
        {
            var lineItem = new LineItem(mainWindowLogic, -1, "", -1, -1, "", 0, true);
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
                    // Publish data
                    editing = false;
                    OnPropertyChanged(() => Category);
                    OnPropertyChanged(() => Memo);
                    OnPropertyChanged(() => Amount);
                    OnPropertyChanged(() => AmountString);
                }
                logic.MoveOneLineDown();
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
