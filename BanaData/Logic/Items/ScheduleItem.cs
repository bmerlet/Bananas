using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Attributes;
using BanaData.Database;
using BanaData.Logic.Main;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Immutable class representing a schedule for the UI
    /// </summary>
    public class ScheduleItem
    {
        // Constructors
        public ScheduleItem(
            int id, DateTime nextDate, DateTime endDate, EScheduleFrequency frequency, EScheduleFlag flags,
            int transactionID, string account, string medium, string payee, string memo, LineItem[] lineItems)
        {
            (ID, NextDate, EndDate, Frequency, Flags) = (id, nextDate, endDate, frequency, flags);
            (TransactionID, Account, Medium, Payee, Memo, LineItems) = (transactionID, account, medium, payee, memo, lineItems);
        }

        public ScheduleItem(MainWindowLogic mainWindowLogic, Household.ScheduleRow scheduleRow)
        {
            (ID, NextDate, EndDate, Frequency, Flags) = (scheduleRow.ID, scheduleRow.NextDate, scheduleRow.EndDate, scheduleRow.Frequency, scheduleRow.Flags);
            var transactionRow = scheduleRow.TransactionRow;
            TransactionID = transactionRow.ID;
            Account = transactionRow.AccountRow.Name;
            Payee = transactionRow.IsPayeeNull() ? "" : transactionRow.Payee;
            Memo = transactionRow.IsMemoNull() ? "" : transactionRow.Memo;
            var lineItemRows = transactionRow.GetLineItemRows();
            var lis = new List<LineItem>();
            if (transactionRow.AccountRow.Type == EAccountType.Bank)
            {
                Medium = EnumDescriptionAttribute.GetDescription(transactionRow.GetBankingTransaction().Medium);
            }
            foreach(var lir in lineItemRows)
            {
                lis.Add(new LineItem(mainWindowLogic, lir, true));
            }
            LineItems = lis.ToArray();
        }

        // Logic properties
        public readonly int ID;
        public readonly int TransactionID;
        public readonly LineItem[] LineItems;

        // UI properties

        // Scheduling
        public DateTime NextDate { get; }
        public DateTime EndDate { get; }
        public EScheduleFrequency Frequency { get; }
        public EScheduleFlag Flags { get; }

        // Transaction
        public string Account { get; }
        public string Medium { get; }
        public string Payee { get; }
        public string Memo { get; }
        public string Category => LineItems.Length == 1 ? LineItems[0].Category : "<Split>";
        public decimal Amount => LineItems.Sum(li => li.Amount);
    }
}
