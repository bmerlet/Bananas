using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    public class MainWindowLogic
    {
        #region Constructor

        //private Listener listener = new Listener();

        public MainWindowLogic(Action Exit)
        {
            InfoCommand = new UICommand(Info);
            RefreshCommand = new UICommand(Refresh);
            ExitCommand = new UICommand(Exit);
        }

        #endregion

        #region UI properties

        //
        // Commands
        //
        public UICommand InfoCommand { get; }
        public UICommand RefreshCommand { get; }
        public UICommand ExitCommand { get; }

        #endregion

        #region Actions

        private void Info()
        {
            var refreshManager = new RefreshManager();
            refreshManager.GetFinancialInstitutionInfo();
        }

        private void Refresh()
        {
            var refreshManager = new RefreshManager();
            refreshManager.Connect();
            // ZZZ
        }

        #endregion
    }
}
