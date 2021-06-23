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
    public enum ESecurityType
    {
        [EnumDescription("Stock")]
        Stock,

        [EnumDescription("Mutual Fund")]
        MutualFund,

        [EnumDescription("Market Index")]
        MarketIndex,

        [EnumDescription("Employee Stock Option")]
        EmployeeStockOption,

        [EnumDescription("Other")]
        Invalid
    }

    // Transaction status
    public enum ETransactionStatus { Pending, Cleared, Reconciled };

    // Bank transaction medium
    public enum ETransactionMedium
    {
        [EnumDescription("ATM")]
        ATM,

        [EnumDescription("EFT")]
        EFT,

        [EnumDescription("DEP")]
        Deposit,

        [EnumDescription("Transfer")]
        Transfer,

        [EnumDescription("Check")]
        Check,

        [EnumDescription("PrtChk")]
        PrintCheck,

        [EnumDescription("Div")]
        Dividend,

        [EnumDescription("Cash")]
        Cash,

        [EnumDescription("")]
        None
    };

    // Memorized transaction type
    public enum EMemorizedTransactionType { Check, Payment, Deposit, None };

    // Investment transaction type
    public enum EInvestmentTransactionType
    {
        [EnumDescription("Cash")]
        Cash,

        [EnumDescription("Interest income")]
        InterestIncome,

        [EnumDescription("Transfer cash")]
        TransferCash,

        [EnumDescription("XIn")]
        TransferCashIn,

        [EnumDescription("XOut")]
        TransferCashOut,

        [EnumDescription("MiscIn")]
        TransferMiscellaneousIncomeIn,

        [EnumDescription("SharesIn")]
        SharesIn,

        [EnumDescription("SharesOut")]
        SharesOut,

        [EnumDescription("Bought")]
        Buy,

        [EnumDescription("BoughtX")]
        BuyFromTransferredCash,

        [EnumDescription("Sold")]
        Sell,

        [EnumDescription("SoldX")]
        SellAndTransferCash,

        [EnumDescription("Div")]
        Dividends,

        [EnumDescription("DivX")]
        TransferDividends,

        [EnumDescription("ReinvDiv")]
        ReinvestDividends,

        [EnumDescription("CGShort")]
        ShortTermCapitalGains,

        [EnumDescription("CGShortX")]
        TransferShortTermCapitalGains,

        [EnumDescription("ReinvCGShort")]
        ReinvestShortTermCapitalGains,

        [EnumDescription("ReinvCGMedium")]
        ReinvestMediumTermCapitalGains,

        [EnumDescription("CGLong")]
        LongTermCapitalGains,

        [EnumDescription("CGLongX")]
        TransferLongTermCapitalGains,

        [EnumDescription("ReinvCGLong")]
        ReinvestLongTermCapitalGains,

        [EnumDescription("ROC")]
        ReturnOnCapital,

        [EnumDescription("Grant")]
        Grant,

        [EnumDescription("Vest")]
        Vest,

        [EnumDescription("Exercise")]
        Exercise,

        [EnumDescription("Expire")]
        Expire,

        [EnumDescription("")]
        None
    }


}
