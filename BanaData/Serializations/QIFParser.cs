//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Data;

using BanaData.Database;

namespace BanaData.Serializations
{
    class QIFParser
    {
        #region Constants

        private const string deletedAccountStr = "Deleted Account";
        private readonly string eol = Environment.NewLine;

        #endregion

        #region Private members

        private readonly Household household;

        #endregion

        #region Constructor

        public QIFParser(Household _household) => household = _household;

        #endregion

        #region Log

        public string Log { get; private set; } = "";

        #endregion

        #region QIF main parser

        public void ConvertFromQIF(string fileName)
        {
            Log = "";
            household.Clear();
            household.AcceptChanges();
            Household.AccountsRow accountRow = null;

            using (var sr = new StreamReader(fileName))
            {
                while (!sr.EndOfStream)
                {
                    accountRow = ParseOneSection(sr, accountRow);
                }
            }

            // Log what we got
            Log += $"Imported {household.Accounts.Rows.Count:N0} accounts" + eol;
            Log += $"Imported {household.Categories.Rows.Count:N0} categories" + eol;
            Log += $"Imported {household.Securities.Rows.Count:N0} securities" + eol;
            Log += $"Imported {household.Transactions.Rows.Count:N0} transactions" + eol;
            Log += $"Imported {household.MemorizedPayees.Rows.Count:N0} memorized payees" + eol;
            Log += eol;

            // Find other sides of transfers
            PairTransfers();

            household.AcceptChanges();
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

            // Create category and add it to the database
            household.Categories.Add(name, description, parentRow, income, taxInfo);
        }

        #endregion

        #region Parse QIF account

        private Household.AccountsRow ParseAccounts(StreamReader sr)
        {
            Household.AccountsRow result = null;

            while (sr.Peek() != '!' && !sr.EndOfStream)
            {
                result = ParseOneAccount(sr);
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

            // Create account if it does not exist, and make it the current account
            var accountRow = household.Accounts.GetByName(name);
            if (accountRow == null)
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

                household.Accounts.Add(name, description, type, creditLimit, kind, hidden);
            }

            return accountRow;
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

            // Create security and add it to the list
            household.Securities.Add(name, symbol, type);

        }

        #endregion

        # region Parse QIF transactions

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

            List<LineItemHolder> lineItemHodlers = new List<LineItemHolder>();
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
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID) = ParseTransactionTarget(household, accountRow, l.Substring(1));
                        break;
                    
                    case 'N':
                        medium = ParseBankTransactionMedium(l.Substring(1), out checkNumber);
                        break;
                    
                    case 'S':
                        // Indicates beginning of a new split line item - commit previous one if any
                        if (parsingSplitLineItem)
                        {
                            lineItemHodlers.Add(lineItemHolder);
                            lineItemHolder = new LineItemHolder();
                        }
                        parsingSplitLineItem = true;
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID) = ParseTransactionTarget(household, accountRow, l.Substring(1));
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

            // Create main transaction
            var transRow = household.Transactions.Add(accountRow, date, payee, memo, status);

            // Add bank-specific stuff
            if (accountRow.Type == EAccountType.Bank)
            {
                household.BankingTransactions.Add(transRow, medium, checkNumber);
            }

            // Add the line item(s)
            lineItemHodlers.Add(lineItemHolder);
            foreach (var lih in lineItemHodlers)
            {
                household.LineItems.Add(
                    transRow, 
                    lih.CategoryID,
                    lih.AccountID, 
                    lih.Memo, 
                    lih.Amount);
            }
        }

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
            string mainMemo = null;
            string altMemo = null;
            int categoryAccountID = -1;
            int categoryID = -1;
            ETransactionStatus status = ETransactionStatus.Pending;
            EInvestmentTransactionType type = EInvestmentTransactionType.None;
            Household.SecuritiesRow securityRow = null;
            decimal securityPrice = 0;
            decimal securityQuantity = 0;
            decimal commission = 0;

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
                        mainMemo = arg;
                        break;

                    case 'P':
                        altMemo = arg;
                        break;

                    case 'C':
                        status = ParseTransactionStatus(arg);
                        break;

                    case 'L':
                        (categoryAccountID, categoryID) = ParseTransactionTarget(household, accountRow, l.Substring(1));
                        break;

                    case 'N':
                        type = ParseInvestmentTransactionType(arg);
                        break;

                    default:
                        throw new InvalidDataException("Unknown investment transaction attribute: " + l);
                }
            }

            // Check we have all info
            if (date == DateTime.MinValue)
            {
                throw new InvalidDataException("QIF parser: Investment transaction has no date - " + altMemo);
            }
            if (amount != otherMysteriousAmount)
            {
                throw new InvalidDataException("QIF parser: Mysterious amount not the same as regular amount - " + amount + " - " + otherMysteriousAmount);
            }

            if (Household.InvestmentTransactionsRow.CashOut(type) || Household.InvestmentTransactionsRow.TransferOut(type))
            {
                amount = -amount;
            }

            var transRow = household.Transactions.Add(accountRow, date, altMemo, mainMemo, status);
            household.LineItems.Add(transRow, categoryID, categoryAccountID, null, amount);
            household.InvestmentTransactions.Add(transRow, type, securityRow, securityPrice, securityQuantity, commission);
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

            List<LineItemHolder> lineItemHodlers = new List<LineItemHolder>();
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
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID) = ParseTransactionTarget(household, account, l.Substring(1));
                        break;

                    // Payment/deposit
                    case 'K':
                        //type = ParseMemorizedTransactionType(l.Substring(1));
                        break;

                    case 'S':
                        // Indicates beginning of a new split line item - commit previous one if any
                        if (parsingSplitLineItem)
                        {
                            lineItemHodlers.Add(lineItemHolder);
                            lineItemHolder = new LineItemHolder();
                        }
                        parsingSplitLineItem = true;
                        (lineItemHolder.AccountID, lineItemHolder.CategoryID) = ParseTransactionTarget(household, account, l.Substring(1));
                        break;

                    case 'A':
                        // Address (up to 6 lines) - ignore.
                        break;

                    default:
                        throw new InvalidDataException("Unknown transaction attribute: " + l);
                }
            }

            // Check we have all info
            if (amountToCheck != otherMysteriousAmount)
            {
                throw new InvalidDataException("QIF parser: Mysterious amount not the same as regular amount - " + amountToCheck + " - " + otherMysteriousAmount);
            }

            // Create memorized payee
            var memorizedPayees = household.MemorizedPayees.Add(payee, status, memo);

            // Add the line item(s)
            lineItemHodlers.Add(lineItemHolder);
            foreach (var lih in lineItemHodlers)
            {
                household.MemorizedLineItems.Add(memorizedPayees,
                    lih.CategoryID,
                    lih.AccountID,
                    lih.Memo,
                    lih.Amount);
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
                    household.SecurityPrices.Add(securityRow, date, price);
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

        private static EInvestmentTransactionType ParseInvestmentTransactionType(string typeStr)
        {
            EInvestmentTransactionType type;

            switch (typeStr)
            {
                case "Cash":
                    type = EInvestmentTransactionType.Cash;
                    break;
                case "IntInc":
                    type = EInvestmentTransactionType.InterestIncome;
                    break;
                case "ContribX":
                    type = EInvestmentTransactionType.TransferCash;
                    break;
                case "XIn":
                    type = EInvestmentTransactionType.TransferCashIn;
                    break;
                case "XOut":
                    type = EInvestmentTransactionType.TransferCashOut;
                    break;
                case "MiscIncX":
                    type = EInvestmentTransactionType.TransferMiscellaneousIncomeIn;
                    break;
                case "ShrsIn":
                    type = EInvestmentTransactionType.SharesIn;
                    break;
                case "ShrsOut":
                    type = EInvestmentTransactionType.SharesOut;
                    break;
                case "Buy":
                    type = EInvestmentTransactionType.Buy;
                    break;
                case "BuyX":
                    type = EInvestmentTransactionType.BuyFromTransferredCash;
                    break;
                case "Sell":
                    type = EInvestmentTransactionType.Sell;
                    break;
                case "SellX":
                    type = EInvestmentTransactionType.SellAndTransferCash;
                    break;
                case "Div":
                    type = EInvestmentTransactionType.Dividends;
                    break;
                case "DivX":
                    type = EInvestmentTransactionType.TransferDividends;
                    break;
                case "ReinvDiv":
                    type = EInvestmentTransactionType.ReinvestDividends;
                    break;
                case "CGShort":
                    type = EInvestmentTransactionType.ShortTermCapitalGains;
                    break;
                case "CGShortX":
                    type = EInvestmentTransactionType.TransferShortTermCapitalGains;
                    break;
                case "ReinvSh":
                    type = EInvestmentTransactionType.ReinvestShortTermCapitalGains;
                    break;
                case "ReinvMd":
                    type = EInvestmentTransactionType.ReinvestMediumTermCapitalGains;
                    break;
                case "CGLong":
                    type = EInvestmentTransactionType.LongTermCapitalGains;
                    break;
                case "CGLongX":
                    type = EInvestmentTransactionType.TransferLongTermCapitalGains;
                    break;
                case "ReinvLg":
                    type = EInvestmentTransactionType.ReinvestLongTermCapitalGains;
                    break;
                case "RtrnCap":
                    type = EInvestmentTransactionType.ReturnOnCapital;
                    break;
                case "Grant":
                    type = EInvestmentTransactionType.Grant;
                    break;
                case "Vest":
                    type = EInvestmentTransactionType.Vest;
                    break;
                case "Exercise":
                    type = EInvestmentTransactionType.Exercise;
                    break;
                case "Expire":
                    type = EInvestmentTransactionType.Expire;
                    break;
                default:
                    throw new InvalidDataException("Unknown investment transaction type: " + typeStr);
            }

            return type;
        }

        private static (int accountID, int categoryID) ParseTransactionTarget(Household household, Household.AccountsRow currentAccount, string target)
        {
            int accountID = -1;
            int categoryID = -1;

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
                        // Transfer to self
                        accountID = currentAccount.ID;
                    }
                }
                else
                {
                    categoryID = categoryRow.ID;
                }

                if (categoryID < 0 && accountID < 0)
                {
                    throw new InvalidDataException("Unknown category: " + target);
                }
            }

            return (accountID, categoryID);
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

                    var sourceTransactionRow = household.Transactions.FindByID(sourceLineItemRow.TransactionID);
                    int targetAccountID = sourceLineItemRow.AccountID;

                    // Skip transfers to self
                    if (targetAccountID == sourceTransactionRow.AccountID)
                    {
                        continue;
                    }

                    Tuple<int, int> tuple = null;

                    // Look for transactions on the same date in the target account
                    foreach (Household.TransactionsRow targetTransactionRow in household.Transactions.Rows)
                    {
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
                var transactionRow1 = household.Transactions.FindByID(lineItemRow1.TransactionID);
                var lineItemsRow1 = household.LineItems.GetByTransaction(transactionRow1);

                var lineItemRow2 = household.LineItems.FindByID(pair.Item2);
                var transactionRow2 = household.Transactions.FindByID(lineItemRow2.TransactionID);
                var lineItemsRow2 = household.LineItems.GetByTransaction(transactionRow2);

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
                    var accountRow1 = household.Accounts.FindByID(transactionRow1.AccountID);
                    var accountRow2 = household.Accounts.FindByID(transactionRow2.AccountID);

                    if (accountRow1.Type == EAccountType.Investment &&
                        household.InvestmentTransactions.GetByTransaction(transactionRow1) is Household.InvestmentTransactionsRow investmentTransactionRow1 &&
                        investmentTransactionRow1.Type != EInvestmentTransactionType.TransferCash &&
                        investmentTransactionRow1.Type != EInvestmentTransactionType.TransferCashIn &&
                        investmentTransactionRow1.Type != EInvestmentTransactionType.TransferCashOut)
                    {
                        transactionToDelete = transactionRow2;
                        lineItemToDelete = lineItemRow2;
                        lineItemToKeep = lineItemRow1;
                    }
                    else if (accountRow2.Type == EAccountType.Investment &&
                        household.InvestmentTransactions.GetByTransaction(transactionRow2) is Household.InvestmentTransactionsRow investmentTransactionRow2 &&
                        investmentTransactionRow2.Type != EInvestmentTransactionType.TransferCash &&
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
                var accountOfTransactionToDeleteRow = household.Accounts.FindByID(transactionToDelete.AccountID);
                if (accountOfTransactionToDeleteRow.Type == EAccountType.Investment)
                {
                    household.InvestmentTransactions.GetByTransaction(transactionToDelete).Delete();
                }
                else if (accountOfTransactionToDeleteRow.Type == EAccountType.Bank)
                {
                    household.BankingTransactions.GetByTransaction(transactionToDelete).Delete();
                }
                transactionToDelete.Delete();
            }
        }

        #endregion
    }
}
