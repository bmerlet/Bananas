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

            TaxInfo = categoryItem.TaxInfo;
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
        public IEnumerable<string> TaxInfoSource { get; } = CategoryItem.TaxInfoDictionary.Values;
        public string TaxInfo { get; set; }

        #endregion

        #region Result

        public CategoryItem NewCategoryItem =>
            new CategoryItem(
                oldCategoryItem.ID, 
                Name,
                Description,
                Type == INCOME,
                GetTaxInfoKey(),
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

            if (Name.StartsWith("_"))
            {
                mainWindowLogic.ErrorMessage("Category name starting with an underscore are reserved");
                return null;
            }

            var parent = GetParent();

            foreach (Household.CategoryRow cat in mainWindowLogic.Household.Category.Rows)
            {
                if (cat.ID != oldCategoryItem.ID && cat.Name == Name &&
                    ((parent == null && cat.IsParentIDNull()) || (parent != null && !cat.IsParentIDNull() && parent.ID == cat.ParentID)))
                {
                    mainWindowLogic.ErrorMessage("There is already a category with this name");
                    return null;
                }
            }

            bool change =
                oldCategoryItem.Name != Name ||
                oldCategoryItem.Description != Description ||
                oldCategoryItem.IsIncome != (Type == INCOME) ||
                oldCategoryItem.TaxInfo != TaxInfo ||
                oldCategoryItem.Parent != parent;

            return change;
        }

        private CategoryItem GetParent()
        {
            return string.IsNullOrWhiteSpace(Parent) ? null : Categories.FirstOrDefault(c => c.FullName == Parent);
        }

        private string GetTaxInfoKey()
        {
            if (string.IsNullOrWhiteSpace(TaxInfo))
            {
                return "";
            }

            foreach (string key in CategoryItem.TaxInfoDictionary.Keys)
            {
                if (CategoryItem.TaxInfoDictionary[key] == TaxInfo)
                {
                    return key;
                }
            }

            return "";
        }

        #endregion
    }
}
