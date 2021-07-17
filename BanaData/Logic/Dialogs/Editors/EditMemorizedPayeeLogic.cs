using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using Toolbox.UILogic.Dialogs;
using BanaData.Logic.Main;
using BanaData.Logic.Items;

namespace BanaData.Logic.Dialogs.Editors
{
    /// <summary>
    /// Logic to edit a specific memorized payees
    /// </summary>
    public class EditMemorizedPayeeLogic : LogicDialogBase
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;
        private readonly MemorizedPayeeItem item;
        private LineItem[] lineItems;
        private readonly bool add;

        private const string DEPOSIT = "Deposit";
        private const string PAYMENT = "Payment";

        #endregion

        #region Constructor

        public EditMemorizedPayeeLogic(MainWindowLogic _mainWindowLogic, MemorizedPayeeItem _item, bool _add)
        {
            (mainWindowLogic, item, add, lineItems) = (_mainWindowLogic, _item, _add, _item.LineItems);

            // Setup UI properties
            EditSplit = new CommandBase(OnEditSplit);
            Payee = item.Payee;
            Memo = item.Memo;
            UpdateAfterLineItemChanges();
        }

        #endregion

        #region UI properties

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

        // Amount (when not split)
        public decimal AbsoluteAmount { get; set; }
        public bool? AbsoluteAmountEnabled { get; private set; }

        // Split button
        public CommandBase EditSplit { get; }

        #endregion

        #region Result

        public MemorizedPayeeItem NewMemorizedPayeeItem => BuildNewItem();

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

            bool change
                = add || item.Payee != Payee || item.Memo != Memo || item.LineItems.Length != lineItems.Length;

            if (!change)
            {
                if (lineItems.Length > 1)
                {
                    // Look to see if any of the line items has changed
                    for (int i = 0; i < lineItems.Length; i++)
                    {
                        if (item.LineItems[i].Category != lineItems[i].Category ||
                            item.LineItems[i].Memo != lineItems[i].Memo ||
                            item.LineItems[i].Amount != lineItems[i].Amount)
                        {
                            change = true;
                            break;
                        }
                    }
                }
                else
                {
                    // non split case - see if anything is different
                    if (item.Category != Category ||
                        item.Amount != GetAmountFromControls())
                    {
                        change = true;
                    }
                }
            }

            return change;
        }

        private decimal GetAmountFromControls()
        {
            decimal amount = AbsoluteAmount;
            if (Type == PAYMENT)
            {
                amount *= -1;
            }
            return amount;
        }

        private MemorizedPayeeItem BuildNewItem()
        {
            if (lineItems.Length == 1)
            {
                if (string.IsNullOrWhiteSpace(Category))
                {
                    lineItems[0] = new LineItem(mainWindowLogic, lineItems[0].ID, "", -1, -1, "", GetAmountFromControls(), true);
                }
                else
                {
                    var categoryItem = Categories.First(c => c.FullName == Category);
                    lineItems[0] = new LineItem(mainWindowLogic, lineItems[0].ID, Category, categoryItem.ID, categoryItem.AccountID, "", GetAmountFromControls(), true);
                }
            }

            var result = new MemorizedPayeeItem(item.ID, Payee, Memo, lineItems);

            return result;
        }

        #endregion
    }
}
