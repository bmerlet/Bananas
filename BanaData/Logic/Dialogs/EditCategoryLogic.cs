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

        public EditCategoryLogic(MainWindowLogic _mainWindowLogic, CategoryItem categoryItem, bool add)
        {
            (mainWindowLogic, oldCategoryItem) = (_mainWindowLogic, categoryItem);

            Name = categoryItem.Name;
            Description = categoryItem.Description;

            //type = categoryItem.Type;
            //TypeEnabled = add;
        }

        #endregion

        #region UI properties

        // Name
        public string Name { get; set; }

        // Description
        public string Description { get; set; }

        #endregion

        #region Result

        public CategoryItem NewCategoryItem => new CategoryItem(oldCategoryItem.ID, Name, Description, oldCategoryItem.IsIncome, oldCategoryItem.TaxInfo, oldCategoryItem.Parent);

        #endregion

        #region Actions

        protected override bool? Commit()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                mainWindowLogic.ErrorMessage("Category name cannot be blank");
                return null;
            }

            foreach (Household.CategoriesRow cat in mainWindowLogic.Household.Categories.Rows)
            {
                if (cat.ID != oldCategoryItem.ID && cat.Name == Name)
                {
                    // ZZZ May be too restrictive.
                    mainWindowLogic.ErrorMessage("There is already a category with this name");
                    return null;
                }
            }

            // ZZZZ
            bool change =
                oldCategoryItem.Name != Name ||
                oldCategoryItem.Description != Description;

            return change;
        }

        #endregion
    }
}
