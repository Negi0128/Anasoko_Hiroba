namespace Anasoko_Hiroba
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxPath = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.labelPcName = new System.Windows.Forms.Label();
            this.textBoxPcName = new System.Windows.Forms.TextBox();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonUpdateCatalog = new System.Windows.Forms.Button();
            this.labelStatus = new System.Windows.Forms.Label();
            this.buttonBulkRegister = new System.Windows.Forms.Button();
            this.checkBoxIndicator = new System.Windows.Forms.CheckBox();
            this.checkBoxStartup = new System.Windows.Forms.CheckBox();
            this.buttonSetIndicatorPosition = new System.Windows.Forms.Button();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Anasoko.exe :";
            //
            // textBoxPath
            //
            this.textBoxPath.Location = new System.Drawing.Point(197, 10);
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.ReadOnly = true;
            this.textBoxPath.Size = new System.Drawing.Size(490, 25);
            this.textBoxPath.TabIndex = 1;
            //
            // buttonBrowse
            //
            this.buttonBrowse.Location = new System.Drawing.Point(697, 8);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(91, 27);
            this.buttonBrowse.TabIndex = 5;
            this.buttonBrowse.Text = "参照...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            //
            // labelPcName
            //
            this.labelPcName.AutoSize = true;
            this.labelPcName.Location = new System.Drawing.Point(12, 45);
            this.labelPcName.Name = "labelPcName";
            this.labelPcName.Size = new System.Drawing.Size(60, 18);
            this.labelPcName.TabIndex = 8;
            this.labelPcName.Text = "PC名 :";
            //
            // textBoxPcName
            //
            this.textBoxPcName.Location = new System.Drawing.Point(197, 42);
            this.textBoxPcName.Name = "textBoxPcName";
            this.textBoxPcName.Size = new System.Drawing.Size(300, 25);
            this.textBoxPcName.TabIndex = 9;
            this.textBoxPcName.TextChanged += new System.EventHandler(this.textBoxPcName_TextChanged);
            //
            // buttonStart
            //
            this.buttonStart.Location = new System.Drawing.Point(15, 82);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(142, 64);
            this.buttonStart.TabIndex = 2;
            this.buttonStart.Text = "モニター開始";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            //
            // buttonStop
            //
            this.buttonStop.Location = new System.Drawing.Point(175, 82);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(142, 64);
            this.buttonStop.TabIndex = 3;
            this.buttonStop.Text = "モニター終了";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            //
            // buttonUpdateCatalog
            //
            this.buttonUpdateCatalog.Location = new System.Drawing.Point(335, 82);
            this.buttonUpdateCatalog.Name = "buttonUpdateCatalog";
            this.buttonUpdateCatalog.Size = new System.Drawing.Size(142, 64);
            this.buttonUpdateCatalog.TabIndex = 6;
            this.buttonUpdateCatalog.Text = "曲名データベース更新";
            this.buttonUpdateCatalog.UseVisualStyleBackColor = true;
            this.buttonUpdateCatalog.Click += new System.EventHandler(this.buttonUpdateCatalog_Click);
            //
            // labelStatus
            //
            this.labelStatus.Font = new System.Drawing.Font("Yu Gothic UI", 14F, System.Drawing.FontStyle.Bold);
            this.labelStatus.ForeColor = System.Drawing.Color.Gray;
            this.labelStatus.Location = new System.Drawing.Point(495, 82);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(293, 64);
            this.labelStatus.TabIndex = 7;
            this.labelStatus.Text = "○ 停止中";
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // buttonBulkRegister
            //
            this.buttonBulkRegister.Location = new System.Drawing.Point(15, 152);
            this.buttonBulkRegister.Name = "buttonBulkRegister";
            this.buttonBulkRegister.Size = new System.Drawing.Size(200, 30);
            this.buttonBulkRegister.TabIndex = 10;
            this.buttonBulkRegister.Text = "スコア一括登録";
            this.buttonBulkRegister.UseVisualStyleBackColor = true;
            this.buttonBulkRegister.Click += new System.EventHandler(this.buttonBulkRegister_Click);
            //
            // checkBoxIndicator
            //
            this.checkBoxIndicator.AutoSize = true;
            this.checkBoxIndicator.Checked = true;
            this.checkBoxIndicator.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIndicator.Location = new System.Drawing.Point(230, 159);
            this.checkBoxIndicator.Name = "checkBoxIndicator";
            this.checkBoxIndicator.Size = new System.Drawing.Size(200, 22);
            this.checkBoxIndicator.TabIndex = 11;
            this.checkBoxIndicator.Text = "モニター中インジケーター表示";
            this.checkBoxIndicator.UseVisualStyleBackColor = true;
            this.checkBoxIndicator.CheckedChanged += new System.EventHandler(this.checkBoxIndicator_CheckedChanged);
            //
            // checkBoxStartup
            //
            this.checkBoxStartup.AutoSize = true;
            this.checkBoxStartup.Location = new System.Drawing.Point(440, 159);
            this.checkBoxStartup.Name = "checkBoxStartup";
            this.checkBoxStartup.Size = new System.Drawing.Size(200, 22);
            this.checkBoxStartup.TabIndex = 12;
            this.checkBoxStartup.Text = "Windows起動時に自動起動";
            this.checkBoxStartup.UseVisualStyleBackColor = true;
            this.checkBoxStartup.CheckedChanged += new System.EventHandler(this.checkBoxStartup_CheckedChanged);
            //
            // buttonSetIndicatorPosition
            //
            this.buttonSetIndicatorPosition.Location = new System.Drawing.Point(650, 155);
            this.buttonSetIndicatorPosition.Name = "buttonSetIndicatorPosition";
            this.buttonSetIndicatorPosition.Size = new System.Drawing.Size(138, 30);
            this.buttonSetIndicatorPosition.TabIndex = 13;
            this.buttonSetIndicatorPosition.Text = "位置を指定";
            this.buttonSetIndicatorPosition.UseVisualStyleBackColor = true;
            this.buttonSetIndicatorPosition.Click += new System.EventHandler(this.buttonSetIndicatorPosition_Click);
            //
            // listBoxLog
            //
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.ItemHeight = 18;
            this.listBoxLog.Location = new System.Drawing.Point(15, 191);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(773, 300);
            this.listBoxLog.TabIndex = 4;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 510);
            this.Controls.Add(this.buttonSetIndicatorPosition);
            this.Controls.Add(this.checkBoxStartup);
            this.Controls.Add(this.checkBoxIndicator);
            this.Controls.Add(this.buttonBulkRegister);
            this.Controls.Add(this.listBoxLog);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.buttonUpdateCatalog);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.textBoxPcName);
            this.Controls.Add(this.labelPcName);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.textBoxPath);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Anasoko Score Monitor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxPath;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelPcName;
        private System.Windows.Forms.TextBox textBoxPcName;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonUpdateCatalog;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Button buttonBulkRegister;
        private System.Windows.Forms.CheckBox checkBoxIndicator;
        private System.Windows.Forms.CheckBox checkBoxStartup;
        private System.Windows.Forms.Button buttonSetIndicatorPosition;
        private System.Windows.Forms.ListBox listBoxLog;
    }
}

