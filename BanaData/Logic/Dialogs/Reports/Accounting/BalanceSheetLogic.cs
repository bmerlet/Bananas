using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Main;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Reports.Accounting
{
    public class BalanceSheetLogic : LogicBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion

        #region Constructor

        public BalanceSheetLogic(MainWindowLogic _mainWindowLogic)
        {
            mainWindowLogic = _mainWindowLogic;

            // Setup members
            foreach(var person in mainWindowLogic.Household.Person)
            {
                MembersSource.Add(new MemberItem(person));
            }
            var everybody = MemberItem.GetEverybodyItem();
            MembersSource.Add(everybody);
            selectedMember = everybody;

            // Give the lists to the UI
            AssetsSource = (CollectionView)CollectionViewSource.GetDefaultView(assets);
            AssetsSource.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
            AssetsSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            LiabilitiesSource = (CollectionView)CollectionViewSource.GetDefaultView(liabilities);
            LiabilitiesSource.SortDescriptions.Add(new SortDescription("Group", ListSortDirection.Ascending));
            LiabilitiesSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            // Compute!
            ComputeBalanceSheet();
        }

        #endregion

        #region UI properties

        // Date
        private DateTime date = DateTime.Today;
        public DateTime Date { get => date; set { date = value; ComputeBalanceSheet(); } }

        // Person
        public List<MemberItem> MembersSource { get; } = new List<MemberItem>();
        private MemberItem selectedMember;
        public MemberItem SelectedMember { get => selectedMember; set { selectedMember = value; ComputeBalanceSheet(); } }

        // Assets
        private readonly ObservableCollection<BalanceSheetItem> assets = new ObservableCollection<BalanceSheetItem>();
        public CollectionView AssetsSource { get; }

        // Liabilities
        private readonly ObservableCollection<BalanceSheetItem> liabilities = new ObservableCollection<BalanceSheetItem>();
        public CollectionView LiabilitiesSource { get; }

        #endregion

        #region Actions

        private void ComputeBalanceSheet()
        {
            var household = mainWindowLogic.Household;

            //
            // Compute assets
            //
            assets.Clear();
            liabilities.Clear();
            decimal totalAssets = 0;
            decimal totalLiabilityAndEquity = 0;
            decimal networth = 0;
            bool hasBank = false;
            bool hasInvestment = false;
            bool hasAssets = false;
            bool hasCreditCard = false;
            bool hasLiabilities = false;


            // Find all accounts for the selected member
            foreach (var account in household.Account)
            {
                if (selectedMember.Member == null || (!account.IsPersonIDNull() && account.PersonRow == selectedMember.Member))
                {
                    decimal amount = account.Type == EAccountType.Investment ? account.GetInvestmentValue(date) : account.GetBalance(date);

                    // Don't show zeroes
                    if (amount == 0)
                    {
                        continue;
                    }

                    ObservableCollection<BalanceSheetItem> list = liabilities;
                    string group = "9Unknown";

                    switch(account.Type)
                    {
                        case EAccountType.Cash:
                        case EAccountType.Bank:
                            list = assets;
                            group = "11Bank";
                            hasBank = true;
                            break;
                        case EAccountType.CreditCard:
                            list = liabilities;
                            group = "11CreditCard";
                            hasCreditCard = true;
                            break;
                        case EAccountType.Investment:
                            list = assets;
                            group = "21Investment";
                            hasInvestment = true;
                            break;
                        case EAccountType.OtherAsset:
                            list = assets;
                            group = "31Assets";
                            hasAssets = true;
                            break;
                        case EAccountType.OtherLiability:
                            list = liabilities;
                            group = "21Other";
                            break;
                    }

                    networth += amount;
                    if (list == liabilities)
                    {
                        totalLiabilityAndEquity -= amount;
                    }
                    else
                    {
                        totalAssets += amount;
                    }

                    list.Add(BalanceSheetItem.GetItem(account.Name, Toolbox.Attributes.EnumDescriptionAttribute.GetDescription(account.Type), group, amount));
                }
            }

            // Add necessary titles
            if (hasBank)
            {
                assets.Add(BalanceSheetItem.GetTitle("Cash and Bank accounts", "10Bank"));
            }
            if (hasInvestment)
            {
                assets.Add(BalanceSheetItem.GetTitle("Investments", "20Investment"));
            }
            if (hasAssets)
            {
                assets.Add(BalanceSheetItem.GetTitle("Long-term assets", "30Assets"));
            }
            if (hasCreditCard)
            {
                liabilities.Add(BalanceSheetItem.GetTitle("Credit cards", "10CreditCard"));
            }
            if (hasLiabilities)
            {
                liabilities.Add(BalanceSheetItem.GetTitle("Other liabilities", "20Other"));
            }

            // Compute and add net worth
            liabilities.Add(BalanceSheetItem.GetTitle("Net Worth", "70NetWorth"));
            liabilities.Add(BalanceSheetItem.GetItem("Equity", "Equity", "71NetWorth", networth));
            totalLiabilityAndEquity += networth;

            // Fillers
            for (int i = assets.Count; i < liabilities.Count; i++)
            {
                assets.Add(BalanceSheetItem.GetFiller("80Filler"));
            }
            for (int i = liabilities.Count; i < assets.Count; i++)
            {
                liabilities.Add(BalanceSheetItem.GetFiller("80Filler"));
            }

            // Totals
            assets.Add(BalanceSheetItem.GetTotal("90Total", totalAssets));
            liabilities.Add(BalanceSheetItem.GetTotal("90Total", totalLiabilityAndEquity));
        }

        #endregion

        #region Support classes

        //
        // One balance sheet item
        //
        public class BalanceSheetItem
        {
            private BalanceSheetItem(string name, string tip, string group, decimal value, bool bold, bool showValue, bool indented)
                => (Name, Tip, Group, Value, Bold, ShowValue) = ((indented ? "\t" : "") + name, tip, group, value, bold, showValue);

            static public BalanceSheetItem GetTitle(string name, string group)
                => new BalanceSheetItem(name, null, group, 0, true, false, false);

            static public BalanceSheetItem GetItem(string name, string tip, string group, decimal value)
                => new BalanceSheetItem(name, tip, group, value, false, true, true);

            static public BalanceSheetItem GetTotal(string group, decimal value)
                => new BalanceSheetItem("Total:", null, group, value, true, true, false);

            static public BalanceSheetItem GetFiller(string group)
                => new BalanceSheetItem("", null, group, 0, false, false, false);

            public string Name { get; }
            public string Tip { get; }
            public string Group { get; }
            public decimal Value { get; }
            public bool ShowValue { get; }
            public bool Bold { get; }
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

        #endregion
    }
}
