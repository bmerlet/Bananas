using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;

namespace BanaData.Logic.Items
{
    public class TransactionReportItem
    {
        public TransactionReportItem(Household.TransactionReportRow transactionReportRow, string name, string description, DateTime startDate, DateTime endDate)
        {
            (TransactionReportRow, Name, Description, StartDate, EndDate) = (transactionReportRow, name, description, startDate, endDate);
        }

        static public TransactionReportItem CreateFromDB(Household.TransactionReportRow transactionReportRow)
        {
            string description = transactionReportRow.IsDescriptionNull() ? "" : transactionReportRow.Description;
            return new TransactionReportItem(transactionReportRow, transactionReportRow.Name, description, transactionReportRow.StartDate, transactionReportRow.EndDate);
        }

        public readonly Household.TransactionReportRow TransactionReportRow;

        public string Name { get; }
        public string Description { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
    }
}
