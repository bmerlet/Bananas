
namespace WinformsUI.UserControls
{
    partial class AccountGroup
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanelBankAccounts = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanelInvestmentAccounts = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanelAssetAccounts = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanelNetWorth = new System.Windows.Forms.TableLayoutPanel();
            this.labelNetWorthValue = new System.Windows.Forms.Label();
            this.labelNetWorthText = new System.Windows.Forms.Label();
            this.flowLayoutPanelAccountGroups = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanelNetWorth.SuspendLayout();
            this.flowLayoutPanelAccountGroups.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanelBankAccounts
            // 
            this.tableLayoutPanelBankAccounts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelBankAccounts.ColumnCount = 2;
            this.tableLayoutPanelBankAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelBankAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelBankAccounts.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanelBankAccounts.Name = "tableLayoutPanelBankAccounts";
            this.tableLayoutPanelBankAccounts.RowCount = 1;
            this.tableLayoutPanelBankAccounts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 103F));
            this.tableLayoutPanelBankAccounts.Size = new System.Drawing.Size(214, 25);
            this.tableLayoutPanelBankAccounts.TabIndex = 0;
            // 
            // tableLayoutPanelInvestmentAccounts
            // 
            this.tableLayoutPanelInvestmentAccounts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelInvestmentAccounts.ColumnCount = 2;
            this.tableLayoutPanelInvestmentAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelInvestmentAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelInvestmentAccounts.Location = new System.Drawing.Point(3, 34);
            this.tableLayoutPanelInvestmentAccounts.Name = "tableLayoutPanelInvestmentAccounts";
            this.tableLayoutPanelInvestmentAccounts.RowCount = 1;
            this.tableLayoutPanelInvestmentAccounts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 103F));
            this.tableLayoutPanelInvestmentAccounts.Size = new System.Drawing.Size(214, 24);
            this.tableLayoutPanelInvestmentAccounts.TabIndex = 1;
            // 
            // tableLayoutPanelAssetAccounts
            // 
            this.tableLayoutPanelAssetAccounts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelAssetAccounts.ColumnCount = 2;
            this.tableLayoutPanelAssetAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelAssetAccounts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanelAssetAccounts.Location = new System.Drawing.Point(3, 64);
            this.tableLayoutPanelAssetAccounts.Name = "tableLayoutPanelAssetAccounts";
            this.tableLayoutPanelAssetAccounts.RowCount = 1;
            this.tableLayoutPanelAssetAccounts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanelAssetAccounts.Size = new System.Drawing.Size(214, 23);
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
            this.tableLayoutPanelNetWorth.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanelNetWorth.Size = new System.Drawing.Size(214, 23);
            this.tableLayoutPanelNetWorth.TabIndex = 3;
            // 
            // labelNetWorthValue
            // 
            this.labelNetWorthValue.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.labelNetWorthValue.AutoSize = true;
            this.labelNetWorthValue.Location = new System.Drawing.Point(183, 5);
            this.labelNetWorthValue.Name = "labelNetWorthValue";
            this.labelNetWorthValue.Size = new System.Drawing.Size(28, 13);
            this.labelNetWorthValue.TabIndex = 1;
            this.labelNetWorthValue.Text = "0.00";
            this.labelNetWorthValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
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
            this.flowLayoutPanelAccountGroups.Size = new System.Drawing.Size(220, 256);
            this.flowLayoutPanelAccountGroups.TabIndex = 1;
            this.flowLayoutPanelAccountGroups.WrapContents = false;
            // 
            // AccountGroup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.flowLayoutPanelAccountGroups);
            this.Name = "AccountGroup";
            this.Size = new System.Drawing.Size(220, 256);
            this.tableLayoutPanelNetWorth.ResumeLayout(false);
            this.tableLayoutPanelNetWorth.PerformLayout();
            this.flowLayoutPanelAccountGroups.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelBankAccounts;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelInvestmentAccounts;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelAssetAccounts;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelNetWorth;
        private System.Windows.Forms.Label labelNetWorthValue;
        private System.Windows.Forms.Label labelNetWorthText;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelAccountGroups;
    }
}
