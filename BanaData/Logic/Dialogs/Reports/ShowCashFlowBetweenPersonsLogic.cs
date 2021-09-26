using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowCashFlowBetweenPersonsLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public ShowCashFlowBetweenPersonsLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Setup years
            YearPickerLogic = new YearPickerLogic(mainWindowLogic);
            YearPickerLogic.YearChanged += (s, e) => ComputeCashFlow();

            // Setup members
            Members = mainWindowLogic.Household.Person.ToArray();
            // ZZZ Reorder manually
            if (Members.Length >= 3)
            {
                if (Members[1].Name.StartsWith("Su"))
                {
                    var s = Members[1];
                    Members[1] = Members[0];
                    Members[0] = s;
                }
                else if (Members[2].Name.StartsWith("Su"))
                {
                    var s = Members[2];
                    Members[2] = Members[0];
                    Members[0] = s;
                }
                if (Members[2].Name.StartsWith("Be"))
                {
                    var s = Members[2];
                    Members[2] = Members[1];
                    Members[1] = s;
                }
            }

            // Compute!
            ComputeCashFlow();
        }

        #endregion

        #region UI properties

        // Year
        public YearPickerLogic YearPickerLogic { get; }

        // Frequency of expense/income report
        private const string FREQ_MONTHLY = "month";
        private const string FREQ_QUARTERLY = "quarter";
        private const string FREQ_YEARLY = "year";
        public string[] FrequencySource { get; } = new string[] { FREQ_MONTHLY, FREQ_QUARTERLY, FREQ_YEARLY };

        private string selectedFrequency = FREQ_QUARTERLY;
        public string SelectedFrequency { get => selectedFrequency; set { selectedFrequency = value; ComputeCashFlow(); } }

        // If grouping similar accounts
        private bool isGroupingAccounts = false;
        public bool? IsGroupingAccounts { get => isGroupingAccounts; set { isGroupingAccounts = value == true; ComputeCashFlow(); } }

        // RFU
        public CommandBase PickMembersCommand { get; }
        public Household.PersonRow[] Members;

        #endregion

        #region Actions

        private void ComputeCashFlow()
        {
            // Compute member assets at beginning of year

            // ZZZZ

            // Compute member assets at end of year
        }

        #endregion

        #region Support classes

        public class CashFlowItem
        {
            public CashFlowItem(DateTime date, string description, MemberItem[] memberItems) 
                => (Date, Description, MemberItems) = (date, description, memberItems);

            public DateTime Date { get; }
            public string Description { get; }
            public MemberItem[] MemberItems { get; }
        }

        public class MemberItem
        {
            public bool IsVisible { get; }
            public decimal Amount { get; }
            public decimal Balance { get; }
        }

        #endregion
    }
}
