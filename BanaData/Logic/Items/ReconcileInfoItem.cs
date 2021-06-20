using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Immutable representing the info needed to perform a reconcile information
    /// </summary>
    public class ReconcileInfoItem
    {
        // Minimal constructor
        public ReconcileInfoItem(int accountID) =>
            AccountID = accountID;

        // Explicit constructor
        public ReconcileInfoItem(int accountID, DateTime statementEndDate, decimal statementBalance,
            decimal interestAmount, DateTime interestDate, string interestCategory) =>
            (AccountID, StatementEndDate, StatementBalance, InterestAmount, InterestDate, InterestCategory) =
            (accountID, statementEndDate, statementBalance, interestAmount, interestDate, interestCategory);

        // Clone
        public ReconcileInfoItem(ReconcileInfoItem src) =>
            (AccountID, StatementEndDate, StatementBalance, InterestAmount, InterestDate, InterestCategory) =
            (src.AccountID, src.StatementEndDate, src.StatementBalance, src.InterestAmount, src.InterestDate, src.InterestCategory);

        public readonly int AccountID;

        // Statement end date for the current reconcile
        public readonly DateTime StatementEndDate;

        // Statement balance for the current reconcile
        public readonly decimal StatementBalance;

        // Banking: Interest info
        public readonly decimal InterestAmount;
        public readonly DateTime InterestDate;
        public readonly string InterestCategory = "Interest Inc";

        public override bool Equals(object obj)
        {
            return
                obj is ReconcileInfoItem r &&
                AccountID == r.AccountID &&
                StatementEndDate == r.StatementEndDate &&
                StatementBalance == r.StatementBalance &&
                InterestAmount == r.InterestAmount &&
                InterestDate == r.InterestDate &&
                InterestCategory == r.InterestCategory;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
