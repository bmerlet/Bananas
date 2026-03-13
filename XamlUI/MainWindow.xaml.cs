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
using BanaData.Logic.Dialogs.Basics;
using BanaData.Logic.Dialogs.Editors;
using BanaData.Logic.Dialogs.Listers;
using BanaData.Logic.Dialogs.Pickers;
using BanaData.Logic.Dialogs.Reports;
using XamlUI.Dialogs;
using XamlUI.Dialogs.Basics;
using XamlUI.Dialogs.Editors;
using XamlUI.Dialogs.Listers;
using XamlUI.Dialogs.Pickers;
using XamlUI.Dialogs.Reports;
using BanaData.Logic.Dialogs.Reports.Accounting;
using XamlUI.Dialogs.Reports.Accounting;

namespace XamlUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IGuiServices
    {
        #region Constructor

        public MainWindow()
        {
            // Initialize all components
            InitializeComponent();

            // Create main window logic
            var logic = new MainWindowLogic(this);

            // Set window to location specified by logic (if initialized)
            if (logic.Width > 40 && logic.Height > 40)
            {
                // For some unknown reason, leftX and topy got set to -32000 one day, making the main window invisible.
                // This corrects the issue, but the root cause remains unknown.
                Left = Math.Max(0, logic.LeftX);
                Top = Math.Max(0, logic.TopY);
                Width = logic.Width;
                Height = logic.Height;
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

        #endregion

        #region Gui services implementation

        // Map of dialog logic to dialog 
        private class DialogMap
        {
            public readonly Type logic;
            public readonly Type dialog;
            public DialogMap(Type logic, Type dialog)
            {
                this.logic = logic;
                this.dialog = dialog;
            }
        }

        static private DialogMap[] dialogTable = new DialogMap[] {
            new DialogMap(typeof(ListAccountsLogic), typeof(ListAccounts)),
            new DialogMap(typeof(EditAccountLogic), typeof(EditAccount)),
            new DialogMap(typeof(AccountPickerLogic), typeof(AccountPicker)),
            new DialogMap(typeof(CategoryListPickerLogic), typeof(CategoryListPicker)),
            new DialogMap(typeof(PayeeListPickerLogic), typeof(PayeeListPicker)),
            new DialogMap(typeof(QIFExportPickerLogic), typeof(QIFExportPicker)),
            new DialogMap(typeof(QIFImportPickerLogic), typeof(QIFImportPicker)),
            new DialogMap(typeof(ListCategoriesLogic), typeof(ListCategories)),
            new DialogMap(typeof(EditCategoryLogic), typeof(EditCategory)),
            new DialogMap(typeof(ListMemorizedPayeesLogic), typeof(ListMemorizedPayees)),
            new DialogMap(typeof(RenamePayeeLogic), typeof(RenamePayee)),
            new DialogMap(typeof(EditMemorizedPayeeLogic), typeof(EditMemorizedPayee)),
            new DialogMap(typeof(EditPersonsLogic), typeof(EditPersons)),
            new DialogMap(typeof(ListSchedulesLogic), typeof(ListSchedules)),
            new DialogMap(typeof(EditScheduleLogic), typeof(EditSchedule)),
            new DialogMap(typeof(ListSecuritiesLogic), typeof(ListSecurities)),
            new DialogMap(typeof(EditSecurityLogic), typeof(EditSecurity)),
            new DialogMap(typeof(ListSecurityPricesLogic), typeof(ListSecurityPrices)),
            new DialogMap(typeof(ListTransactionReportsLogic), typeof(ListTransactionReports)),
            new DialogMap(typeof(ListStatementAccountHintsLogic), typeof(ListStatementAccountHints)),
            new DialogMap(typeof(EditStatementAccountHintsLogic), typeof(EditStatementAccountHints)),
            new DialogMap(typeof(EditTransactionReportLogic), typeof(EditTransactionReport)),
            new DialogMap(typeof(EditSplitLogic), typeof(EditSplit)),
            new DialogMap(typeof(ReconcileInfoLogic), typeof(ReconcileInfo)),
            new DialogMap(typeof(ReconcileLogic), typeof(Reconcile)),
            new DialogMap(typeof(ReconcileInvestmentsLogic), typeof(ReconcileInvestments)),
            new DialogMap(typeof(SearchResultLogic), typeof(SearchResult)),
            new DialogMap(typeof(ShowHoldingsLogic), typeof(ShowHoldings)),
            new DialogMap(typeof(ShowHoldingsPerPersonLogic), typeof(ShowHoldingsPerPerson)),
            new DialogMap(typeof(ShowReturnsLogic), typeof(ShowReturns)),
            new DialogMap(typeof(ShowCashFlowBetweenPersonsLogic), typeof(ShowCashFlowBetweenPersons)),
            new DialogMap(typeof(ShowWealthOverTimeLogic), typeof(ShowWealthOverTime)),
            new DialogMap(typeof(ShowCapitalGainsLogic), typeof(ShowCapitalGains)),
            new DialogMap(typeof(ShowYearlyCGDivIntLogic), typeof(ShowYearlyCGDivInt)),
            new DialogMap(typeof(ShowRebalanceLogic), typeof(ShowRebalance)),
            new DialogMap(typeof(ShowQuoteUpdateLogic), typeof(ShowQuoteUpdate)),
            new DialogMap(typeof(TransactionReportLogic), typeof(TransactionReport)),
            new DialogMap(typeof(PasswordPromptLogic), typeof(PasswordPrompt)),
            new DialogMap(typeof(BalanceSheetLogic), typeof(BalanceSheet)),
            new DialogMap(typeof(IncomeStatementLogic), typeof(IncomeStatement)),
            new DialogMap(typeof(JournalLogic), typeof(Journal)),
            new DialogMap(typeof(EditImportedTransactionsLogic), typeof(EditImportedTransactions))
        };

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
            // Look for the dialog corresponding to the passed in logic in the dialog map
            Type logicType = logic.GetType();
            var dialogMap = dialogTable.FirstOrDefault(m => m.logic == logicType);
            if (dialogMap == null)
            {
                throw new NotImplementedException();
            }

            // Create an instance of the dialog, passing the logic as a parameter
            Window dialog = Activator.CreateInstance(dialogMap.dialog, new Object[] { logic }) as Window;

            // Set the owner
            dialog.Owner = this;

            // Show the dialog
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

        public void SetCursor(bool wait)
        {
            Dispatcher.Invoke(() => Mouse.OverrideCursor = wait ? Cursors.Wait : Cursors.Arrow);
        }

        public void KaChing()
        {
            Sound.Play();
        }

        public void Exit()
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Sound support

        static private class Sound
        {
            static private readonly System.Media.SoundPlayer soundPlayer;
            static Sound()
            {
                // Embedded as "Resource" doesn't seem to work
                //var sri = Application.GetResourceStream(new Uri("pack://application:,,,.Sounds.kaching.wav", UriKind.RelativeOrAbsolute));
                soundPlayer = new System.Media.SoundPlayer("Sounds/kaching.wav");
            }

            static public void Play()
            {
                soundPlayer.Play();
            }
        }

        #endregion
    }
}
