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
    /// Type of imports
    /// </summary>
    public enum EImportType { FullQIF, QIFTransactions, PDFTransactions, None };

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

        private readonly Household household;
        private Household.CheckpointRow checkpointRow;
        private readonly List<string> accountNames = new List<string>();

        // When importing only transactions, transaction counter
        private int numberOfTransactions;

        #endregion

        #region Constructor

        public QIFParser(Household _household) => household = _household;

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

        #endregion

        #region Entry points

        //
        // Parse the whole DB from a QIF file
        //
        public void ImportFullDBFromQIF(string path)
        {
            // Init
            Log = "";
            accountNames.Clear();
            numberOfTransactions = 0;

            // Preserve info not present in QIF hoping that it could be used against the new DB
            var supplementalInfo = GetSupplemtalInfo();

            // Zap the database
            household.Clear();
            household.AcceptChanges();

            // Create a checkpoint - all transactions are created under this checkpoint
            household.Checkpoint.CreateNewCheckpoint();
            checkpointRow = household.Checkpoint.GetCurrentCheckpoint();

            // Parse the file
            ParseFile(path, false);

            // Log what we got
            Log += $"Imported {household.Account.Rows.Count:N0} accounts" + eol;
            Log += $"Imported {household.Category.Rows.Count:N0} categories" + eol;
            Log += $"Imported {household.Security.Rows.Count:N0} securities" + eol;
            Log += $"Imported {household.SecurityPrice.Rows.Count:N0} security prices" + eol;
            Log += $"Imported {household.RegularTransactions.Count():N0} transactions" + eol;
            Log += $"Imported {household.MemorizedPayees.Count():N0} memorized payees" + eol;
            Log += eol;

            // Find other sides of transfers
            PairTransfers();

            // Re-apply supplemental info
            ApplySupplementalInfo(supplementalInfo);

            // Create a new checkpoint, all transactions created by the user henceforth
            // will be associated with this new checkpoint
            household.Checkpoint.CreateNewCheckpoint();

            // Commit
            household.AcceptChanges();
        }

        //
        // Parse QIF transactions
        //
        public void ImportTransactionsFromQIF(string path)
        {
            // Init
            Log = "";
            accountNames.Clear();
            numberOfTransactions = 0;

            // Create a checkpoint - all transactions are created under this checkpoint
            household.Checkpoint.CreateNewCheckpoint();
            checkpointRow = household.Checkpoint.GetCurrentCheckpoint();

            // Parse the file
            ParseFile(path, true);

            // Log what we got
            Log += $"Imported {numberOfTransactions:N0} transactions" + eol;

            // Create a new checkpoint, all transactions created by the user henceforth
            // will be associated with this new checkpoint
            household.Checkpoint.CreateNewCheckpoint();

            household.AcceptChanges();
        }

        #endregion

        #region QIF main parser

        // Parse a QIF file
        private void ParseFile(string path, bool onlyTransactions)
        {
            // Pick the first credit card account in the DB if parsing only transactions
            Household.AccountRow accountRow = onlyTransactions ? household.Account.First(a => a.Type == EAccountType.CreditCard) : null;

            // Read the file
            using (var sr = new QIFReader(path))
            {
                // Parse all sections
                while (!sr.EndOfStream)
                {
                    accountRow = ParseOneSection(sr, accountRow, onlyTransactions);
                }
            }
        }

        private Household.AccountRow ParseOneSection(StreamReader sr, Household.AccountRow accountRow, bool onlyTransactions)
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
                                ComplainIfOnlyTransactions("Category", onlyTransactions);
                                ParseCategories(sr);
                                break;
                            case "Security":
                                ComplainIfOnlyTransactions("Security", onlyTransactions);
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
                                ComplainIfOnlyTransactions("Memorized payee", onlyTransactions);
                                ParseMemorizedPayees(sr);
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
                    accountRow = ParseAccounts(sr, onlyTransactions);
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

        private void ComplainIfOnlyTransactions(string what, bool onlyTransactions)
        {
            if (onlyTransactions)
            {
                throw new InvalidDataException($"QIF parser: {what} encountered while parsing only transactions");
            }
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

                switch (l[0])
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
            Household.CategoryRow parentRow = null;
            for (int c = 0; c < (components.Length - 1); c++)
            {
                parentRow = household.Category.GetByParentAndName(parentRow, components[c]);
            }

            // Add category to the database
            household.Category.Add(name, description, parentRow, income, taxInfo);
        }

        #endregion

        #region Parse QIF account

        private Household.AccountRow ParseAccounts(StreamReader sr, bool onlyTransactions)
        {
            Household.AccountRow result = null;

            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                result = ParseOneAccount(sr, onlyTransactions);
            }

            return result;
        }

        private Household.AccountRow ParseOneAccount(TextReader sr, bool onlyTransactions)
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
                                    throw new InvalidDataException("Other asset account type (Oth A) not supported");
                                case "Oth L":
                                    throw new InvalidDataException("Other Liability account type (Oth L) not supported");
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

            Household.AccountRow accountRow;

            // If only importing transactions this account should already exist
            if (onlyTransactions)
            {
                accountRow = household.Account.GetByName(name);
                if (accountRow == null)
                {
                    throw new InvalidDataException($"QIF parser: Account {name} does not exist");
                }
            }
            // Skip if we already saw this name (account name are present at least twice in QIF files)
            else if (accountNames.Contains(name))
            {
                accountRow = household.Account.GetByName(name);
            }
            else
            {
                accountNames.Add(name);

                // Create account
                accountRow = CreateAccount(name, description, type, creditLimit, kind);
            }

            // Make this account the current account
            return accountRow;
        }

        private Household.AccountRow CreateAccount(string name, string description, EAccountType type, decimal creditLimit, EInvestmentKind kind)
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

            return household.Account.Add(name, description, type, creditLimit, kind, hidden, null);
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
            string symbol = Household.SecurityRow.SYMBOL_NONE;
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

            // Create security and add it to the DB
            household.Security.Add(name, symbol, type);
        }

        #endregion

        # region Parse QIF banking transactions

        private void ParseBankTransactions(StreamReader sr, Household.AccountRow account)
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

        private void ParseOneBankTransaction(TextReader sr, Household.AccountRow accountRow)
        {
            DateTime date = DateTime.MinValue;
            decimal amount = 0;
            decimal mysteriousAmount = 0;
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
                            amount = lineItemHolder.Amount;
                        }
                        break;

                    case 'U':
                        decimal.TryParse(l.Substring(1), out mysteriousAmount);
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
            if (mysteriousAmount != 0 && amount != mysteriousAmount)
            {
                throw new InvalidDataException($"QIF parser: Mysterious amount ({mysteriousAmount}) not the same as regular amount ({amount})");
            }

            // Flush last line item
            lineItemHolders.Add(lineItemHolder);

            // Create transaction
            CreateBankingTransaction(accountRow, date, payee, memo, status, medium, checkNumber, lineItemHolders);
            numberOfTransactions += 1;
        }

        private Household.TransactionRow CreateBankingTransaction(
            Household.AccountRow accountRow,
            DateTime date,
            string payee,
            string memo,
            ETransactionStatus status,
            ETransactionMedium medium,
            uint checkNumber,
            IEnumerable<LineItemHolder> lineItemHolders)
        {
            // Create main transaction
            var transRow = household.Transaction.Add(accountRow, date, payee, memo, status, checkpointRow, ETransactionType.Regular);

            // Add bank-specific stuff
            if (accountRow.Type == EAccountType.Bank)
            {
                household.BankingTransaction.Add(transRow, medium, checkNumber);
            }

            // Add the line item(s)
            foreach (var lih in lineItemHolders)
            {
                var li = household.LineItem.Add(transRow, lih.Memo, lih.Amount);

                if (lih.CategoryID != -1)
                {
                    var lineItemCategoryRow = household.LineItemCategory.NewLineItemCategoryRow();
                    lineItemCategoryRow.LineItemID = li.ID;
                    lineItemCategoryRow.CategoryID = lih.CategoryID;
                    household.LineItemCategory.AddLineItemCategoryRow(lineItemCategoryRow);
                }
                else if (lih.AccountID != -1)
                {
                    var lineItemTransferRow = household.LineItemTransfer.NewLineItemTransferRow();
                    lineItemTransferRow.LineItemID = li.ID;
                    lineItemTransferRow.AccountID = lih.AccountID;
                    lineItemTransferRow.PeerTransID = -1;
                    household.LineItemTransfer.AddLineItemTransferRow(lineItemTransferRow);
                }
            }

            return transRow;
        }

        #endregion

        #region Parse QIF investment transactions

        private void ParseInvestmentTransactions(StreamReader sr, Household.AccountRow account)
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

        private void ParseOneInvestmentTransaction(TextReader sr, Household.AccountRow accountRow)
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
            Household.SecurityRow securityRow = null;
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
                        securityRow = household.Security.GetByName(arg);
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
                var reinvDivTrans = accountRow.GetRegularTransactionRows()
                    .Where(t => t.Date == date && t.GetInvestmentTransaction().Type == EInvestmentTransactionType.ReinvestDividends)
                    .Single();
                reinvDivTrans.GetInvestmentTransaction().Commission = amount;
                reinvDivTrans.GetLineItemRows()[0].Amount += amount;
                return;
            }

            // Get rid of old transfer types and move them to CashIn/CashOut and XIn/XOut
            if (extendedType == EExtendedInvestmentTransactionType.Cash ||
                extendedType == EExtendedInvestmentTransactionType.TransferCash ||
                extendedType == EExtendedInvestmentTransactionType.TransferMiscellaneousIncomeIn)
            {
                if (amount >= 0)
                {
                    type = categoryAccountID >= 0 ? EInvestmentTransactionType.TransferCashIn : EInvestmentTransactionType.CashIn;
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

            if (Household.InvestmentTransactionRow.CashOut(type) || Household.InvestmentTransactionRow.TransferOut(type))
            {
                amount = -amount;
            }

            CreateInvestmentTransaction(accountRow, date, payee, memo, status, categoryID, categoryAccountID, amount, type, securityRow, securityPrice, securityQuantity, commission);
            numberOfTransactions += 1;
        }

        private Household.TransactionRow CreateInvestmentTransaction(
            Household.AccountRow accountRow,
            DateTime date,
            string payee,
            string memo,
            ETransactionStatus status,
            int categoryID,
            int categoryAccountID,
            decimal amount,
            EInvestmentTransactionType type,
            Household.SecurityRow securityRow,
            decimal securityPrice,
            decimal securityQuantity,
            decimal commission)
        {
            var transRow = household.Transaction.Add(accountRow, date, payee, memo, status, checkpointRow, ETransactionType.Regular);
            household.InvestmentTransaction.Add(transRow, type, securityRow, securityPrice, securityQuantity, commission);
            var li = household.LineItem.Add(transRow, null, amount);
            if (categoryID != -1)
            {
                var lineItemCategoryRow = household.LineItemCategory.NewLineItemCategoryRow();
                lineItemCategoryRow.LineItemID = li.ID;
                lineItemCategoryRow.CategoryID = categoryID;
                household.LineItemCategory.AddLineItemCategoryRow(lineItemCategoryRow);
            }
            else if (categoryAccountID != -1)
            {
                var lineItemTransferRow = household.LineItemTransfer.NewLineItemTransferRow();
                lineItemTransferRow.LineItemID = li.ID;
                lineItemTransferRow.AccountID = categoryAccountID;
                lineItemTransferRow.PeerTransID = -1;
                household.LineItemTransfer.AddLineItemTransferRow(lineItemTransferRow);
            }

            return transRow;
        }

        #endregion

        #region Parse memorized payee

        private void ParseMemorizedPayees(StreamReader sr)
        {
            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                ParseOneMemorizedPayee(sr);
            }
        }

        private void ParseOneMemorizedPayee(TextReader sr)
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
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID, _) = ParseTransactionTarget(household, null, l.Substring(1));
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
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID, _) = ParseTransactionTarget(household, null, l.Substring(1));
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

            CreateMemorizedPayee(payee, status, memo, lineItemHolders);
        }

        private void CreateMemorizedPayee(string payee, ETransactionStatus status, string memo, IEnumerable<LineItemHolder> lineItemHolders)
        {
            // Create memorized payee
            var transRow = household.Transaction.Add(null, DateTime.MinValue, payee, memo, status, checkpointRow, ETransactionType.MemorizedPayee);

            // Add the line item(s)
            foreach (var lih in lineItemHolders)
            {
                var li = household.LineItem.Add(transRow, lih.Memo, lih.Amount);

                if (lih.CategoryID != -1)
                {
                    var lineItemCategoryRow = household.LineItemCategory.NewLineItemCategoryRow();
                    lineItemCategoryRow.LineItemID = li.ID;
                    lineItemCategoryRow.CategoryID = lih.CategoryID;
                    household.LineItemCategory.AddLineItemCategoryRow(lineItemCategoryRow);
                }
                else if (lih.AccountID != -1)
                {
                    var lineItemTransferRow = household.LineItemTransfer.NewLineItemTransferRow();
                    lineItemTransferRow.LineItemID = li.ID;
                    lineItemTransferRow.AccountID = lih.AccountID;
                    lineItemTransferRow.PeerTransID = -1;
                    household.LineItemTransfer.AddLineItemTransferRow(lineItemTransferRow);
                }
            }
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
                if (comps.Length == 4)
                {
                    comps[1] += "," + comps[2];
                    comps[2] = comps[3];
                }
                else if (comps.Length != 3)
                {
                    throw new InvalidDataException("Malformed security price: " + l);
                }

                var subComps = comps[0].Split('"');
                var securityRow = household.Security.GetBySymbol(subComps[1]);
                if (securityRow == null)
                {
                    throw new InvalidDataException("Unknown security symbol (db): " + l);
                }

                // The price may in the m a/b, or a/b format
                string mainStr = "0";
                string numStr = "0";
                string denomStr = "1";

                subComps = comps[1].Split(new char[] { ' ', '/' });

                switch (subComps.Length)
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
                    household.SecurityPrice.Add(securityRow, date, price);
                }
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
                // Either MM/dd/yy (pre-2000) or MM/dd/yyyy
                int.TryParse(comps[0], out month);
                int.TryParse(comps[1], out day);
                int.TryParse(comps[2], out year);
                if (year < 1900)
                {
                    year += 1900;
                }
            }
            else if (comps.Length == 2)
            {
                // 2000 and afterwards: MM/dd'yy. Also allow for MM/dd/yyyy.
                int.TryParse(comps[0], out month);
                var subComps = comps[1].Split('\'');
                int.TryParse(subComps[0], out day);
                int.TryParse(subComps[1], out year);
                if (year < 2000)
                {
                    year += 2000;
                }
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
                case "*":
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

        private static (int accountID, int categoryID, bool commissionOnDividend) ParseTransactionTarget(Household household, Household.AccountRow currentAccount, string target)
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
                    var accountRow = household.Account.GetByName(target);
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
                var categoryRow = household.Category.GetByFullName(target);

                // Special case of _DivInc|[<currentAccount>}
                if (categoryRow == null && currentAccount != null)
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
            int pairCounter = 0;
            var lineItemTransfersToDelete = new List<Household.LineItemTransferRow>();

            //
            // Go over all transfer line items
            //
            foreach (Household.LineItemTransferRow sourceLineItemTransferRow in household.LineItemTransfer.Rows)
            {
                // Verify we don't know about it already
                if (sourceLineItemTransferRow.PeerTransID != -1)
                {
                    // Already in
                    continue;
                }

                var sourceTransactionRow = sourceLineItemTransferRow.LineItemRow.TransactionRow;
                int sourceAccountID = sourceTransactionRow.AccountID;
                int targetAccountID = sourceLineItemTransferRow.AccountID;

                // Skip transfers to self
                if (targetAccountID == sourceAccountID)
                {
                    sourceLineItemTransferRow.PeerTransID = sourceTransactionRow.ID;
                    continue;
                }

                // Look for transfers of the opposite amount on the target account
                foreach (Household.LineItemTransferRow targetLineItemTransferRow in household.LineItemTransfer.Rows)
                {
                    if (targetLineItemTransferRow.AccountID == sourceAccountID &&
                        sourceLineItemTransferRow.LineItemRow.Amount == -targetLineItemTransferRow.LineItemRow.Amount)
                    {
                        // Check date close enough
                        var targetTransactionRow = targetLineItemTransferRow.LineItemRow.TransactionRow;

                        var diffDate = sourceTransactionRow.Date.Subtract(targetTransactionRow.Date);

                        if (Math.Abs(diffDate.Days) <= 2)
                        {
                            // Found pair!
                            sourceLineItemTransferRow.PeerTransID = targetTransactionRow.ID;
                            targetLineItemTransferRow.PeerTransID = sourceTransactionRow.ID;
                            pairCounter += 1;
                            break;
                        }
                    }
                }

                if (sourceLineItemTransferRow.PeerTransID == -1)
                {
                    var targetAccountRow = household.Account.FindByID(targetAccountID);
                    Log += $"Warning: Could not pair transaction on account {sourceTransactionRow.AccountRow.Name}" + eol +
                        $"to/from account {targetAccountRow.Name} on {sourceTransactionRow.Date} for ${sourceLineItemTransferRow.LineItemRow.Amount};" + eol;
                    Log += "Changing category to <none>." + eol;

                    lineItemTransfersToDelete.Add(sourceLineItemTransferRow);
                }
            }

            // Remove "bad" transfer line items
            lineItemTransfersToDelete.ForEach(t => t.Delete());

            Log += eol + $"Paired {pairCounter} transfers." + eol;
        }

        #endregion

        #region Save and restore non-QIF info

        private SupplementalInfo GetSupplemtalInfo()
        {
            var supplementalInfo = new SupplementalInfo();

            // Persons
            foreach (Household.PersonRow personRow in household.Person.Rows)
            {
                supplementalInfo.Persons.Add(personRow.Name);
            }

            // Accounts: Hidden flag in accounts, IRA kind in accounts, last statement date
            foreach (Household.AccountRow accountRow in household.Account.Rows)
            {
                supplementalInfo.Accounts.Add(
                    new SupplementalInfo.Account(
                        accountRow.Name,
                        accountRow.Hidden,
                        accountRow.Type == EAccountType.Investment && accountRow.Kind == EInvestmentKind.TraditionalIRA,
                        accountRow.IsLastStatementDateNull() ? null : accountRow.LastStatementDate as DateTime?,
                        accountRow.Owner));
            }

            // ReconcileInfo table and SecurityReconcileInfo table
            foreach (Household.ReconcileInfoRow reconcileInfoRow in household.ReconcileInfo)
            {
                var reconcileInfo = new SupplementalInfo.ReconcileInfo(
                    reconcileInfoRow.AccountRow.Name,
                    reconcileInfoRow.StatementDate,
                    reconcileInfoRow.StatementBalance,
                    reconcileInfoRow.IsInterestAmountNull() ? 0 : reconcileInfoRow.InterestAmount,
                    reconcileInfoRow.IsInterestDateNull() ? null : reconcileInfoRow.InterestDate as DateTime?,
                    reconcileInfoRow.IsInterestCategoryIDNull() ? null : reconcileInfoRow.CategoryRow.FullName);

                reconcileInfo.SecurityReconcileInfos.AddRange(
                    reconcileInfoRow.GetSecurityReconcileInfoRows()
                    .Select(srir => new SupplementalInfo.ReconcileInfo.SecurityReconcileInfo(
                            srir.SecurityRow.Name, srir.SecurityQuantity)));

                supplementalInfo.ReconcileInfos.Add(reconcileInfo);
            }

            // RebalanceTarget table
            foreach (Household.RebalanceTargetRow rebalanceTargetRow in household.RebalanceTarget)
            {
                supplementalInfo.RebalanceTargets.Add(
                    new SupplementalInfo.RebalanceTarget(
                        rebalanceTargetRow.AccountRow.Name, rebalanceTargetRow.SecurityRow.Name, rebalanceTargetRow.Target));
            }

            // TransactionReport table and related info
            foreach (Household.TransactionReportRow transactionReportRow in household.TransactionReport)
            {
                var transactionReport = new SupplementalInfo.TransactionReport(
                    transactionReportRow.Name,
                    transactionReportRow.IsDescriptionNull() ? null : transactionReportRow.Description,
                    transactionReportRow.StartDate,
                    transactionReportRow.EndDate,
                    transactionReportRow.Flags);

                transactionReport.Accounts.AddRange(transactionReportRow.GetTransactionReportAccountRows().Select(a => a.AccountRow.Name));
                transactionReport.Payees.AddRange(transactionReportRow.GetTransactionReportPayeeRows().Select(p => p.Payee));
                transactionReport.Categories.AddRange(transactionReportRow.GetTransactionReportCategoryRows().Select(c => c.CategoryRow.FullName));

                supplementalInfo.TransactionReports.Add(transactionReport);
            }

            // Scheduled transactions
            foreach (Household.ScheduleRow scheduleRow in household.Schedule)
            {
                var transactionRow = scheduleRow.TransactionRow;
                var medium = transactionRow.AccountRow.Type == EAccountType.Bank ? transactionRow.GetBankingTransaction().Medium : ETransactionMedium.None;
                var schedule = new SupplementalInfo.Schedule(
                    scheduleRow.NextDate, scheduleRow.EndDate, scheduleRow.Frequency, scheduleRow.Flags,
                    transactionRow.AccountRow.Name,
                    medium,
                    transactionRow.IsPayeeNull() ? null : transactionRow.Payee,
                    transactionRow.IsMemoNull() ? null : transactionRow.Memo);

                foreach (var lineItemRow in transactionRow.GetLineItemRows())
                {
                    string memo = lineItemRow.IsMemoNull() ? null : lineItemRow.Memo;
                    if (lineItemRow.GetLineItemCategoryRow() is Household.LineItemCategoryRow licr)
                    {
                        schedule.ScheduleLineItems.Add(new SupplementalInfo.Schedule.ScheduleLineItem(memo, licr.CategoryRow.FullName, null, lineItemRow.Amount));
                    }
                    else if (lineItemRow.GetLineItemTransferRow() is Household.LineItemTransferRow litr)
                    {
                        schedule.ScheduleLineItems.Add(new SupplementalInfo.Schedule.ScheduleLineItem(memo, null, litr.AccountRow.Name, lineItemRow.Amount));
                    }
                }

                supplementalInfo.Schedules.Add(schedule);
            }

            return supplementalInfo;
        }

        private void ApplySupplementalInfo(SupplementalInfo supplementalInfo)
        {
            // Persons
            foreach (var person in supplementalInfo.Persons)
            {
                var personRow = household.Person.NewPersonRow();
                personRow.Name = person;
                household.Person.AddPersonRow(personRow);
            }

            // Accounts: Hidden flag in accounts, IRA kind in accounts, last statement date, owner
            foreach (Household.AccountRow accountRow in household.Account.Rows)
            {
                var sai = supplementalInfo.Accounts.FirstOrDefault(a => a.Name == accountRow.Name);
                if (sai != null)
                {
                    accountRow.Hidden = sai.Hidden;
                    if (sai.IsIRA && !accountRow.IsIKindNull())
                    {
                        accountRow.Kind = EInvestmentKind.TraditionalIRA;
                    }
                    if (sai.LastStatementDate.HasValue)
                    {
                        accountRow.LastStatementDate = sai.LastStatementDate.Value;
                    }
                    if (sai.Person != null)
                    {
                        accountRow.PersonID = household.Person.Rows.Cast<Household.PersonRow>().Single(p => p.Name == sai.Person).ID;
                    }
                }
            }

            // ReconcileInfo table and SecurityReconcileInfo table
            foreach (var reconcileInfo in supplementalInfo.ReconcileInfos)
            {
                var accountRow = household.Account.GetByName(reconcileInfo.AccountName);
                if (accountRow != null)
                {
                    var reconcileInfoRow = household.ReconcileInfo.NewReconcileInfoRow();

                    reconcileInfoRow.AccountID = accountRow.ID;
                    reconcileInfoRow.StatementDate = reconcileInfo.StatementDate;
                    reconcileInfoRow.StatementBalance = reconcileInfo.StatementBalance;

                    if (reconcileInfo.InterestDate != null)
                    {
                        reconcileInfoRow.InterestAmount = reconcileInfo.InterestAmount;
                        reconcileInfoRow.InterestDate = reconcileInfo.InterestDate.Value;
                        var categoryRow = household.Category.GetByFullName(reconcileInfo.InterestCategory);
                        if (categoryRow != null)
                        {
                            reconcileInfoRow.InterestCategoryID = categoryRow.ID;
                        }
                    }

                    household.ReconcileInfo.AddReconcileInfoRow(reconcileInfoRow);

                    foreach (var securityReconcileInfo in reconcileInfo.SecurityReconcileInfos)
                    {
                        var securityRow = household.Security.GetByName(securityReconcileInfo.Name);
                        if (securityRow != null)
                        {
                            var securityReconcileInfoRow = household.SecurityReconcileInfo.NewSecurityReconcileInfoRow();
                            securityReconcileInfoRow.ReconcileInfoID = reconcileInfoRow.ID;
                            securityReconcileInfoRow.SecurityID = securityRow.ID;
                            securityReconcileInfoRow.SecurityQuantity = securityReconcileInfo.Quantity;
                            household.SecurityReconcileInfo.AddSecurityReconcileInfoRow(securityReconcileInfoRow);
                        }
                    }
                }
            }

            // RebalanceTarget table
            foreach (var rebalanceTarget in supplementalInfo.RebalanceTargets)
            {
                var accountRow = household.Account.GetByName(rebalanceTarget.AccountName);
                if (accountRow != null)
                {
                    var securityRow = household.Security.GetByName(rebalanceTarget.SecurityName);
                    if (securityRow != null)
                    {
                        var rebalanceTargetRow = household.RebalanceTarget.NewRebalanceTargetRow();
                        rebalanceTargetRow.AccountID = accountRow.ID;
                        rebalanceTargetRow.SecurityID = securityRow.ID;
                        rebalanceTargetRow.Target = rebalanceTarget.Target;
                        household.RebalanceTarget.AddRebalanceTargetRow(rebalanceTargetRow);
                    }
                }
            }

            // Transaction reports
            foreach (var transactionReport in supplementalInfo.TransactionReports)
            {
                var transactionReportRow = household.TransactionReport.NewTransactionReportRow();
                transactionReportRow.Name = transactionReport.Name;
                if (transactionReport.Description != null)
                {
                    transactionReportRow.Description = transactionReport.Description;
                }
                transactionReportRow.StartDate = transactionReport.StartDate;
                transactionReportRow.EndDate = transactionReport.EndDate;
                transactionReportRow.Flags = transactionReport.Flags;
                household.TransactionReport.AddTransactionReportRow(transactionReportRow);

                foreach (var accountName in transactionReport.Accounts)
                {
                    var accountRow = household.Account.GetByName(accountName);
                    if (accountRow != null)
                    {
                        var transactionReportAccountRow = household.TransactionReportAccount.NewTransactionReportAccountRow();
                        transactionReportAccountRow.TransactionReportID = transactionReportRow.ID;
                        transactionReportAccountRow.AccountID = accountRow.ID;
                        household.TransactionReportAccount.AddTransactionReportAccountRow(transactionReportAccountRow);
                    }
                }

                foreach (var payeeName in transactionReport.Payees)
                {
                    var transactionReportPayeeRow = household.TransactionReportPayee.NewTransactionReportPayeeRow();
                    transactionReportPayeeRow.TransactionReportID = transactionReportRow.ID;
                    transactionReportPayeeRow.Payee = payeeName;
                    household.TransactionReportPayee.AddTransactionReportPayeeRow(transactionReportPayeeRow);
                }

                foreach (var categoryFullName in transactionReport.Categories)
                {
                    var categoryRow = household.Category.GetByFullName(categoryFullName);
                    if (categoryRow != null)
                    {
                        var transactionReportCategoryRow = household.TransactionReportCategory.NewTransactionReportCategoryRow();
                        transactionReportCategoryRow.TransactionReportID = transactionReportRow.ID;
                        transactionReportCategoryRow.CategoryID = categoryRow.ID;
                        household.TransactionReportCategory.AddTransactionReportCategoryRow(transactionReportCategoryRow);
                    }
                }
            }

            // Scheduled transactions
            foreach (var schedule in supplementalInfo.Schedules)
            {
                // Check the categories and accounts referenced are still present
                var accountRow = household.Account.GetByName(schedule.Account);
                if (accountRow == null)
                {
                    continue;
                }
                if (schedule.ScheduleLineItems.Any(li => li.Category != null && household.Category.GetByFullName(li.Category) == null))
                {
                    continue;
                }
                if (schedule.ScheduleLineItems.Any(li => li.Account != null && household.Account.GetByName(li.Account) == null))
                {
                    continue;
                }

                // Build transaction
                var transactionRow = household.Transaction.Add(
                    accountRow,
                    DateTime.MinValue,
                    schedule.Payee,
                    schedule.Memo,
                    ETransactionStatus.Pending,
                    checkpointRow,
                    ETransactionType.ScheduledTransaction);

                if (accountRow.Type == EAccountType.Bank)
                {
                    household.BankingTransaction.Add(transactionRow, schedule.Medium, 0);
                }

                // Commit all line items
                foreach (var lineItem in schedule.ScheduleLineItems)
                {
                    var newRow = household.LineItem.Add(transactionRow, lineItem.Memo, lineItem.Amount);
                    if (lineItem.Category != null)
                    {
                        household.LineItemCategory.AddLineItemCategoryRow(newRow, household.Category.GetByFullName(lineItem.Category));
                    }
                    else if (lineItem.Account != null)
                    {
                        household.LineItemTransfer.AddLineItemTransferRow(newRow, household.Account.GetByName(lineItem.Account), transactionRow);
                    }
                }

                // Commit the schedule
                var newScheduleRow = household.Schedule.AddScheduleRow(
                    schedule.NextDate,
                    schedule.EndDate,
                    (int)schedule.Frequency,
                    (int)schedule.Flags,
                    transactionRow);
            }
        }

        private class SupplementalInfo
        {
            // Persons in the household
            public readonly List<string> Persons = new List<string>();

            // Account info
            public readonly List<Account> Accounts = new List<Account>();
            public class Account
            {
                public Account(string name, bool hidden, bool isIRA, DateTime? lastStatementDate, string person) =>
                    (Name, Hidden, IsIRA, LastStatementDate, Person) = (name, hidden, isIRA, lastStatementDate, person);

                public readonly string Name;
                public readonly bool Hidden;
                public readonly bool IsIRA;
                public readonly DateTime? LastStatementDate;
                public readonly string Person;
            }

            // Reconcile info table
            public readonly List<ReconcileInfo> ReconcileInfos = new List<ReconcileInfo>();
            public class ReconcileInfo
            {
                public ReconcileInfo(string accountName, DateTime statementDate, decimal statementBalance, decimal interestAmount, DateTime? interestDate, string interestCategory) =>
                    (AccountName, StatementDate, StatementBalance, InterestAmount, InterestDate, InterestCategory) =
                        (accountName, statementDate, statementBalance, interestAmount, interestDate, interestCategory);
                public readonly string AccountName;
                public readonly DateTime StatementDate;
                public readonly decimal StatementBalance;
                public readonly decimal InterestAmount;
                public readonly DateTime? InterestDate;
                public readonly string InterestCategory;
                public readonly List<SecurityReconcileInfo> SecurityReconcileInfos = new List<SecurityReconcileInfo>();

                public class SecurityReconcileInfo
                {
                    public SecurityReconcileInfo(string name, decimal quantity) => (Name, Quantity) = (name, quantity);
                    public readonly string Name;
                    public readonly decimal Quantity;
                }
            }

            // Rebalance targets
            public readonly List<RebalanceTarget> RebalanceTargets = new List<RebalanceTarget>();
            public class RebalanceTarget
            {
                public RebalanceTarget(string accountName, string securityName, decimal target) =>
                    (AccountName, SecurityName, Target) = (accountName, securityName, target);

                public readonly string AccountName;
                public readonly string SecurityName;
                public readonly decimal Target;
            }

            // Transaction reports
            public readonly List<TransactionReport> TransactionReports = new List<TransactionReport>();
            public class TransactionReport
            {
                public TransactionReport(string name, string description, DateTime startDate, DateTime endDate, ETransactionReportFlag flags) =>
                    (Name, Description, StartDate, EndDate, Flags) = (name, description, startDate, endDate, flags);

                public readonly string Name;
                public readonly string Description;
                public readonly DateTime StartDate;
                public readonly DateTime EndDate;
                public readonly ETransactionReportFlag Flags;

                public readonly List<string> Accounts = new List<string>();
                public readonly List<string> Payees = new List<string>();
                public readonly List<string> Categories = new List<string>();
            }

            // Scheduled transactions
            public readonly List<Schedule> Schedules = new List<Schedule>();
            public class Schedule
            {
                public Schedule(
                    DateTime nextDate, DateTime endDate, EScheduleFrequency frequency, EScheduleFlag flags,
                    string account, ETransactionMedium medium, string payee, string memo)
                {
                    (NextDate, EndDate, Frequency, Flags) = (nextDate, endDate, frequency, flags);
                    (Account, Medium, Payee, Memo) = (account, medium, payee, memo);
                }

                public readonly DateTime NextDate;
                public readonly DateTime EndDate;
                public readonly EScheduleFrequency Frequency;
                public readonly EScheduleFlag Flags;

                public readonly string Account;
                public readonly ETransactionMedium Medium;
                public readonly string Payee;
                public readonly string Memo;
                public readonly List<ScheduleLineItem> ScheduleLineItems = new List<ScheduleLineItem>();

                public class ScheduleLineItem
                {
                    public ScheduleLineItem(string memo, string category, string account, decimal amount) =>
                        (Memo, Category, Account, Amount) = (memo, category, account, amount);

                    public readonly string Memo;
                    public readonly string Category;
                    public readonly string Account;
                    public readonly decimal Amount;
                }
            }
        }

        #endregion

        #region Stream reader that skips blank lines

        private class QIFReader : StreamReader
        {
            public QIFReader(string path) : base(path)
            {
                SkipBlankLines();
            }

            public override string ReadLine()
            {
                var line = base.ReadLine();
                SkipBlankLines();

                return line;
            }

            private void SkipBlankLines()
            {
                while(Peek() == '\n' || Peek() == '\r')
                {
                    base.ReadLine();
                }
            }
        }

        #endregion
    }
}
