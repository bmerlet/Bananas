using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanaData.Database;
using BanaData.Logic.Items;
using Toolbox.UILogic;

namespace BanaData.Logic.Main
{
    public class InvestmentTransactionLogic : AbstractTransactionLogic
    {
        #region Private fields

        // Parent logics
        private readonly MainWindowLogic mainWindowLogic;
        private readonly InvestmentRegisterLogic investmentRegisterLogic;

        // Account this transaction is for
        private readonly int accountID;

        // Transaction data
        private readonly InvestmentTransactionData data;

        // Backup of data (taken at edit start)
        private InvestmentTransactionData backup;

        #endregion

        #region Constructors

        public InvestmentTransactionLogic(
            MainWindowLogic _mainWindowLogic,
            InvestmentRegisterLogic _investmentRegisterLogic,
            int _accountID,
            int transID,
            InvestmentTransactionData _data)
            : base(_mainWindowLogic, _accountID, transID, _data)
        {
            (mainWindowLogic, investmentRegisterLogic, accountID, TransID, data) =
                (_mainWindowLogic, _investmentRegisterLogic, _accountID, transID, _data);
        }

        public InvestmentTransactionLogic(
            MainWindowLogic _mainWindowLogic,
            InvestmentRegisterLogic _investmentRegisterLogic,
            int _accountID)
            : this(_mainWindowLogic, _investmentRegisterLogic, _accountID, -1,
                  new InvestmentTransactionData(DateTime.Today, "", ETransactionStatus.Pending, new LineItem[] { new LineItem(_mainWindowLogic, -1, "", -1, -1, "", 0, false) },
                    EInvestmentTransactionType.None, -1, 0, 0, 0)) { }

        #endregion

        #region UI Properties


        #endregion

        #region IEditable implementation

        public override void BeginEdit()
        {
            // throw new NotImplementedException();
        }

        public override void CancelEdit()
        {
            base.CancelEdit();

            // throw new NotImplementedException();
        }

        public override void EndEdit()
        {
            // throw new NotImplementedException();
        }

        #endregion

        #region Actions

        #endregion

        #region Supporting classes

        public class InvestmentTransactionData : AbstractTransactionLogic.BaseTransactionData
        {
            // Construct from scratch
            public InvestmentTransactionData(
                DateTime date,
                string payee,
                ETransactionStatus status,
                IEnumerable<LineItem> lineItems,
                EInvestmentTransactionType type,
                int securityID,
                decimal securityPrice,
                decimal securityQuantity,
                decimal commission)
                : base(date, payee, status, lineItems) =>
                (Type, SecurityID, SecurityPrice, SecurityQuantity, Commission) =
                    (type, securityID, securityPrice, securityQuantity, commission);

            // Clone
            public InvestmentTransactionData(InvestmentTransactionData src)
                : base(src) =>
                (Type, SecurityID, SecurityPrice, SecurityQuantity, Commission) =
                    (src.Type, src.SecurityID, src.SecurityPrice, src.SecurityQuantity, src.Commission);

            // Properties
            public EInvestmentTransactionType Type;
            public int SecurityID;
            public decimal SecurityPrice;
            public decimal SecurityQuantity;
            public decimal Commission;

            public override bool Equals(object obj)
            {
                return
                    obj is InvestmentTransactionData o &&
                    base.Equals(o) &&
                    o.Type == Type &&
                    o.SecurityID == SecurityID &&
                    o.SecurityPrice == SecurityPrice &&
                    o.SecurityQuantity == SecurityQuantity &&
                    o.Commission == Commission;
            }


            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        #endregion
    }
}
