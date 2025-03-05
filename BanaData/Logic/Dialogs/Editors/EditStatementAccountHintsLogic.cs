using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic.Dialogs;

using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using System.Xml.Linq;
using Toolbox.Attributes;

namespace BanaData.Logic.Dialogs.Editors
{
    public class EditStatementAccountHintsLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;
        private readonly StatementAccountHintItem oldHint;

        #endregion
        #region Constructor

        public EditStatementAccountHintsLogic(MainWindowLogic _mainWindowLogic, Household _household, StatementAccountHintItem hintItem, bool add)
        {
            (mainWindowLogic, household, oldHint) = (_mainWindowLogic, _household, hintItem);

            institution = hintItem.Institution;
            AccountName = hintItem.AccountName;
            AccountNameSource = household.Account.Rows.Cast<Household.AccountRow>().Where(a => !a.Hidden).Select<Household.AccountRow, string>(a => a.Name).ToArray();
            Array.Sort(AccountNameSource);
            minPage = hintItem.MinPage;
            maxPage = hintItem.MaxPage;
            StringsForEdit = hintItem.StringsForEdit;
        }

        #endregion

        #region UI properties

        // Institution
        private EInstitution institution;
        public string Institution
        {
            get => EnumDescriptionAttribute.GetDescription(institution);
            set => institution = EnumDescriptionAttribute.MatchDescription<EInstitution>(value);
        }
        public string[] InstitutionSource { get; } = EnumDescriptionAttribute.GetDescriptions<EInstitution>();

        // Account name
        public string AccountName { get; set; }
        public string[] AccountNameSource { get; }

        // Min page
        private int minPage;
        public string MinPage
        {
            get => minPage.ToString();
            set => int.TryParse(value, out minPage);
        }

        // Max page
        private int maxPage;
        public string MaxPage
        {
            get => maxPage.ToString();
            set => int.TryParse(value, out maxPage);
        }

        // Strings to edit
        public string StringsForEdit { get; set;  }

        #endregion

        #region Result

        public StatementAccountHintItem NewStatementAccountHintItem
        { 
            get
            {
                var strings = GetArrayFromEditString();

                return new StatementAccountHintItem(
                    institution,
                    AccountName,
                    minPage,
                    maxPage,
                    strings,
                    oldHint.StatementAccountHintRow);
            }
        }

        public bool IsChangingOtherThanStrings =>
                oldHint.Institution != institution ||
                oldHint.AccountName != AccountName ||
                oldHint.MinPage != minPage ||
                oldHint.MaxPage != maxPage;

        public bool IsChangingStrings => oldHint.StringsForEdit != StringsForEdit;


        #endregion

        #region Actions

        protected override bool? Commit()
        {
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                mainWindowLogic.ErrorMessage("Account name cannot be blank");
                return null;
            }

            if (institution != EInstitution.Vanguard && institution != EInstitution.Chase)
            {
                mainWindowLogic.ErrorMessage("Invalid institution");
                return null;
            }

            if (minPage <= 0)
            {
                mainWindowLogic.ErrorMessage("Start page must be greater than 0");
                return null;
            }

            if (maxPage <= 0)
            {
                mainWindowLogic.ErrorMessage("End page must be greater than 0");
                return null;
            }

            if (maxPage < minPage)
            {
                mainWindowLogic.ErrorMessage("End page must be greater than or equal to end page");
                return null;
            }

            var strings = GetArrayFromEditString();
            if (strings.Length == 0)
            {
                mainWindowLogic.ErrorMessage("There must be at least one string to search for");
                return null;
            }

            bool change =
                oldHint.Institution != institution ||
                oldHint.AccountName != AccountName ||
                oldHint.MinPage != minPage ||
                oldHint.MaxPage != maxPage ||
                oldHint.StringsForEdit != StringsForEdit;

            return change;
        }

        private string[] GetArrayFromEditString()
        {
            // Remove \r
            var str = StringsForEdit.Replace("\r\n", "\n");

            // Remove empty lines
            while (str.Contains("\n\n"))
            {
                str = StringsForEdit.Replace("\n\n", "\n");
            }
            if (str.StartsWith("\n"))
            {
                str = str.Substring(1);
            }
            if (str.EndsWith("\n"))
            {
                str = str.Substring(0, str.Length - 1);
            }

            return str.Split('\n');
        }

        #endregion
    }
}
