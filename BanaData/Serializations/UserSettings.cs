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

        // Column widths (bank register)
        public double WidthOfDateColumn = 80;
        public double WidthOfMediumColumn = 80;
        public double WidthOfPayeeColumn = 140;
        public double WidthOfMemoColumn = 140;
        public double WidthOfCategoryColumn = 140;
        public double WidthOfPaymentColumn = 90;
        public double WidthOfStatusColumn = 40;
        public double WidthOfDepositColumn = 90;
        public double WidthOfBalanceColumn = 90;

        // Column widths (investment register)
        public double InvstWidthOfStatusColumn = 40;
        public double InvstWidthOfDateColumn = 80;
        public double InvstWidthOfTypeColumn = 80;
        public double InvstWidthOfPayeeColumn = 140;
        public double InvstWidthOfMemoColumn = 140;
        public double InvstWidthOfCategoryColumn = 140;
        public double InvstWidthOfPaymentColumn = 90;
        public double InvstWidthOfDepositColumn = 90;
        public double InvstWidthOfBalanceColumn = 90;
    }
}
