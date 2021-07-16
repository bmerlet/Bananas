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
        public TransactionReportItem(
            Household.TransactionReportRow transactionReportRow, 
            string name, 
            string description,
            DateTime startDate,
            DateTime endDate,
            bool isFilteringOnAccounts,
            bool isFilteringOnPayees,
            bool isFilteringOnCategories)
        {
            (TransactionReportRow, Name, Description, StartDate, EndDate, IsFilteringOnAccounts, IsFilteringOnPayees, IsFilteringOnCategories) = 
                (transactionReportRow, name, description, startDate, endDate, isFilteringOnAccounts, isFilteringOnPayees, isFilteringOnCategories);
        }

        static public TransactionReportItem CreateFromDB(Household.TransactionReportRow transactionReportRow)
        {
            string description = transactionReportRow.IsDescriptionNull() ? "" : transactionReportRow.Description;
            return new TransactionReportItem(
                transactionReportRow, 
                transactionReportRow.Name,
                description,
                transactionReportRow.StartDate,
                transactionReportRow.EndDate,
                transactionReportRow.IsFilteringOnAccounts,
                transactionReportRow.IsFilteringOnPayees,
                transactionReportRow.IsFilteringOnCategories);
        }

        public readonly Household.TransactionReportRow TransactionReportRow;

        public string Name { get; }
        public string Description { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public bool IsFilteringOnAccounts { get; }
        public bool IsFilteringOnPayees { get; }
        public bool IsFilteringOnCategories { get; }
    }
}
