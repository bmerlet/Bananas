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

namespace BanaData.Logic.Dialogs.Editors
{
    public class EditSecurityLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;
        private readonly SecurityItem oldSecurityItem;

        #endregion

        #region Constructor

        public EditSecurityLogic(MainWindowLogic _mainWindowLogic, Household _household, SecurityItem securityItem)
        {
            (mainWindowLogic, household, oldSecurityItem) = (_mainWindowLogic, _household, securityItem);

            Name = securityItem.Name;
            Symbol = securityItem.Symbol;
            Type = EnumDescriptionAttribute.GetDescription(securityItem.Type);
        }

        #endregion

        #region UI properties

        // Name
        public string Name { get; set; }

        // Symbol
        public string Symbol { get; set; }
        public bool? SymbolEnabled { get; private set; }

        // Type
        private ESecurityType type;
        public string Type
        {
            get => EnumDescriptionAttribute.GetDescription(type);
            set
            {
                var oldType = type;
                type = EnumDescriptionAttribute.MatchDescription<ESecurityType>(value);
                if (oldType != type)
                {
                    switch(type)
                    {
                        case ESecurityType.Stock:
                        case ESecurityType.MutualFund:
                        case ESecurityType.MarketIndex:
                        case ESecurityType.EmployeeStockOption:
                            SymbolEnabled = true;
                            if (oldType == ESecurityType.Asset)
                            {
                                Symbol = "";
                            }
                            break;

                        case ESecurityType.Asset:
                            SymbolEnabled = false;
                            Symbol = "[Same as name]";
                            break;
                    }

                    InvokePropertyChanged(nameof(SymbolEnabled));
                    InvokePropertyChanged(nameof(Symbol));
                }
            }
        }
        public string[] TypeSource { get; } = new string[] {
            EnumDescriptionAttribute.GetDescription(ESecurityType.Stock),
            EnumDescriptionAttribute.GetDescription(ESecurityType.MutualFund),
            EnumDescriptionAttribute.GetDescription(ESecurityType.Asset),
            EnumDescriptionAttribute.GetDescription(ESecurityType.MarketIndex),
            EnumDescriptionAttribute.GetDescription(ESecurityType.EmployeeStockOption)
        };

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

            if (type == ESecurityType.Asset)
            {
                Symbol = Name;
            }

            if (string.IsNullOrWhiteSpace(Symbol))
            {
                mainWindowLogic.ErrorMessage($"Security symbol cannot be blank. Use '{Household.SecurityRow.SYMBOL_NONE}' for securities without a symbol.");
                return null;
            }

            foreach (Household.SecurityRow security in household.Security.Rows)
            {
                if (security.ID != oldSecurityItem.ID)
                {
                    if (security.Name == Name)
                    {
                        mainWindowLogic.ErrorMessage($"There is already a security with the name '{Name}'");
                        return null;
                    }
                    if (Symbol != Household.SecurityRow.SYMBOL_NONE && security.Symbol == Symbol)
                    {
                        mainWindowLogic.ErrorMessage($"There is already a security with the symbol '{Symbol}'");
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
