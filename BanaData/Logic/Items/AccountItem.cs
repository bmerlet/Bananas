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
        public AccountItem(Household.AccountRow accountsRow, string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind, bool hidden, string owner) =>
            (AccountRow, Name, Description, Type, CreditLimit, InvestmentKind, Hidden, Owner) = (accountsRow, name, description, type, creditLimit, kind, hidden, owner);

        // Clone with a new accountRow
        public AccountItem(AccountItem src, Household.AccountRow accountsRow) =>
            (AccountRow, Name, Description, Type, CreditLimit, InvestmentKind, Hidden, Owner) = 
            (accountsRow, src.Name, src.Description, src.Type, src.CreditLimit, src.InvestmentKind, src.Hidden, src.Owner);

        // Factory from DB row
        public static AccountItem CreateFromDB(Household.AccountRow accountsRow)
        {
            var desc = accountsRow.IsDescriptionNull() ? "" : accountsRow.Description;
            EInvestmentKind kind = accountsRow.IsIKindNull() ? EInvestmentKind.Invalid : accountsRow.Kind;
            string owner = accountsRow.IsPersonIDNull() ? null : accountsRow.PersonRow.Name;

            return new AccountItem(accountsRow, accountsRow.Name, desc, accountsRow.Type, accountsRow.CreditLimit, kind, accountsRow.Hidden, owner);
        }

        // Account row (null when not in DB)
        public readonly Household.AccountRow AccountRow;

        // Properties
        public string Name { get; }
        public string Description { get; }
        public EAccountType Type { get; }
        public string TypeString => EnumDescriptionAttribute.GetDescription(Type);
        public decimal CreditLimit { get; }
        public EInvestmentKind InvestmentKind { get; }
        public string InvestmentKindString => EnumDescriptionAttribute.GetDescription(InvestmentKind);
        public bool Hidden { get; }
        public string Owner { get; }
    }
}
