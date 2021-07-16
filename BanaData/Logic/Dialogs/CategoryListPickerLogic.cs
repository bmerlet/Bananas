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

namespace BanaData.Logic.Dialogs
{
    public class CategoryListPickerLogic : LogicDialogBase
    {
        #region Private memebers

        private readonly MainWindowLogic mainWindowLogic;
        private readonly IEnumerable<Household.CategoryRow> oldPickedCategories;

        #endregion

        #region Constructor

        public CategoryListPickerLogic(MainWindowLogic _mainWindowLogic, IEnumerable<Household.CategoryRow> pickedCategories)
        {
            (mainWindowLogic, oldPickedCategories) = (_mainWindowLogic, pickedCategories);

            foreach (Household.CategoryRow categoryRow in mainWindowLogic.Household.Category)
            {
                var pickerItem = new CategoryPickerItem(categoryRow, mainWindowLogic.Categories, oldPickedCategories.Contains(categoryRow));
                if (!pickerItem.CategoryItem.Hidden)
                {
                    categories.Add(pickerItem);
                }
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
        private readonly ObservableCollection<CategoryPickerItem> categories = new ObservableCollection<CategoryPickerItem>();
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
                if (cat.IsSelected == true)
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
                cat.IsSelected = false;
            }
        }

        private void OnSelectAllCommand()
        {
            foreach (var cat in categories)
            {
                cat.IsSelected = true;
            }
        }

        private void OnSelectIncomeCommand()
        {
            foreach (var cat in categories)
            {
                cat.IsSelected = cat.CategoryRow.IsIncome;
            }
        }

        private void OnSelectExpenseCommand()
        {
            foreach (var cat in categories)
            {
                cat.IsSelected = !cat.CategoryRow.IsIncome;
            }
        }

        #endregion

        #region Supporting class

        public class CategoryPickerItem : LogicBase
        {
            public CategoryPickerItem(Household.CategoryRow categoryRow, IEnumerable<CategoryItem> parents, bool selected) =>
                (CategoryRow, CategoryItem, isSelected) = (categoryRow, CategoryItem.CreateFromDB(categoryRow, parents), selected);

            public readonly Household.CategoryRow CategoryRow;

            public CategoryItem CategoryItem { get; }

            private bool? isSelected;
            public bool? IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                        OnPropertyChanged(() => IsSelected);
                    }
                }
            }
        }

        #endregion
    }
}
