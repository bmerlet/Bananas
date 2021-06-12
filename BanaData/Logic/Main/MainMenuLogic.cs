using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Serializations;
using BanaData.Logic.Dialogs;


namespace BanaData.Logic.Main
{
    public class MainMenuLogic
    {
        #region Private members

        private readonly MainWindowLogic mainWindow;

        #endregion

        #region Constructor

        public MainMenuLogic(MainWindowLogic main)
        {
            this.mainWindow = main;

            Open = new CommandBase(OnOpen);
            EditMemorizedPayees = new CommandBase(OnEditMemorizedPayees);

            Test = new CommandBase(OnTest);
        }

        #endregion

        #region File memu

        public CommandBase Open { get; }

        private void OnOpen()
        {
            string ZZZfile = @"C:\Users\bmerlet\Documents\Lab\Projects\C#\Bananas\sgbjm.qif";
            OpenFileLogic logic = new OpenFileLogic(ZZZfile, "Banana files (*.ban)|*.ban|Quicken Interchange Format files (*.QIF)|*.QIF|Any file (*.*)|*.*");
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.OpenFile(logic.File);
            }
        }

        #endregion

        #region Edit menu

        //
        // Edit memorized Payees
        //
        public CommandBase EditMemorizedPayees { get; }

        private void OnEditMemorizedPayees()
        {
            var logic = new EditMemorizedPayeesLogic(mainWindow);
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                mainWindow.UpdateMemorizedPayees();
            }
        }

        #endregion

        #region View menu

        public bool ShowClosedAccounts
        {
            get => !mainWindow.UserSettings.HideClosedAccounts;
            set { mainWindow.UserSettings.HideClosedAccounts = !value; mainWindow.UpdateAll(); }
        }

        #endregion

        #region Test button

        public CommandBase Test { get; }

        private void OnTest()
        {
            var logic = new EditMemorizedPayeeLogic(mainWindow, mainWindow.MemorizedPayees[4], false);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        #endregion
    }
}
