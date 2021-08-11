using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Main;

namespace BanaData.Logic.Items
{
    /// <summary>
    /// Sealable class representing a line item for the UI
    /// </summary>
    public class LineItem
    {
        #region Private members

        private readonly MainWindowLogic mainWindowLogic;

        #endregion
        
        #region Constructors

        // Explicit constructor
        public LineItem(
            MainWindowLogic _mainWindowLogic,
            int _id, 
            string _category,
            int categoryID,
            int categoryAccountID,
            string _memo,
            decimal _amount,
            bool @sealed) =>
            (mainWindowLogic, id, category, CategoryID, CategoryAccountID, memo, amount, Sealed) =
            (_mainWindowLogic, _id, _category, categoryID, categoryAccountID, _memo, _amount, @sealed);

        // Lookup category ids and construct
        static public LineItem GetLineItem(MainWindowLogic _mainWindowLogic,
            int id,
            string category,
            string memo,
            decimal amount,
            bool @sealed)
        {
            (int catID, int catAccntId) = GetCategoryIds(_mainWindowLogic, category);

            return new LineItem(_mainWindowLogic, id, category, catID, catAccntId, memo, amount, @sealed);
        }

        // Construct from DB
        public LineItem(MainWindowLogic _mainWindowLogic, Household.LineItemRow lineItemRow, bool @sealed)
        {
            (int catID, int catAccntID, string category) = lineItemRow.GetCategory();
            string _memo = lineItemRow.IsMemoNull() ? "" : lineItemRow.Memo;

            (mainWindowLogic, id, Category, CategoryID, CategoryAccountID, memo, amount, Sealed) =
                (_mainWindowLogic, lineItemRow.ID, category, catID, catAccntID, _memo, lineItemRow.Amount, @sealed);
        }

        // Clone with a new ID
        public LineItem(LineItem src, int id)
            : this(src.mainWindowLogic, id, src.Category, src.CategoryID, src.CategoryAccountID, src.Memo, src.Amount, src.Sealed) { }

        // Clone
        public LineItem(LineItem src)
            : this(src, src.ID) { }

        static private (int catID, int catAccntId) GetCategoryIds(MainWindowLogic mainWindowLogic, string category)
        {
            int categoryID;
            int categoryAccountID;

            if (string.IsNullOrWhiteSpace(category))
            {
                categoryID = -1;
                categoryAccountID = -1;
            }
            else
            {
                var cat = mainWindowLogic.CategoriesAndTransfers.FirstOrDefault(c => c.FullName == category);
                if (cat == null)
                {
                    cat = mainWindowLogic.HiddenTransfers.FirstOrDefault(c => c.FullName == category);
                }
                if (cat == null)
                {
                    throw new ArgumentException($"Category {category} does not exist");
                }
                categoryID = cat.ID;
                categoryAccountID = cat.AccountID;
            }

            return (categoryID, categoryAccountID);
        }

        #endregion

        #region Logic properties

        // line item DB ID
        private int id;
        public int ID
        {
            get => id;
            set
            {
                if (id != value)
                {
                    if (Sealed)
                    {
                        throw new InvalidOperationException("Trying to set ID on a sealed LineItem");
                    }

                    id = value;
                }
            }
        }

        // Category ids dervide from category string
        public int CategoryID { get; private set; }
        public int CategoryAccountID { get; private set; }

        // If this instance is sealed
        public bool Sealed;

        #endregion

        #region UI properties

        //
        // Category
        //
        private string category;
        public string Category
        {
            get => category;
            set
            {
                if (category != value)
                {
                    if (Sealed)
                    {
                        throw new InvalidOperationException("Trying to set Category on a sealed LineItem");
                    }

                    var tmp = value.Trim();
                    (CategoryID, CategoryAccountID) = GetCategoryIds(mainWindowLogic, tmp);
                    category = tmp;
                }
            }
        }

        //
        // Memo
        //
        private string memo;
        public string Memo
        {
            get => memo;
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException("Trying to set Memo on a sealed LineItem");
                }
                memo = value;
            }
        }

        //
        // Amount
        //
        private decimal amount;
        public decimal Amount 
        {
            get => amount;
            set
            {
                if (Sealed)
                {
                    throw new InvalidOperationException("Trying to set Amount on a sealed LineItem");
                }
                amount = value;
            }
        }

        //
        // Amount derived values
        //
        public string AmountString => amount.ToString("N");
        public bool Payment => Amount < 0;
        public bool Deposit => Amount >= 0;
        public decimal AbsoluteAmount => Math.Abs(Amount);
        public string AbsoluteAmountString => Math.Abs(Amount).ToString("N");

        #endregion

        #region Overrides

        public override bool Equals(object obj)
        {
            return
                obj is LineItem li &&
                li.ID == ID &&
                li.Category == Category &&
                li.Memo == Memo &&
                li.Amount == Amount;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}
