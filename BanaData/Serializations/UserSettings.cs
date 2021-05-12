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
    }
}
