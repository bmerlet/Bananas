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
    public class MainMenu
    {
        #region Private members

        private readonly MainWindow mainWindow;

        #endregion

        #region Constructor

        public MainMenu(MainWindow main)
        {
            this.mainWindow = main;

            Open = new CommandBase(OnOpen);
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
                var file = logic.File;
                if (file.EndsWith(".QIF", StringComparison.InvariantCultureIgnoreCase))
                {
                    Converter.ConvertFromQIF(file, mainWindow.Household);
                    mainWindow.UpdateAll();
                }
            }
        }

        #endregion
    }
}
