
namespace WinformsUI.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lastToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.flowLayoutPanelAccountGroups = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanelBankAccounts = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanelInvestmentAccounts = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanelAssetAccounts = new System.Windows.Forms.TableLayoutPanel();
            this.accountAndBalanceBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.tableLayoutPanelNetWorth = new System.Windows.Forms.TableLayoutPanel();
            this.labelNetWorthText = new System.Windows.Forms.Label();
            this.labelNetWorthValue = new System.Windows.Forms.Label();
            this.menuStrip.SuspendLayout();
            this.tableLayoutPanelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.flowLayoutPanelAccountGroups.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.accountAndBalanceBindingSource)).BeginInit();
            this.tableLayoutPanelNetWorth.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(800, 24);
            this.menuStrip.TabIndex = 1;
            this.menuStrip.Text = "menuStrip";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.lastToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // lastToolStripMenuItem
            // 
            this.lastToolStripMenuItem.Name = "lastToolStripMenuItem";
            this.lastToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.lastToolStripMenuItem.Text = "&Last";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 1;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Controls.Add(this.splitContainerMain, 0, 0);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 24);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.RowCount = 2;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(800, 426);
            this.tableLayoutPanelMain.TabIndex = 2;
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(3, 3);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.flowLayoutPanelAccountGroups);
            this.splitContainerMain.Size = new System.Drawing.Size(794, 390);
            this.splitContainerMain.SplitterDistance = 234;
            this.splitContainerMain.TabIndex = 0;
            this.splitContainerMain.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.SplitContainerMain_SplitterMoved);
            // 
            // flowLayoutPanelAccountGroups
            // 
            this.flowLayoutPanelAccountGroups.AutoScroll = true;
            this.flowLayoutPanelAccountGroups.Controls.Add(this.tableLayoutPanelBankAccounts);
            this.flowLayoutPanelAccountGroups.Controls.Add(this.tableLayoutPanelInvestmentAccounts);
            this.flowLayoutPanelAccountGroups.Controls.Add(this.tableLayoutPanelAssetAccounts);
            this.flowLayoutPanelAccountGroups.Controls.Add(this.tableLayoutPanelNetWorth);
            this.flowLayoutPanelAccountGroups.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelAccountGroups.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelAccountGroups.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelAccountGroups.Name = "flowLayoutPanelAccountGroups";
            this.flowLayoutPanelAccountGroups.Size = new System.Drawing.Size(234, 390);
            this.flowLayoutPanelAccountGroups.TabIndex = 0;
            this.flowLayoutPanelAccountGroups.WrapContents = false;
            // 
            // tableLayoutPanelBankAccounts
            // 
            this.tableLayoutPanelBankAccounts.ColumnCount = 2;
            this.tableLayoutPanelBankAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelBankAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelBankAccounts.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanelBankAccounts.Name = "tableLayoutPanelBankAccounts";
            this.tableLayoutPanelBankAccounts.RowCount = 1;
            this.tableLayoutPanelBankAccounts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 103F));
            this.tableLayoutPanelBankAccounts.Size = new System.Drawing.Size(200, 25);
            this.tableLayoutPanelBankAccounts.TabIndex = 0;
            // 
            // tableLayoutPanelInvestmentAccounts
            // 
            this.tableLayoutPanelInvestmentAccounts.ColumnCount = 2;
            this.tableLayoutPanelInvestmentAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelInvestmentAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelInvestmentAccounts.Location = new System.Drawing.Point(3, 34);
            this.tableLayoutPanelInvestmentAccounts.Name = "tableLayoutPanelInvestmentAccounts";
            this.tableLayoutPanelInvestmentAccounts.RowCount = 1;
            this.tableLayoutPanelInvestmentAccounts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 103F));
            this.tableLayoutPanelInvestmentAccounts.Size = new System.Drawing.Size(200, 24);
            this.tableLayoutPanelInvestmentAccounts.TabIndex = 1;
            // 
            // tableLayoutPanelAssetAccounts
            // 
            this.tableLayoutPanelAssetAccounts.ColumnCount = 2;
            this.tableLayoutPanelAssetAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelAssetAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelAssetAccounts.Location = new System.Drawing.Point(3, 64);
            this.tableLayoutPanelAssetAccounts.Name = "tableLayoutPanelAssetAccounts";
            this.tableLayoutPanelAssetAccounts.RowCount = 1;
            this.tableLayoutPanelAssetAccounts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelAssetAccounts.Size = new System.Drawing.Size(200, 23);
            this.tableLayoutPanelAssetAccounts.TabIndex = 2;
            // 
            // tableLayoutPanelNetWorth
            // 
            this.tableLayoutPanelNetWorth.ColumnCount = 2;
            this.tableLayoutPanelNetWorth.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelNetWorth.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelNetWorth.Controls.Add(this.labelNetWorthValue, 1, 0);
            this.tableLayoutPanelNetWorth.Controls.Add(this.labelNetWorthText, 0, 0);
            this.tableLayoutPanelNetWorth.Location = new System.Drawing.Point(3, 93);
            this.tableLayoutPanelNetWorth.Name = "tableLayoutPanelNetWorth";
            this.tableLayoutPanelNetWorth.RowCount = 1;
            this.tableLayoutPanelNetWorth.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelNetWorth.Size = new System.Drawing.Size(200, 23);
            this.tableLayoutPanelNetWorth.TabIndex = 3;
            // 
            // labelNetWorthText
            // 
            this.labelNetWorthText.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelNetWorthText.AutoSize = true;
            this.labelNetWorthText.Location = new System.Drawing.Point(3, 5);
            this.labelNetWorthText.Name = "labelNetWorthText";
            this.labelNetWorthText.Size = new System.Drawing.Size(59, 13);
            this.labelNetWorthText.TabIndex = 0;
            this.labelNetWorthText.Text = "Net Worth:";
            this.labelNetWorthText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelNetWorthValue
            // 
            this.labelNetWorthValue.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelNetWorthValue.AutoSize = true;
            this.labelNetWorthValue.Location = new System.Drawing.Point(169, 5);
            this.labelNetWorthValue.Name = "labelNetWorthValue";
            this.labelNetWorthValue.Size = new System.Drawing.Size(28, 13);
            this.labelNetWorthValue.TabIndex = 1;
            this.labelNetWorthValue.Text = "0.00";
            this.labelNetWorthValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Controls.Add(this.menuStrip);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bananas";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.splitContainerMain.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.flowLayoutPanelAccountGroups.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.accountAndBalanceBindingSource)).EndInit();
            this.tableLayoutPanelNetWorth.ResumeLayout(false);
            this.tableLayoutPanelNetWorth.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lastToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.BindingSource accountAndBalanceBindingSource;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelAccountGroups;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelBankAccounts;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelInvestmentAccounts;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelAssetAccounts;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelNetWorth;
        private System.Windows.Forms.Label labelNetWorthText;
        private System.Windows.Forms.Label labelNetWorthValue;
    }
}