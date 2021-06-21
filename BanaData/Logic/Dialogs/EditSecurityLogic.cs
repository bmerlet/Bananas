using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.Attributes;
using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using BanaData.Database;

namespace BanaData.Logic.Dialogs
{
    public class EditSecurityLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly SecurityItem oldSecurityItem;

        #endregion

        #region Constructor

        public EditSecurityLogic(MainWindowLogic _mainWindowLogic, SecurityItem securityItem)
        {
            (mainWindowLogic, oldSecurityItem) = (_mainWindowLogic, securityItem);

            Name = securityItem.Name;
            Symbol = securityItem.Symbol;
            type = securityItem.Type;
        }

        #endregion

        #region UI properties

        // Name
        public string Name { get; set; }

        // Symbol
        public string Symbol { get; set; }

        // Type
        private ESecurityType  type;
        public string Type
        {
            get => EnumDescriptionAttribute.GetDescription(type);
            set => type = EnumDescriptionAttribute.MatchDescription<ESecurityType>(value);
        }
        public string[] TypeSource { get; } = EnumDescriptionAttribute.GetDescriptions<ESecurityType>();

        #endregion

        #region Result

        public SecurityItem NewSecurityItem { get; private set; }

        #endregion

        #region Actions

        protected override bool? Commit()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                mainWindowLogic.ErrorMessage("Security name cannot be blank");
                return null;
            }

            if (string.IsNullOrWhiteSpace(Symbol))
            {
                mainWindowLogic.ErrorMessage("Security symbol cannot be blank");
                return null;
            }

            foreach (Household.SecuritiesRow security in mainWindowLogic.Household.Securities.Rows)
            {
                if (security.ID != oldSecurityItem.ID)
                {
                    if (security.Name == Name)
                    {
                        mainWindowLogic.ErrorMessage("There is already a security with this name");
                        return null;
                    }
                    if (!security.IsSymbolNull() && security.Symbol == Symbol)
                    {
                        mainWindowLogic.ErrorMessage("There is already a security with this symbol");
                        return null;
                    }
                }
            }

            bool change =
                oldSecurityItem.Name != Name ||
                oldSecurityItem.Symbol != Symbol ||
                oldSecurityItem.Type != type;

            if (change)
            {
                NewSecurityItem = new SecurityItem(oldSecurityItem.ID, Name, Symbol, type);
            }

            return change;
        }

        #endregion
    }
}
