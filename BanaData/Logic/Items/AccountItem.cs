using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.Attributes;
using BanaData.Database;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Immutable class describing an account for the UI
    /// </summary>
    public class AccountItem
    {
        // Explicit constructor
        public AccountItem(int id, string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind, bool hidden) =>
            (ID, Name, Description, Type, CreditLimit, InvestmentKind, Hidden) = (id, name, description, type, creditLimit, kind, hidden);

        // Clone with a new ID
        public AccountItem(AccountItem src, int id) =>
            (ID, Name, Description, Type, CreditLimit, InvestmentKind, Hidden) = 
            (id, src.Name, src.Description, src.Type, src.CreditLimit, src.InvestmentKind, src.Hidden);

        // Factory from DB row
        public static AccountItem CreateFromDB(Household.AccountsRow accountsRow)
        {
            var desc = accountsRow.IsDescriptionNull() ? "" : accountsRow.Description;
            decimal creditLimit = accountsRow.IsCreditLimitNull() ? 0 : accountsRow.CreditLimit;
            EInvestmentKind kind = accountsRow.IsIKindNull() ? EInvestmentKind.Invalid : accountsRow.Kind;

            return new AccountItem(accountsRow.ID, accountsRow.Name, desc, accountsRow.Type, creditLimit, kind, accountsRow.Hidden);
        }

        public readonly int ID;

        public string Name { get; }
        public string Description { get; }
        public EAccountType Type { get; }
        public string TypeString => EnumDescriptionAttribute.GetDescription(Type);
        public decimal CreditLimit { get; }
        public EInvestmentKind InvestmentKind { get; }
        public string InvestmentKindString => EnumDescriptionAttribute.GetDescription(InvestmentKind);
        public bool Hidden { get; }
    }
}
