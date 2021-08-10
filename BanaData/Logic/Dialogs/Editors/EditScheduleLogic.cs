using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Items;
using BanaData.Logic.Main;
using Toolbox.Attributes;
using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs.Editors
{
    public class EditScheduleLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly ScheduleItem scheduleItem;
        private LineItem[] lineItems;
        private readonly bool add;

        private const string DEPOSIT = "Deposit";
        private const string PAYMENT = "Payment";

        #endregion

        #region Constructor

        public EditScheduleLogic(MainWindowLogic _mainWindowLogic, ScheduleItem _scheduleItem, bool _add)
        {
            (mainWindowLogic, scheduleItem, add, lineItems) = (_mainWindowLogic, _scheduleItem, _add, _scheduleItem.LineItems);

            // Setup UI properties
            NextDate = scheduleItem.NextDate;
            EndDate = scheduleItem.EndDate;
            Frequency = EnumDescriptionAttribute.GetDescription(scheduleItem.Frequency);
            flags = scheduleItem.Flags;

            Account = scheduleItem.Account;
            Payee = scheduleItem.Payee;
            Memo = scheduleItem.Memo;
            UpdateAfterLineItemChanges();

            EditSplit = new CommandBase(OnEditSplit);
        }

        #endregion

        #region UI properties

        // Next date
        public DateTime NextDate { get; set; }

        // End date
        public DateTime EndDate { get; set; }

        // Frequency
        public string Frequency { get; set; }
        public string[] Frequencies = EnumDescriptionAttribute.GetDescriptions<EScheduleFrequency>();

        // Flags
        private EScheduleFlag flags;
        public bool? PromptBefore
        {
            get => flags.HasFlag(EScheduleFlag.PromptBefore);
            set => flags = value == true ? flags | EScheduleFlag.PromptBefore : flags & ~EScheduleFlag.PromptBefore;
        }

        public bool? NotifyAfter
        {
            get => flags.HasFlag(EScheduleFlag.NotifyAfter);
            set => flags = value == true ? flags | EScheduleFlag.NotifyAfter : flags & ~EScheduleFlag.NotifyAfter;
        }

        // Account name
        public string Account { get; set; }
        public IEnumerable<string> Accounts =>
            mainWindowLogic.Household.Account
            .Where(acc => !acc.Hidden)
            .Where(acc => acc.Type == Database.EAccountType.Bank || acc.Type == Database.EAccountType.Cash || acc.Type == Database.EAccountType.CreditCard)
            .Select(acc => acc.Name); 

        // Name of payee
        public string Payee { get; set; }

        // Memo
        public string Memo { get; set; }

        // Category (when not split)
        public string Category { get; set; }
        public bool? CategoryEnabled { get; private set; }
        public IEnumerable<CategoryItem> Categories => mainWindowLogic.CategoriesAndTransfers;

        // Type (when not split)
        public string[] TypeSource { get; } = new string[] { DEPOSIT, PAYMENT };
        public bool? TypeEnabled { get; private set; }
        public string Type { get; set; }

        // Amount (editable when not split)
        public decimal AbsoluteAmount { get; set; }
        public bool? AbsoluteAmountEnabled { get; private set; }

        // Split button
        public CommandBase EditSplit { get; }

        #endregion

        #region Result

        public ScheduleItem NewScheduleItem { get; private set; }

        #endregion

        #region Actions

        private void OnEditSplit()
        {
            var logic = new EditSplitLogic(mainWindowLogic, lineItems);
            if (mainWindowLogic.GuiServices.ShowDialog(logic))
            {
                // Get new line items from logic
                lineItems = logic.NewLineItems;

                // Update this dialog
                UpdateAfterLineItemChanges();
            }
        }

        private void UpdateAfterLineItemChanges()
        {
            if (lineItems.Length > 1)
            {
                Category = "<Split>";
                CategoryEnabled = false;
                TypeEnabled = false;
                AbsoluteAmountEnabled = false;
            }
            else
            {
                Category = lineItems[0].Category;
                CategoryEnabled = true;
                TypeEnabled = true;
                AbsoluteAmountEnabled = true;
            }

            decimal amount = lineItems.Sum(li => li.Amount);
            AbsoluteAmount = Math.Abs(amount);
            Type = amount > 0 ? DEPOSIT : PAYMENT;

            OnPropertyChanged(() => Category);
            OnPropertyChanged(() => CategoryEnabled);
            OnPropertyChanged(() => Type);
            OnPropertyChanged(() => TypeEnabled);
            OnPropertyChanged(() => AbsoluteAmount);
            OnPropertyChanged(() => AbsoluteAmountEnabled);
        }

        protected override bool? Commit()
        {
            // We require a payee name
            if (string.IsNullOrWhiteSpace(Payee))
            {
                mainWindowLogic.ErrorMessage("Payee name cannot be blank");
                return null;
            }

            var frequency = EnumDescriptionAttribute.MatchDescription<EScheduleFrequency>(Frequency);
            decimal amountFromAmountBox = AbsoluteAmount;
            if (Type == PAYMENT)
            {
                amountFromAmountBox *= -1;
            }

            bool change = add ||
                scheduleItem.NextDate != NextDate || scheduleItem.EndDate != EndDate ||
                scheduleItem.Frequency != frequency || scheduleItem.Flags != flags ||
                scheduleItem.Account != Account || scheduleItem.Payee != Payee ||
                scheduleItem.Memo != Memo || scheduleItem.LineItems.Length != lineItems.Length;

            if (!change)
            {
                if (lineItems.Length > 1)
                {
                    // Look to see if any of the line items has changed
                    for (int i = 0; i < lineItems.Length; i++)
                    {
                        if (scheduleItem.LineItems[i].Category != lineItems[i].Category ||
                            scheduleItem.LineItems[i].Memo != lineItems[i].Memo ||
                            scheduleItem.LineItems[i].Amount != lineItems[i].Amount)
                        {
                            change = true;
                            break;
                        }
                    }
                }
                else
                {
                    // non split case - see if anything is different
                    if (scheduleItem.LineItems[0].Category != Category ||
                        scheduleItem.LineItems[0].Amount != amountFromAmountBox)
                    {
                        change = true;
                    }
                }
            }

            if (change)
            {
                if (lineItems.Length == 1)
                {
                    if (string.IsNullOrWhiteSpace(Category))
                    {
                        lineItems[0] = new LineItem(mainWindowLogic, lineItems[0].ID, "", -1, -1, "", amountFromAmountBox, true);
                    }
                    else
                    {
                        var categoryItem = Categories.First(c => c.FullName == Category);
                        lineItems[0] = new LineItem(mainWindowLogic, lineItems[0].ID, Category, categoryItem.ID, categoryItem.AccountID, "", amountFromAmountBox, true);
                    }
                }

                NewScheduleItem = new ScheduleItem(
                    scheduleItem.ID,
                    NextDate,
                    EndDate,
                    frequency,
                    flags,
                    scheduleItem.TransactionID,
                    Account,
                    Payee,
                    Memo,
                    lineItems);
            }

            return change;
        }

        #endregion
    }
}
