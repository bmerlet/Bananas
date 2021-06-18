using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;

namespace BanaData.Logic
{
    public interface IGuiServices
    {
        // Show a dialog
        bool ShowDialog(LogicBase logic);

        // Execute async on UI thread
        void ExecuteAsync(Delegate method, params object[] args);

        // Exit the application
        void Exit();
    }
}
