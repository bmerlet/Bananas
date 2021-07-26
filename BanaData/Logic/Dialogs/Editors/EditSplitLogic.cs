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

namespace BanaData.Logic.Dialogs.Editors
{
    /// <summary>
    /// Logic to edit a split
    /// </summary>

    #region Main dialog class

    public class EditSplitLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly LineItem[] oldLineItems;

        private const string PAYMENT = "Payment";
        private const string DEPOSIT = "Deposit";

        #endregion

        #region Constructor

        public EditSplitLogic(MainWindowLogic _mainWindowLogic, LineItem[] _lineItems)
        {
            (mainWindowLogic, oldLineItems) = (_mainWindowLogic, _lineItems);

            // Deposit or payment?
            decimal total = oldLineItems.Sum(li => li.Amount);
            bool isDeposit = total > 0;
            Type = isDeposit ? DEPOSIT : PAYMENT;
            Total = Math.Abs(total).ToString("N");

            Register = new LineItemRegister(mainWindowLogic, oldLineItems, this, isDeposit);
        }

        #endregion

        #region UI properties

        // Register
        public LineItemRegister Register { get; }

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

        // Called by line item when amount changes and also when the collection of line items changes
        public void UpdateTotal()
        {
            Total = Register.Transactions.Cast<GridViewLineItem>().Sum(gvli => gvli.Amount).ToString("N");
            OnPropertyChanged(() => Total);
        }

        protected override bool? Commit()
        {
            // Build the new line items
            var newLineItems = new List<LineItem>();

            foreach (GridViewLineItem gvli in Register.Transactions)
            {
                // Skip uncommitted with amount of 0
                if (gvli.ID == -1 && gvli.Amount == 0 && string.IsNullOrWhiteSpace(gvli.Memo))
                {
                    continue;
                }

                decimal amount = Type == DEPOSIT ? gvli.Amount : -gvli.Amount;
                LineItem li;
                try
                {
                    li = LineItem.GetLineItem(mainWindowLogic, gvli.ID, gvli.Category, gvli.Memo, amount, true);
                }
                catch (ArgumentException e)
                {
                    mainWindowLogic.ErrorMessage(e.Message);
                    return null; // Stay on dialog
                }

                newLineItems.Add(li);
            }

            // Make sure there are no multiple transfers to the same account
            foreach(var li in newLineItems)
            {
                if (li.CategoryAccountID != -1 &&
                    newLineItems.Count(l => l.CategoryAccountID == li.CategoryAccountID) > 1)
                {
                    mainWindowLogic.ErrorMessage($"Entering multiple transfers to the same account ({mainWindowLogic.Household.Account.FindByID(li.CategoryAccountID).Name}) is not supported.");
                    return null;
                }
            }

            // Publish them
            NewLineItems = newLineItems.ToArray();

            // Any change?
            bool change = oldLineItems.Length != NewLineItems.Length;
            if (!change)
            {
                for (int i = 0; i < oldLineItems.Length; i++)
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
    }

    #endregion

    #region Line item register class

    // Logic for the register part of the dialog
    public class LineItemRegister : BaseRegisterLogic
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly EditSplitLogic editSplitLogic;
        private readonly List<CategoryItem> categories;
        //private readonly LineItem[] oldLineItems;

        #endregion

        #region Constructor

        public LineItemRegister(MainWindowLogic _mainWindowLogic, LineItem[] oldLineItems, EditSplitLogic _editSplitLogic, bool isDeposit)
        {
            mainWindowLogic = _mainWindowLogic;
            editSplitLogic = _editSplitLogic;

            // Build line item collection
            gridViewLineItems = new ObservableCollection<GridViewLineItem>();
            foreach (var li in oldLineItems)
            {
                gridViewLineItems.Add(new GridViewLineItem(editSplitLogic, li, isDeposit));
            }

            // Create our own copy of the category list, to avoid filter issue on nested dialogs
            categories = new List<CategoryItem>(mainWindowLogic.CategoriesAndTransfers);

            // Update total when a line is deleted
            gridViewLineItems.CollectionChanged += (o, e) => editSplitLogic.UpdateTotal();

            // Give the default view to the UI
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(gridViewLineItems);

            // Delete line item command
            DeleteLineItem = new CommandBase(OnDeleteLineItem);

            // Select first transaction
            logicIsChangingSelection = true;
            SelectedLineItem = gridViewLineItems[0];
            logicIsChangingSelection = false;
        }

        #endregion

        #region UI properties

        // Collection of line items
        private readonly ObservableCollection<GridViewLineItem> gridViewLineItems;

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

                        CategoryFocus = false;
                        OnPropertyChanged(() => CategoryFocus);
                        UpdateOverlayPosition = () =>
                        {
                            CategoryFocus = true;
                            OnPropertyChanged(() => CategoryFocus);
                        };
                    }
                    else
                    {
                        UpdateOverlayPosition = null;
                    }

                    OnPropertyChanged(() => EditedLineItem);
                    OnPropertyChanged(() => UpdateOverlayPosition);
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

        public IEnumerable<CategoryItem> Categories => categories;

        // Delete command from context menu
        public CommandBase DeleteLineItem { get; }

        // To focus the overlay on the category
        public bool CategoryFocus { get; private set; }

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

        #region Actions

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

        public override void MoveUp()
        {
            if (GetPreviousTransaction(SelectedLineItem) is GridViewLineItem prevTransaction)
            {
                logicIsChangingSelection = true;
                SelectedLineItem = prevTransaction;
                logicIsChangingSelection = false;
            }
        }

        public override void MoveDown()
        {
            if (GetNextTransaction(SelectedLineItem) is GridViewLineItem nextTransaction)
            {
                logicIsChangingSelection = true;
                SelectedLineItem = nextTransaction;
                logicIsChangingSelection = false;
            }
        }


        public override void ProcessEnter()
        {
            if (selectedLineItem != null)
            {
                if (selectedLineItem.Amount == 0)
                {
                    mainWindowLogic.ErrorMessage("Please enter an amount.");
                    return;
                }

                selectedLineItem.EndEdit();
            }

            if (GetNextTransaction(selectedLineItem) is GridViewLineItem nextTransaction)
            {
                logicIsChangingSelection = true;
                SelectedLineItem = nextTransaction;
                logicIsChangingSelection = false;
            }
            else
            {
                // Need to add a new item
                var lineItem = new LineItem(mainWindowLogic, -1, "", -1, -1, "", 0, true);
                var gvli = new GridViewLineItem(editSplitLogic, lineItem, false);
                gridViewLineItems.Add(gvli);

                logicIsChangingSelection = true;
                SelectedLineItem = gvli;
                logicIsChangingSelection = false;
            }
        }


        public override void RecomputeBalances()
        {
            editSplitLogic.UpdateTotal();
        }

        private void OnDeleteLineItem()
        {
            var gvli = editedLineItem;
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

        #endregion
    }

    #endregion

    #region Line item class

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

        // Explicit constructor
        public GridViewLineItem(EditSplitLogic _logic, LineItem lineItem, bool deposit) =>
            (logic, ID, data.Memo, data.Category, data.Amount) =
            (_logic, lineItem.ID, lineItem.Memo, lineItem.Category, deposit ? lineItem.Amount : -lineItem.Amount);

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

        // Category
        public string Category
        {
            get => data.Category;
            set => data.Category = value;
        }

        // Amount, and its string representation
        public decimal Amount
        {
            get => data.Amount;
            set
            {
                data.Amount = value;
                logic.UpdateTotal();
            }
        }

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
            }
        }

        public override bool Equals(object obj)
        {
            return
                obj is GridViewLineItem o &&
                o.Category == Category &&
                o.Memo == Memo &&
                o.Amount == Amount;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    #endregion
}

