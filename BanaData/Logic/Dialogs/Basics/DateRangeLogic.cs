using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.Attributes;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Basics
{
    /// <summary>
    /// Logic for a date range user control
    /// </summary>
    public class DateRangeLogic : LogicBase
    {
        #region Private members

        private readonly Func<DateTime> getEarliestAvailableDate;

        #endregion

        #region Constructor

        public DateRangeLogic(ERange _dateRange, Func<DateTime> _getEarliestAvailableDate)
        {
            SetDateRange(_dateRange);
            getEarliestAvailableDate = _getEarliestAvailableDate;
        }

        #endregion

        #region Enums

        public enum ERange
        {
            [EnumDescription("Last week")]
            LastWeek,

            [EnumDescription("Last month")]
            LastMonth,

            [EnumDescription("Last quarter")]
            LastQuarter,

            [EnumDescription("Year-to-date")]
            YearToDate,

            [EnumDescription("Last year")]
            LastYear,

            [EnumDescription("Last five years")]
            LastFiveYears,

            [EnumDescription("Last ten years")]
            LastTenYears,

            [EnumDescription("All available")]
            AllAvailable,

            [EnumDescription("Custom")]
            Custom
        }

        #endregion

        #region Events

        public EventHandler<DateRangeChangedArgs> DateRangeChanged;

        public class DateRangeChangedArgs : EventArgs
        {
            public DateRangeChangedArgs(DateTime startDate, DateTime endDate) => (StartDate, EndDate) = (startDate, endDate);
            public readonly DateTime StartDate;
            public readonly DateTime EndDate;
        }

        #endregion

        #region UI properties

        //
        // Date range combo box
        //
        public string[] DateRangesSource => EnumDescriptionAttribute.GetDescriptions<ERange>();
        private ERange dateRange = ERange.LastTenYears;
        public string DateRange { get => EnumDescriptionAttribute.GetDescription(dateRange); set => SetDateRange(EnumDescriptionAttribute.MatchDescription<ERange>(value)); }

        // Custom dates enabled
        public bool? AreDatesEnabled => dateRange == ERange.Custom;

        // Custom start date
        private DateTime startDate;
        public DateTime StartDate { get => startDate; set => SetCustomDateRange(value, endDate); }

        // Custom end date
        private DateTime endDate;
        public DateTime EndDate { get => endDate; set => SetCustomDateRange(startDate, value); }

        #endregion

        #region Actions

        private void SetDateRange(ERange value)
        {
            dateRange = value;

            switch (dateRange)
            {
                case ERange.LastWeek:
                    endDate = DateTime.Today;
                    startDate = endDate.AddDays(-7);
                    break;
                case ERange.LastMonth:
                    endDate = DateTime.Today;
                    startDate = endDate.AddMonths(-1);
                    break;
                case ERange.LastQuarter:
                    endDate = DateTime.Today;
                    startDate = endDate.AddMonths(-3);
                    break;
                case ERange.LastYear:
                    endDate = DateTime.Today;
                    startDate = endDate.AddYears(-1);
                    break;
                case ERange.YearToDate:
                    endDate = DateTime.Today;
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    break;
                case ERange.LastFiveYears:
                    endDate = DateTime.Today;
                    startDate = endDate.AddYears(-5);
                    break;
                case ERange.LastTenYears:
                    endDate = DateTime.Today;
                    startDate = endDate.AddYears(-10);
                    break;
                case ERange.AllAvailable:
                    endDate = DateTime.Today;
                    startDate = getEarliestAvailableDate();
                    break;
                case ERange.Custom:
                    break;
            }

            OnPropertyChanged(() => StartDate);
            OnPropertyChanged(() => EndDate);
            OnPropertyChanged(() => AreDatesEnabled);

            DateRangeChanged?.Invoke(this, new DateRangeChangedArgs(StartDate, EndDate));
        }

        private void SetCustomDateRange(DateTime startDate, DateTime endDate)
        {
            this.startDate = startDate;
            this.endDate = endDate;

            DateRangeChanged?.Invoke(this, new DateRangeChangedArgs(StartDate, EndDate));
        }

        #endregion
    }
}
