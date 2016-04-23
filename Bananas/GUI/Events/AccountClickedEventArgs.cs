//
// Copyright 2016 Benoit J. Merlet
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bananas.Data;

namespace Bananas.GUI.Events
{
    public delegate void AccountClickedEventHandler(object sender, AccountClickedEventArgs e);

    public class AccountClickedEventArgs : EventArgs
    {
        private int accountID;

        public AccountClickedEventArgs(int accountID)
        {
            this.accountID = accountID;
        }

        public int AccountID
        {
            get { return accountID; }
        }
    }
}
