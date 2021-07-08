//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;

using BanaData.Database;
using BanaData.Logic.Main;

namespace BanaData.Serializations
{
    /// <summary>
    /// Parse a QIF file, creating a Household database or merging it in. 
    /// </summary>
    class QIFParser
    {
        #region Constants

        private const string deletedAccountStr = "Deleted Account";
        private readonly string eol = Environment.NewLine;

        #endregion

        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly Household household;
        private int checkpointID;
        private bool merging;
        private readonly List<string> accountNames = new List<string>();

        #endregion

        #region Constructor

        public QIFParser(MainWindowLogic _mainWindowLogic) =>
            (mainWindowLogic, household) = (_mainWindowLogic, _mainWindowLogic.Household);

        #endregion

        #region Log

        public string Log { get; private set; } = "";

        private class Tracker
        {
            public Tracker(string item) => Item = item;

            // Name of tracked items
            public readonly string Item;

            // Counters
            public int Added => AddedIDs.Count;
            public int Deleted => DeletedIDs.Count;
            public int Updated => UpdatedIDs.Count;
            public int Unchanged => FoundIDs.Count;

            // Convenience
            public bool HasChange => Added != 0 || Updated != 0;

            // List of IDs found, updated, added or deleted (to reconcile items)
            public readonly List<int> FoundIDs = new List<int>();
            public readonly List<int> UpdatedIDs = new List<int>();
            public readonly List<int> AddedIDs = new List<int>();
            public readonly List<int> DeletedIDs = new List<int>();

            // Pretty output
            public override string ToString()
            {
                return $"{Item}: {Added} added, {Updated} updated, {Deleted} deleted, {Unchanged} unchanged.";
            }
        }

        private Tracker accountTracker;
        private Tracker categoryTracker;
        private Tracker securityTracker;
        private Tracker securityPriceTracker;
        private Tracker bankTransactionTracker;
        private Tracker investmentTransactionTracker;
        private Tracker memorizedPayeeTransactionTracker;

        #endregion

        #region Entry points

        public void ImportFromQIF(string fileName)
        {
            // Init
            Log = "";
            accountNames.Clear();
            merging = false;

            // Zap the database
            household.Clear();
            household.AcceptChanges();

            // Create a checkpoint - all transactions are created under this checkpoint
            household.Checkpoints.AddCheckpointsRow(DateTime.Now);

            // Parse the file
            ParseFile(fileName);

            // Log what we got
            Log += $"Imported {household.Accounts.Rows.Count:N0} accounts" + eol;
            Log += $"Imported {household.Categories.Rows.Count:N0} categories" + eol;
            Log += $"Imported {household.Securities.Rows.Count:N0} securities" + eol;
            Log += $"Imported {household.SecurityPrices.Rows.Count:N0} security prices" + eol;
            Log += $"Imported {household.Transactions.Rows.Count:N0} transactions" + eol;
            Log += $"Imported {household.MemorizedPayees.Rows.Count:N0} memorized payees" + eol;
            Log += eol;

            // Find other sides of transfers
            PairTransfers();

            household.AcceptChanges();

            // Create a new checkpoint, all transactions created by the user
            // will be associated with this new checkpoint
            household.Checkpoints.AddCheckpointsRow(DateTime.Now);
        }

        public bool MergeFromQIF(string fileName)
        {
            // Init
            Log = "";
            accountNames.Clear();
            merging = true;
            accountTracker = new Tracker("Accounts");
            categoryTracker = new Tracker("Categories");
            securityTracker = new Tracker("Securities");
            securityPriceTracker = new Tracker("Security prices");
            bankTransactionTracker = new Tracker("Banking transactions");
            investmentTransactionTracker = new Tracker("Investment trans.");
            memorizedPayeeTransactionTracker = new Tracker("Memorized payees");
            // ZZZ Memorized payees

            // Parse the file
            ParseFile(fileName);

            // Log what we got
            Log += accountTracker.ToString() + eol;
            Log += categoryTracker.ToString() + eol;
            Log += securityTracker.ToString() + eol;
            Log += securityPriceTracker.ToString() + eol;
            Log += bankTransactionTracker.ToString() + eol;
            Log += investmentTransactionTracker.ToString() + eol;
            Log += memorizedPayeeTransactionTracker.ToString() + eol;
            Log += eol;

            // Find other sides of transfers for added transactions
            if (bankTransactionTracker.Added > 0 || investmentTransactionTracker.Added > 0)
            {
                PairTransfers();
            }

            household.AcceptChanges();

            return 
                accountTracker.HasChange | 
                categoryTracker.HasChange | 
                securityTracker.HasChange |
                securityPriceTracker.HasChange |
                bankTransactionTracker.HasChange |
                investmentTransactionTracker.HasChange |
                memorizedPayeeTransactionTracker.HasChange;
        }

        #endregion

        #region QIF main parser

        // Parse a QIF file file
        private void ParseFile(string fileName)
        {
            // Get current checkpoint
            checkpointID = household.Checkpoints.GetMostRecentCheckpointID();

            // Read the file
            using (var sr = new StreamReader(fileName))
            {
                // Parse all sections
                Household.AccountsRow accountRow = null;
                while (!sr.EndOfStream)
                {
                    accountRow = ParseOneSection(sr, accountRow);
                }
            }
        }

        private Household.AccountsRow ParseOneSection(StreamReader sr, Household.AccountsRow accountRow)
        {
            var sectionStr = sr.ReadLine();
            if (!sectionStr.StartsWith("!"))
            {
                throw new InvalidDataException("QIF parser: section does not start with '!': " + sectionStr);
            }

            sectionStr = sectionStr.Substring(1);
            var comps = sectionStr.Split(new char[] { ':' });

            switch (comps[0])
            {
                case "Type":
                    {
                        switch (comps[1].Trim())
                        {
                            case "Tag":
                                SkipToNextType(sr);
                                break;
                            case "Cat":
                                ParseCategories(sr);
                                break;
                            case "Security":
                                ParseSecurities(sr);
                                break;
                            case "Bank":
                                ParseBankTransactions(sr, accountRow);
                                break;
                            case "CCard":
                                ParseBankTransactions(sr, accountRow);
                                break;
                            case "Cash":
                                ParseBankTransactions(sr, accountRow);
                                break;
                            case "Oth A":
                            case "Oth L":
                                ParseBankTransactions(sr, accountRow);
                                break;
                            case "Invst":
                                ParseInvestmentTransactions(sr, accountRow);
                                break;
                            case "Memorized":
                                ParseMemorizedPayees(sr, accountRow);
                                break;
                            case "Prices":
                                ParseSecurityPrices(sr);
                                break;

                            default:
                                throw new InvalidDataException("QIF parser: Unknown type: " + sectionStr);
                        }
                    }
                    break;

                case "Account":
                    accountRow = ParseAccounts(sr);
                    break;

                case "Option":
                    {
                        switch (comps[1])
                        {
                            case "AutoSwitch":
                            // No idea what that means
                                break;
                            default:
                                throw new InvalidDataException("QIF parser: Unknown option: " + sectionStr);
                        }
                    }
                    break;

                case "Clear":
                    {
                        switch (comps[1])
                        {
                            case "AutoSwitch":
                                // No idea what that means
                                break;
                            default:
                                throw new InvalidDataException("QIF parser: Unknown clear: " + sectionStr);
                        }
                    }
                    break;

                default:
                    throw new InvalidDataException("QIF parser: unknown section type: " + sectionStr);
            }

            return accountRow;
        }

        #endregion

        #region Parse QIF Category

        private void ParseCategories(StreamReader sr)
        {
            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                ParseOneCategory(sr);
            }
        }

        private void ParseOneCategory(TextReader sr)
        {
            string path = null;
            string description = null;
            bool income = false;
            string taxInfo = null;

            while (true)
            {
                var l = sr.ReadLine();
                if ((l == null) || (l == "^"))
                {
                    break;
                }

                switch(l[0])
                {
                    case 'N': path = l.Substring(1); break;
                    case 'D': description = l.Substring(1); break;
                    case 'T': /* tax info follows indication */ break;
                    case 'R': taxInfo = l.Substring(1); break;
                    case 'I': income = true; break;
                    case 'E': income = false; break;
                }
            }

            // Check we have all info
            if (path == null)
            {
                throw new InvalidDataException("QIF parser: Category has no path - " + description);
            }

            // Find parent
            var components = path.Split(new char[] { ':', '\n', '\r' });
            string name = components[components.Length - 1];
            Household.CategoriesRow parentRow = null;
            for (int c = 0; c < (components.Length - 1); c++)
            {
                parentRow = household.Categories.GetByParentAndName(parentRow, components[c]);
            }

            // Add or merge category into the database
            if (merging)
            {
                MergeCategory(name, description, parentRow, income, taxInfo);
            }
            else
            {
                household.Categories.Add(name, description, parentRow, income, taxInfo);
            }

        }

        private void MergeCategory(string name, string description, Household.CategoriesRow parentRow, bool income, string taxInfo)
        {
            var existingCategoryRow = household.Categories.GetByParentAndName(parentRow, name);
            if (existingCategoryRow == null)
            {
                // New category
                var newRow = household.Categories.Add(name, description, parentRow, income, taxInfo);
                categoryTracker.AddedIDs.Add(newRow.ID);
            }
            else if (existingCategoryRow.HasSame(description, income, taxInfo))
            {
                // Exactly the same
                categoryTracker.FoundIDs.Add(existingCategoryRow.ID);
            }
            else
            {
                // Updated category
                household.Categories.Update(existingCategoryRow, name, description, parentRow, income, taxInfo);
                categoryTracker.UpdatedIDs.Add(existingCategoryRow.ID);
            }
        }

        #endregion

        #region Parse QIF account

        private Household.AccountsRow ParseAccounts(StreamReader sr)
        {
            Household.AccountsRow result = null;
            bool firstTime = accountNames.Count == 0;

            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                result = ParseOneAccount(sr);
            }

            if (merging && firstTime)
            {
                ReconcileMergedAccounts();
            }

            return result;
        }

        private Household.AccountsRow ParseOneAccount(TextReader sr)
        {
            string name = null;
            string description = null;
            decimal creditLimit = 0;
            EAccountType type = EAccountType.Invalid;
            EInvestmentKind kind = EInvestmentKind.Invalid;

            while (true)
            {
                var l = sr.ReadLine();
                if ((l == null) || (l == "^"))
                {
                    break;
                }

                switch (l[0])
                {
                    case 'N': name = l.Substring(1); break;
                    case 'D': description = l.Substring(1); break;
                    case 'T':
                        {
                            switch (l.Substring(1))
                            {
                                case "Bank":
                                    type = EAccountType.Bank;
                                    break;
                                case "CCard":
                                    type = EAccountType.CreditCard;
                                    break;
                                case "Cash":
                                    type = EAccountType.Cash;
                                    break;
                                case "Invst":
                                    type = EAccountType.Investment;
                                    break;
                                case "Port":
                                    type = EAccountType.Investment;
                                    kind = EInvestmentKind.Brokerage;
                                    break;
                                case "Mutual":
                                    type = EAccountType.Investment;
                                    kind = EInvestmentKind.SingleMutualFund;
                                    break;
                                case "401(k)/403(b)":
                                    type = EAccountType.Investment;
                                    kind = EInvestmentKind._401k;
                                    break;
                                case "Oth A":
                                    type = EAccountType.OtherAsset;
                                    break;
                                case "Oth L":
                                    type = EAccountType.OtherLiability;
                                    break;
                                default:
                                    throw new InvalidDataException("Unknown account type: " + l);
                            }
                        }
                        break;
                    case 'L':
                        decimal.TryParse(l.Substring(1), out creditLimit);
                        break;
                    default:
                        throw new InvalidDataException("Unknown account attribute: " + l);
                }
            }

            // Check we have all info
            if (name == null)
            {
                throw new InvalidDataException("QIF parser: Account has no name");
            }
            if (type == EAccountType.Invalid)
            {
                throw new InvalidDataException("QIF parser: Account has no type");
            }

            Household.AccountsRow accountRow;

            // Skip if we already saw this name (account name are present at least twicw in QIF files)
            if (accountNames.Contains(name))
            {
                accountRow = household.Accounts.GetByName(name);
            }
            else
            {
                accountNames.Add(name);

                if (merging)
                {
                    // Merge account
                    accountRow = MergeAccount(name, description, type, creditLimit, kind);
                }
                else
                {
                    // Create account
                    accountRow = CreateAccount(name, description, type, creditLimit, kind);
                }
            }

            // Make this account the current account
            return accountRow;
        }

        private Household.AccountsRow MergeAccount(string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind)
        {
            var accountRow = household.Accounts.GetByName(name);
            if (accountRow == null)
            {
                // New account
                accountRow = CreateAccount(name, description, type, creditLimit, kind);
                accountTracker.AddedIDs.Add(accountRow.ID);
            }
            else if (accountRow.HasSame(description, type, creditLimit, kind))
            {
                // Exactly the same
                accountTracker.FoundIDs.Add(accountRow.ID);
            }
            else
            {
                // Updated account
                household.Accounts.Update(accountRow.ID, name, description, type, creditLimit, kind, accountRow.Hidden);
                accountTracker.UpdatedIDs.Add(accountRow.ID);
            }

            return accountRow;
        }

        private Household.AccountsRow CreateAccount(string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind)
        {
            // By convention ,accounts with a name starting with _CLOSED are hidden
            // (QIF does not have a flag for hidden accounts)
            bool hidden = name.StartsWith("_CLOSED");

            // By convention, investment accounts with " IRA" in them are traditional IRA
            // (QIF does not have a flag for traditional IRAs)
            if (type == EAccountType.Investment && kind == EInvestmentKind.Brokerage && name.Contains(" IRA"))
            {
                kind = EInvestmentKind.TraditionalIRA;
            }

            return household.Accounts.Add(name, description, type, creditLimit, kind, hidden);
        }

        private void ReconcileMergedAccounts()
        {
            if (accountTracker.FoundIDs.Count == household.Accounts.Rows.Count)
            {
                // No problem, existing accounts have not changed
                return;
            }

            // See if we have a rename scenario
            bool again = true;
            while (again && accountTracker.AddedIDs.Count > 0)
            {
                // Find deleted accounts
                foreach (Household.AccountsRow accountRow in household.Accounts.Rows)
                {
                    if (!accountTracker.FoundIDs.Contains(accountRow.ID))
                    {
                        bool rename = false;

                        foreach(var addedID in accountTracker.AddedIDs)
                        {
                            var addedRow = household.Accounts.FindByID(addedID);
                            var question = $"Was the account {accountRow.Name} renamed to {addedRow.Name}?";
                            if (mainWindowLogic.YesNoQuestion(question))
                            {
                                // Ha, this was really a rename
                                var name = addedRow.Name;
                                accountTracker.AddedIDs.Remove(addedRow.ID);
                                addedRow.Delete();
                                accountRow.Name = name;
                                accountTracker.UpdatedIDs.Add(accountRow.ID);

                                again = true;
                                rename = true;
                                break;
                            }
                        }
                        if (rename)
                        {
                            break;
                        }
                        accountTracker.DeletedIDs.Add(accountRow.ID);
                    }
                }
            }
        }

        #endregion

        #region Parse QIF security

        private void ParseSecurities(StreamReader sr)
        {
            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                ParseOneSecurity(sr);
            }
        }

        private void ParseOneSecurity(TextReader sr)
        {
            string name = null;
            string symbol = null;
            ESecurityType type = ESecurityType.Invalid;

            while (true)
            {
                var l = sr.ReadLine();
                if ((l == null) || (l == "^"))
                {
                    break;
                }

                switch (l[0])
                {
                    case 'N': name = l.Substring(1); break;
                    case 'S': symbol = l.Substring(1); break;
                    case 'T':
                        {
                            switch (l.Substring(1))
                            {
                                case "Mutual Fund": type = ESecurityType.MutualFund; break;
                                case "Stock": type = ESecurityType.Stock; break;
                                case "Market Index": type = ESecurityType.MarketIndex; break;
                                case "Emp. Stock Opt.": type = ESecurityType.EmployeeStockOption; break;
                                default:
                                    throw new InvalidDataException("Unknown security type: " + l);
                            }
                        }
                        break;
                    default:
                        throw new InvalidDataException("Unknown account attribute: " + l);
                }
            }

            // Check we have all info
            if (name == null)
            {
                throw new InvalidDataException("QIF parser: Security has no name - symbol " + symbol);
            }
            if (type == ESecurityType.Invalid)
            {
                throw new InvalidDataException("QIF parser: Security has no type");
            }

            if (merging)
            {
                // Merge security
                MergeSecurity(name, symbol, type);
            }
            else
            {
                // Create security and add it to the list
                household.Securities.Add(name, symbol, type);
            }

        }

        private void MergeSecurity(string name, string symbol, ESecurityType type)
        {
            var existingSecurityRow = household.Securities.GetByName(name);
            if (existingSecurityRow == null)
            {
                // New  security
                var newRow = household.Securities.Add(name, symbol, type);
                securityTracker.AddedIDs.Add(newRow.ID);
            }
            else if (existingSecurityRow.HasSame(symbol, type))
            {
                // Exactly the same
                securityTracker.FoundIDs.Add(existingSecurityRow.ID);
            }
            else
            {
                // Updated security
                household.Securities.Update(existingSecurityRow.ID, name, symbol, type);
                securityTracker.UpdatedIDs.Add(existingSecurityRow.ID);
            }
        }

        #endregion

        # region Parse QIF banking transactions

        private void ParseBankTransactions(StreamReader sr, Household.AccountsRow account)
        {
            if (account == null)
            {
                throw new InvalidDataException("Bank transaction without current account");
            }

            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                ParseOneBankTransaction(sr, account);
            }
        }

        private class LineItemHolder
        {
            public int AccountID = -1;
            public int CategoryID = -1;
            public string Memo = null;
            public decimal Amount = 0;
        }

        private void ParseOneBankTransaction(TextReader sr, Household.AccountsRow accountRow)
        {
            DateTime date = DateTime.MinValue;
            decimal amountToCheck = 0;
            decimal otherMysteriousAmount = 0;
            string payee = null;
            string memo = null;
            ETransactionStatus status = ETransactionStatus.Pending;
            ETransactionMedium medium = ETransactionMedium.None;
            uint checkNumber = 0;

            List<LineItemHolder> lineItemHolders = new List<LineItemHolder>();
            var lineItemHolder = new LineItemHolder();
            bool parsingSplitLineItem = false;

            while (true)
            {
                var l = sr.ReadLine();
                if ((l == null) || (l == "^"))
                {
                    break;
                }

                switch (l[0])
                {
                    case 'D':
                        // Date
                        date = ParseDate(l.Substring(1));
                        break;

                    case 'T':
                    case '$':
                        decimal.TryParse(l.Substring(1), out lineItemHolder.Amount);
                        if (!parsingSplitLineItem)
                        {
                            amountToCheck = lineItemHolder.Amount;
                        }
                        break;

                    case 'U':
                        decimal.TryParse(l.Substring(1), out otherMysteriousAmount);
                        break;

                    case 'M':
                        // Main memo
                        memo = l.Substring(1);
                        break;

                    case 'E':
                        // Line item memo
                        lineItemHolder.Memo = l.Substring(1);
                        break;

                    case 'P':
                        // Payee
                        payee = l.Substring(1);
                        break;
                    
                    case 'C':
                        // Transaction status
                        status = ParseTransactionStatus(l.Substring(1));
                        break;
                    
                    case 'L':
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID, _) = ParseTransactionTarget(household, accountRow, l.Substring(1));
                        break;
                    
                    case 'N':
                        medium = ParseBankTransactionMedium(l.Substring(1), out checkNumber);
                        break;
                    
                    case 'S':
                        // Indicates beginning of a new split line item - commit previous one if any
                        if (parsingSplitLineItem)
                        {
                            lineItemHolders.Add(lineItemHolder);
                            lineItemHolder = new LineItemHolder();
                        }
                        parsingSplitLineItem = true;
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID, _) = ParseTransactionTarget(household, accountRow, l.Substring(1));
                        break;

                    default:
                        throw new InvalidDataException("Unknown transaction attribute: " + l);
                }
            }

            // Check we have all info
            if (date == DateTime.MinValue)
            {
                throw new InvalidDataException("QIF parser: Transaction has no date - " + payee);
            }
            if (amountToCheck != otherMysteriousAmount)
            {
                throw new InvalidDataException("QIF parser: Mysterious amount not the same as regular amount - " + amountToCheck + " - " + otherMysteriousAmount);
            }

            // Flush last line item
            lineItemHolders.Add(lineItemHolder);

            if (merging)
            {
                MergeBankingTransaction(accountRow, date, payee, memo, status, medium, checkNumber, lineItemHolders);
            }
            else
            {
                // Create transaction
                CreateBankingTransaction(accountRow, date, payee, memo, status, medium, checkNumber, lineItemHolders);
            }
        }

        private void MergeBankingTransaction(
            Household.AccountsRow accountRow,
            DateTime date,
            string payee,
            string memo,
            ETransactionStatus status,
            ETransactionMedium medium,
            uint checkNumber,
            List<LineItemHolder> lineItemHolders)
        {
            var transAlmostTheSame = new List<Household.TransactionsRow>();

            // Try to find this exact same transaction
            foreach (var transRow in accountRow.GetTransactionsRows())
            {
                if (!transRow.HasSame(date, payee, memo, status))
                {
                    continue;
                }

                // Found same transaction, see if it has the same line items
                var lineItemRows = transRow.GetLineItemsRows();
                if (lineItemRows.Length != lineItemHolders.Count)
                {
                    // Not the same number of line items, but the rest matched...
                    transAlmostTheSame.Add(transRow);
                    continue;
                }

                // Compare the line items
                bool lineItemsMatch = true;
                for (int i = 0; i < lineItemRows.Length; i++)
                {
                    if (!lineItemRows[i].HasSame(lineItemHolders[i].CategoryID, lineItemHolders[i].AccountID, lineItemHolders[i].Memo, lineItemHolders[i].Amount))
                    {
                        lineItemsMatch = false;
                        break;
                    }
                }

                if (!lineItemsMatch)
                {
                    // Not the same line item(s), but the rest matched...
                    transAlmostTheSame.Add(transRow);
                    continue;
                }

                // Compare banking transaction if applicable
                if (accountRow.Type == EAccountType.Bank)
                {
                    var bankingTransactionRow = transRow.GetBankingTransaction();
                    if (!bankingTransactionRow.HasSame(medium, checkNumber))
                    {
                        // Not the same line medimu/check number, but the rest matched...
                        transAlmostTheSame.Add(transRow);
                        continue;
                    }
                }

                // Miracle! we found the exact same transaction
                bankTransactionTracker.FoundIDs.Add(transRow.ID);
                return;
            }

            // This may be the end of a transfer that was deleted by the pairing algorithm
            if (lineItemHolders.Count == 1 && lineItemHolders[0].AccountID >= 0)
            {
                foreach(Household.LineItemsRow li in household.LineItems.Rows)
                {
                    if (!li.IsAccountIDNull() && li.AccountID == accountRow.ID &&
                        !li.IsITransferStatusNull() &&
                        li.Amount == -lineItemHolders[0].Amount)
                    {
                        // Verify this transaction took place in the account targetted by our line item
                        if (li.TransactionsRow.AccountID == lineItemHolders[0].AccountID)
                        {
                            // Found you
                            bankTransactionTracker.FoundIDs.Add(li.ID); // ZZZ ???
                            return;
                        }
                    }
                }
            }

            // Let's see if this is one of the transfers that are incorrect in the QIF
            // and that were modified to have an undefined category
            if (transAlmostTheSame.Count == 1 && lineItemHolders.Count == 1)
            {
                var transRow = transAlmostTheSame[0];
                var lineItemRows = transRow.GetLineItemsRows();
                if (lineItemRows.Length == 1 &&
                    lineItemRows[0].HasSame(lineItemHolders[0].CategoryID, -1, lineItemHolders[0].Memo, lineItemHolders[0].Amount))
                {
                    // Yeah, that's it
                    bankTransactionTracker.FoundIDs.Add(transRow.ID);
                    return;
                }
            }

            // Create a new one, the user can delete the extra ones
            var newTrans = CreateBankingTransaction(accountRow, date, payee, memo, status, medium, checkNumber, lineItemHolders);
            bankTransactionTracker.AddedIDs.Add(newTrans.ID);
        }

        private Household.TransactionsRow CreateBankingTransaction(
            Household.AccountsRow accountRow, 
            DateTime date,
            string payee, 
            string memo,
            ETransactionStatus status,
            ETransactionMedium medium,
            uint checkNumber,
            IEnumerable<LineItemHolder> lineItemHolders)
        {
            // Create main transaction
            var transRow = household.Transactions.Add(accountRow, date, payee, memo, status, checkpointID);

            // Add bank-specific stuff
            if (accountRow.Type == EAccountType.Bank)
            {
                household.BankingTransactions.Add(transRow, medium, checkNumber);
            }

            // Add the line item(s)
            foreach (var lih in lineItemHolders)
            {
                household.LineItems.Add(
                    transRow,
                    lih.CategoryID,
                    lih.AccountID,
                    lih.Memo,
                    lih.Amount);
            }

            return transRow;
        }

        #endregion

        #region Parse QIF investment transactions

        private void ParseInvestmentTransactions(StreamReader sr, Household.AccountsRow account)
        {
            if (account == null)
            {
                throw new InvalidDataException("Investment transaction without current account");
            }

            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                ParseOneInvestmentTransaction(sr, account);
            }
        }

        private void ParseOneInvestmentTransaction(TextReader sr, Household.AccountsRow accountRow)
        {
            DateTime date = DateTime.MinValue;
            decimal amount = 0;
            decimal otherMysteriousAmount = 0;
            string memo = null;
            string payee = null;
            int categoryAccountID = -1;
            int categoryID = -1;
            ETransactionStatus status = ETransactionStatus.Pending;
            EExtendedInvestmentTransactionType extendedType = EExtendedInvestmentTransactionType.None;
            EInvestmentTransactionType type;
            Household.SecuritiesRow securityRow = null;
            decimal securityPrice = 0;
            decimal securityQuantity = 0;
            decimal commission = 0;
            bool commissionOnDividend = false;

            while (true)
            {
                var l = sr.ReadLine();
                if ((l == null) || (l == "^"))
                {
                    break;
                }

                string arg = l.Substring(1);

                switch (l[0])
                {
                    case 'D':
                        date = ParseDate(arg);
                        break;

                    case 'Y':
                        securityRow = household.Securities.GetByName(arg);
                        if (securityRow == null)
                        {
                            throw new InvalidDataException("Unknown security for investment transaction - db - " + arg);
                        }
                        break;

                    case 'I':
                        if (!decimal.TryParse(arg, out securityPrice))
                        {
                            throw new InvalidDataException("Cannot parse security price - " + arg);
                        }
                        break;

                    case 'Q':
                        if (!decimal.TryParse(arg, out securityQuantity))
                        {
                            throw new InvalidDataException("Cannot parse security quantity - " + arg);
                        }
                        break;

                    case 'O':
                        if (!decimal.TryParse(arg, out commission))
                        {
                            throw new InvalidDataException("Cannot parse commision - " + arg);
                        }
                        break;

                    case 'T':
                    case '$':
                        decimal.TryParse(arg, out amount);
                        break;

                    case 'U':
                        decimal.TryParse(arg, out otherMysteriousAmount);
                        break;
                        
                    //case 'E':
                    case 'M':
                        memo = arg;
                        break;

                    case 'P':
                        payee = arg;
                        break;

                    case 'C':
                        status = ParseTransactionStatus(arg);
                        break;

                    case 'L':
                        (categoryAccountID, categoryID, commissionOnDividend) = ParseTransactionTarget(household, accountRow, l.Substring(1));
                        break;

                    case 'N':
                        extendedType = ParseInvestmentTransactionType(arg);
                        break;

                    default:
                        throw new InvalidDataException("Unknown investment transaction attribute: " + l);
                }
            }

            // Check we have all info
            if (date == DateTime.MinValue)
            {
                throw new InvalidDataException("QIF parser: Investment transaction has no date - " + payee);
            }
            if (amount != otherMysteriousAmount)
            {
                throw new InvalidDataException("QIF parser: Mysterious amount not the same as regular amount - " + amount + " - " + otherMysteriousAmount);
            }
            if (extendedType == EExtendedInvestmentTransactionType.None)
            {
                throw new InvalidDataException("QIF parser: Investment transaction has no type - " + payee);
            }

            // For the fee on reinvestment, find the corresponding reinvestment transaction and modify its commission
            if (commissionOnDividend)
            {
                if (!merging) // ZZZZ
                {
                    var reinvDivTrans = accountRow.GetTransactionsRows()
                        .Where(t => t.Date == date && t.GetInvestmentTransaction().Type == EInvestmentTransactionType.ReinvestDividends)
                        .Single();
                    reinvDivTrans.GetInvestmentTransaction().Commission = amount;
                    reinvDivTrans.GetLineItemsRows()[0].Amount += amount;
                }
                return;
            }

            // Get rid of old transfer types and move them to CashIn/CashOut and XIn/XOut
            if (extendedType == EExtendedInvestmentTransactionType.Cash || 
                extendedType == EExtendedInvestmentTransactionType.TransferCash ||
                extendedType == EExtendedInvestmentTransactionType.TransferMiscellaneousIncomeIn)
            {
                if (amount >= 0)
                {
                    type = categoryAccountID >=0 ? EInvestmentTransactionType.TransferCashIn : EInvestmentTransactionType.CashIn;
                }
                else
                {
                    type = categoryAccountID >= 0 ? EInvestmentTransactionType.TransferCashOut : EInvestmentTransactionType.CashOut;
                    amount = -amount;
                }
            }
            else
            {
                type = (EInvestmentTransactionType)extendedType;
            }

            if (Household.InvestmentTransactionsRow.CashOut(type) || Household.InvestmentTransactionsRow.TransferOut(type))
            {
                amount = -amount;
            }

            if (merging)
            {
                MergeInvestmentTransaction(accountRow, date, payee, memo, status, categoryID, categoryAccountID, amount, type, securityRow, securityPrice, securityQuantity, commission);
            }
            else
            {
                CreateInvestmentTransaction(accountRow, date, payee, memo, status, categoryID, categoryAccountID, amount, type, securityRow, securityPrice, securityQuantity, commission);
            }
        }

        private void MergeInvestmentTransaction(
            Household.AccountsRow accountRow,
            DateTime date,
            string payee,
            string memo,
            ETransactionStatus status,
            int categoryID,
            int categoryAccountID,
            decimal amount,
            EInvestmentTransactionType type,
            Household.SecuritiesRow securityRow,
            decimal securityPrice,
            decimal securityQuantity,
            decimal commission)
        {
            // Try to find this exact same transaction
            foreach (var transRow in accountRow.GetTransactionsRows())
            {
                if (!transRow.HasSame(date, payee, memo, status))
                {
                    continue;
                }

                // Found same transaction, see if it has the same line items
                var lineItemRows = transRow.GetLineItemsRows();
                if (lineItemRows.Length != 1)
                {
                    // Should never get here
                    continue;
                }

                // Compare investment transaction
                var investmentTransactionRow = transRow.GetInvestmentTransaction();
                if (!investmentTransactionRow.HasSame(type, securityRow, securityPrice, securityQuantity, commission))
                {
                    continue;
                }

                // Compare the line items
                var liRow = lineItemRows[0];
                if (!liRow.HasSame(categoryID, categoryAccountID, null, amount))
                {
                    // May be an edited one? see pair transfer algorithm
                    if (categoryAccountID < 0 || !liRow.HasSame(categoryID, -1, null, amount))
                    {
                        // Not the same
                        continue;
                    }
                }

                // Miracle! we found the exact same transaction
                investmentTransactionTracker.FoundIDs.Add(transRow.ID);
                return;
            }

            // This may be the end of a transfer that was deleted by the pairing algorithm
            if (categoryAccountID >= 0)
            {
                foreach (Household.LineItemsRow li in household.LineItems.Rows)
                {
                    if (!li.IsAccountIDNull() && li.AccountID == accountRow.ID &&
                        !li.IsITransferStatusNull() &&
                        li.Amount == -amount)
                    {
                        // Verify this transaction took place in the account targetted by our line item
                        if (li.TransactionsRow.AccountID == categoryAccountID)
                        {
                            // Found you
                            investmentTransactionTracker.FoundIDs.Add(li.ID); // ZZZ ???
                            return;
                        }
                    }
                }
            }

            // Create a new transaction, the user can delete the extra ones
            var newTrans = CreateInvestmentTransaction(accountRow, date, payee, memo, status, categoryID, categoryAccountID, amount, type, securityRow, securityPrice, securityQuantity, commission);
            investmentTransactionTracker.AddedIDs.Add(newTrans.ID);
        }

        private Household.TransactionsRow CreateInvestmentTransaction(
            Household.AccountsRow accountRow,
            DateTime date,
            string payee,
            string memo,
            ETransactionStatus status,
            int categoryID,
            int categoryAccountID,
            decimal amount,
            EInvestmentTransactionType type,
            Household.SecuritiesRow securityRow,
            decimal securityPrice,
            decimal securityQuantity,
            decimal commission)
        {
            var transRow = household.Transactions.Add(accountRow, date, payee, memo, status, checkpointID);
            household.LineItems.Add(transRow, categoryID, categoryAccountID, null, amount);
            household.InvestmentTransactions.Add(transRow, type, securityRow, securityPrice, securityQuantity, commission);

            return transRow;
        }

        #endregion

        #region Parse memorized payee

        private void ParseMemorizedPayees(StreamReader sr, Household.AccountsRow account)
        {
            if (account == null)
            {
                throw new InvalidDataException("Memorized transaction without current account");
            }

            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                ParseOneMemorizedPayee(sr, account);
            }
        }

        private void ParseOneMemorizedPayee(TextReader sr, Household.AccountsRow account)
        {
            decimal amountToCheck = 0;
            decimal otherMysteriousAmount = 0;
            string payee = null;
            string memo = null;
            ETransactionStatus status = ETransactionStatus.Pending;
            //EMemorizedTransactionType type = EMemorizedTransactionType.None;

            List<LineItemHolder> lineItemHolders = new List<LineItemHolder>();
            var lineItemHolder = new LineItemHolder();
            bool parsingSplitLineItem = false;

            while (true)
            {
                var l = sr.ReadLine();
                if ((l == null) || (l == "^"))
                {
                    break;
                }

                switch (l[0])
                {
                    // Amount
                    case 'T':
                    case '$':
                        decimal.TryParse(l.Substring(1), out lineItemHolder.Amount);
                        if (!parsingSplitLineItem)
                        {
                            amountToCheck = lineItemHolder.Amount;
                        }
                        break;

                    // Amount again
                    case 'U':
                        decimal.TryParse(l.Substring(1), out otherMysteriousAmount);
                        break;

                    // Memo
                    case 'M':
                        memo = l.Substring(1);
                        break;

                    case 'E':
                        lineItemHolder.Memo = l.Substring(1);
                        break;

                    // Payee name
                    case 'P':
                        payee = l.Substring(1);
                        break;

                    // Status (none/cleared/reconciled)
                    case 'C':
                        status = ParseTransactionStatus(l.Substring(1));
                        break;

                    // Category
                    case 'L':
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID, _) = ParseTransactionTarget(household, account, l.Substring(1));
                        break;

                    // Payment/deposit
                    case 'K':
                        //type = ParseMemorizedTransactionType(l.Substring(1));
                        break;

                    case 'S':
                        // Indicates beginning of a new split line item - commit previous one if any
                        if (parsingSplitLineItem)
                        {
                            lineItemHolders.Add(lineItemHolder);
                            lineItemHolder = new LineItemHolder();
                        }
                        parsingSplitLineItem = true;
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID, _) = ParseTransactionTarget(household, account, l.Substring(1));
                        break;

                    case 'A':
                        // Address (up to 6 lines) - ignore.
                        break;

                    default:
                        throw new InvalidDataException("Unknown transaction attribute: " + l);
                }
            }

            // Flush last line item
            lineItemHolders.Add(lineItemHolder);

            // Check we have all info
            if (amountToCheck != otherMysteriousAmount)
            {
                throw new InvalidDataException("QIF parser: Mysterious amount not the same as regular amount - " + amountToCheck + " - " + otherMysteriousAmount);
            }

            if (merging)
            {
                MergeMemorizedPayee(payee, status, memo, lineItemHolders);
            }
            else
            {
                CreateMemorizedPayee(payee, status, memo, lineItemHolders);
            }
        }

        private void MergeMemorizedPayee(string payee, ETransactionStatus status, string memo, List<LineItemHolder> lineItemHolders)
        {
            // Try to find the same memorized payee
            foreach (var mpr in household.MemorizedPayees.Rows.Cast< Household.MemorizedPayeesRow>().Where(m => m.HasSame(payee, status, memo)))
            {
                // Compare line items
                var lineItemRows = mpr.GetMemorizedLineItemsRows();
                if (lineItemRows.Length != lineItemHolders.Count)
                {
                    continue;
                }

                bool samelineItems = true;
                for (int i = 0; i < lineItemRows.Length; i++)
                {
                    var lih = lineItemHolders[i];
                    if (!lineItemRows[i].HasSame(lih.CategoryID, lih.AccountID, lih.Memo, lih.Amount))
                    {
                        samelineItems = false;
                        break;
                    }
                }

                // We found the same memorized payee - all done
                if (samelineItems)
                {
                    memorizedPayeeTransactionTracker.FoundIDs.Add(mpr.ID);
                    return;
                }
            }

            // If we get here we did not find this memorized payee. Add it.
            var newRow = CreateMemorizedPayee(payee, status, memo, lineItemHolders);
            memorizedPayeeTransactionTracker.AddedIDs.Add(newRow.ID);
        }

        private Household.MemorizedPayeesRow CreateMemorizedPayee(string payee, ETransactionStatus status, string memo, IEnumerable<LineItemHolder> lineItemHolders)
        {
            // Create memorized payee
            var memorizedPayeeRow = household.MemorizedPayees.Add(payee, status, memo);

            // Add the line item(s)
            foreach (var lih in lineItemHolders)
            {
                household.MemorizedLineItems.Add(memorizedPayeeRow,
                    lih.CategoryID,
                    lih.AccountID,
                    lih.Memo,
                    lih.Amount);
            }

            return memorizedPayeeRow;
        }

        #endregion

        #region Parse QIF security prices

        private void ParseSecurityPrices(StreamReader sr)
        {
            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                ParseOneSecurityPrice(sr);
            }
        }

        private void ParseOneSecurityPrice(TextReader sr)
        {
            while (true)
            {
                var l = sr.ReadLine();
                if ((l == null) || (l == "^"))
                {
                    break;
                }

                l = l.Trim();

                // "BALCX",19.79," 6/29' 7"
                var comps = l.Split(',');
                if (comps.Length != 3)
                {
                    throw new InvalidDataException("Malformed security price: " + l);
                }

                var subComps = comps[0].Split('"');
                var securityRow = household.Securities.GetBySymbol(subComps[1]);
                if (securityRow == null)
                {
                    throw new InvalidDataException("Unknown security symbol (db): " + l);
                }

                // The price may in the m a/b, or a/b format
                string mainStr = "0";
                string numStr = "0";
                string denomStr = "1";
                
                subComps = comps[1].Split(new char[] { ' ', '/' });

                switch(subComps.Length)
                {
                    case 1:
                        mainStr = subComps[0];
                        break;
                    case 2:
                        numStr = subComps[0];
                        denomStr = subComps[1];
                        break;
                    case 3:
                        mainStr = subComps[0];
                        numStr = subComps[1];
                        denomStr = subComps[2];
                        break;
                    default:
                        throw new InvalidDataException("Garbled security price: " + l);
                }

                if (!decimal.TryParse(mainStr, out decimal main) ||
                    !decimal.TryParse(numStr, out decimal num) ||
                    !decimal.TryParse(denomStr, out decimal denom) ||
                    denom == 0)
                {
                    throw new InvalidDataException("Garbled security price: " + l);
                }

                decimal price = main + num / denom;

                // Parse date
                subComps = comps[2].Split('"');
                var date = ParseDate(subComps[1]);
                if (date.CompareTo(DateTime.Now) <= 0)
                {
                    if (merging)
                    {
                        MergeSecurityPrice(securityRow, date, price);
                    }
                    else
                    {
                        household.SecurityPrices.Add(securityRow, date, price);
                    }
                }
            }
        }

        private void MergeSecurityPrice(Household.SecuritiesRow securityRow, DateTime date, decimal price)
        {
            var existingSecurityPriceRow = securityRow.GetSecurityPricesRows().FirstOrDefault(spr => spr.Date == date && spr.Value == price);
            if (existingSecurityPriceRow == null)
            {
                // New  security price
                var newRow = household.SecurityPrices.Add(securityRow, date, price);
                securityPriceTracker.AddedIDs.Add(newRow.ID);
            }
            else
            {
                // Exactly the same
                securityPriceTracker.FoundIDs.Add(existingSecurityPriceRow.ID);
            }
        }


        #endregion

        #region Parse QIF basic fields

        private static DateTime ParseDate(string str)
        {
            // Date is in the format <m>/<d>'<y> (post 2000) or <m>/<d>/<y>
            int day = 0;
            int month = 0;
            int year = 0;

            var comps = str.Split('/');
            if (comps.Length == 3)
            {
                // pre-2000
                int.TryParse(comps[0], out month);
                int.TryParse(comps[1], out day);
                int.TryParse(comps[2], out year);
                year += 1900;
            }
            else if (comps.Length == 2)
            {
                // 2000 and afterwards
                int.TryParse(comps[0], out month);
                var subComps = comps[1].Split('\'');
                int.TryParse(subComps[0], out day);
                int.TryParse(subComps[1], out year);
                year += 2000;
            }

            return new DateTime(year, month, day);
        }

        private static ETransactionStatus ParseTransactionStatus(string statusStr)
        {
            ETransactionStatus status;

            switch (statusStr)
            {
                case "R":
                case "X":
                    status = ETransactionStatus.Reconciled;
                    break;
                case "c":
                    status = ETransactionStatus.Cleared;
                    break;
                default:
                    status = ETransactionStatus.Pending;
                    break;
            }

            return status;
        }

        private static ETransactionMedium ParseBankTransactionMedium(string typeStr, out uint checkNumber)
        {
            ETransactionMedium type;
            checkNumber = 0;

            switch (typeStr)
            {
                case "ATM":
                    type = ETransactionMedium.ATM;
                    break;
                case "EFT":
                    type = ETransactionMedium.EFT;
                    break;
                case "DEP":
                    type = ETransactionMedium.Deposit;
                    break;
                case "TXFR":
                    type = ETransactionMedium.Transfer;
                    break;
                case "DIV":
                    type = ETransactionMedium.Dividend;
                    break;
                case "Print":
                    type = ETransactionMedium.PrintCheck;
                    break;
                default:
                    if (uint.TryParse(typeStr, out checkNumber))
                    {
                        type = ETransactionMedium.Check;
                    }
                    else
                    {
                        throw new InvalidDataException("Unknown bank transaction type: " + typeStr);
                    }
                    break;
            }

            return type;
        }

        //private static EMemorizedTransactionType ParseMemorizedTransactionType(string typeStr)
        //{
        //    EMemorizedTransactionType type;

        //    switch (typeStr)
        //    {
        //        case "P":
        //            type = EMemorizedTransactionType.Payment;
        //            break;
        //        case "D":
        //            type = EMemorizedTransactionType.Deposit;
        //            break;
        //        case "C":
        //            type = EMemorizedTransactionType.Check;
        //            break;
        //        default:
        //            throw new InvalidDataException("Unknown memorized transaction type: " + typeStr);
        //    }

        //    return type;
        //}

        private enum EExtendedInvestmentTransactionType
        {
            InterestIncome = EInvestmentTransactionType.InterestIncome,
            TransferCashIn = EInvestmentTransactionType.TransferCashIn,
            TransferCashOut = EInvestmentTransactionType.TransferCashOut,
            SharesIn = EInvestmentTransactionType.SharesIn,
            SharesOut = EInvestmentTransactionType.SharesOut,
            Buy = EInvestmentTransactionType.Buy,
            BuyFromTransferredCash = EInvestmentTransactionType.BuyFromTransferredCash,
            Sell = EInvestmentTransactionType.Sell,
            SellAndTransferCash = EInvestmentTransactionType.SellAndTransferCash,
            Dividends = EInvestmentTransactionType.Dividends,
            TransferDividends = EInvestmentTransactionType.TransferDividends,
            ReinvestDividends = EInvestmentTransactionType.ReinvestDividends,
            ShortTermCapitalGains = EInvestmentTransactionType.ShortTermCapitalGains,
            TransferShortTermCapitalGains = EInvestmentTransactionType.TransferShortTermCapitalGains,
            ReinvestShortTermCapitalGains = EInvestmentTransactionType.ReinvestShortTermCapitalGains,
            ReinvestMediumTermCapitalGains = EInvestmentTransactionType.ReinvestMediumTermCapitalGains,
            LongTermCapitalGains = EInvestmentTransactionType.LongTermCapitalGains,
            TransferLongTermCapitalGains = EInvestmentTransactionType.TransferLongTermCapitalGains,
            ReinvestLongTermCapitalGains = EInvestmentTransactionType.ReinvestLongTermCapitalGains,
            ReturnOnCapital = EInvestmentTransactionType.ReturnOnCapital,
            Grant = EInvestmentTransactionType.Grant,
            Vest = EInvestmentTransactionType.Vest,
            Exercise = EInvestmentTransactionType.Exercise,
            Expire = EInvestmentTransactionType.Expire,
            Cash,
            TransferCash,
            TransferMiscellaneousIncomeIn,
            None
        }

        private static EExtendedInvestmentTransactionType ParseInvestmentTransactionType(string typeStr)
        {
            EExtendedInvestmentTransactionType type;

            switch (typeStr)
            {
                case "Cash":
                    type = EExtendedInvestmentTransactionType.Cash;
                    break;
                case "IntInc":
                    type = EExtendedInvestmentTransactionType.InterestIncome;
                    break;
                case "ContribX":
                    type = EExtendedInvestmentTransactionType.TransferCash;
                    break;
                case "XIn":
                    type = EExtendedInvestmentTransactionType.TransferCashIn;
                    break;
                case "XOut":
                    type = EExtendedInvestmentTransactionType.TransferCashOut;
                    break;
                case "MiscIncX":
                    type = EExtendedInvestmentTransactionType.TransferMiscellaneousIncomeIn;
                    break;
                case "ShrsIn":
                    type = EExtendedInvestmentTransactionType.SharesIn;
                    break;
                case "ShrsOut":
                    type = EExtendedInvestmentTransactionType.SharesOut;
                    break;
                case "Buy":
                    type = EExtendedInvestmentTransactionType.Buy;
                    break;
                case "BuyX":
                    type = EExtendedInvestmentTransactionType.BuyFromTransferredCash;
                    break;
                case "Sell":
                    type = EExtendedInvestmentTransactionType.Sell;
                    break;
                case "SellX":
                    type = EExtendedInvestmentTransactionType.SellAndTransferCash;
                    break;
                case "Div":
                    type = EExtendedInvestmentTransactionType.Dividends;
                    break;
                case "DivX":
                    type = EExtendedInvestmentTransactionType.TransferDividends;
                    break;
                case "ReinvDiv":
                    type = EExtendedInvestmentTransactionType.ReinvestDividends;
                    break;
                case "CGShort":
                    type = EExtendedInvestmentTransactionType.ShortTermCapitalGains;
                    break;
                case "CGShortX":
                    type = EExtendedInvestmentTransactionType.TransferShortTermCapitalGains;
                    break;
                case "ReinvSh":
                    type = EExtendedInvestmentTransactionType.ReinvestShortTermCapitalGains;
                    break;
                case "ReinvMd":
                    type = EExtendedInvestmentTransactionType.ReinvestMediumTermCapitalGains;
                    break;
                case "CGLong":
                    type = EExtendedInvestmentTransactionType.LongTermCapitalGains;
                    break;
                case "CGLongX":
                    type = EExtendedInvestmentTransactionType.TransferLongTermCapitalGains;
                    break;
                case "ReinvLg":
                    type = EExtendedInvestmentTransactionType.ReinvestLongTermCapitalGains;
                    break;
                case "RtrnCap":
                    type = EExtendedInvestmentTransactionType.ReturnOnCapital;
                    break;
                case "Grant":
                    type = EExtendedInvestmentTransactionType.Grant;
                    break;
                case "Vest":
                    type = EExtendedInvestmentTransactionType.Vest;
                    break;
                case "Exercise":
                    type = EExtendedInvestmentTransactionType.Exercise;
                    break;
                case "Expire":
                    type = EExtendedInvestmentTransactionType.Expire;
                    break;
                default:
                    throw new InvalidDataException("Unknown investment transaction type: " + typeStr);
            }

            return type;
        }

        private static (int accountID, int categoryID, bool commissionOnDividend) ParseTransactionTarget(Household household, Household.AccountsRow currentAccount, string target)
        {
            int accountID = -1;
            int categoryID = -1;
            bool commissionOnDividend = false;

            if (target == "")
            {
                // null is OK
            }
            else if (target.StartsWith("["))
            {
                // Transfer to another account
                int ix = target.IndexOf(']');
                target = target.Substring(1, ix - 1);
                if (target != deletedAccountStr)
                {
                    var accountRow = household.Accounts.GetByName(target);
                    if (accountRow == null)
                    {
                        throw new InvalidDataException("Unknown destination account: " + target);
                    }
                    accountID = accountRow.ID;
                }
            }
            else
            {
                // ZZZ Remove "classes"
                int ix = target.IndexOf('/');
                if (ix >= 0)
                {
                    target = target.Substring(0, ix);
                }

                // Category
                var categoryRow = household.Categories.GetByFullName(target);

                // Special case of _DivInc|[<currentAccount>}
                if (categoryRow == null)
                {
                    if (target == "_DivInc|[" + currentAccount.Name + "]")
                    {
                        // Amount received as dividend but payed as a fee to reinvest
                        // This is in effect a commission on ReinvDiv
                        commissionOnDividend = true;
                    }
                }
                else
                {
                    categoryID = categoryRow.ID;
                }

                if (categoryID < 0 && accountID < 0 && !commissionOnDividend)
                {
                    throw new InvalidDataException("Unknown category: " + target);
                }
            }

            return (accountID, categoryID, commissionOnDividend);
        }

        private static void SkipToNextType(TextReader sr)
        {
            while (sr.Peek() != '!')
            {
                sr.ReadLine();
            }
        }

        #endregion

        #region Pair transfers

        private void PairTransfers()
        {
            var pairs = new List<Tuple<int, int>>();

            //
            // Go over all line items
            //
            foreach(Household.LineItemsRow sourceLineItemRow in household.LineItems.Rows)
            {
                // Find transfers
                if (!sourceLineItemRow.IsAccountIDNull())
                {
                    // Found one. Verify we don't know about it already
                    if (pairs.Find(p => p.Item2 == sourceLineItemRow.ID) != null)
                    {
                        // Already in
                        continue;
                    }

                    var sourceTransactionRow = sourceLineItemRow.TransactionsRow;
                    int targetAccountID = sourceLineItemRow.AccountID;

                    // Skip transfers to self
                    if (targetAccountID == sourceTransactionRow.AccountID)
                    {
                        continue;
                    }

                    // if merging, only consider added transactions
                    if (merging &&
                        !bankTransactionTracker.AddedIDs.Contains(sourceTransactionRow.ID) &&
                        !investmentTransactionTracker.AddedIDs.Contains(sourceTransactionRow.ID))
                    {
                        continue;
                    }

                    Tuple<int, int> tuple = null;

                    // Look for transactions on the same date in the target account
                    foreach (Household.TransactionsRow targetTransactionRow in household.Transactions.Rows)
                    {
                        // if merging, only consider added transactions
                        if (merging &&
                            !bankTransactionTracker.AddedIDs.Contains(targetTransactionRow.ID) &&
                            !investmentTransactionTracker.AddedIDs.Contains(targetTransactionRow.ID))
                        {
                            continue;
                        }

                        var diffDate = sourceTransactionRow.Date.Subtract(targetTransactionRow.Date);

                        if (Math.Abs(diffDate.Days) <= 2 &&
                            targetTransactionRow.AccountID == targetAccountID)
                        {
                            // Look in line item of the same amount that are transfer to the source account
                            foreach (Household.LineItemsRow targetLineItemRow in targetTransactionRow.GetLineItemsRows())
                            {
                                if (!targetLineItemRow.IsAccountIDNull() &&
                                    targetLineItemRow.AccountID == sourceTransactionRow.AccountID &&
                                    Math.Abs(targetLineItemRow.Amount) == Math.Abs(sourceLineItemRow.Amount) && // Refine abs ZZZ
                                    pairs.Find(p => p.Item2 == targetLineItemRow.ID) == null) // Not used already
                                {
                                    // Found pair!
                                    tuple = new Tuple<int, int>(sourceLineItemRow.ID, targetLineItemRow.ID);
                                    break;
                                }
                            }
                        }

                        if (tuple != null)
                        {
                            break;
                        }
                    }

                    if (tuple == null)
                    {
                        var targetAccountRow = household.Accounts.FindByID(targetAccountID);
                        Log += $"Warning: Could not pair transaction on account {sourceTransactionRow.AccountsRow.Name}" + eol+
                            $"to/from account {targetAccountRow.Name} on {sourceTransactionRow.Date} for ${sourceLineItemRow.Amount};" + eol;
                        Log += "Changing category to <none>." + eol;

                        sourceLineItemRow.SetAccountIDNull();
                    }
                    else
                    {
                        pairs.Add(tuple);
                    }
                }
            }

            Log += $"Paired {pairs.Count} transfers." + eol;

            //
            // Remove one end of the pair
            //
            foreach (var pair in pairs)
            {
                var lineItemRow1 = household.LineItems.FindByID(pair.Item1);
                var transactionRow1 = lineItemRow1.TransactionsRow;
                var lineItemsRow1 =  transactionRow1.GetLineItemsRows();

                var lineItemRow2 = household.LineItems.FindByID(pair.Item2);
                var transactionRow2 = lineItemRow2.TransactionsRow;
                var lineItemsRow2 = transactionRow2.GetLineItemsRows();

                Household.TransactionsRow transactionToDelete = null;
                Household.LineItemsRow lineItemToDelete = null;
                Household.LineItemsRow lineItemToKeep = null;

                // If one of the transaction is a split, delete the other
                if (lineItemsRow1.Length > 1)
                {
                    transactionToDelete = transactionRow2;
                    lineItemToDelete = lineItemRow2;
                    lineItemToKeep = lineItemRow1;
                }
                else if (lineItemsRow2.Length > 1)
                {
                    transactionToDelete = transactionRow1;
                    lineItemToDelete = lineItemRow1;
                    lineItemToKeep = lineItemRow2;
                }
                else
                {
                    // If one of the transactions is an investment transaction involving securities, delete the other
                    var accountRow1 = transactionRow1.AccountsRow;
                    var accountRow2 = transactionRow2.AccountsRow;

                    if (accountRow1.Type == EAccountType.Investment &&
                        transactionRow1.GetInvestmentTransaction() is Household.InvestmentTransactionsRow investmentTransactionRow1 &&
                        investmentTransactionRow1.Type != EInvestmentTransactionType.TransferCashIn &&
                        investmentTransactionRow1.Type != EInvestmentTransactionType.TransferCashOut)
                    {
                        transactionToDelete = transactionRow2;
                        lineItemToDelete = lineItemRow2;
                        lineItemToKeep = lineItemRow1;
                    }
                    else if (accountRow2.Type == EAccountType.Investment &&
                        transactionRow2.GetInvestmentTransaction() is Household.InvestmentTransactionsRow investmentTransactionRow2 &&
                        investmentTransactionRow2.Type != EInvestmentTransactionType.TransferCashIn &&
                        investmentTransactionRow2.Type != EInvestmentTransactionType.TransferCashOut)
                    {
                        transactionToDelete = transactionRow1;
                        lineItemToDelete = lineItemRow1;
                        lineItemToKeep = lineItemRow2;
                    }
                    else
                    {
                        // Arbitrarily delete the destination (where the funds go to)
                        if (lineItemRow1.Amount < 0)
                        {
                            // No 1 is the source
                            transactionToDelete = transactionRow2;
                            lineItemToDelete = lineItemRow2;
                            lineItemToKeep = lineItemRow1;
                        }
                        else
                        {
                            // No 2 is the source
                            transactionToDelete = transactionRow2;
                            lineItemToDelete = lineItemRow2;
                            lineItemToKeep = lineItemRow1;
                        }
                    }
                }

                // Memorize the status of the transaction we are about to delete
                lineItemToKeep.TransferStatus = transactionToDelete.Status;

                // Delete line item
                lineItemToDelete.Delete();

                // Delete transaction
                if (transactionToDelete.AccountsRow.Type == EAccountType.Investment)
                {
                    transactionToDelete.GetInvestmentTransaction().Delete();
                }
                else if (transactionToDelete.AccountsRow.Type == EAccountType.Bank)
                {
                    transactionToDelete.GetBankingTransaction().Delete();
                }
                transactionToDelete.Delete();
            }
        }

        #endregion
    }
}
