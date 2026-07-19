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
            this.groupBoxFolders = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxPath = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.labelSongsPath = new System.Windows.Forms.Label();
            this.textBoxSongsPath = new System.Windows.Forms.TextBox();
            this.buttonBrowseSongs = new System.Windows.Forms.Button();
            this.labelDaniPath = new System.Windows.Forms.Label();
            this.textBoxDaniPath = new System.Windows.Forms.TextBox();
            this.buttonBrowseDani = new System.Windows.Forms.Button();
            this.labelScoresPath = new System.Windows.Forms.Label();
            this.textBoxScoresPath = new System.Windows.Forms.TextBox();
            this.buttonBrowseScores = new System.Windows.Forms.Button();
            this.labelExcludedFolders = new System.Windows.Forms.Label();
            this.textBoxExcludedFolders = new System.Windows.Forms.TextBox();
            this.groupBoxMonitor = new System.Windows.Forms.GroupBox();
            this.labelPcName = new System.Windows.Forms.Label();
            this.textBoxPcName = new System.Windows.Forms.TextBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonUpdateCatalog = new System.Windows.Forms.Button();
            this.buttonBulkRegister = new System.Windows.Forms.Button();
            this.groupBoxDisplay = new System.Windows.Forms.GroupBox();
            this.checkBoxIndicator = new System.Windows.Forms.CheckBox();
            this.buttonSetIndicatorPosition = new System.Windows.Forms.Button();
            this.checkBoxStartup = new System.Windows.Forms.CheckBox();
            this.buttonOpenNotifySettings = new System.Windows.Forms.Button();
            this.groupBoxPack = new System.Windows.Forms.GroupBox();
            this.buttonAddPack = new System.Windows.Forms.Button();
            this.buttonDeletePack = new System.Windows.Forms.Button();
            this.listViewPacks = new System.Windows.Forms.ListView();
            this.columnHeaderPackName = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderPackDan = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderPackStatus = new System.Windows.Forms.ColumnHeader();
            this.groupBoxLog = new System.Windows.Forms.GroupBox();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPageMonitor = new System.Windows.Forms.TabPage();
            this.tabPageSettings = new System.Windows.Forms.TabPage();
            this.groupBoxFolders.SuspendLayout();
            this.groupBoxMonitor.SuspendLayout();
            this.groupBoxDisplay.SuspendLayout();
            this.groupBoxPack.SuspendLayout();
            this.groupBoxLog.SuspendLayout();
            this.tabControlMain.SuspendLayout();
            this.tabPageMonitor.SuspendLayout();
            this.tabPageSettings.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBoxFolders
            //
            this.groupBoxFolders.Controls.Add(this.label1);
            this.groupBoxFolders.Controls.Add(this.textBoxPath);
            this.groupBoxFolders.Controls.Add(this.buttonBrowse);
            this.groupBoxFolders.Controls.Add(this.labelSongsPath);
            this.groupBoxFolders.Controls.Add(this.textBoxSongsPath);
            this.groupBoxFolders.Controls.Add(this.buttonBrowseSongs);
            this.groupBoxFolders.Controls.Add(this.labelDaniPath);
            this.groupBoxFolders.Controls.Add(this.textBoxDaniPath);
            this.groupBoxFolders.Controls.Add(this.buttonBrowseDani);
            this.groupBoxFolders.Controls.Add(this.labelScoresPath);
            this.groupBoxFolders.Controls.Add(this.textBoxScoresPath);
            this.groupBoxFolders.Controls.Add(this.buttonBrowseScores);
            this.groupBoxFolders.Controls.Add(this.labelExcludedFolders);
            this.groupBoxFolders.Controls.Add(this.textBoxExcludedFolders);
            this.groupBoxFolders.Location = new System.Drawing.Point(12, 12);
            this.groupBoxFolders.Name = "groupBoxFolders";
            this.groupBoxFolders.Size = new System.Drawing.Size(776, 200);
            this.groupBoxFolders.TabIndex = 0;
            this.groupBoxFolders.TabStop = false;
            this.groupBoxFolders.Text = "フォルダ設定";
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Anasoko.exe :";
            //
            // textBoxPath
            //
            this.textBoxPath.Location = new System.Drawing.Point(200, 28);
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.ReadOnly = true;
            this.textBoxPath.Size = new System.Drawing.Size(476, 25);
            this.textBoxPath.TabIndex = 1;
            //
            // buttonBrowse
            //
            this.buttonBrowse.Location = new System.Drawing.Point(684, 26);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(80, 27);
            this.buttonBrowse.TabIndex = 2;
            this.buttonBrowse.Text = "参照...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            //
            // labelSongsPath
            //
            this.labelSongsPath.AutoSize = true;
            this.labelSongsPath.Location = new System.Drawing.Point(14, 63);
            this.labelSongsPath.Name = "labelSongsPath";
            this.labelSongsPath.Size = new System.Drawing.Size(103, 18);
            this.labelSongsPath.TabIndex = 3;
            this.labelSongsPath.Text = "Songsフォルダ :";
            //
            // textBoxSongsPath
            //
            this.textBoxSongsPath.Location = new System.Drawing.Point(200, 60);
            this.textBoxSongsPath.Name = "textBoxSongsPath";
            this.textBoxSongsPath.ReadOnly = true;
            this.textBoxSongsPath.Size = new System.Drawing.Size(476, 25);
            this.textBoxSongsPath.TabIndex = 4;
            this.textBoxSongsPath.TextChanged += new System.EventHandler(this.textBoxSongsPath_TextChanged);
            //
            // buttonBrowseSongs
            //
            this.buttonBrowseSongs.Location = new System.Drawing.Point(684, 58);
            this.buttonBrowseSongs.Name = "buttonBrowseSongs";
            this.buttonBrowseSongs.Size = new System.Drawing.Size(80, 27);
            this.buttonBrowseSongs.TabIndex = 5;
            this.buttonBrowseSongs.Text = "参照...";
            this.buttonBrowseSongs.UseVisualStyleBackColor = true;
            this.buttonBrowseSongs.Click += new System.EventHandler(this.buttonBrowseSongs_Click);
            //
            // labelDaniPath
            //
            this.labelDaniPath.AutoSize = true;
            this.labelDaniPath.Location = new System.Drawing.Point(14, 95);
            this.labelDaniPath.Name = "labelDaniPath";
            this.labelDaniPath.Size = new System.Drawing.Size(103, 18);
            this.labelDaniPath.TabIndex = 6;
            this.labelDaniPath.Text = "Daniフォルダ :";
            //
            // textBoxDaniPath
            //
            this.textBoxDaniPath.Location = new System.Drawing.Point(200, 92);
            this.textBoxDaniPath.Name = "textBoxDaniPath";
            this.textBoxDaniPath.ReadOnly = true;
            this.textBoxDaniPath.Size = new System.Drawing.Size(476, 25);
            this.textBoxDaniPath.TabIndex = 7;
            //
            // buttonBrowseDani
            //
            this.buttonBrowseDani.Location = new System.Drawing.Point(684, 90);
            this.buttonBrowseDani.Name = "buttonBrowseDani";
            this.buttonBrowseDani.Size = new System.Drawing.Size(80, 27);
            this.buttonBrowseDani.TabIndex = 8;
            this.buttonBrowseDani.Text = "参照...";
            this.buttonBrowseDani.UseVisualStyleBackColor = true;
            this.buttonBrowseDani.Click += new System.EventHandler(this.buttonBrowseDani_Click);
            //
            // labelScoresPath
            //
            this.labelScoresPath.AutoSize = true;
            this.labelScoresPath.Location = new System.Drawing.Point(14, 127);
            this.labelScoresPath.Name = "labelScoresPath";
            this.labelScoresPath.Size = new System.Drawing.Size(103, 18);
            this.labelScoresPath.TabIndex = 9;
            this.labelScoresPath.Text = "Scoresフォルダ :";
            //
            // textBoxScoresPath
            //
            this.textBoxScoresPath.Location = new System.Drawing.Point(200, 124);
            this.textBoxScoresPath.Name = "textBoxScoresPath";
            this.textBoxScoresPath.ReadOnly = true;
            this.textBoxScoresPath.Size = new System.Drawing.Size(476, 25);
            this.textBoxScoresPath.TabIndex = 10;
            //
            // buttonBrowseScores
            //
            this.buttonBrowseScores.Location = new System.Drawing.Point(684, 122);
            this.buttonBrowseScores.Name = "buttonBrowseScores";
            this.buttonBrowseScores.Size = new System.Drawing.Size(80, 27);
            this.buttonBrowseScores.TabIndex = 11;
            this.buttonBrowseScores.Text = "参照...";
            this.buttonBrowseScores.UseVisualStyleBackColor = true;
            this.buttonBrowseScores.Click += new System.EventHandler(this.buttonBrowseScores_Click);
            //
            // labelExcludedFolders
            //
            this.labelExcludedFolders.AutoSize = true;
            this.labelExcludedFolders.Location = new System.Drawing.Point(14, 165);
            this.labelExcludedFolders.Name = "labelExcludedFolders";
            this.labelExcludedFolders.Size = new System.Drawing.Size(180, 18);
            this.labelExcludedFolders.TabIndex = 12;
            this.labelExcludedFolders.Text = "除外カテゴリ（カンマ区切り）:";
            //
            // textBoxExcludedFolders
            //
            this.textBoxExcludedFolders.Location = new System.Drawing.Point(200, 162);
            this.textBoxExcludedFolders.Name = "textBoxExcludedFolders";
            this.textBoxExcludedFolders.Size = new System.Drawing.Size(564, 25);
            this.textBoxExcludedFolders.TabIndex = 13;
            this.textBoxExcludedFolders.TextChanged += new System.EventHandler(this.textBoxExcludedFolders_TextChanged);
            //
            // groupBoxMonitor
            //
            this.groupBoxMonitor.Controls.Add(this.labelPcName);
            this.groupBoxMonitor.Controls.Add(this.textBoxPcName);
            this.groupBoxMonitor.Controls.Add(this.labelStatus);
            this.groupBoxMonitor.Controls.Add(this.buttonStart);
            this.groupBoxMonitor.Controls.Add(this.buttonStop);
            this.groupBoxMonitor.Controls.Add(this.buttonUpdateCatalog);
            this.groupBoxMonitor.Controls.Add(this.buttonBulkRegister);
            this.groupBoxMonitor.Location = new System.Drawing.Point(12, 12);
            this.groupBoxMonitor.Name = "groupBoxMonitor";
            this.groupBoxMonitor.Size = new System.Drawing.Size(776, 166);
            this.groupBoxMonitor.TabIndex = 1;
            this.groupBoxMonitor.TabStop = false;
            this.groupBoxMonitor.Text = "モニター";
            //
            // labelPcName
            //
            this.labelPcName.AutoSize = true;
            this.labelPcName.Location = new System.Drawing.Point(14, 31);
            this.labelPcName.Name = "labelPcName";
            this.labelPcName.Size = new System.Drawing.Size(60, 18);
            this.labelPcName.TabIndex = 0;
            this.labelPcName.Text = "PC名 :";
            //
            // textBoxPcName
            //
            this.textBoxPcName.Location = new System.Drawing.Point(90, 28);
            this.textBoxPcName.Name = "textBoxPcName";
            this.textBoxPcName.Size = new System.Drawing.Size(280, 25);
            this.textBoxPcName.TabIndex = 1;
            this.textBoxPcName.TextChanged += new System.EventHandler(this.textBoxPcName_TextChanged);
            //
            // labelStatus
            //
            this.labelStatus.Font = new System.Drawing.Font("Yu Gothic UI", 14F, System.Drawing.FontStyle.Bold);
            this.labelStatus.ForeColor = System.Drawing.Color.Gray;
            this.labelStatus.Location = new System.Drawing.Point(440, 22);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(324, 36);
            this.labelStatus.TabIndex = 2;
            this.labelStatus.Text = "○ 停止中";
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // buttonStart
            //
            this.buttonStart.Location = new System.Drawing.Point(14, 66);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(160, 52);
            this.buttonStart.TabIndex = 3;
            this.buttonStart.Text = "モニター開始";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            //
            // buttonStop
            //
            this.buttonStop.Location = new System.Drawing.Point(184, 66);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(160, 52);
            this.buttonStop.TabIndex = 4;
            this.buttonStop.Text = "モニター終了";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            //
            // buttonUpdateCatalog
            //
            this.buttonUpdateCatalog.Location = new System.Drawing.Point(354, 66);
            this.buttonUpdateCatalog.Name = "buttonUpdateCatalog";
            this.buttonUpdateCatalog.Size = new System.Drawing.Size(190, 52);
            this.buttonUpdateCatalog.TabIndex = 5;
            this.buttonUpdateCatalog.Text = "曲名データベース更新";
            this.buttonUpdateCatalog.UseVisualStyleBackColor = true;
            this.buttonUpdateCatalog.Click += new System.EventHandler(this.buttonUpdateCatalog_Click);
            //
            // buttonBulkRegister
            //
            this.buttonBulkRegister.Location = new System.Drawing.Point(564, 66);
            this.buttonBulkRegister.Name = "buttonBulkRegister";
            this.buttonBulkRegister.Size = new System.Drawing.Size(200, 52);
            this.buttonBulkRegister.TabIndex = 6;
            this.buttonBulkRegister.Text = "スコア一括登録";
            this.buttonBulkRegister.UseVisualStyleBackColor = true;
            this.buttonBulkRegister.Click += new System.EventHandler(this.buttonBulkRegister_Click);
            //
            // groupBoxDisplay
            //
            this.groupBoxDisplay.Controls.Add(this.checkBoxIndicator);
            this.groupBoxDisplay.Controls.Add(this.buttonSetIndicatorPosition);
            this.groupBoxDisplay.Controls.Add(this.checkBoxStartup);
            this.groupBoxDisplay.Controls.Add(this.buttonOpenNotifySettings);
            this.groupBoxDisplay.Location = new System.Drawing.Point(12, 220);
            this.groupBoxDisplay.Name = "groupBoxDisplay";
            this.groupBoxDisplay.Size = new System.Drawing.Size(776, 100);
            this.groupBoxDisplay.TabIndex = 2;
            this.groupBoxDisplay.TabStop = false;
            this.groupBoxDisplay.Text = "表示・通知";
            //
            // checkBoxIndicator
            //
            this.checkBoxIndicator.AutoSize = true;
            this.checkBoxIndicator.Checked = true;
            this.checkBoxIndicator.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIndicator.Location = new System.Drawing.Point(14, 30);
            this.checkBoxIndicator.Name = "checkBoxIndicator";
            this.checkBoxIndicator.Size = new System.Drawing.Size(233, 22);
            this.checkBoxIndicator.TabIndex = 0;
            this.checkBoxIndicator.Text = "モニター中インジケーター表示";
            this.checkBoxIndicator.UseVisualStyleBackColor = true;
            this.checkBoxIndicator.CheckedChanged += new System.EventHandler(this.checkBoxIndicator_CheckedChanged);
            //
            // buttonSetIndicatorPosition
            //
            this.buttonSetIndicatorPosition.Location = new System.Drawing.Point(290, 26);
            this.buttonSetIndicatorPosition.Name = "buttonSetIndicatorPosition";
            this.buttonSetIndicatorPosition.Size = new System.Drawing.Size(160, 30);
            this.buttonSetIndicatorPosition.TabIndex = 1;
            this.buttonSetIndicatorPosition.Text = "位置を指定";
            this.buttonSetIndicatorPosition.UseVisualStyleBackColor = true;
            this.buttonSetIndicatorPosition.Click += new System.EventHandler(this.buttonSetIndicatorPosition_Click);
            //
            // checkBoxStartup
            //
            this.checkBoxStartup.AutoSize = true;
            this.checkBoxStartup.Location = new System.Drawing.Point(14, 64);
            this.checkBoxStartup.Name = "checkBoxStartup";
            this.checkBoxStartup.Size = new System.Drawing.Size(219, 22);
            this.checkBoxStartup.TabIndex = 2;
            this.checkBoxStartup.Text = "Windows起動時に自動起動";
            this.checkBoxStartup.UseVisualStyleBackColor = true;
            this.checkBoxStartup.CheckedChanged += new System.EventHandler(this.checkBoxStartup_CheckedChanged);
            //
            // buttonOpenNotifySettings
            //
            this.buttonOpenNotifySettings.Location = new System.Drawing.Point(290, 60);
            this.buttonOpenNotifySettings.Name = "buttonOpenNotifySettings";
            this.buttonOpenNotifySettings.Size = new System.Drawing.Size(220, 30);
            this.buttonOpenNotifySettings.TabIndex = 3;
            this.buttonOpenNotifySettings.Text = "Discord通知設定を開く";
            this.buttonOpenNotifySettings.UseVisualStyleBackColor = true;
            this.buttonOpenNotifySettings.Click += new System.EventHandler(this.buttonOpenNotifySettings_Click);
            //
            // groupBoxPack
            //
            this.groupBoxPack.Controls.Add(this.buttonAddPack);
            this.groupBoxPack.Controls.Add(this.buttonDeletePack);
            this.groupBoxPack.Controls.Add(this.listViewPacks);
            this.groupBoxPack.Location = new System.Drawing.Point(12, 186);
            this.groupBoxPack.Name = "groupBoxPack";
            this.groupBoxPack.Size = new System.Drawing.Size(776, 198);
            this.groupBoxPack.TabIndex = 3;
            this.groupBoxPack.TabStop = false;
            this.groupBoxPack.Text = "楽曲解禁パック";
            //
            // buttonAddPack
            //
            this.buttonAddPack.Location = new System.Drawing.Point(14, 26);
            this.buttonAddPack.Name = "buttonAddPack";
            this.buttonAddPack.Size = new System.Drawing.Size(160, 30);
            this.buttonAddPack.TabIndex = 0;
            this.buttonAddPack.Text = "anskpack読み込み";
            this.buttonAddPack.UseVisualStyleBackColor = true;
            this.buttonAddPack.Click += new System.EventHandler(this.buttonAddPack_Click);
            //
            // buttonDeletePack
            //
            this.buttonDeletePack.Location = new System.Drawing.Point(184, 26);
            this.buttonDeletePack.Name = "buttonDeletePack";
            this.buttonDeletePack.Size = new System.Drawing.Size(140, 30);
            this.buttonDeletePack.TabIndex = 1;
            this.buttonDeletePack.Text = "パック削除";
            this.buttonDeletePack.UseVisualStyleBackColor = true;
            this.buttonDeletePack.Click += new System.EventHandler(this.buttonDeletePack_Click);
            //
            // listViewPacks
            //
            this.listViewPacks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderPackName,
            this.columnHeaderPackDan,
            this.columnHeaderPackStatus});
            this.listViewPacks.FullRowSelect = true;
            this.listViewPacks.Location = new System.Drawing.Point(14, 62);
            this.listViewPacks.Name = "listViewPacks";
            this.listViewPacks.Size = new System.Drawing.Size(750, 124);
            this.listViewPacks.TabIndex = 2;
            this.listViewPacks.UseCompatibleStateImageBehavior = false;
            this.listViewPacks.View = System.Windows.Forms.View.Details;
            //
            // columnHeaderPackName
            //
            this.columnHeaderPackName.Text = "パック名";
            this.columnHeaderPackName.Width = 250;
            //
            // columnHeaderPackDan
            //
            this.columnHeaderPackDan.Text = "対象段位";
            this.columnHeaderPackDan.Width = 320;
            //
            // columnHeaderPackStatus
            //
            this.columnHeaderPackStatus.Text = "状態";
            this.columnHeaderPackStatus.Width = 178;
            //
            // groupBoxLog
            //
            this.groupBoxLog.Controls.Add(this.listBoxLog);
            this.groupBoxLog.Location = new System.Drawing.Point(12, 392);
            this.groupBoxLog.Name = "groupBoxLog";
            this.groupBoxLog.Size = new System.Drawing.Size(776, 182);
            this.groupBoxLog.TabIndex = 4;
            this.groupBoxLog.TabStop = false;
            this.groupBoxLog.Text = "ログ";
            //
            // listBoxLog
            //
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.ItemHeight = 18;
            this.listBoxLog.Location = new System.Drawing.Point(14, 24);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(750, 148);
            this.listBoxLog.TabIndex = 0;
            //
            // tabPageMonitor
            //
            this.tabPageMonitor.Controls.Add(this.groupBoxMonitor);
            this.tabPageMonitor.Controls.Add(this.groupBoxPack);
            this.tabPageMonitor.Controls.Add(this.groupBoxLog);
            this.tabPageMonitor.Location = new System.Drawing.Point(4, 29);
            this.tabPageMonitor.Name = "tabPageMonitor";
            this.tabPageMonitor.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageMonitor.Size = new System.Drawing.Size(792, 601);
            this.tabPageMonitor.TabIndex = 0;
            this.tabPageMonitor.Text = "モニター";
            this.tabPageMonitor.UseVisualStyleBackColor = true;
            //
            // tabPageSettings
            //
            this.tabPageSettings.Controls.Add(this.groupBoxFolders);
            this.tabPageSettings.Controls.Add(this.groupBoxDisplay);
            this.tabPageSettings.Location = new System.Drawing.Point(4, 29);
            this.tabPageSettings.Name = "tabPageSettings";
            this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSettings.Size = new System.Drawing.Size(792, 601);
            this.tabPageSettings.TabIndex = 1;
            this.tabPageSettings.Text = "設定";
            this.tabPageSettings.UseVisualStyleBackColor = true;
            //
            // tabControlMain
            //
            this.tabControlMain.Controls.Add(this.tabPageMonitor);
            this.tabControlMain.Controls.Add(this.tabPageSettings);
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(800, 634);
            this.tabControlMain.TabIndex = 0;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 650);
            this.Controls.Add(this.tabControlMain);
            this.Name = "Form1";
            this.Text = "Anasoko Score Monitor";
            this.groupBoxFolders.ResumeLayout(false);
            this.groupBoxFolders.PerformLayout();
            this.groupBoxMonitor.ResumeLayout(false);
            this.groupBoxMonitor.PerformLayout();
            this.groupBoxDisplay.ResumeLayout(false);
            this.groupBoxDisplay.PerformLayout();
            this.groupBoxPack.ResumeLayout(false);
            this.groupBoxLog.ResumeLayout(false);
            this.tabControlMain.ResumeLayout(false);
            this.tabPageMonitor.ResumeLayout(false);
            this.tabPageSettings.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxFolders;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxPath;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelSongsPath;
        private System.Windows.Forms.TextBox textBoxSongsPath;
        private System.Windows.Forms.Button buttonBrowseSongs;
        private System.Windows.Forms.Label labelDaniPath;
        private System.Windows.Forms.TextBox textBoxDaniPath;
        private System.Windows.Forms.Button buttonBrowseDani;
        private System.Windows.Forms.Label labelScoresPath;
        private System.Windows.Forms.TextBox textBoxScoresPath;
        private System.Windows.Forms.Button buttonBrowseScores;
        private System.Windows.Forms.Label labelExcludedFolders;
        private System.Windows.Forms.TextBox textBoxExcludedFolders;
        private System.Windows.Forms.GroupBox groupBoxMonitor;
        private System.Windows.Forms.Label labelPcName;
        private System.Windows.Forms.TextBox textBoxPcName;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonUpdateCatalog;
        private System.Windows.Forms.Button buttonBulkRegister;
        private System.Windows.Forms.GroupBox groupBoxDisplay;
        private System.Windows.Forms.CheckBox checkBoxIndicator;
        private System.Windows.Forms.Button buttonSetIndicatorPosition;
        private System.Windows.Forms.CheckBox checkBoxStartup;
        private System.Windows.Forms.Button buttonOpenNotifySettings;
        private System.Windows.Forms.GroupBox groupBoxPack;
        private System.Windows.Forms.Button buttonAddPack;
        private System.Windows.Forms.Button buttonDeletePack;
        private System.Windows.Forms.ListView listViewPacks;
        private System.Windows.Forms.ColumnHeader columnHeaderPackName;
        private System.Windows.Forms.ColumnHeader columnHeaderPackDan;
        private System.Windows.Forms.ColumnHeader columnHeaderPackStatus;
        private System.Windows.Forms.GroupBox groupBoxLog;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPageMonitor;
        private System.Windows.Forms.TabPage tabPageSettings;
    }
}
