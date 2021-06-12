using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.Attributes;

namespace BanaData.Database
{
    // Account type
    public enum EAccountType
    {
        [EnumDescription("Cash")]
        Cash,

        [EnumDescription("Bank account")]
        Bank,

        [EnumDescription("Credit card")]
        CreditCard,

        [EnumDescription("Investment")]
        Investment,

        [EnumDescription("Asset")]
        OtherAsset,

        [EnumDescription("Liability")]
        OtherLiability, 

        Invalid 
    };

    // Kind of investment, for investment accounts
    public enum EInvestmentKind 
    {
        [EnumDescription("Brokerage")]
        Brokerage,

        [EnumDescription("Single Security")]
        SingleMutualFund,

        [EnumDescription("401k")]
        _401k,

        [EnumDescription("N/A")]
        Invalid
    };

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
        TransferDividends,
        ReinvestDividends,
        ShortTermCapitalGains,
        TransferShortTermCapitalGains,
        ReinvestShortTermCapitalGains,
        ReinvestMediumTermCapitalGains,
        LongTermCapitalGains,
        TransferLongTermCapitalGains,
        ReinvestLongTermCapitalGains,
        ReturnOnCapital,
        Grant,
        Vest,
        Exercise,
        Expire,
        None
    }


}
