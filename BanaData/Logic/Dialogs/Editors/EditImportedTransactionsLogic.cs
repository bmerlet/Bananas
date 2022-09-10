using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using BanaData.Serializations;
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

            // Create account list for autocomplete text box
            foreach (Household.AccountRow accountRow in household.Account)
            {
                var accountItem = AccountItem.CreateFromDB(accountRow);
                accounts.Add(accountItem);
            }
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

        #endregion

        #region Actions

        private void OnRemoveAllCommentsCommand()
        {
            foreach(var trans in household.Transaction)
            {
                trans.SetMemoNull();
            }
        }

        private void OnLowerCasePayeeName()
        {
            foreach (var trans in household.Transaction)
            {
                trans.Payee = MakePayeeLowercase(trans.Payee);
            }
        }

        private void OnImportAllCommand()
        {
            // ZZZ
        }

        private void OnImportNoneCommand()
        {
            // ZZZ
        }

        protected override bool? Commit()
        {
            mainWindowLogic.CommitChanges(household);

            return true;
        }

        static private string MakePayeeLowercase(string payee)
        {
            var words = payee.Trim().Split(new char[] { ' ' });
            string result = "";
            foreach (var word in words)
            {
                var lowercaseWord = word[0] + word.Substring(1).ToLower();
                result += (result == "") ? "" : " ";
                result += lowercaseWord;
            }

            return result;
        }

        #endregion
    }
}
