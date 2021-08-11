//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Database
{
    public partial class Household
    {
        partial class TransactionRow
        {
            // Bridges to local enum types
            public ETransactionStatus Status
            {
                get => (ETransactionStatus)IStatus;
                set => IStatus = (int)value;
            }

            public ETransactionType Type
            {
                get => (ETransactionType)IType;
                set => IType = (int)value;
            }

            // Compute amount of a transaction by adding up all lines item
            public decimal GetAmount()
            {
                return GetLineItemRows().Sum(lir => lir.Amount);
            }

            public BankingTransactionRow GetBankingTransaction()
            {
                return GetBankingTransactionRows().Single();
            }

            public InvestmentTransactionRow GetInvestmentTransaction()
            {
                return GetInvestmentTransactionRows().Single();
            }

            public void CreatePeerTransaction(int targetAccountID, LineItemRow liRow, decimal peerAmount)
            {
                var household = this.Table.DataSet as Household;

                var targetAccountRow = household.Account.FindByID(targetAccountID);

                // Special case of transfer to self
                if (targetAccountRow == AccountRow)
                {
                    household.LineItemTransfer.AddLineItemTransferRow(liRow, AccountRow, this);
                    return;
                }

                // Add transaction on "other side"
                var peerTransactionRow = household.Transaction.Add(
                    targetAccountRow,
                    Date,
                    "",
                    IsMemoNull() ? null : Memo,
                    ETransactionStatus.Pending,
                    household.Checkpoint.GetMostRecentCheckpointID(),
                    ETransactionType.Regular);
                var peerLiRow = household.LineItem.Add(peerTransactionRow, null, peerAmount);

                // Create the investment/banking transactions
                if (targetAccountRow.Type == EAccountType.Bank)
                {
                    household.BankingTransaction.Add(peerTransactionRow, ETransactionMedium.None, 0);
                }
                else if (targetAccountRow.Type == EAccountType.Investment)
                {
                    var type = peerLiRow.Amount >= 0 ? EInvestmentTransactionType.TransferCashIn : EInvestmentTransactionType.TransferCashOut;
                    household.InvestmentTransaction.Add(peerTransactionRow, type, null, 0, 0, 0);
                }

                // Create the transfer line items
                household.LineItemTransfer.AddLineItemTransferRow(liRow, targetAccountRow, peerTransactionRow);
                household.LineItemTransfer.AddLineItemTransferRow(peerLiRow, AccountRow, this);
            }

        }

        public IEnumerable<TransactionRow> RegularTransactions => Transaction.Rows.Cast<Household.TransactionRow>().Where(t => t.Type == ETransactionType.Regular);
        public IEnumerable<TransactionRow> MemorizedPayees => Transaction.Rows.Cast<Household.TransactionRow>().Where(t => t.Type == ETransactionType.MemorizedPayee);
        public IEnumerable<TransactionRow> ScheduledTransactions => Transaction.Rows.Cast<Household.TransactionRow>().Where(t => t.Type == ETransactionType.ScheduledTransaction);

        partial class TransactionDataTable
        {
            public TransactionRow Add(AccountRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status, int checkpointID, ETransactionType type)
            {
                var transactionRow = NewTransactionRow();

                UpdateTransaction(transactionRow, accountRow, date, payee, memo, status, checkpointID, type);

                Rows.Add(transactionRow);

                return transactionRow;
            }

            public TransactionRow Update(int transactionID, AccountRow accountRow, DateTime date, string payee, string memo, ETransactionStatus status, int checkpointID, ETransactionType type)
            {
                var transactionRow = FindByID(transactionID);

                UpdateTransaction(transactionRow, accountRow, date, payee, memo, status, checkpointID, type);

                return transactionRow;
            }

            private static TransactionRow UpdateTransaction(
                TransactionRow transactionRow, 
                AccountRow accountRow,
                DateTime date, 
                string payee, 
                string memo, 
                ETransactionStatus status,
                int checkpointID,
                ETransactionType type)
            {
                if (accountRow == null)
                {
                    transactionRow.SetAccountIDNull();
                }
                else
                {
                    transactionRow.AccountID = accountRow.ID;
                }

                transactionRow.Date = date;

                if (string.IsNullOrWhiteSpace(payee))
                {
                    transactionRow.SetPayeeNull();
                }
                else
                {
                    transactionRow.Payee = payee;
                }

                if (string.IsNullOrWhiteSpace(memo))
                {
                    transactionRow.SetMemoNull();
                }
                else
                {
                    transactionRow.Memo = memo;
                }

                transactionRow.Status = status;
                transactionRow.CheckpointID = checkpointID;
                transactionRow.Type = type;

                return transactionRow;
            }
        }
    }
}
