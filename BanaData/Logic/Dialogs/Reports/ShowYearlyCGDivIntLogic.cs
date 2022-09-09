using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using Toolbox.Attributes;
using BanaData.Logic.Main;
using BanaData.Collections;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Dialogs.Pickers;
using BanaData.Logic.Dialogs.Basics;
using BanaData.Serializations;

namespace BanaData.Logic.Dialogs.Reports
{
    public class ShowYearlyCGDivIntLogic : LogicBase
    {
        #region Private members

        private readonly Household household;
        private readonly IGuiServices guiServices;
        private readonly List<Household.AccountRow> shownAccounts = new List<Household.AccountRow>();
        private readonly List<int> interestCategories = new List<int>();

        #endregion

        #region Constructor

        public ShowYearlyCGDivIntLogic(Household _household, UserSettings userSettings, IGuiServices _guiServices)
        {
            household = _household;
            guiServices = _guiServices;

            // Setup years
            YearPickerLogic = new YearPickerLogic(household, userSettings);
            YearPickerLogic.YearChanged += (s, e) => UpdateTransactions();

            // Setup accounts - skip IRAs as they are not taxable
            foreach (Household.AccountRow accountRow in household.Account.Rows)
            {
                if (!(accountRow.Type == EAccountType.Investment) || !(accountRow.Kind == EInvestmentKind.TraditionalIRA))
                {
                    shownAccounts.Add(accountRow);
                }
            }

            // Find the categories that hold interest income
            foreach (Household.CategoryRow categoryRow in household.Category.Rows)
            {
                if (!categoryRow.IsTaxInfoNull() && CategoryItem.TaxInfoDictionary[categoryRow.TaxInfo].Contains("Interest income"))
                {
                    interestCategories.Add(categoryRow.ID);
                }
            }

            // Setup transaction view
            Transactions = (CollectionView)CollectionViewSource.GetDefaultView(transactions);
            Transactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));

            // Pick accounts command
            PickAccountsCommand = new CommandBase(OnPickAccountsCommand);

            // Build transaction list
            UpdateTransactions();
        }

        #endregion

        #region UI properties

        //
        // Year
        //
        public YearPickerLogic YearPickerLogic { get; }

        //
        // Show interest or not
        //
        private bool isShowingInterest;
        public bool? IsShowingInterest
        {
            get => isShowingInterest;
            set
            {
                if (isShowingInterest != value)
                {
                    isShowingInterest = value == true;
                    UpdateTransactions();
                }
            }
        }

        //
        // Show dividends or not
        //
        private bool isShowingDividends = true;
        public bool? IsShowingDividends
        {
            get => isShowingDividends;
            set
            {
                if (isShowingDividends != value)
                {
                    isShowingDividends = value == true;
                    UpdateTransactions();
                }
            }
        }

        //
        // Show dividends or not
        //
        private bool isShowingCapGains = true;
        public bool? IsShowingCapGains
        {
            get => isShowingCapGains;
            set
            {
                if (isShowingCapGains != value)
                {
                    isShowingCapGains = value == true;
                    UpdateTransactions();
                }
            }
        }

        //
        // Pick accounts
        //
        public CommandBase PickAccountsCommand { get; }

        //
        // Transactions
        //
        private readonly WpfObservableRangeCollection<TransactionItem> transactions = new WpfObservableRangeCollection<TransactionItem>();
        public CollectionView Transactions { get; }

        //
        // Totals
        //
        public decimal TotalDividend { get; private set; }
        public decimal TotalSTCG { get; private set; }
        public decimal TotalLTCG { get; private set; }
        public decimal TotalInterest { get; private set; }

        //
        // Widths
        //
        private double dateColumnWidth = 80;
        public double DateColumnWidth
        {
            get => dateColumnWidth;
            set
            {
                if (dateColumnWidth != value)
                {
                    dateColumnWidth = value;
                    OnPropertyChanged(() => dateColumnWidth);
                }
            }
        }

        private double accountColumnWidth = 200;
        public double AccountColumnWidth
        {
            get => accountColumnWidth;
            set
            {
                if (accountColumnWidth != value)
                {
                    accountColumnWidth = value;
                    OnPropertyChanged(() => accountColumnWidth);
                }
            }
        }

        private double symbolColumnWidth = 100;
        public double SymbolColumnWidth
        {
            get => symbolColumnWidth;
            set
            {
                if (symbolColumnWidth != value)
                {
                    symbolColumnWidth = value;
                    OnPropertyChanged(() => symbolColumnWidth);
                }
            }
        }

        private double descriptionColumnWidth = 100;
        public double DescriptionColumnWidth
        {
            get => descriptionColumnWidth;
            set
            {
                if (descriptionColumnWidth != value)
                {
                    descriptionColumnWidth = value;
                    OnPropertyChanged(() => descriptionColumnWidth);
                }
            }
        }

        private double dividendColumnWidth = 80;
        public double DividendColumnWidth
        {
            get => dividendColumnWidth;
            set
            {
                if (dividendColumnWidth != value)
                {
                    dividendColumnWidth = value;
                    OnPropertyChanged(() => dividendColumnWidth);
                }
            }
        }

        private double stcgColumnWidth = 80;
        public double STCGColumnWidth
        {
            get => stcgColumnWidth;
            set
            {
                if (stcgColumnWidth != value)
                {
                    stcgColumnWidth = value;
                    OnPropertyChanged(() => stcgColumnWidth);
                }
            }
        }

        private double ltcgColumnWidth = 80;
        public double LTCGColumnWidth
        {
            get => ltcgColumnWidth;
            set
            {
                if (ltcgColumnWidth != value)
                {
                    ltcgColumnWidth = value;
                    OnPropertyChanged(() => ltcgColumnWidth);
                }
            }
        }

        private double interestColumnWidth = 80;
        public double InterestColumnWidth
        {
            get => interestColumnWidth;
            set
            {
                if (interestColumnWidth != value)
                {
                    interestColumnWidth = value;
                    OnPropertyChanged(() => interestColumnWidth);
                }
            }
        }

        #endregion

        #region Actions

        private void UpdateTransactions()
        {
            // Accumulate the transactions in a temp list
            var tempTransList = new List<TransactionItem>();
            TotalDividend = 0;
            TotalSTCG = 0;
            TotalLTCG = 0;
            TotalInterest = 0;

            //
            // Compute interests
            //
            if (isShowingInterest)
            {
                // Go through all shown accounts
                foreach (var accountRow in shownAccounts)
                {
                    var accountName = accountRow.Name;

                    // Go through all the transactions for that year
                    foreach (var transactionRow in accountRow.GetRegularTransactionRows().Where(tr => tr.Date.Year == YearPickerLogic.SelectedYear))
                    {
                        // Go through all the line items
                        foreach (var li in transactionRow.GetLineItemRows())
                        {
                            if (li.GetLineItemCategoryRow() is Household.LineItemCategoryRow lineItemCategoryRow && interestCategories.Contains(lineItemCategoryRow.CategoryID))
                            {
                                string description = transactionRow.IsPayeeNull() ? "Interest" : transactionRow.Payee;
                                tempTransList.Add(new TransactionItem(transactionRow.Date, accountName, "", description, 0, 0, 0, li.Amount));
                                TotalInterest += li.Amount;
                            }
                        }
                    }
                }
            }

            //
            // Compute Dividends and CG
            //
            if (isShowingDividends || isShowingCapGains)
            {
                foreach (var accountRow in shownAccounts)
                {
                    var accountName = accountRow.Name;

                    // Skip non-investment accounts
                    if (accountRow.Type != EAccountType.Investment)
                    {
                        continue;
                    }

                    // Go through all the transactions for that year
                    foreach (var transactionRow in accountRow.GetRegularTransactionRows().Where(tr => tr.Date.Year == YearPickerLogic.SelectedYear))
                    {
                        // Get the investment transaction
                        var investmentTransactionRow = transactionRow.GetInvestmentTransaction();

                        // Capture dividends
                        var type = investmentTransactionRow.Type;
                        if (isShowingDividends &&
                            (type == EInvestmentTransactionType.Dividends ||
                             type == EInvestmentTransactionType.ReinvestDividends ||
                             type == EInvestmentTransactionType.TransferDividends))
                        {
                            string symbol = investmentTransactionRow.SecurityRow.Symbol;
                            string description = EnumDescriptionAttribute.GetDescription(type);
                            decimal amount = transactionRow.GetAmount() * (type == EInvestmentTransactionType.TransferDividends ? -1 : 1);
                            tempTransList.Add(new TransactionItem(transactionRow.Date, accountName, symbol, description, amount, 0, 0, 0));

                            TotalDividend += amount;
                        }

                        // Capture STCG
                        if (isShowingCapGains &&
                            (type == EInvestmentTransactionType.ShortTermCapitalGains ||
                             type == EInvestmentTransactionType.ReinvestShortTermCapitalGains ||
                             type == EInvestmentTransactionType.TransferShortTermCapitalGains))
                        {
                            string symbol = investmentTransactionRow.SecurityRow.Symbol;
                            string description = EnumDescriptionAttribute.GetDescription(type);
                            decimal amount = transactionRow.GetAmount() * (type == EInvestmentTransactionType.TransferShortTermCapitalGains ? -1 : 1);
                            tempTransList.Add(new TransactionItem(transactionRow.Date, accountName, symbol, description, 0, amount, 0, 0));

                            TotalSTCG += amount;
                        }

                        // Capture LTCG
                        if (isShowingCapGains &&
                            (type == EInvestmentTransactionType.LongTermCapitalGains ||
                             type == EInvestmentTransactionType.ReinvestLongTermCapitalGains ||
                             type == EInvestmentTransactionType.TransferLongTermCapitalGains))
                        {
                            string symbol = investmentTransactionRow.SecurityRow.Symbol;
                            string description = EnumDescriptionAttribute.GetDescription(type);
                            decimal amount = transactionRow.GetAmount() * (type == EInvestmentTransactionType.TransferLongTermCapitalGains ? -1 : 1);
                            tempTransList.Add(new TransactionItem(transactionRow.Date, accountName, symbol, description, 0, 0, amount, 0));

                            TotalLTCG += amount;
                        }

                        // Capture share sales
                        if (isShowingCapGains &&
                            (type == EInvestmentTransactionType.Sell ||
                             type == EInvestmentTransactionType.SellAndTransferCash))
                        {
                            string symbol = investmentTransactionRow.SecurityRow.Symbol;
                            var sale = Portfolio.ComputeSaleCapitalGains(household, transactionRow.ID, false);

                            tempTransList.Add(new TransactionItem(transactionRow.Date, accountName, symbol, sale.Description, 0, sale.ShortTermGain, sale.LongTermGain, 0));

                            TotalSTCG += sale.ShortTermGain;
                            TotalLTCG += sale.LongTermGain;
                        }
                    }
                }
            }

            // Publish the new list and the new totals
            transactions.ReplaceRange(tempTransList);
            OnPropertyChanged(() => TotalDividend);
            OnPropertyChanged(() => TotalSTCG);
            OnPropertyChanged(() => TotalLTCG);
            OnPropertyChanged(() => TotalInterest);
        }

        private void OnPickAccountsCommand()
        {
            var logic = new AccountListPickerLogic(household, shownAccounts);
            if (guiServices.ShowDialog(logic))
            {
                shownAccounts.Clear();
                shownAccounts.AddRange(logic.PickedAccounts);
                UpdateTransactions();
            }
        }

        #endregion

        #region Supporting classes

        public class TransactionItem
        {
            public TransactionItem(DateTime date, string account, string symbol, string description, decimal dividend, decimal stcg, decimal ltcg, decimal interest) =>
                (Date, Account, Symbol, Description, Dividend, STCG, LTCG, Interest) =
                    (date, account, symbol, description, dividend, stcg, ltcg, interest);

            public DateTime Date { get; }
            public string Account { get; }
            public string Symbol { get; }
            public string Description { get; }
            public decimal Dividend { get; }
            public decimal STCG { get; }
            public decimal LTCG { get; }
            public decimal Interest { get; }
        }

        #endregion
    }
}
