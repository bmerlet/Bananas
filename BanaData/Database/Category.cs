//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class CategoryRow
        {
            public string FullName => GetFullName();

            private string GetFullName()
            {
                string fullName = Name;

                if (!IsParentIDNull())
                {
                    // Get parent
                    var parentRow = GetParentCategoryRow();

                    // Recurse
                    fullName = parentRow.FullName + ":" + fullName;
                }

                return fullName;
            }

            public CategoryRow GetParentCategoryRow()
            {
                return IsParentIDNull() ? null : (Table as CategoryDataTable).FindByID(ParentID);
            }

        }

        // Extensions to the category table
        partial class CategoryDataTable
        {
            public CategoryRow GetByFullName(string name)
            {
                return this.FirstOrDefault(cat => cat.FullName == name);
            }

            public CategoryRow GetByParentAndName(CategoryRow parentRow, string name)
            {
                var lquery =
                    from cat in this
                    where
                        (cat.Name == name) &&
                        (((parentRow == null) && cat.IsParentIDNull()) ||
                         ((parentRow != null) && (cat.ParentID == parentRow.ID)))
                    select cat;

                var selected = lquery.ToArray();

                return (selected.Length == 0) ? null : selected[0];
            }

            // Adding a category
            public CategoryRow Add(string name, string description, CategoryRow parent, bool income, string taxInfo)
            {
                var catRow = NewCategoryRow();

                UpdateCategory(catRow, name, description, parent, income, taxInfo);

                Rows.Add(catRow);

                return catRow;
            }

            public CategoryRow Update(CategoryRow catRow, string name, string description, CategoryRow parent, bool income, string taxInfo)
            {
                UpdateCategory(catRow, name, description, parent, income, taxInfo);

                return catRow;
            }

            private static CategoryRow UpdateCategory(CategoryRow catRow, string name, string description, CategoryRow parent, bool income, string taxInfo)
            {
                catRow.Name = name;

                if (string.IsNullOrWhiteSpace(description))
                {
                    catRow.SetDescriptionNull();
                }
                else
                {
                    catRow.Description = description;
                }

                catRow.IsIncome = income;

                if (string.IsNullOrWhiteSpace(taxInfo))
                {
                    catRow.SetTaxInfoNull();
                }
                else
                {
                    catRow.TaxInfo = taxInfo;
                }

                if (parent == null)
                {
                    catRow.SetParentIDNull();
                }
                else
                {
                    catRow.ParentID = parent.ID;
                }

                return catRow;
            }
        }
    }
}
