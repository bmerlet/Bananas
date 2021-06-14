using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Logic.Items
{
    public class CategoryItem
    {
        // Explicitely build a regular category
        public CategoryItem(int id, string name, string description, bool isIncome, string taxInfo, CategoryItem parent) =>
            (ID, Name, Description, IsIncome, TaxInfo, Parent, FullName, AccountID) =
            (id, name, description, isIncome, taxInfo, parent, parent == null ? name : $"{parent.FullName}:{name}", -1);

        // Explicitely build a transfer category
        public CategoryItem (int accountID, string accountName)
        {
            string name = $"[{accountName}]";
            (ID, Name, Description, IsIncome, TaxInfo, Parent, FullName, AccountID) = 
                (-1, name, null, false, null, null, name, accountID);
        }

        // Clone a regular category changing the ID
        public CategoryItem(CategoryItem src, int id) =>
            (ID, Name, Description, IsIncome, TaxInfo, Parent, FullName, AccountID) =
            (id, src.Name, src.Description, src.IsIncome, src.TaxInfo, src.Parent, src.FullName, -1);

        // IDs
        public readonly int ID;
        public readonly CategoryItem Parent;
        public readonly int AccountID;

        // UI properties
        public string Name { get; }
        public string FullName { get; }
        public string Description { get; }
        public bool IsIncome { get; }
        public string TaxInfo { get; }

        private string indentedName;
        public string IndentedName => GetIndentedName();
        public bool FontWeight => Parent == null;

        public string TypeString => (ID >= 0) ? (IsIncome ? "Income" : "Expense") : (AccountID >= 0 ? "Transfer" : "");

        private string GetIndentedName()
        {
            if (indentedName == null)
            {
                indentedName = Name;
                for(var p = Parent; p != null; p = p.Parent)
                {
                    indentedName = "  " + indentedName;
                }
            }
            return indentedName;
        }


        public bool IsDescendantOf(CategoryItem presumedParent)
        {
            for (CategoryItem cat = Parent; cat != null; cat = cat.Parent)
            {
                if (cat.ID == presumedParent.ID)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            return
                obj is CategoryItem o &&
                o.ID == ID &&
                o.AccountID == AccountID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode() + AccountID.GetHashCode();
        }

    }
}
