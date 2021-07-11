using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.UILogic.Dialogs;

namespace BanaData.Logic.Dialogs
{
    public class PasswordPromptLogic : LogicDialogBase
    {
        public PasswordPromptLogic(string oldPassword, string title) =>
            (Password, Title) = (oldPassword, title);

        public string Title { get; }
        public string Password { get; set; }

        protected override bool? Commit()
        {
            return true;
        }
    }
}
