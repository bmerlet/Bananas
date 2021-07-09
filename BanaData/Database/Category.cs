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
            private string fullName = null;

            public string FullName => GetFullName();

            private string GetFullName()
            {
                if (fullName == null)
                {
                    if (IsParentIDNull())
                    {
                        // no parent, done
                        fullName = Name;
                    }
                    else
                    {
                        // Get parent
                        var parentRow = (Table as CategoryDataTable).Single(c => c.ID == ParentID);

                        // Recurse
                        fullName = parentRow.FullName + ":" + Name;
                    }
                }

                return fullName;
            }

            public bool HasSame(string description, bool income, string taxInfo)
            {
                if (IsDescriptionNull())
                {
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        return false;
                    }
                }
                else if (Description != description)
                {
                    return false;
                }

                if (IsIncome != income)
                {
                    return false;
                }

                if (IsTaxInfoNull())
                {
                    if (!string.IsNullOrWhiteSpace(taxInfo))
                    {
                        return false;
                    }
                }
                else if (TaxInfo != taxInfo)
                {
                    return false;
                }

                return true;
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
