using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Logic.Items
{
    public class CategoryItem
    {
        public CategoryItem(int id, string name, CategoryItem parent)
        {
            (ID, Name, Parent, FullName, AccountID) = (id, name, parent, parent == null ? name : $"{parent.FullName}:{name}", -1);
        }

        public CategoryItem (int accountID, string accountName)
        {
            string name = $"[{accountName}]";
            (ID, Name, Parent, FullName, AccountID) = (-1, name, null, name, accountID);
        }

        public readonly int ID = -1;
        public readonly CategoryItem Parent = null;
        public readonly int AccountID = -1;

        public string Name { get; }
        public string FullName { get; }
        public bool IsBold => Parent == null;

        public override string ToString()
        {
            return (Parent != null ? "  " : "") + Name;
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
