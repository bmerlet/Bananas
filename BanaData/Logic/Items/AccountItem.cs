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
        public AccountItem(int id, string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind) =>
            (ID, Name, Description, Type, CreditLimit, InvestmentKind) = (id, name, description, type, creditLimit, kind);

        public readonly int ID;

        public string Name { get; }
        public string Description { get; }
        public EAccountType Type { get; }
        public string TypeString => EnumDescriptionAttribute.GetDescription(Type);
        public decimal CreditLimit { get; }
        public EInvestmentKind InvestmentKind { get; }
    }
}
