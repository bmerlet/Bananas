using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.UILogic;

namespace BanaData.Logic.Dialogs.Basics
{
    public class ErrorLogic : LogicBase
    {
        public ErrorLogic(string error, string title) => (Error, Title) = (error, title);

        public string Error { get; }
        public string Title { get; }
    }

    public class QuestionLogic : LogicBase
    {
        public QuestionLogic(string question) => Question = question;

        public string Question { get; }
    }
}
