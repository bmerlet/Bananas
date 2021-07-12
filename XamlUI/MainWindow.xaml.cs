using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

using Toolbox.UILogic;
using BanaData.Logic;
using BanaData.Logic.Dialogs;
using BanaData.Logic.Main;
using XamlUI.Dialogs;

namespace XamlUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IGuiServices
    {
        // Logic for this window
        private readonly MainWindowLogic logic;

        public MainWindow()
        {
            // Initialize all components
            InitializeComponent();

            // Create main window logic
            logic = new MainWindowLogic(this);

            // Init subcomponents
            //accountGroup.Init(logic);

            // Set window to location specified by logic (if initialized)
            if (logic.Width > 40 && logic.Height > 40)
            {
                Left = logic.LeftX;
                Top = logic.TopY;
                Width = logic.Width;
                Height = logic.Height;
                //ZZZZ splitContainerMain.SplitterDistance = logic.SplitterX;
                //ZZZZ accountGroup.UpdateSize();
            }

            // Logic is the data context
            DataContext = logic;

            // Memorize size on resize
            SizeChanged += (o, e) =>
            {
                logic.Width = (int)Width;
                logic.Height = (int)Height;
            };

            // Memorize change of location
            LocationChanged += (o, e) =>
            {
                logic.LeftX = (int)Left;
                logic.TopY = (int)Top;
            };

            // Save settings and data on close
            Closed += (o, e) =>
            {
                logic.SaveUserSettings();
                logic.SaveIfDirty();
            };
        }

        #region Gui services implementation

        //
        // Show a dialog
        //
        public bool ShowDialog(LogicBase logic)
        {
            //
            // Windows-provided dialogs
            //
            if (logic is OpenFileLogic openFileLogic)
            {
                return ShowOpenFileDialog(openFileLogic);
            }
            if (logic is SaveFileLogic saveFileLogic)
            {
                return ShowSaveFileDialog(saveFileLogic);
            }
            if (logic is ErrorLogic errorLogic)
            {
                MessageBox.Show(errorLogic.Error, errorLogic.Title);
                return false;
            }
            if (logic is QuestionLogic questionLogic)
            {
                return MessageBox.Show(questionLogic.Question, "Question", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            }

            //
            // Our own dialogs
            //
            Window dialog;
            if (logic is EditAccountsLogic editAccountsLogic)
            {
                dialog = new EditAccounts(editAccountsLogic);
            }
            else if (logic is EditAccountLogic editAccountLogic)
            {
                dialog = new EditAccount(editAccountLogic);
            }
            else if (logic is AccountPickerLogic accountPickerLogic)
            {
                dialog = new AccountPicker(accountPickerLogic);
            }
            else if (logic is EditCategoriesLogic editCategoriesLogic)
            {
                dialog = new EditCategories(editCategoriesLogic);
            }
            else if (logic is EditCategoryLogic editCategoryLogic)
            {
                dialog = new EditCategory(editCategoryLogic);
            }
            else if (logic is EditMemorizedPayeesLogic editMemorizedPayeesLogic)
            {
                dialog = new EditMemorizedPayees(editMemorizedPayeesLogic);
            }
            else if (logic is EditMemorizedPayeeLogic editMemorizedPayeeLogic)
            {
                dialog = new EditMemorizedPayee(editMemorizedPayeeLogic);
            }
            else if (logic is EditPersonsLogic editPersonsLogic)
            {
                dialog = new EditPersons(editPersonsLogic);
            }
            else if (logic is EditSecuritiesLogic editSecuritiesLogic)
            {
                dialog = new EditSecurities(editSecuritiesLogic);
            }
            else if (logic is EditSecurityLogic editSecurityLogic)
            {
                dialog = new EditSecurity(editSecurityLogic);
            }
            else if (logic is EditSplitLogic editSplitLogic)
            {
                dialog = new EditSplit(editSplitLogic);
            }
            else if (logic is ReconcileInfoLogic reconcileInfoLogic)
            {
                dialog = new ReconcileInfo(reconcileInfoLogic);
            }
            else if (logic is ReconcileLogic reconcileLogic)
            {
                dialog = new Reconcile(reconcileLogic);
            }
            else if (logic is ReconcileInvestmentsLogic reconcileInvestmentsLogic)
            {
                dialog = new ReconcileInvestments(reconcileInvestmentsLogic);
            }
            else if (logic is ShowHoldingsLogic showHoldingsLogic)
            {
                dialog = new ShowHoldings(showHoldingsLogic);
            }
            else if (logic is ShowCapitalGainsLogic showCapitalGainsLogic)
            {
                dialog = new ShowCapitalGains(showCapitalGainsLogic);
            }
            else if (logic is ShowYearlyCapGainsAndDividendsLogic showYearlyCapGainsAndDividendsLogic)
            {
                dialog = new ShowYearlyCapGainsAndDividends(showYearlyCapGainsAndDividendsLogic);
            }
            else if (logic is ShowRebalanceLogic showRebalanceLogic)
            {
                dialog = new ShowRebalance(showRebalanceLogic);
            }
            else if (logic is PasswordPromptLogic passwordPromptLogic)
            {
                dialog = new PasswordPrompt(passwordPromptLogic);
            }
            else
            {
                throw new NotImplementedException();
            }

            // Set the owner
            dialog.Owner = this;

            bool change = dialog.ShowDialog() == true;

            // See comments in Harmony checker ScoreWindow.ShowDialog
            dialog.DataContext = null;

            return change;
        }

        // Show open file dialog
        private bool ShowOpenFileDialog(OpenFileLogic logic)
        {
            string dir = logic.InitialDirectory;
            if (!System.IO.Directory.Exists(dir))
            {
                dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            var ofd = new OpenFileDialog()
            {
                Filter = logic.Filter,
                FilterIndex = 1,
                InitialDirectory = dir,
                FileName = logic.File,
                Title = logic.Title,
                Multiselect = false
            };

            bool result = ofd.ShowDialog() == true;
            logic.File = result ? ofd.FileName : null;

            return result;
        }

        // Show save file dialog
        private bool ShowSaveFileDialog(SaveFileLogic logic)
        {
            string dir = logic.InitialDirectory;
            if (!System.IO.Directory.Exists(dir))
            {
                dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            var ofd = new SaveFileDialog()
            {
                Filter = logic.Filter,
                FilterIndex = 1,
                InitialDirectory = dir,
                FileName = logic.File,
                Title = logic.Title
            };

            bool result = ofd.ShowDialog() == true;
            logic.File = result ? ofd.FileName : null;

            return result;
        }

        public void ExecuteAsync(Delegate method, params object[] args)
        {
            Dispatcher.BeginInvoke(method, System.Windows.Threading.DispatcherPriority.ContextIdle, args);
        }


        public void Exit()
        {
            Application.Current.Shutdown();
        }

        #endregion
    }
}
