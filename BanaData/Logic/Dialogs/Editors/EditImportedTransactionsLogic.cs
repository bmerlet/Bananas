using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using BanaData.Serializations;
using Toolbox.Attributes;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Editors
{
    public class EditImportedTransactionsLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;

        #endregion

        #region Constructor

        public EditImportedTransactionsLogic(MainWindowLogic _mainWindowLogic, Household _household)
        {
            (mainWindowLogic, household) = (_mainWindowLogic, _household);

            // Create commands
            RemoveAllCommentsCommand = new CommandBase(OnRemoveAllCommentsCommand);
            LowerCasePayeeName = new CommandBase(OnLowerCasePayeeName);
            ImportAllCommand = new CommandBase(OnImportAllCommand);
            ImportNoneCommand = new CommandBase(OnImportNoneCommand);

            // Check if we are dealing with investment or banking transactions
            bool investment = GetTransactionType();
            InvestmentVisible = investment;
            BankingVisible = !investment;

            // Create account list for autocomplete text box
            foreach (Household.AccountRow accountRow in investment ? household.Account.GetInvestmentAccounts() : household.Account.GetBankingAccounts())
            {
                var accountItem = AccountItem.CreateFromDB(accountRow);
                accounts.Add(accountItem);
            }

            // If account is assigned, preselect it
            if (!household.Transaction[0].IsAccountIDNull())
            {
                ImportAccount = household.Transaction[0].AccountRow.Name;
            }

            // Create transaction list and give it to the UI
            if (investment)
            {
                foreach (var trans in household.Transaction)
                {
                    var inv = trans.GetInvestmentTransaction();

                    var it = new ImportedInvestmentTransaction()
                    {
                        Row = trans,
                        Import = true,
                        Date = trans.Date,
                        Type = EnumDescriptionAttribute.GetDescription(inv.Type),
                        Memo = trans.IsMemoNull() ? "" : trans.Memo,
                        Security = inv.IsSecurityIDNull() ? "" : inv.SecurityRow.Symbol,
                        SecurityQuantity = inv.IsSecurityQuantityNull() ? "" : inv.SecurityQuantity.ToString("N4"),
                        SecurityPrice = inv.IsSecurityPriceNull() ? "" : inv.SecurityPrice.ToString("N2"),
                        Commission = inv.Commission == 0 ? "" : inv.Commission.ToString("N2"),
                        Amount = trans.GetAmount()
                    };
                    importedInvestmentTransactions.Add(it);
                }

                ImportedInvestmentTransactions = (CollectionView)CollectionViewSource.GetDefaultView(importedInvestmentTransactions);
                ImportedInvestmentTransactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            }
            else
            {
                foreach (var trans in household.Transaction)
                {
                    var it = new ImportedBankingTransaction()
                    {
                        Row = trans,
                        Import = true,
                        Date = trans.Date,
                        Payee = trans.IsPayeeNull() ? "" : trans.Payee,
                        Memo = trans.IsMemoNull() ? "" : trans.Memo,
                        Amount = trans.GetAmount()
                    };
                    importedBankingTransactions.Add(it);
                }

                ImportedBankingTransactions = (CollectionView)CollectionViewSource.GetDefaultView(importedBankingTransactions);
                ImportedBankingTransactions.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Ascending));
            }
        }

        private bool GetTransactionType()
        {
            bool? investment = null;

            foreach(var trans in household.Transaction)
            {
                bool inv = trans.GetInvestmentTransactionRows().Length != 0;
                {
                    if (investment == null)
                    {
                        investment = inv;
                    }
                    else if (investment != inv)
                    {
                        throw new InvalidOperationException("Mix of investment and banking transactions in imported transactions");
                    }
                }
            }

            return investment == true;
        }

        #endregion

        #region UI properties

        //
        // Account we are importing into
        //
        public string ImportAccount { get; set; }
        private readonly List<AccountItem> accounts = new List<AccountItem>();
        public IEnumerable<AccountItem> Accounts => accounts;

        //
        // Commands
        //
        public CommandBase RemoveAllCommentsCommand { get; }
        public CommandBase LowerCasePayeeName { get; }
        public CommandBase ImportAllCommand { get; }
        public CommandBase ImportNoneCommand { get; }

        // Banking transactions
        private readonly ObservableCollection<ImportedBankingTransaction> importedBankingTransactions = new ObservableCollection<ImportedBankingTransaction>();
        public CollectionView ImportedBankingTransactions { get; private set; }
        public bool BankingVisible { get; private set; }

        // Investment transactions
        private readonly ObservableCollection<ImportedInvestmentTransaction> importedInvestmentTransactions = new ObservableCollection<ImportedInvestmentTransaction>();
        public CollectionView ImportedInvestmentTransactions { get; private set; }
        public bool InvestmentVisible { get; private set; }

        #endregion

        #region Actions

        private void OnRemoveAllCommentsCommand()
        {
            foreach(var trans in importedBankingTransactions)
            {
                trans.ResetMemo();
            }
            foreach (var trans in importedInvestmentTransactions)
            {
                trans.ResetMemo();
            }
        }

        private void OnLowerCasePayeeName()
        {
            foreach (var trans in importedBankingTransactions)
            {
                trans.MakePayeeLowerCase();
            }
        }

        private void OnImportAllCommand()
        {
            foreach (var trans in importedBankingTransactions)
            {
                trans.SetImport(true);
            }
            foreach (var trans in importedInvestmentTransactions)
            {
                trans.SetImport(true);
            }
        }

        private void OnImportNoneCommand()
        {
            foreach (var trans in importedBankingTransactions)
            {
                trans.SetImport(false);
            }
            foreach (var trans in importedInvestmentTransactions)
            {
                trans.SetImport(false);
            }
        }

        protected override bool? Commit()
        {
            // Check we have an account
            if (string.IsNullOrWhiteSpace(ImportAccount))
            {
                mainWindowLogic.ErrorMessage("Please choose an account.");
                return null;
            }

            var accountRow = household.Account.First(a => a.Name == ImportAccount);
            var checkpointRow = household.Checkpoint.GetCurrentCheckpoint();

            // Get the transactions
            foreach (var it in importedBankingTransactions)
            {
                if (it.Import == false)
                {
                    // Remove from DB
                    foreach(var lineItem in it.Row.GetLineItemRows())
                    {
                        lineItem.Delete();
                    }
                    
                    if (!it.Row.IsAccountIDNull() && it.Row.AccountRow.Type == EAccountType.Investment)
                    {
                        // Should not happen
                        it.Row.GetInvestmentTransaction().Delete();
                    }
                    else if (!it.Row.IsAccountIDNull() && it.Row.AccountRow.Type == EAccountType.Bank)
                    {
                        it.Row.GetBankingTransaction().Delete();
                    }

                    it.Row.Delete();
                }
                else
                {
                    household.Transaction.Update(
                        it.Row.ID,
                        accountRow,
                        it.Date,
                        string.IsNullOrWhiteSpace(it.Payee) ? null : it.Payee,
                        string.IsNullOrWhiteSpace(it.Memo) ? null : it.Memo,
                        ETransactionStatus.Pending,
                        checkpointRow,
                        ETransactionType.Regular);

                    if (accountRow.Type == EAccountType.Bank)
                    {
                        household.BankingTransaction.Add(it.Row, ETransactionMedium.None, 0);
                    }
                }
            }

            foreach (var it in importedInvestmentTransactions)
            {
                if (it.Import == false)
                {
                    // Remove from DB
                    foreach (var lineItem in it.Row.GetLineItemRows())
                    {
                        lineItem.Delete();
                    }

                    if (!it.Row.IsAccountIDNull() && it.Row.AccountRow.Type == EAccountType.Investment)
                    {
                        it.Row.GetInvestmentTransaction().Delete();
                    }
                    else if (!it.Row.IsAccountIDNull() && it.Row.AccountRow.Type == EAccountType.Bank)
                    {
                        // Should not happen
                        it.Row.GetBankingTransaction().Delete();
                    }

                    it.Row.Delete();
                }
                else
                {
                    household.Transaction.Update(
                        it.Row.ID,
                        accountRow,
                        it.Date,
                        null,
                        string.IsNullOrWhiteSpace(it.Memo) ? null : it.Memo,
                        ETransactionStatus.Pending,
                        checkpointRow,
                        ETransactionType.Regular);
                }
            }

            // Update DB
            mainWindowLogic.CommitChanges(household);

            return true;
        }

        #endregion

        #region Supporting classes

        public class ImportedBankingTransaction : LogicBase
        {
            public Household.TransactionRow Row;
            public bool? Import { get; set; }
            public DateTime Date { get; set; }
            public string Payee { get; set; }
            public string Memo { get; set; }
            public decimal Amount { get; set; }

            public void ResetMemo()
            {
                Memo = "";
                InvokePropertyChanged(nameof(Memo));
            }

            public void MakePayeeLowerCase()
            {
                Payee = MakePayeeLowercase(Payee);
                InvokePropertyChanged(nameof(Payee));
            }

            public void SetImport(bool val)
            {
                Import = val;
                InvokePropertyChanged(nameof(Import));
            }

            static private string MakePayeeLowercase(string payee)
            {
                var words = payee.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string result = "";
                foreach (var word in words)
                {
                    var lowercaseWord = word[0] + word.Substring(1).ToLower();
                    result += (result == "") ? "" : " ";
                    result += lowercaseWord;
                }

                return result;
            }
        }

        public class ImportedInvestmentTransaction : LogicBase
        {
            public Household.TransactionRow Row;
            public bool? Import { get; set; }
            public DateTime Date { get; set; }
            public string Type { get; set; }
            public string Memo { get; set; }
            public string Security { get; set; }
            public string SecurityQuantity { get; set; }
            public string SecurityPrice { get; set; }
            public string Commission { get; set; }
            public decimal Amount { get; set; }

            public void ResetMemo()
            {
                Memo = "";
                InvokePropertyChanged(nameof(Memo));
            }

            public void SetImport(bool val)
            {
                Import = val;
                InvokePropertyChanged(nameof(Import));
            }
        }

        #endregion
    }

}
