using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Main;

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
            IEnumerable<Household.AccountRow> accounts,
            bool isFilteringOnPayees,
            IEnumerable<string> payees,
            bool isFilteringOnCategories,
            IEnumerable<Household.CategoryRow> categories)
        {
            (TransactionReportRow, Name, Description, StartDate, EndDate, IsFilteringOnAccounts, Accounts, IsFilteringOnPayees, Payees, IsFilteringOnCategories, Categories) =
                (transactionReportRow, name, description, startDate, endDate, isFilteringOnAccounts, accounts, isFilteringOnPayees, payees, isFilteringOnCategories, categories);
        }

        static public TransactionReportItem CreateEmpty()
        {
            return new TransactionReportItem(
                null, "", "", DateTime.Today, DateTime.Today,
                false, Enumerable.Empty<Household.AccountRow>(),
                false, Enumerable.Empty<string>(),
                false, Enumerable.Empty<Household.CategoryRow>());
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
                transactionReportRow.GetTransactionReportAccountRows().Select(trar => trar.AccountRow),
                transactionReportRow.IsFilteringOnPayees,
                transactionReportRow.GetTransactionReportPayeeRows().Select(trpr => trpr.Payee),
                transactionReportRow.IsFilteringOnCategories,
                transactionReportRow.GetTransactionReportCategoryRows().Select(trcr => trcr.CategoryRow));
        }

        public readonly Household.TransactionReportRow TransactionReportRow;

        public string Name { get; }
        public string Description { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public bool IsFilteringOnAccounts { get; }
        public IEnumerable<Household.AccountRow> Accounts { get; }
        public bool IsFilteringOnPayees { get; }
        public IEnumerable<string> Payees { get; }
        public bool IsFilteringOnCategories { get; }
        public IEnumerable<Household.CategoryRow> Categories { get; }
    }
}
