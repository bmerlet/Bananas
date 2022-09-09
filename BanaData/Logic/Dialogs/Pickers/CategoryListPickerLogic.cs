using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Pickers
{
    /// <summary>
    /// Category list picker
    /// </summary>
    public class CategoryListPickerLogic : LogicDialogBase
    {
        #region Constructor

        public CategoryListPickerLogic(Household household, IEnumerable<Household.CategoryRow> pickedCategories)
        {
            // Create category tree
            var categoryTable = household.Category;
            foreach (Household.CategoryRow categoryRow in categoryTable)
            {
                if (categoryRow.IsParentIDNull())
                {
                    var categoryNode = new CategoryPickerNode(categoryTable, categoryRow, null);
                    if (!categoryNode.CategoryItem.Hidden)
                    {
                        categories.Add(categoryNode);
                    }
                }
            }

            // Setup initial selection
            foreach(var node in categories)
            {
                node.SetInitialSelection(pickedCategories);
            }

            // Setup categories view
            Categories = (CollectionView)CollectionViewSource.GetDefaultView(categories);
            Categories.SortDescriptions.Add(new SortDescription("CategoryItem.FullName", ListSortDirection.Ascending));

            // Setup commands
            ClearAllCommand = new CommandBase(OnClearAllCommand);
            SelectAllCommand = new CommandBase(OnSelectAllCommand);
            SelectIncomeCommand = new CommandBase(OnSelectIncomeCommand);
            SelectExpenseCommand = new CommandBase(OnSelectExpenseCommand);
        }

        #endregion

        #region UI properties

        //
        // List of categories
        //
        private readonly ObservableCollection<CategoryPickerNode> categories = new ObservableCollection<CategoryPickerNode>();
        public CollectionView Categories { get; }

        //
        // Buttons
        //
        public CommandBase ClearAllCommand { get; }
        public CommandBase SelectAllCommand { get; }
        public CommandBase SelectIncomeCommand { get; }
        public CommandBase SelectExpenseCommand { get; }

        #endregion

        #region Actions

        // Result
        public IEnumerable<Household.CategoryRow> PickedCategories;

        protected override bool? Commit()
        {
            var pickedCategories = new List<Household.CategoryRow>();

            foreach (var cat in categories)
            {
                cat.AddToListIfChecked(pickedCategories);
                if (cat.IsChecked == true)
                {
                    pickedCategories.Add(cat.CategoryRow);
                }
            }

            PickedCategories = pickedCategories;

            // Err on the side of caution
            return true;
        }

        private void OnClearAllCommand()
        {
            foreach (var cat in categories)
            {
                cat.IsChecked = false;
            };
        }

        private void OnSelectAllCommand()
        {
            foreach (var cat in categories)
            {
                cat.IsChecked = true;
            }
        }

        private void OnSelectIncomeCommand()
        {
            foreach (var cat in categories)
            {
                cat.IsChecked = cat.CategoryRow.IsIncome;
            }
        }

        private void OnSelectExpenseCommand()
        {
            foreach (var cat in categories)
            {
                cat.IsChecked = !cat.CategoryRow.IsIncome;
            }
        }

        #endregion
    }

    /// <summary>
    /// Node class for the category list picker 
    /// (should be nested into above, but treeview does not support nested classes)
    /// </summary>
    public class CategoryPickerNode : LogicBase
    {
        #region Constructor

        public CategoryPickerNode(Household.CategoryDataTable categoryTable, Household.CategoryRow categoryRow, CategoryPickerNode _parent)
        {
            // Memo input
            CategoryRow = categoryRow;
            parent = _parent;

            CategoryItem = CategoryItem.CreateFromDB(
                categoryRow,
                parent == null ? null : new CategoryItem[] { parent.CategoryItem });

            // Build children
            foreach (var cat in categoryTable)
            {
                if (!cat.IsParentIDNull() && cat.ParentID == categoryRow.ID)
                {
                    children.Add(new CategoryPickerNode(categoryTable, cat, this));
                }
            }

            Children = (CollectionView)CollectionViewSource.GetDefaultView(children);
            Children.SortDescriptions.Add(new SortDescription("CategoryItem.FullName", ListSortDirection.Ascending));
        }

        #endregion

        #region Logic properties

        private readonly CategoryPickerNode parent;
        public readonly Household.CategoryRow CategoryRow;

        #endregion

        #region UI properties

        // This category
        public CategoryItem CategoryItem { get; }

        // If it is selected
        private bool? isChecked;
        public bool? IsChecked
        {
            get => isChecked;
            set
            {
                if (isChecked != value)
                {
                    // Set my value
                    SetIsCheckedNonRecursively(value);

                    // Propagate down
                    PropagateDown(value);

                    // Propagate up
                    PropagateUp(value);
                }
            }
        }

        // Category children
        private readonly ObservableCollection<CategoryPickerNode> children = new ObservableCollection<CategoryPickerNode>();
        public CollectionView Children { get; }

        #endregion

        #region Action

        // Initial selection
        public void SetInitialSelection(IEnumerable<Household.CategoryRow> pickedCategories)
        {
            IsChecked = pickedCategories.Contains(CategoryRow);
            foreach(var child in children)
            {
                child.SetInitialSelection(pickedCategories);
            }
        }

        // Commit
        public void AddToListIfChecked(List<Household.CategoryRow> pickedCategories)
        {
            // If this category is fully checked, add it to the list
            if (IsChecked == true)
            {
                pickedCategories.Add(CategoryRow);
            }

            // Always recurse to be sure to get all checked categories
            foreach (var child in children)
            {
                child.AddToListIfChecked(pickedCategories);
            }
        }

        // Set children value
        private void PropagateDown(bool? value)
        {
            foreach (var child in children)
            {
                if (child.IsChecked != value)
                {
                    child.SetIsCheckedNonRecursively(value);
                    child.PropagateDown(value);
                }
            }
        }

        // Set parent value
        private void PropagateUp(bool? value)
        {
            if (parent != null && parent.IsChecked != value)
            {
                if (!parent.children.All(c => c.IsChecked == value))
                {
                    // Mix of checked and unchecked
                    value = null;
                }
                parent.SetIsCheckedNonRecursively(value);
                parent.PropagateUp(value);
            }
        }

        private void SetIsCheckedNonRecursively(bool? value)
        {
            isChecked = value;
            OnPropertyChanged(() => IsChecked);
        }

        #endregion
    }
}
