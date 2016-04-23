using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bananas.Data
{
    // Account type
    public enum EAccountType { Cash, Bank, CreditCard, Investment, OtherAsset, OtherLiability, Invalid };

    // Kind of investment, for investment accounts
    public enum EInvestmentKind { Brokerage, SingleMutualFund, _401k, Invalid };

    // Security type
    public enum ESecurityType { Stock, MutualFund, MarketIndex, EmployeeStockOption, Invalid }

    // Transaction status
    public enum ETransactionStatus { Pending, Cleared, Reconciled };

    // Bank transaction medium
    public enum ETransactionMedium { ATM, EFT, Deposit, Transfer, Check, PrintCheck, Dividend, Cash, None };

    // Memorized transaction type
    public enum EMemorizedTransactionType { Check, Payment, Deposit, None };

    // Investment transaction type
    public enum EInvestmentTransactionType
    {
        Cash,
        InterestIncome,
        TransferCash,
        TransferCashIn,
        TransferCashOut,
        TransferMiscellaneousIncomeIn,
        SharesIn,
        SharesOut,
        Buy,
        BuyFromTransferredCash,
        Sell,
        SellAndTransferCash,
        Dividends,
        ReinvestDividends,
        ReinvestShortTermCapitalGains,
        ReinvestMediumTermCapitalGains,
        ReinvestLongTermCapitalGains,
        Grant,
        Vest,
        Exercise,
        Expire,
        None
    }


}
