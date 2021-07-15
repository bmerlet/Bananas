using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.Attributes;
using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Logic.Items;
using BanaData.Database;

namespace BanaData.Logic.Dialogs
{
    public class EditAccountLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly AccountItem oldAccountItem;

        #endregion

        #region Constructor

        public EditAccountLogic(MainWindowLogic _mainWindowLogic, AccountItem accountItem, bool add)
        {
            (mainWindowLogic, oldAccountItem) = (_mainWindowLogic, accountItem);

            Name = accountItem.Name;
            Description = accountItem.Description;

            type = accountItem.Type;
            TypeEnabled = add; // Can only change the type on new accounts

            CreditLimit = accountItem.CreditLimit;

            investmentKind = accountItem.InvestmentKind;

            IsHidden = accountItem.Hidden;

            var owners = new List<string>();
            owners.AddRange(mainWindowLogic.Household.Person.Rows.Cast<Household.PersonRow>().Select<Household.PersonRow, string>(p => p.Name));
            owners.Sort();
            owners.Insert(0, "<None>");
            Owners = owners.ToArray();
            Owner = accountItem.Owner ?? Owners[0];
        }

        #endregion

        #region UI properties

        // Name
        public string Name { get; set; }
        
        // Description
        public string Description { get; set; }

        // Type
        private EAccountType type;
        public string Type
        {
            get => EnumDescriptionAttribute.GetDescription(type);
            set
            {
                type = EnumDescriptionAttribute.MatchDescription<EAccountType>(value);
                OnPropertyChanged(() => CreditLimitEnabled);
                OnPropertyChanged(() => InvestmentKindEnabled);
            }
        }
        public string[] TypeSource { get; } = EnumDescriptionAttribute.GetDescriptions<EAccountType>();
        public bool? TypeEnabled { get; private set; }

        // Credit limit
        public decimal CreditLimit { get; set; }
        public bool? CreditLimitEnabled => type == EAccountType.CreditCard;

        // Investment kind
        private EInvestmentKind investmentKind;
        public string InvestmentKind
        {
            get => EnumDescriptionAttribute.GetDescription(investmentKind);
            set => investmentKind = EnumDescriptionAttribute.MatchDescription<EInvestmentKind>(value);
        }
        public string[] InvestmentKindSource { get; } = EnumDescriptionAttribute.GetDescriptions<EInvestmentKind>();
        public bool? InvestmentKindEnabled => type == EAccountType.Investment;

        // Hidden account
        public bool? IsHidden { get; set; }
        public bool? IsHiddenEnabled => true;

        // Owner
        public string[] Owners { get; }
        public string Owner { get; set; }

        #endregion

        #region Result

        public AccountItem NewAccountItem => new AccountItem(oldAccountItem.AccountRow, Name, Description, type, CreditLimit, investmentKind, IsHidden == true, Owner == Owners[0] ? null : Owner); 

        #endregion

        #region Actions

        protected override bool? Commit()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                mainWindowLogic.ErrorMessage("Account name cannot be blank");
                return null;
            }

            foreach (Household.AccountRow acct in mainWindowLogic.Household.Account.Rows)
            {
                if (acct != oldAccountItem.AccountRow && acct.Name == Name)
                {
                    mainWindowLogic.ErrorMessage("There is already an account with this name");
                    return null;
                }
            }


            if (type == EAccountType.Investment && investmentKind == EInvestmentKind.Invalid)
            {
                mainWindowLogic.ErrorMessage("Investment type must be specified");
                return null;
            }

            string owner = Owner == Owners[0] ? null : Owner;

            bool change =
                oldAccountItem.Name != Name ||
                oldAccountItem.Description != Description ||
                oldAccountItem.Type != type ||
                oldAccountItem.CreditLimit != CreditLimit ||
                oldAccountItem.InvestmentKind != investmentKind ||
                oldAccountItem.Hidden != (IsHidden == true) ||
                oldAccountItem.Owner != owner;

            return change;
        }

        #endregion
    }
}
