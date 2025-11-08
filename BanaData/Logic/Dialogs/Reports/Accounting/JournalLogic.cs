using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports.Accounting
{
    public class JournalLogic : LogicBase
    {
        #region Private members

        private readonly Household household;

        #endregion

        #region Constructor

        public JournalLogic(Household _household)
        {
            household = _household;

            // Setup date range
            DateRangeLogic = new DateRangeLogic(DateRangeLogic.ERange.YearToDate,
                () => household.RegularTransactions.Select(tr => tr.Date).Min());
            DateRangeLogic.DateRangeChanged += (s, e) => ComputeJournal();

            // Setup members
            foreach (var person in household.Person)
            {
                MembersSource.Add(new MemberItem(person));
            }
            var everybody = MemberItem.GetEverybodyItem();
            MembersSource.Add(everybody);
            selectedMember = everybody;

            // Give the list to the UI
            EntriesSource = (CollectionView)CollectionViewSource.GetDefaultView(entries);
            //EntriesSource.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
            //EntriesSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            // Compute!
            ComputeJournal();
        }

        #endregion

        #region UI properties

        // Date range
        public DateRangeLogic DateRangeLogic { get; }

        // Person
        public List<MemberItem> MembersSource { get; } = new List<MemberItem>();
        private MemberItem selectedMember;
        public MemberItem SelectedMember { get => selectedMember; set { selectedMember = value; ComputeJournal(); } }

        // Income statements
        private readonly ObservableCollection<JournalEntry> entries = new ObservableCollection<JournalEntry>();
        public CollectionView EntriesSource { get; }

        #endregion

        #region Actions

        private void ComputeJournal()
        {
            var startDate = DateRangeLogic.StartDate;
            var endDate = DateRangeLogic.EndDate;
            var member = selectedMember.Member;
            entries.Clear();

            // Loop on all transaction for the selected period and selected member
            foreach (var transactionRow in household.RegularTransactions
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate && (member == null || (!tr.AccountRow.IsPersonIDNull() && tr.AccountRow.PersonRow == member))))
            {
                foreach (var li in transactionRow.GetLineItemRows())
                {
                    // Special parsing of investment transactions
                    if (transactionRow.AccountRow.Type == EAccountType.Investment)
                    {
                        var investmentTransactionRow = transactionRow.GetInvestmentTransaction();
                        string description = null;

                        switch (investmentTransactionRow.Type)
                        {
                            case EInvestmentTransactionType.CashIn:
                                description = "Added cash";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, li.Amount, 0));
                                break;
                            case EInvestmentTransactionType.CashOut:
                                description = "Removed cash";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Purchase, description, description, -li.Amount, 0));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Purchase, description, transactionRow.AccountRow.Name, 0, -li.Amount));
                                break;
                            case EInvestmentTransactionType.TransferCashIn:
                            case EInvestmentTransactionType.TransferCashOut:
                                break;
                            case EInvestmentTransactionType.InterestIncome:
                                description = "Interest Inc.";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, li.Amount, 0));
                                break;

                            case EInvestmentTransactionType.SharesIn:
                            case EInvestmentTransactionType.SharesOut:
                            case EInvestmentTransactionType.XSharesIn:
                            case EInvestmentTransactionType.XSharesOut:
                                // Used for share exchange, revenue-neutral
                                break;

                            case EInvestmentTransactionType.Buy:
                            case EInvestmentTransactionType.BuyFromTransferredCash:
                                // Buying shares is revenue-neutral
                                break;

                            case EInvestmentTransactionType.Sell:
                            case EInvestmentTransactionType.SellAndTransferCash:
                                var cg = Portfolio.ComputeSaleCapitalGains(household, transactionRow.ID, false);
                                if (cg.LongTermGain != 0)
                                {
                                    description = "Long term capital gains from sale of " + investmentTransactionRow.SecurityRow.Symbol;
                                    entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Sales, description, "LTCG", 0, li.Amount));
                                    entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Sales, description, transactionRow.AccountRow.Name, li.Amount, 0));
                                }
                                if (cg.ShortTermGain != 0)
                                {
                                    description = "Short term capital gains from sale of " + investmentTransactionRow.SecurityRow.Symbol;
                                    entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Sales, description, "STCG", 0, li.Amount));
                                    entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Sales, description, transactionRow.AccountRow.Name, li.Amount, 0));
                                }
                                break;

                            case EInvestmentTransactionType.Dividends:
                            case EInvestmentTransactionType.ReinvestDividends:
                                description = "Dividends";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, li.Amount, 0));
                                break;

                            case EInvestmentTransactionType.TransferDividends:
                                description = "Dividends";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, -li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, -li.Amount, 0));
                                break;

                            case EInvestmentTransactionType.ShortTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestShortTermCapitalGains:
                                description = "Short-term capital gains";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, li.Amount, 0));
                                break;

                            case EInvestmentTransactionType.TransferShortTermCapitalGains:
                                description = "Short-term capital gains";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, -li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, -li.Amount, 0));
                                break;

                            case EInvestmentTransactionType.LongTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestMediumTermCapitalGains:
                            case EInvestmentTransactionType.ReinvestLongTermCapitalGains:
                                description = "Long-term capital gains";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, li.Amount, 0));
                                break;

                            case EInvestmentTransactionType.TransferLongTermCapitalGains:
                                description = "Long-term capital gains";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, -li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, -li.Amount, 0));
                                break;

                            case EInvestmentTransactionType.ReturnOnCapital:
                                description = "Return on capital";
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, description, 0, li.Amount));
                                entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, li.Amount, 0));
                                break;
                        }
                    }

                    //
                    // Process category-based income and expense
                    //
                    if (li.GetLineItemCategoryRow() is Household.LineItemCategoryRow lic)
                    {
                        // Get description
                        string description = li.IsMemoNull() ? (transactionRow.IsMemoNull() ? "" : transactionRow.Memo) : li.Memo;

                        // Get to top-level category
                        var cat = lic.CategoryRow;
                        while (!cat.IsParentIDNull())
                        {
                            cat = household.Category.FindByID(cat.ParentID);
                        }
                        //var catItem = new CategoryItem(cat.Name);

                        if (cat.IsIncome)
                        {
                            // Regular income. credit the income "account" (increase of asset) and debit the account the money is going to (increase of equity)
                            entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, cat.Name, 0, li.Amount));
                            entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, li.Amount, 0));
                        }
                        else
                        {
                            // Regular expense. Debit the expense "account" (increase of asset) and credit the account the money is coming from (reduction of assets)
                            entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Purchase, description, cat.Name, -li.Amount, 0));
                            entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Purchase, description, transactionRow.AccountRow.Name, 0, -li.Amount));
                        }
                    }
                    //
                    // Process transfer-based income and expense
                    //
                    else if (li.GetLineItemTransferRow() is Household.LineItemTransferRow lit)
                    {
                        // Transfer to self does not count
                        if (member == null || (!lit.AccountRow.IsPersonIDNull() && lit.AccountRow.PersonRow == member))
                        {
                            continue;
                        }

                        string transferee = lit.AccountRow.IsPersonIDNull() ? "Unknown person" : lit.AccountRow.PersonRow.Name;
                        if (li.Amount < 0)
                        {
                            string description = $"Transfer(s) to {transferee}";
                            entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Transfer, description, transferee, -li.Amount, 0));
                            entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Transfer, description, transactionRow.AccountRow.Name, 0, -li.Amount));
                        }
                        else
                        {
                            string description = $"Transfer(s) from {transferee}";
                            entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transferee, 0, li.Amount));
                            entries.Add(new JournalEntry(transactionRow.Date, JournalEntry.EType.Receipt, description, transactionRow.AccountRow.Name, li.Amount, 0));
                        }
                    }
                }
            }
        }

        #endregion

        #region Support classes

        //
        // One journal entry
        //
        public class JournalEntry
        {
            public enum EType { Receipt, Payment, Sales, Purchase, Transfer, General };
            public JournalEntry(DateTime date, EType type, string description, string accountName, decimal debit, decimal credit) =>
                (Date, Type, Description, AccountName, Debit, Credit) = (date, type, description, accountName, debit, credit);

            public DateTime Date { get; }
            public EType Type { get; }
            public string Description { get; }
            public string AccountName { get; }
            public decimal Debit { get; }
            public decimal Credit { get; }
        }

        //
        // One member
        //
        public class MemberItem
        {
            public MemberItem(Household.PersonRow member) =>
                Member = member;

            static public MemberItem GetEverybodyItem()
                => new MemberItem(null);

            public Household.PersonRow Member { get; }

            public override string ToString()
            {
                return Member == null ? "Everybody" : Member.Name;
            }
        }

        //
        // Simplified category
        //
        /*
        class CategoryItem
        {
            public CategoryItem(string name) => Name = name;

            public readonly string Name;

            public override bool Equals(object obj)
            {
                return
                    obj is CategoryItem o &&
                    Name.Equals(o.Name);
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
        }*/

        #endregion
    }
}
