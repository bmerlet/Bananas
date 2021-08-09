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

        [EnumDescription("Traditional IRA")]
        TraditionalIRA,

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

    // Transaction type
    public enum ETransactionType { Regular, MemorizedPayee, RecurringTransaction }

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

        [EnumDescription("")]
        None
    };

    // Memorized transaction type
    public enum EMemorizedTransactionType { Check, Payment, Deposit, None };

    // Investment transaction type
    public enum EInvestmentTransactionType
    {
        [EnumDescription("CashIn")]
        CashIn,

        [EnumDescription("CashOut")]
        CashOut,

        [EnumDescription("XIn")]
        TransferCashIn,

        [EnumDescription("XOut")]
        TransferCashOut,

        [EnumDescription("Int")]
        InterestIncome,

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
        Expire
    }

    // Composite transaction state foir the UI
    [Flags]
    public enum ETransactionState
    {
        Idle = 0,
        Reconciled = 1,
        NegativeAmount = 2
    }

    // Transaction report flags
    [Flags]
    public enum ETransactionReportFlag
    {
        None = 0,
        IsFilteringOnAccounts = 0x1,
        IsFilteringOnPayees = 0x2,
        IsFilteringOnCategories = 0x4,

        ShowTransactions = 0x10,
        ShowSubtotals = 0x20,

        GroupByAccount = 0x100,
        GroupByPayee = 0x200,
        GroupByCategory = 0x400,

        ShowAccountColumn = 0x1000,
        ShowDateColumn = 0x2000,
        ShowPayeeColumn = 0x4000,
        ShowMemoColumn = 0x8000,
        ShowCategoryColumn = 0x10000,
        ShowStatusColumn = 0x20000
    }
}
