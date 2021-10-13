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
                Left = logic.LeftX;
                Top = logic.TopY;
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
            if (logic is ListAccountsLogic listAccountsLogic)
            {
                dialog = new ListAccounts(listAccountsLogic);
            }
            else if (logic is EditAccountLogic editAccountLogic)
            {
                dialog = new EditAccount(editAccountLogic);
            }
            else if (logic is AccountPickerLogic accountPickerLogic)
            {
                dialog = new AccountPicker(accountPickerLogic);
            }
            else if (logic is AccountListPickerLogic accountListPickerLogic)
            {
                dialog = new AccountListPicker(accountListPickerLogic);
            }
            else if (logic is CategoryListPickerLogic categoryListPickerLogic)
            {
                dialog = new CategoryListPicker(categoryListPickerLogic);
            }
            else if (logic is PayeeListPickerLogic payeeListPickerLogic)
            {
                dialog = new PayeeListPicker(payeeListPickerLogic);
            }
            else if (logic is QIFExportPickerLogic qifExportPickerLogic)
            {
                dialog = new QIFExportPicker(qifExportPickerLogic);
            }
            else if (logic is ListCategoriesLogic listCategoriesLogic)
            {
                dialog = new ListCategories(listCategoriesLogic);
            }
            else if (logic is EditCategoryLogic editCategoryLogic)
            {
                dialog = new EditCategory(editCategoryLogic);
            }
            else if (logic is ListMemorizedPayeesLogic listMemorizedPayeesLogic)
            {
                dialog = new ListMemorizedPayees(listMemorizedPayeesLogic);
            }
            else if (logic is EditMemorizedPayeeLogic editMemorizedPayeeLogic)
            {
                dialog = new EditMemorizedPayee(editMemorizedPayeeLogic);
            }
            else if (logic is EditPersonsLogic editPersonsLogic)
            {
                dialog = new EditPersons(editPersonsLogic);
            }
            else if (logic is ListSchedulesLogic listSchedulesLogic)
            {
                dialog = new ListSchedules(listSchedulesLogic);
            }
            else if (logic is EditScheduleLogic editScheduleLogic)
            {
                dialog = new EditSchedule(editScheduleLogic);
            }
            else if (logic is ListSecuritiesLogic listSecuritiesLogic)
            {
                dialog = new ListSecurities(listSecuritiesLogic);
            }
            else if (logic is EditSecurityLogic editSecurityLogic)
            {
                dialog = new EditSecurity(editSecurityLogic);
            }
            else if (logic is ListSecurityPricesLogic listSecurityPricesLogic)
            {
                dialog = new ListSecurityPrices(listSecurityPricesLogic);
            }
            else if (logic is ListTransactionReportsLogic listTransactionReportsLogic)
            {
                dialog = new ListTransactionReports(listTransactionReportsLogic);
            }
            else if (logic is EditTransactionReportLogic editTransactionReportLogic)
            {
                dialog = new EditTransactionReport(editTransactionReportLogic);
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
            else if (logic is SearchResultLogic searchResultLogic)
            {
                dialog = new SearchResult(searchResultLogic);
            }
            else if (logic is ShowHoldingsLogic showHoldingsLogic)
            {
                dialog = new ShowHoldings(showHoldingsLogic);
            }
            else if (logic is ShowHoldingsPerPersonLogic showHoldingsPerPersonLogic)
            {
                dialog = new ShowHoldingsPerPerson(showHoldingsPerPersonLogic);
            }
            else if (logic is ShowCashFlowBetweenPersonsLogic showCashFlowBetweenPersonsLogic)
            {
                dialog = new ShowCashFlowBetweenPersons(showCashFlowBetweenPersonsLogic);
            }
            else if (logic is ShowWealthOverTimeLogic showWealthOverTimeLogic)
            {
                dialog = new ShowWealthOverTime(showWealthOverTimeLogic);
            }
            else if (logic is ShowCapitalGainsLogic showCapitalGainsLogic)
            {
                dialog = new ShowCapitalGains(showCapitalGainsLogic);
            }
            else if (logic is ShowYearlyCGDivIntLogic showYearlyCGDivIntLogic)
            {
                dialog = new ShowYearlyCGDivInt(showYearlyCGDivIntLogic);
            }
            else if (logic is ShowRebalanceLogic showRebalanceLogic)
            {
                dialog = new ShowRebalance(showRebalanceLogic);
            }
            else if (logic is ShowQuoteUpdateLogic showQuoteUpdateLogic)
            {
                dialog = new ShowQuoteUpdate(showQuoteUpdateLogic);
            }
            else if (logic is TransactionReportLogic transactionReportLogic)
            {
                dialog = new TransactionReport(transactionReportLogic);
            }
            else if (logic is PasswordPromptLogic passwordPromptLogic)
            {
                dialog = new PasswordPrompt(passwordPromptLogic);
            }
            else if (logic is BalanceSheetLogic balanceSheetLogic)
            {
                dialog = new BalanceSheet(balanceSheetLogic);
            }
            else if (logic is IncomeStatementLogic incomeStatementLogic)
            {
                dialog = new IncomeStatement(incomeStatementLogic);
            }
            else if (logic is JournalLogic journalLogic)
            {
                dialog = new Journal(journalLogic);
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
