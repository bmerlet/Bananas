using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;

namespace BanaData.Logic.Items
{
    public class CategoryItem : IComparable<CategoryItem>
    {
        // Explicitely build a regular category
        public CategoryItem(int id, string name, string description, bool isIncome, string taxInfoKey, CategoryItem parent)
        {
            (ID, Name, Description, IsIncome, TaxInfoKey, Parent, FullName, AccountID) =
                (id, name, description, isIncome, taxInfoKey, parent, parent == null ? name : $"{parent.FullName}:{name}", -1);

            TaxInfo = TaxInfoDictionary.TryGetValue(taxInfoKey, out string taxInfoVal) ? taxInfoVal : taxInfoKey;
        }

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

        // Build from DB
        static public CategoryItem CreateFromDB(Household.CategoryRow categoryRow, IEnumerable<CategoryItem> categoryItems)
        {
            string description = categoryRow.IsDescriptionNull() ? "" : categoryRow.Description;
            string taxInfo = categoryRow.IsTaxInfoNull() ? "" : categoryRow.TaxInfo;
            CategoryItem parent = categoryRow.IsParentIDNull() ? null : categoryItems.FirstOrDefault(c => c.ID == categoryRow.ParentID);
            return new CategoryItem(categoryRow.ID, categoryRow.Name, description, categoryRow.IsIncome, taxInfo, parent);
        }

        // IDs
        public readonly int ID;
        public readonly CategoryItem Parent;
        public readonly int AccountID;
        public readonly string TaxInfoKey;

        // UI properties
        public string Name { get; }
        public bool Hidden => Name.StartsWith("_");
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

        // Sort on full name
        public int CompareTo(CategoryItem other)
        {
            // Categories first, transfers next
            if (AccountID >= 0 && other.AccountID < 0)
            {
                return 1;
            }
            if (AccountID < 0 && other.AccountID >= 0)
            {
                return -1;
            }

            return FullName.CompareTo(other.FullName);
        }

        // Tax info
        static public IReadOnlyDictionary<string, string> TaxInfoDictionary => taxInfoDictionary;
        static private readonly Dictionary<string, string> taxInfoDictionary = new Dictionary<string, string>();

        static CategoryItem()
        {
            taxInfoDictionary.Add("", "");
            taxInfoDictionary.Add("257", "Form 1040: Other income, misc.");
            taxInfoDictionary.Add("262", "Form 1040: IRA contribution, self");
            taxInfoDictionary.Add("273", "Schedule A: Medecine and drugs");
            taxInfoDictionary.Add("274", "Schedule A: Medical travel and lodging");
            taxInfoDictionary.Add("276", "Schedule A: Real estate taxes");
            taxInfoDictionary.Add("280", "Schedule A: Cash charity contributions");
            taxInfoDictionary.Add("282", "Schedule A: Investment management fee");
            taxInfoDictionary.Add("283", "Schedule A: Home mortgage interest (1098)");
            taxInfoDictionary.Add("286", "Schedule B: Dividend income");
            taxInfoDictionary.Add("287", "Schedule B: Interest income");
            taxInfoDictionary.Add("331", "Schedule E: Commision");
            taxInfoDictionary.Add("332", "Schedule E: Insurance");
            taxInfoDictionary.Add("336", "Schedule E: Repairs");
            taxInfoDictionary.Add("337", "Schedule E: Supplies");
            taxInfoDictionary.Add("338", "Schedule E: Taxes");
            taxInfoDictionary.Add("339", "Schedule E: Utilities");
            taxInfoDictionary.Add("341", "Schedule E: Other expenses");
            taxInfoDictionary.Add("401", "Form 2441: Qualifying childcare expenses");
            taxInfoDictionary.Add("426", "Form 4952: Investment interest expense");
            taxInfoDictionary.Add("460", "W-2: Salary or wages, self");
            taxInfoDictionary.Add("461", "W-2: Federal tax withheld, self");
            taxInfoDictionary.Add("462", "W-2: Soc. Sec. tax withheld, self");
            taxInfoDictionary.Add("464", "W-2: State tax withheld, self");
            taxInfoDictionary.Add("480", "W-2: Medicare tax withheld, self");
            taxInfoDictionary.Add("484", "Schedule A: Doctors, dentists, hospitals");
            taxInfoDictionary.Add("488", "Schedule D: Dividend income, cap gain distribution");
            taxInfoDictionary.Add("489", "Schedule B: Interest income, non-taxable");
            taxInfoDictionary.Add("487", "Schedule B: Dividend income, non-taxable");
            taxInfoDictionary.Add("502", "Schedule E: Management fees");
            taxInfoDictionary.Add("506", "W-2: Salary or wages, spouse");
            taxInfoDictionary.Add("521", "Form 1040: Federal estimated taxes, quarterly");
            taxInfoDictionary.Add("535", "Schedule A: Personal property taxes");
            taxInfoDictionary.Add("1003", "Schedule E: Unspecified rental income");
        }
    }
}
