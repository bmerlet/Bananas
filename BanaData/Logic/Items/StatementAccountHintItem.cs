using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.Attributes;
using BanaData.Database;
using static BanaData.Database.Household;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Immutable class representing a statement hint for the UI
    /// </summary>
    public class StatementAccountHintItem
    {
        //
        // Constructors
        //
        // Explicit
        public StatementAccountHintItem(EInstitution institution, string accountName, int minPage, int maxPage, string[] strings, Household.StatementAccountHintRow row) =>
            (Institution, AccountName, MinPage, MaxPage, Strings, StatementAccountHintRow) = (institution, accountName, minPage, maxPage, strings, row);

        // From DB
        public StatementAccountHintItem(Household.StatementAccountHintRow sah)
        {
            StatementAccountHintRow = sah;
            Institution = sah.Institution;
            AccountName = sah.AccountRow.Name;
            MinPage = sah.MinPage;
            MaxPage = sah.MaxPage;
            var stringRows = sah.GetStatementAccountStringRows();
            Strings = stringRows.Select<Household.StatementAccountStringRow, String>(s => s.String).ToArray();
        }

        // Properties
        public readonly Household.StatementAccountHintRow StatementAccountHintRow;

        public EInstitution Institution { get; }
        public string InstitutionName => EnumDescriptionAttribute.GetDescription(Institution);

        public string AccountName { get; }

        public int MinPage { get; }
        public int MaxPage { get; }

        public string[] Strings { get; }
        public string StringsForEdit => String.Join("\n", Strings);
        public string StringsForList => "\"" + String.Join("\", \"", Strings) + "\"";
    }
}
