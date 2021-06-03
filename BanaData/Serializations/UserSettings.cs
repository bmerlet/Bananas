using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanaData.Serializations
{
    [Serializable]
    public class UserSettings
    {
        // Size and placement of main window on screen
        public int LeftX;
        public int TopY;
        public int Width;
        public int Height;

        // Position account summary width
        public int AccountWidth;

        // If showing closed accounts
        public bool HideClosedAccounts;

        // Last file opened
        public string LastFileOpened;

        // Column widths
        public double WidthOfDateColumn = 80;
        public double WidthOfMediumColumn = 80;
        public double WidthOfPayeeColumn = 140;
        public double WidthOfMemoColumn = 140;
        public double WidthOfCategoryColumn = 140;
        public double WidthOfPaymentColumn = 90;
        public double WidthOfStatusColumn = 40;
        public double WidthOfDepositColumn = 90;
        public double WidthOfBalanceColumn = 90;
    }
}
