using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Toolbox.UILogic;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Collections;
using System.ComponentModel;

namespace BanaData.Logic.Main
{
    public class BankRegisterLogic :  AbstractRegisterLogic
    {
        #region Constructor

        public BankRegisterLogic(MainWindowLogic mainWindowLogic)
            :  base(mainWindowLogic)
        {
            // Create column width manager
            Widths = new ColumnWidths(mainWindowLogic, this);

            // Payee view
            MemorizedPayees = (CollectionView)CollectionViewSource.GetDefaultView(memorizedPayees);
            MemorizedPayees.SortDescriptions.Add(new SortDescription("Payee", ListSortDirection.Ascending));

            // Be notified when memorized payees change
            mainWindowLogic.MemorizedPayeesChanged += (s, e) => UpdateMemorizedPayees();
            UpdateMemorizedPayees();

            // Categories view - already sorted by the main window code
            CategoriesAndTransfers = (CollectionView)CollectionViewSource.GetDefaultView(categoriesAndTransfers);

            // Be notified when categories change
            mainWindowLogic.CategoriesChanged += (s, e) => UpdateCategoriesAndTransfers();
            UpdateCategoriesAndTransfers();
        }

        #endregion

        #region UI properties

        // If banking account (as opposed to credit card)
        public bool IsBank { get; private set; }

        // Memorized payees
        private readonly WpfObservableRangeCollection<MemorizedPayeeItem> memorizedPayees = new WpfObservableRangeCollection<MemorizedPayeeItem>();
        public CollectionView MemorizedPayees { get; }

        // Categories
        private readonly WpfObservableRangeCollection<CategoryItem> categoriesAndTransfers = new WpfObservableRangeCollection<CategoryItem>();
        public CollectionView CategoriesAndTransfers { get; }

        // Column widths
        public ColumnWidths Widths { get; }

        #endregion

        #region Actions

        // Manage the visibility of the medium column when displaying a new account
        protected override void OnNewAccount() 
        {
            if (IsBank != (accountRow.Type == EAccountType.Bank))
            {
                IsBank = accountRow.Type == EAccountType.Bank;
                OnPropertyChanged(() => IsBank);
                Widths.IsBankHasChanged();
            }
        }

        // Routine to create a transaction from the DB
        protected override AbstractTransactionLogic CreateTransactionFromDB(Household.AccountRow accountRow, Household.TransactionRow transRow, List<LineItem> lineItems) 
        {
            // Get banking details
            Household.BankingTransactionRow transBankRow = (accountRow.Type == EAccountType.Bank) ? transRow.GetBankingTransaction() : null;

            var transactionData = new BankingTransactionLogic.BankTransactionData(
                transRow.Date,
                transBankRow == null ? ETransactionMedium.None : transBankRow.Medium,
                transBankRow == null ? 0 : (transBankRow.IsCheckNumberNull() ? 0 : (uint)transBankRow.CheckNumber),
                transRow.IsPayeeNull() ? "" : transRow.Payee,
                transRow.IsMemoNull() ? "" : transRow.Memo,
                transRow.Status,
                lineItems);

            var bankingTransaction = new BankingTransactionLogic(mainWindowLogic, this, accountRow, transRow.ID, transactionData);

            return bankingTransaction;
        }

        protected override AbstractTransactionLogic CreateEmptyTransaction()
        {
            return new BankingTransactionLogic(mainWindowLogic, this, accountRow);
        }

        private void UpdateMemorizedPayees()
        {
            memorizedPayees.ReplaceRange(mainWindowLogic.MemorizedPayees);
        }

        private void UpdateCategoriesAndTransfers()
        {
            categoriesAndTransfers.ReplaceRange(mainWindowLogic.CategoriesAndTransfers);
        }

        #endregion

        #region Supporting classes

        public class ColumnWidths : LogicBase
        {
            private readonly MainWindowLogic mainWindowLogic;
            private readonly BankRegisterLogic bankRegisterLogic;

            public ColumnWidths(MainWindowLogic _mainWindowLogic, BankRegisterLogic _bankRegisterLogic)
                => (mainWindowLogic, bankRegisterLogic) = (_mainWindowLogic, _bankRegisterLogic);

            public void IsBankHasChanged()
            {
                OnPropertyChanged(() => WidthOfMediumColumn);
            }

            public double WidthOfDateColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfDateColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfDateColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfDateColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDateColumn);
                    }
                }
            }

            public double WidthOfMediumColumn
            {
                get => bankRegisterLogic.IsBank ? mainWindowLogic.UserSettings.WidthOfMediumColumn : 0;
                set
                {
                    if (bankRegisterLogic.IsBank && mainWindowLogic.UserSettings.WidthOfMediumColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfMediumColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfMediumColumn);
                    }
                }
            }

            public double WidthOfPayeeColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfPayeeColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfPayeeColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfPayeeColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfPayeeColumn);
                    }
                }
            }

            public double WidthOfMemoColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfMemoColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfMemoColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfMemoColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfMemoColumn);
                    }
                }
            }

            public double WidthOfCategoryColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfCategoryColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfCategoryColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfCategoryColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfCategoryColumn);
                    }
                }
            }

            public double WidthOfPaymentColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfPaymentColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfPaymentColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfPaymentColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfPaymentColumn);
                    }
                }
            }

            public double WidthOfStatusColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfStatusColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfStatusColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfStatusColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfStatusColumn);
                    }
                }
            }

            public double WidthOfDepositColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfDepositColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfDepositColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfDepositColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfDepositColumn);
                    }
                }
            }

            public double WidthOfBalanceColumn
            {
                get => mainWindowLogic.UserSettings.WidthOfBalanceColumn;
                set
                {
                    if (mainWindowLogic.UserSettings.WidthOfBalanceColumn != value)
                    {
                        mainWindowLogic.UserSettings.WidthOfBalanceColumn = value;
                        mainWindowLogic.SaveUserSettings();
                        OnPropertyChanged(() => WidthOfBalanceColumn);
                    }
                }
            }

        }

        #endregion
    }
}
