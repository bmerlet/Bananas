using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Logic.Main;
using BanaData.Logic.Items;

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
            foreach(var cat in mainWindowLogic.Categories)
            {
                bool inUse = false;

                // Find if this category is in use
                inUse = household.LineItems.FirstOrDefault(li => !li.IsCategoryIDNull() && li.CategoryID == cat.ID) != null;
                inUse |= household.MemorizedLineItems.FirstOrDefault(li => !li.IsCategoryIDNull() && li.CategoryID == cat.ID) != null;

                categoriesSource.Add(new EditCategoryItem(cat, inUse));
            }

            CategoriesSource = (CollectionView)CollectionViewSource.GetDefaultView(categoriesSource);
            CategoriesSource.Filter = InUseFilter;

            AddCategory = new CommandBase(OnAddCategory);
            EditCategory = new CommandBase(OnEditCategory);
            DeleteCategory = new CommandBase(OnDeleteCategory);
        }

        #endregion

        #region UI properties

        private readonly ObservableCollection<EditCategoryItem> categoriesSource;
        public CollectionView CategoriesSource { get; }

        public EditCategoryItem SelectedCategory { get; set; }
        public EditCategoryItem CategoryToScrollTo { get; private set; }

        public CommandBase AddCategory { get; }
        public CommandBase EditCategory { get; }
        public CommandBase DeleteCategory { get; }

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

        #endregion

        #region Actions

        private bool InUseFilter(object item)
        {
            bool result = false;

            if (item is EditCategoryItem category)
            {
                bool inUse = category.IsInUse;

                // parent categories are considered in use if any child is in use
                if (!inUse)
                {
                    foreach (EditCategoryItem cat in categoriesSource)
                    {
                        if (cat.CategoryItem.IsDescendantOf(category.CategoryItem) && cat.IsInUse)
                        {
                            inUse = true;
                            break;
                        }
                    }
                }

                result =
                    (show == SHOW_ALL) ||
                    (show == SHOW_USED && inUse) ||
                    (show == SHOW_UNUSED && !inUse);
            }

            return result;
        }

        private void OnAddCategory()
        {

        }

        private void OnEditCategory()
        {

        }

        private void OnDeleteCategory()
        {

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
