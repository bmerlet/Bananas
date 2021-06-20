using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolbox.UILogic;
using BanaData.Serializations;
using BanaData.Logic.Dialogs;
using BanaData.Logic.Items;

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
            Save = new CommandBase(OnSave);
            Exit = new CommandBase(OnExit);

            EditAccounts = new CommandBase(OnEditAccounts);
            EditCategories = new CommandBase(OnEditCategories);
            EditMemorizedPayees = new CommandBase(OnEditMemorizedPayees);

            Reconcile = new CommandBase(OnReconcile);
            Reconcile.SetCanExecute(false);

            Test = new CommandBase(OnTest);
        }

        #endregion

        #region File memu

        //
        // Open
        //
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

        //
        // Save
        //
        public CommandBase Save { get; }

        private void OnSave()
        {
            mainWindow.Save();
        }

        //
        // Exit
        //
        public CommandBase Exit { get; }

        private void OnExit()
        {
            mainWindow.GuiServices.Exit();
        }

        #endregion

        #region Edit menu

        //
        // Edit accounts
        //
        public CommandBase EditAccounts { get; }

        private void OnEditAccounts()
        {
            var logic = new EditAccountsLogic(mainWindow);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit categories
        //
        public CommandBase EditCategories { get; }

        private void OnEditCategories()
        {
            var logic = new EditCategoriesLogic(mainWindow);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        //
        // Edit memorized Payees
        //
        public CommandBase EditMemorizedPayees { get; }

        private void OnEditMemorizedPayees()
        {
            var logic = new EditMemorizedPayeesLogic(mainWindow);
            mainWindow.GuiServices.ShowDialog(logic);
        }

        #endregion

        #region Actions menu

        public CommandBase Reconcile { get; }

        private void OnReconcile()
        {
            // Retreive account info
            int accountID = mainWindow.DisplayedAccountID;

            // Retreive reconcile info ZZZZ TODO
            var reconcileInfo = new ReconcileInfoItem(accountID);

            var logic = new ReconcileInfoLogic(mainWindow, reconcileInfo);
            if (mainWindow.GuiServices.ShowDialog(logic))
            {
                var newReconcileInfo = logic.NewReconcileInfoItem;
                // ZZZZ
            }
        }

        #endregion

        #region View menu

        //
        // Show closed accounts toggle
        //
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
