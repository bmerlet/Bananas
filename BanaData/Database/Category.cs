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
        partial class CategoriesRow
        {
            private string fullName = null;

            public string FullName => GetFullName();

            public void ResetFullName()
            {
                fullName = null;
            }

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
                        var parentRow = (Table as CategoriesDataTable).Single(c => c.ID == ParentID);

                        // Recurse
                        fullName = parentRow.FullName + ":" + Name;
                    }
                }

                return fullName;
            }
        }

        // Extensions to the category table
        partial class CategoriesDataTable
        {
            public CategoriesRow GetByFullName(string name)
            {
                return this.FirstOrDefault(cat => cat.FullName == name);
            }

            public CategoriesRow GetByParentAndName(CategoriesRow parentRow, string name)
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
            public CategoriesRow Add(string name, string description, CategoriesRow parent, bool income, string taxInfo)
            {
                var catRow = NewCategoriesRow();

                UpdateCategory(catRow, name, description, parent, income, taxInfo);

                Rows.Add(catRow);

                return catRow;
            }

            public CategoriesRow Update(CategoriesRow catRow, string name, string description, CategoriesRow parent, bool income, string taxInfo)
            {
                UpdateCategory(catRow, name, description, parent, income, taxInfo);

                return catRow;
            }

            public bool HasSame(CategoriesRow catRow, string description, bool income, string taxInfo)
            {
                if (catRow.IsDescriptionNull())
                {
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        return false;
                    }
                }
                else if (catRow.Description != description)
                {
                    return false;
                }

                if (catRow.IsIncome != income)
                {
                    return false;
                }

                if (catRow.IsTaxInfoNull())
                {
                    if (!string.IsNullOrWhiteSpace(taxInfo))
                    {
                        return false;
                    }
                }
                else if (catRow.TaxInfo != taxInfo)
                {
                    return false;
                }

                return true;
            }

            private static CategoriesRow UpdateCategory(CategoriesRow catRow, string name, string description, CategoriesRow parent, bool income, string taxInfo)
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
