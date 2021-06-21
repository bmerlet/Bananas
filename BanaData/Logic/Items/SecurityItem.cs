using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BanaData.Database;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Immutable UI representation of a security
    /// </summary>
    public class SecurityItem
    {
        // Explicit constructor
        public SecurityItem(int id, string name, string symbol, ESecurityType type) =>
            (ID, Name, Symbol, Type) = (id, name, symbol, type);

        // Clone with ID change
        public SecurityItem(SecurityItem src, int id)
            : this(id, src.Name, src.Symbol, src.Type) { }

        // DB ID
        public readonly int ID;

        // Security name
        public string Name { get; }

        // Security symbol
        public string Symbol { get; }

        // Security type
        public ESecurityType Type { get; }
        public string TypeString => Toolbox.Attributes.EnumDescriptionAttribute.GetDescription(Type);
    }
}
