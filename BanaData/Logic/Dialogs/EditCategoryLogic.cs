using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Logic.Items;
using BanaData.Database;

namespace BanaData.Logic.Dialogs
{
    public class EditCategoryLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly CategoryItem oldCategoryItem;

        #endregion

        #region Constructor

        public EditCategoryLogic(MainWindowLogic _mainWindowLogic, CategoryItem categoryItem)
        {
            (mainWindowLogic, oldCategoryItem) = (_mainWindowLogic, categoryItem);

            Name = categoryItem.Name;
            Description = categoryItem.Description;

            Type = categoryItem.IsIncome ? INCOME : EXPENSE;
            Parent = categoryItem.Parent == null ? "" : categoryItem.Parent.FullName;
            Categories = mainWindowLogic.Categories;

            //TypeEnabled = add;
        }

        #endregion

        #region UI properties

        // Name
        public string Name { get; set; }

        // Description
        public string Description { get; set; }

        // Type
        private const string INCOME = "Income";
        private const string EXPENSE = "Expense";
        public string[] TypeSource { get; } = new string[] { INCOME, EXPENSE };
        public string Type { get; set; }

        // Parent
        public string Parent { get; set; }
        public IEnumerable<CategoryItem> Categories { get; }

        // Tax info
        public string[] TaxInfoSource { get; } = new string[] { "???" };
        public string TaxInfo { get; set; }

        #endregion

        #region Result

        public CategoryItem NewCategoryItem =>
            new CategoryItem(
                oldCategoryItem.ID, 
                Name,
                Description,
                Type == INCOME,
                oldCategoryItem.TaxInfo, // ZZZ
                GetParent());

        #endregion

        #region Actions

        protected override bool? Commit()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                mainWindowLogic.ErrorMessage("Category name cannot be blank");
                return null;
            }

            var parent = GetParent();

            foreach (Household.CategoriesRow cat in mainWindowLogic.Household.Categories.Rows)
            {
                if (cat.ID != oldCategoryItem.ID && cat.Name == Name &&
                    ((parent == null && cat.IsParentIDNull()) || (parent != null && !cat.IsParentIDNull() && parent.ID == cat.ParentID)))
                {
                    mainWindowLogic.ErrorMessage("There is already a category with this name");
                    return null;
                }
            }

            // ZZZZ Tax info missing for now
            bool change =
                oldCategoryItem.Name != Name ||
                oldCategoryItem.Description != Description ||
                oldCategoryItem.IsIncome != (Type == INCOME) ||
                oldCategoryItem.Parent != parent;

            return change;
        }

        private CategoryItem GetParent()
        {
            return string.IsNullOrWhiteSpace(Parent) ? null : Categories.FirstOrDefault(c => c.FullName == Parent);
        }

        #endregion
    }
}
