using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Logic.Main;
using BanaData.Logic.Items;
using BanaData.Database;

namespace BanaData.Logic.Dialogs
{
    public class EditCategoriesLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public EditCategoriesLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;
            var household = mainWindowLogic.Household;

            categoriesSource = new ObservableCollection<EditCategoryItem>();

            foreach (var catList in new IEnumerable<CategoryItem>[] { mainWindowLogic.Categories, mainWindowLogic.HiddenCategories })
            {
                foreach (var cat in catList.Where(c => c.ID >= 0))
                {
                    bool inUse = false;

                    // Find if this category is in use
                    inUse = household.LineItem.FirstOrDefault(li => !li.IsCategoryIDNull() && li.CategoryID == cat.ID) != null;
                    inUse |= household.MemorizedLineItem.FirstOrDefault(li => !li.IsCategoryIDNull() && li.CategoryID == cat.ID) != null;

                    categoriesSource.Add(new EditCategoryItem(cat, inUse));
                }
            }

            CategoriesSource = (CollectionView)CollectionViewSource.GetDefaultView(categoriesSource);
            CategoriesSource.Filter = InUseFilter;
            CategoriesSource.SortDescriptions.Add(new SortDescription("CategoryItem.FullName", ListSortDirection.Ascending));

            AddCommand = new CommandBase(OnAddCategory);
            EditCommand = new CommandBase(OnEditCategory);
            DeleteCommand = new CommandBase(OnDeleteCategory);
        }

        #endregion

        #region UI properties

        // List of categories
        private readonly ObservableCollection<EditCategoryItem> categoriesSource;
        public CollectionView CategoriesSource { get; }

        // Selected category
        public EditCategoryItem SelectedCategory { get; set; }

        // Category we want to show
        public EditCategoryItem CategoryToScrollTo { get; private set; }

        // Commands
        public CommandBase AddCommand { get; }
        public CommandBase EditCommand { get; }
        public CommandBase DeleteCommand { get; }

        // What to show
        private const string SHOW_ALL = "All categories";
        private const string SHOW_USED = "Used categories";
        private const string SHOW_UNUSED = "Unused categories";
        public string[] ShowSource { get; } = new string[] { SHOW_ALL, SHOW_USED, SHOW_UNUSED };

        private string show = SHOW_ALL;
        public string Show
        {
            get => show;
            set { show = value; CategoriesSource.Refresh(); }
        }

        private bool showHidden = false;
        public bool? ShowHidden 
        {
            get => showHidden;
            set { showHidden = value == true; CategoriesSource.Refresh(); }
        }

        #endregion

        #region Actions

        // Filter what to show based on "Show" value
        private bool InUseFilter(object item)
        {
            bool result = false;

            if (item is EditCategoryItem category &&
                (!category.CategoryItem.Hidden || showHidden))
            {
                bool inUse = IsCategoryOrDescendantInUse(category);

                result =
                    (show == SHOW_ALL) ||
                    (show == SHOW_USED && inUse) ||
                    (show == SHOW_UNUSED && !inUse);
            }

            return result;
        }

        // parent categories are considered in use if any child is in use
        private bool IsCategoryOrDescendantInUse(EditCategoryItem category)
        {
            if (category.IsInUse)
            {
                return true;
            }

            foreach (EditCategoryItem cat in categoriesSource)
            {
                if (cat.CategoryItem.IsDescendantOf(category.CategoryItem) && cat.IsInUse)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnAddCategory()
        {
            var logic = new EditCategoryLogic(mainWindowLogic, new CategoryItem(-1, "", "", false, "", null));
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Update DB
                var newCategory = logic.NewCategoryItem;
                newCategory = AddCategoryToDataSet(newCategory);

                // Update UI
                mainWindowLogic.Categories.Add(newCategory);
                mainWindowLogic.Categories.Sort();
                var newEditCategory = new EditCategoryItem(newCategory, false);
                categoriesSource.Add(newEditCategory);
                SelectedCategory = newEditCategory;
                CategoryToScrollTo = newEditCategory;
                OnPropertyChanged(() => SelectedCategory);
                OnPropertyChanged(() => CategoryToScrollTo);
            }
        }

        private void OnEditCategory()
        {
            if (SelectedCategory != null)
            {
                if (SelectedCategory.CategoryItem.Hidden)
                {
                    mainWindowLogic.ErrorMessage("This category is used internally and cannot be edited");
                    return;
                }

                var logic = new EditCategoryLogic(mainWindowLogic, SelectedCategory.CategoryItem);
                if (mainWindowLogic.GuiServices.ShowDialog(logic))
                {
                    // Update DB
                    var updatedCategory = logic.NewCategoryItem;
                    bool oldIsInUse = SelectedCategory.IsInUse;
                    UpdateCategoryInDataSet(updatedCategory);

                    // Update UI
                    mainWindowLogic.Categories.Remove(SelectedCategory.CategoryItem);
                    categoriesSource.Remove(SelectedCategory);

                    mainWindowLogic.Categories.Add(updatedCategory);
                    mainWindowLogic.Categories.Sort();
                    var updatedEditCategory = new EditCategoryItem(updatedCategory, oldIsInUse);
                    categoriesSource.Add(updatedEditCategory);
                    SelectedCategory = updatedEditCategory;
                    CategoryToScrollTo = updatedEditCategory;
                    OnPropertyChanged(() => SelectedCategory);
                    OnPropertyChanged(() => CategoryToScrollTo);
                }
            }
        }

        private void OnDeleteCategory()
        {
            var categoryToDelete = SelectedCategory;
            if (categoryToDelete != null)
            {
                if (categoryToDelete.CategoryItem.AccountID >= 0)
                {
                    mainWindowLogic.ErrorMessage("Transfers cannot be deleted");
                    return;
                }

                if (categoryToDelete.IsInUse)
                {
                    mainWindowLogic.ErrorMessage("This category cannot be deleted because it is used by some transactions");
                    return;
                }

                if (IsCategoryOrDescendantInUse(categoryToDelete))
                {
                    mainWindowLogic.ErrorMessage("This category cannot be deleted because its descendants are used by some transactions");
                    return;
                }

                if (categoryToDelete.CategoryItem.Hidden)
                {
                    mainWindowLogic.ErrorMessage("This category is used internally and cannot be deleted");
                    return;
                }

                DeleteCategoryFromDataSet(categoryToDelete.CategoryItem);
                categoriesSource.Remove(categoryToDelete);
                mainWindowLogic.Categories.Remove(categoryToDelete.CategoryItem);
            }
        }

        private CategoryItem AddCategoryToDataSet(CategoryItem category)
        {
            var household = mainWindowLogic.Household;

            var parentRow = category.Parent ==  null ? null : household.Category.FindByID(category.Parent.ID);
            var catRow = household.Category.Add(category.Name, category.Description, parentRow, category.IsIncome, category.TaxInfo);

            mainWindowLogic.CommitChanges();

            return new CategoryItem(category, catRow.ID);
        }

        private void UpdateCategoryInDataSet(CategoryItem category)
        {
            var household = mainWindowLogic.Household;

            var catRow = household.Category.FindByID(category.ID);
            var parentRow = category.Parent == null ? null : household.Category.FindByID(category.Parent.ID);
            household.Category.Update(catRow, category.Name, category.Description, parentRow, category.IsIncome, category.TaxInfo);

            mainWindowLogic.CommitChanges();
        }

        private void DeleteCategoryFromDataSet(CategoryItem category)
        {
            var household = mainWindowLogic.Household;

            var row = household.Category.FindByID(category.ID);
            row.Delete();

            mainWindowLogic.CommitChanges();
        }

        #endregion

        #region Support classes

        // Category item plus the in-use info
        public class EditCategoryItem
        {
            public EditCategoryItem(CategoryItem categoryItem, bool isInUse) => 
                (CategoryItem, IsInUse) = (categoryItem, isInUse);

            public CategoryItem CategoryItem { get; }
            public bool IsInUse { get; }
        }
        #endregion
    }
}
