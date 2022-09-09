using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Serializations;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Basics
{
    /// <summary>
    /// Simple logic for a combo box to select a year
    /// </summary>
    public class YearPickerLogic : LogicBase
    {
        public YearPickerLogic(Household household, UserSettings userSettings)
        {
            // Setup years
            var thisYear = DateTime.Today.Year;
            int firstYear;
            try
            {
                if (userSettings.HideClosedAccounts)
                {
                    firstYear = household.RegularTransactions.Where(tr => !tr.AccountRow.Hidden).Min(tr => tr.Date).Year;
                }
                else
                {
                    firstYear = household.RegularTransactions.Min(tr => tr.Date).Year;
                }
            }
            catch (Exception) 
            {
                // This is in case there are no transactions in the DB
                firstYear = thisYear;
            }

            var years = new List<int>();
            for (int i = thisYear; i >= firstYear; i--)
            {
                years.Add(i);
            }

            YearSource = years.ToArray();
            SelectedYear = years[0];
        }

        public EventHandler YearChanged;

        private int selectedYear;
        
        public int SelectedYear
        {
            get => selectedYear;
            set
            {
                if (selectedYear != value)
                {
                    selectedYear = value;
                    YearChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public int[] YearSource { get; }
    }
}
