using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Main;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Transaction report item, as shown by the UI
    /// </summary>
    public class TransactionReportItem
    {
        public TransactionReportItem(
            Household.TransactionReportRow transactionReportRow,
            string name,
            string description,
            DateTime startDate,
            DateTime endDate,
            ETransactionReportFlag flags,
            IEnumerable<Household.AccountRow> accounts,
            IEnumerable<string> payees,
            IEnumerable<Household.CategoryRow> categories)
        {
            (TransactionReportRow, Name, Description, StartDate, EndDate) = (transactionReportRow, name, description, startDate, endDate);
            (Flags, Accounts, Payees, Categories) = (flags, accounts.ToArray(), payees.ToArray(), categories.ToArray());
        }

        static public TransactionReportItem CreateEmpty()
        {
            return new TransactionReportItem(
                null, "", "", DateTime.Today, DateTime.Today,
                ETransactionReportFlag.ShowTransactions,
                Enumerable.Empty<Household.AccountRow>(),
                Enumerable.Empty<string>(),
                Enumerable.Empty<Household.CategoryRow>());
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
                transactionReportRow.Flags,
                transactionReportRow.GetTransactionReportAccountRows().Select(trar => trar.AccountRow),
                transactionReportRow.GetTransactionReportPayeeRows().Select(trpr => trpr.Payee),
                transactionReportRow.GetTransactionReportCategoryRows().Select(trcr => trcr.CategoryRow));
        }

        public readonly Household.TransactionReportRow TransactionReportRow;

        public string Name { get; }
        public string Description { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public ETransactionReportFlag Flags { get; }

        public bool IsShowingTransactions => Flags.HasFlag(ETransactionReportFlag.ShowTransactions);
        public bool IsShowingSubtotals => Flags.HasFlag(ETransactionReportFlag.ShowSubtotals);

        public bool IsGroupingByAccount => Flags.HasFlag(ETransactionReportFlag.GroupByAccount);
        public bool IsGroupingByPayee => Flags.HasFlag(ETransactionReportFlag.GroupByPayee);
        public bool IsGroupingByCategory => Flags.HasFlag(ETransactionReportFlag.GroupByCategory);

        public bool IsShowingAccountColumn => Flags.HasFlag(ETransactionReportFlag.ShowAccountColumn);
        public bool IsShowingDateColumn => Flags.HasFlag(ETransactionReportFlag.ShowDateColumn);
        public bool IsShowingPayeeColumn => Flags.HasFlag(ETransactionReportFlag.ShowPayeeColumn);
        public bool IsShowingMemoColumn => Flags.HasFlag(ETransactionReportFlag.ShowMemoColumn);
        public bool IsShowingCategoryColumn => Flags.HasFlag(ETransactionReportFlag.ShowCategoryColumn);

        public bool IsFilteringOnAccounts => Flags.HasFlag(ETransactionReportFlag.IsFilteringOnAccounts);
        public IEnumerable<Household.AccountRow> Accounts { get; }

        public bool IsFilteringOnPayees => Flags.HasFlag(ETransactionReportFlag.IsFilteringOnPayees);
        public IEnumerable<string> Payees { get; }

        public bool IsFilteringOnCategories => Flags.HasFlag(ETransactionReportFlag.IsFilteringOnCategories);
        public IEnumerable<Household.CategoryRow> Categories { get; }
    }
}
