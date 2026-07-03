using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AnasCore; // AnasCore.dll の読み込み

namespace Anasoko_Hiroba
{
    public partial class Form1 : Form
    {
        private FileSystemWatcher watcher;
        // exe のフォルダを基準にした絶対パスにする（起動時のカレントディレクトリに依存させない）
        private string dbPath = Path.Combine(Application.StartupPath, "score_data.db");

        // 同じファイルに対する短時間の重複イベントを無視するための記録
        private readonly Dictionary<string, DateTime> lastProcessedTime = new Dictionary<string, DateTime>();
        private readonly object lastProcessedLock = new object();

        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string[] CourseNames = { "簡単", "普通", "難しい", "おに", "裏" };
        private static readonly string[] ScoreFileNames = { "easy.bin", "normal.bin", "hard.bin", "oni.bin", "ura.bin" };

        private IndicatorForm indicatorForm;

        public Form1()
        {
            InitializeComponent();
            // 起動直後はモニター終了ボタンを無効化しておく
            buttonStop.Enabled = false;

            EnsureDatabase();

            // 前回選択した Anasoko.exe のパスを復元する
            string savedPath = Properties.Settings.Default.AnasokoExePath;
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                textBoxPath.Text = savedPath;
            }

            // PC名を復元する（未設定ならこのPCのコンピューター名を初期値にする）
            string savedPcName = Properties.Settings.Default.PcName;
            textBoxPcName.Text = string.IsNullOrEmpty(savedPcName) ? Environment.MachineName : savedPcName;

            // インジケーター表示設定を復元する
            checkBoxIndicator.Checked = Properties.Settings.Default.ShowIndicator;

            this.Load += Form1_Load;
        }

        // 「PC名」欄が変更されたら保存する
        private void textBoxPcName_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.PcName = textBoxPcName.Text;
            Properties.Settings.Default.Save();
        }

        // インジケーター表示のON/OFFが変更されたら保存し、モニター中なら即座に反映する
        private void checkBoxIndicator_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShowIndicator = checkBoxIndicator.Checked;
            Properties.Settings.Default.Save();

            if (watcher != null)
            {
                if (checkBoxIndicator.Checked) ShowIndicator();
                else HideIndicator();
            }
        }

        private void ShowIndicator()
        {
            if (indicatorForm == null || indicatorForm.IsDisposed)
            {
                indicatorForm = new IndicatorForm();
            }
            indicatorForm.Show();
        }

        private void HideIndicator()
        {
            if (indicatorForm != null && !indicatorForm.IsDisposed)
            {
                indicatorForm.Hide();
            }
        }

        // フォーム表示時、既に Anasoko.exe のパスがあれば曲名データベースの更新とモニター開始を自動で行う
        private async void Form1_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxPath.Text) && File.Exists(textBoxPath.Text))
            {
                await RunCatalogUpdateAsync(interactive: false);
                StartMonitoring(interactive: false);
            }
        }

        // score_data.db（曲名カタログ専用。スコア本体は Supabase に直接保存する）
        private void EnsureDatabase()
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // 曲ハッシュ→曲名の対応表
                string catalogSql = @"CREATE TABLE IF NOT EXISTS song_catalog (
                    hash TEXT PRIMARY KEY,
                    title TEXT NOT NULL,
                    updated_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
                );";
                using (var cmd = new SQLiteCommand(catalogSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                AddColumnIfMissing(conn, "song_catalog", "genre", "TEXT");
            }
        }

        private void AddColumnIfMissing(SQLiteConnection conn, string tableName, string columnName, string columnDef)
        {
            try
            {
                using (var cmd = new SQLiteCommand($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDef};", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SQLiteException)
            {
                // 既に列が存在する場合はここに来るので無視する
            }
        }

        // 「参照...」ボタンが押されたときの処理
        private async void buttonBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Anasoko.exe を選択してください";
                dialog.Filter = "Anasoko.exe|Anasoko.exe|実行ファイル (*.exe)|*.exe";
                dialog.CheckFileExists = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxPath.Text = dialog.FileName;

                    // 次回起動時のために選択したパスを保存する
                    Properties.Settings.Default.AnasokoExePath = dialog.FileName;
                    Properties.Settings.Default.Save();

                    // 選択した直後に曲名データベースの更新とモニター開始を自動で行う
                    await RunCatalogUpdateAsync(interactive: false);
                    StartMonitoring(interactive: false);
                }
            }
        }

        // 「モニター開始」ボタンが押されたときの処理
        private void buttonStart_Click(object sender, EventArgs e)
        {
            StartMonitoring(interactive: true);
        }

        // モニターを開始する（ボタンクリック・自動開始の両方から呼ばれる）
        private void StartMonitoring(bool interactive)
        {
            if (watcher != null) return; // 既にモニター中

            string exePath = textBoxPath.Text;

            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                ReportProblem(interactive, "「参照...」ボタンから Anasoko.exe を選択してください。");
                return;
            }

            // 1. Anasoko.exe のフォルダから、スコアデータフォルダを逆算する
            string exeDir = Path.GetDirectoryName(exePath);
            string scoreFolder = Path.Combine(exeDir, "Data", "Scores");

            if (!Directory.Exists(scoreFolder))
            {
                ReportProblem(interactive, "スコアデータフォルダが見つかりません: " + scoreFolder);
                return;
            }

            // フォルダモニター機能（FileSystemWatcher）の初期設定
            watcher = new FileSystemWatcher();
            watcher.Path = scoreFolder;
            watcher.IncludeSubdirectories = true; // プロフィールGUIDフォルダの下まで再帰的にモニターする
            watcher.Filter = "*.*"; // 全てのファイルをモニター対象とする
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

            // ファイル作成・更新時に実行するイベントを登録
            watcher.Created += OnFileUpdated;
            watcher.Changed += OnFileUpdated;

            watcher.EnableRaisingEvents = true;

            // ボタンの有効/無効を切り替え
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
            buttonBrowse.Enabled = false;

            labelStatus.Text = "● モニター中";
            labelStatus.ForeColor = System.Drawing.Color.Green;

            if (checkBoxIndicator.Checked) ShowIndicator();

            LogMessage("モニターを開始しました: " + scoreFolder);
        }

        // 「モニター終了」ボタンが押されたときの処理
        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }

            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            buttonBrowse.Enabled = true;

            labelStatus.Text = "○ 停止中";
            labelStatus.ForeColor = System.Drawing.Color.Gray;

            HideIndicator();

            LogMessage("モニターを終了しました。");
        }

        // 「曲名データベース更新」ボタンが押されたときの処理
        private async void buttonUpdateCatalog_Click(object sender, EventArgs e)
        {
            await RunCatalogUpdateAsync(interactive: true);
        }

        // 「スコア一括登録」ボタンが押されたときの処理
        // 保存されている全ての .bin ファイルを走査し、自己ベストを更新している分だけ Supabase へ登録する
        private async void buttonBulkRegister_Click(object sender, EventArgs e)
        {
            string exePath = textBoxPath.Text;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show("「参照...」ボタンから Anasoko.exe を選択してください。");
                return;
            }

            string exeDir = Path.GetDirectoryName(exePath);
            string scoreFolder = Path.Combine(exeDir, "Data", "Scores");
            if (!Directory.Exists(scoreFolder))
            {
                MessageBox.Show("スコアデータフォルダが見つかりません: " + scoreFolder);
                return;
            }

            var confirm = MessageBox.Show(
                "保存されている全スコアデータを走査し、自己ベストを更新している分だけ一括登録します。\n件数によっては時間がかかります。続行しますか？",
                "確認", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;

            buttonBulkRegister.Enabled = false;
            LogMessage("スコアの一括登録を開始します: " + scoreFolder);

            string pcName = string.IsNullOrEmpty(textBoxPcName.Text) ? Environment.MachineName : textBoxPcName.Text;

            try
            {
                var (processed, registered) = await Task.Run(() =>
                {
                    var targetFiles = new List<string>();
                    foreach (var name in ScoreFileNames)
                    {
                        targetFiles.AddRange(Directory.GetFiles(scoreFolder, name, SearchOption.AllDirectories));
                    }

                    int registeredCount = 0;
                    foreach (var file in targetFiles)
                    {
                        if (ProcessScoreFile(file, pcName, silent: true)) registeredCount++;
                    }
                    return (targetFiles.Count, registeredCount);
                });

                LogMessage($"スコアの一括登録が完了しました（{processed}件中 {registered}件を新規登録）");
            }
            catch (Exception ex)
            {
                LogMessage("一括登録に失敗しました: " + ex.Message);
            }
            finally
            {
                buttonBulkRegister.Enabled = true;
            }
        }

        // 曲名データベースを更新する（ボタンクリック・自動更新の両方から呼ばれる）
        private async Task RunCatalogUpdateAsync(bool interactive)
        {
            string exePath = textBoxPath.Text;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                ReportProblem(interactive, "「参照...」ボタンから Anasoko.exe を選択してください。");
                return;
            }

            string songsPath = GetSongsPathFromConfig(exePath);
            if (string.IsNullOrEmpty(songsPath) || !Directory.Exists(songsPath))
            {
                ReportProblem(interactive, "Songs フォルダが見つかりません（Config.json の songPath を確認してください）: " + songsPath);
                return;
            }

            buttonUpdateCatalog.Enabled = false;
            LogMessage("曲名データベースの更新を開始します: " + songsPath);

            try
            {
                string exeDir = Path.GetDirectoryName(exePath);
                var (processed, matched) = await Task.Run(() => BuildSongCatalog(exeDir, songsPath));
                LogMessage($"曲名データベースの更新が完了しました（譜面 {processed} 件中 {matched} 件を登録）");
            }
            catch (Exception ex)
            {
                LogMessage("曲名データベースの更新に失敗しました: " + ex.Message);
            }
            finally
            {
                buttonUpdateCatalog.Enabled = true;
            }
        }

        // interactive=true ならメッセージボックスで、false ならログのみで問題を知らせる
        private void ReportProblem(bool interactive, string message)
        {
            if (interactive)
            {
                MessageBox.Show(message);
            }
            else
            {
                LogMessage(message);
            }
        }

        // Anasoko.exe と同じフォルダの Data\Setting\Config.json から songPath を取得する
        private string GetSongsPathFromConfig(string exePath)
        {
            string exeDir = Path.GetDirectoryName(exePath);
            string configPath = Path.Combine(exeDir, "Data", "Setting", "Config.json");
            if (!File.Exists(configPath))
            {
                return null;
            }

            string json = File.ReadAllText(configPath);
            var serializer = new JavaScriptSerializer();
            var config = serializer.Deserialize<Dictionary<string, object>>(json);
            return config.TryGetValue("songPath", out object songPathObj) ? songPathObj as string : null;
        }

        // Songs フォルダ内の全 .tja を走査し、曲名から AnasCore と同じ方式でハッシュを計算して
        // song_catalog テーブルに登録する（バックグラウンドスレッドで実行される）
        private (int processed, int matched) BuildSongCatalog(string exeDir, string songsPath)
        {
            string buildPath = exeDir.EndsWith("\\") ? exeDir : exeDir + "\\";
            var sjis = Encoding.GetEncoding(932);
            var tjaFiles = Directory.GetFiles(songsPath, "*.tja", SearchOption.AllDirectories);

            // Songs直下の各カテゴリフォルダの box.def からジャンル名を読み取る
            var folderGenres = new Dictionary<string, string>();
            foreach (var categoryDir in Directory.GetDirectories(songsPath))
            {
                string boxDefPath = Path.Combine(categoryDir, "box.def");
                if (!File.Exists(boxDefPath)) continue;

                foreach (var line in File.ReadAllLines(boxDefPath, sjis))
                {
                    if (line.StartsWith("#GENRE:"))
                    {
                        folderGenres[Path.GetFileName(categoryDir)] = line.Substring(7).Trim();
                        break;
                    }
                }
            }

            var hashToInfo = new Dictionary<string, (string title, string genre)>();
            foreach (var tjaFile in tjaFiles)
            {
                string title = null;
                string subTitle = "";

                using (var reader = new StreamReader(tjaFile, sjis))
                {
                    string line;
                    int lineCount = 0;
                    while (lineCount < 20 && (line = reader.ReadLine()) != null)
                    {
                        lineCount++;
                        if (line.StartsWith("TITLE:")) title = line.Substring(6).Trim();
                        else if (line.StartsWith("SUBTITLE:")) subTitle = line.Substring(9).Trim();
                        if (title != null && line.StartsWith("COURSE")) break;
                    }
                }

                if (string.IsNullOrEmpty(title)) continue;

                // Songsフォルダ直下のカテゴリフォルダ名から、box.defで定義されたジャンル名を求める
                string relativePath = tjaFile.Substring(songsPath.Length).TrimStart('\\', '/');
                string categoryFolderName = relativePath.Split('\\', '/')[0];
                string genre = folderGenres.TryGetValue(categoryFolderName, out string g) ? g : categoryFolderName;

                // 表示用タイトルを組み立てる（"--"/"++" は表示スタイルを示すプレフィックスなので取り除く）
                string subTitleDisplay = subTitle;
                if (subTitleDisplay.StartsWith("--") || subTitleDisplay.StartsWith("++"))
                {
                    subTitleDisplay = subTitleDisplay.Substring(2).Trim();
                }
                string displayTitle = string.IsNullOrEmpty(subTitleDisplay) ? title : $"{title} / {subTitleDisplay}";

                try
                {
                    // ※ ハッシュの計算には元の title/subTitle（プレフィックス込み）をそのまま使う必要がある
                    var record = new Record();
                    record.LoadInfoToData(buildPath, title, subTitle, "x", 0);
                    string[] parts = record.Key.path.Split('\\');
                    if (parts.Length < 6) continue;
                    string hash = parts[5];

                    if (!hashToInfo.ContainsKey(hash))
                    {
                        hashToInfo[hash] = (displayTitle, genre);
                    }
                }
                catch
                {
                    // このファイルは変換できなかったのでスキップする
                }
            }

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    // title は毎回最新の内容に更新するが、genre は既に登録済みなら上書きしない（早い者勝ち）
                    string sql = @"INSERT INTO song_catalog (hash, title, genre, updated_at)
                        VALUES (@hash, @title, @genre, datetime('now', 'localtime'))
                        ON CONFLICT(hash) DO UPDATE SET
                            title = excluded.title,
                            genre = COALESCE(song_catalog.genre, excluded.genre),
                            updated_at = excluded.updated_at";
                    foreach (var kv in hashToInfo)
                    {
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@hash", kv.Key);
                            cmd.Parameters.AddWithValue("@title", kv.Value.title);
                            cmd.Parameters.AddWithValue("@genre", kv.Value.genre);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }

            return (tjaFiles.Length, hashToInfo.Count);
        }

        // ファイルの更新・作成を検知したときの処理
        private void OnFileUpdated(object sender, FileSystemEventArgs e)
        {
            // フォルダの作成・変更（曲GUIDフォルダ自体など）や、対象外のファイルは無視する
            if (!IsScoreFile(e.FullPath))
            {
                return;
            }

            // 同じファイルに対して短時間に連続で発生するイベント（OSの仕様）を無視する
            lock (lastProcessedLock)
            {
                if (lastProcessedTime.TryGetValue(e.FullPath, out DateTime last) &&
                    (DateTime.Now - last).TotalMilliseconds < 1500)
                {
                    return;
                }
                lastProcessedTime[e.FullPath] = DateTime.Now;
            }

            // Anasoko側の書き込みが完了する（ファイルが閉じられる）まで待つ
            // ※ ここはウォッチャーのスレッドで行い、UIスレッドを止めないようにする
            if (!WaitUntilFileIsReady(e.FullPath))
            {
                return;
            }

            // 別スレッドから画面（UI）を操作するための安全な呼び出し
            this.Invoke((MethodInvoker)delegate
            {
                LogMessage("スコアデータの更新を検知しました: " + e.Name);
                ProcessScoreData(e.FullPath);
            });
        }

        // モニター対象として扱うべきスコアファイルかどうかを判定する（フォルダや無関係なファイルを除外）
        private bool IsScoreFile(string path)
        {
            if (!File.Exists(path)) return false;

            string fileName = Path.GetFileName(path).ToLower();
            return fileName == "easy.bin" || fileName == "normal.bin" || fileName == "hard.bin"
                || fileName == "oni.bin" || fileName == "ura.bin";
        }

        // Anasoko側がファイルの書き込みを終える（排他ロックを解放する）まで待機する
        private bool WaitUntilFileIsReady(string path, int retryCount = 15, int delayMs = 200)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    Thread.Sleep(delayMs);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(delayMs);
                }
            }
            return false;
        }

        // 検知したファイルを読み込み、データベースへ登録する処理
        // 検知したファイルを読み込み、データベースへ登録する処理
        private void ProcessScoreData(string fullPath)
        {
            string pcName = string.IsNullOrEmpty(textBoxPcName.Text) ? Environment.MachineName : textBoxPcName.Text;
            ProcessScoreFile(fullPath, pcName, silent: false);
        }

        // 1件の .bin ファイルを読み込み、データベースへ登録する処理
        // silent=true の場合、ログ・Discord通知を出さない（一括登録用）
        // 戻り値: 新規登録できたら true
        // ※ バックグラウンドスレッドから呼ばれる可能性があるため、UIコントロールへは直接アクセスしない
        //   （pcName は呼び出し側があらかじめUIスレッドで読み取って渡す）
        private bool ProcessScoreFile(string fullPath, string pcName, bool silent)
        {
            try
            {
                // 1. ファイル名からコース番号を判定
                string fileName = Path.GetFileName(fullPath).ToLower();
                int course = -1;
                if (fileName == "easy.bin") course = 0;
                else if (fileName == "normal.bin") course = 1;
                else if (fileName == "hard.bin") course = 2;
                else if (fileName == "oni.bin") course = 3;
                else if (fileName == "ura.bin") course = 4;

                if (course == -1) return false; // 指定のbinファイル以外（余計なファイル）は無視

                // 2. 親フォルダ名を曲の識別子として取得（Web側で曲名と照合するため）
                string guid = new DirectoryInfo(Path.GetDirectoryName(fullPath)).Name;

                // 3. AnasCoreを使用して .bin ファイルを直接読み込む
                //    ※ LoadInfoToData は title/subTitle から内部でパスを再計算するため、
                //      実際のファイルパスとズレて正しく読み込めない（常にスコア0になる）。
                //      検知したファイルのフルパスをそのまま渡す Load() を使う。
                AnasCore.Record record = new Record();
                record.Load(fullPath);

                int score = record.Score;
                int maxCombo = record.MaxCombo;
                int great = record.Great;
                int good = record.Good;
                int miss = record.Miss;
                int renda = record.Renda;
                int gauge = record.Gauge;

                // 4. song_catalog（ローカル）から曲名・ジャンルを解決する
                string songName = null;
                string genre = null;
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    using (var lookupCmd = new SQLiteCommand("SELECT title, genre FROM song_catalog WHERE hash = @hash", conn))
                    {
                        lookupCmd.Parameters.AddWithValue("@hash", guid);
                        using (var r = lookupCmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                songName = r["title"] as string;
                                genre = r["genre"] as string;
                            }
                        }
                    }
                }

                // 5. Supabase 上のこの曲・コースの既存スコアを取得してから登録する
                //    （プレイヤー名は最初は付けず、Webのランキング画面から後付けする運用）
                //    ※ oni.bin 等は「自己ベストのみ」を保持するファイルなので、ベストを更新して
                //      いない場合は同じ値が繰り返し検知されるだけ。ベスト更新時のみ登録する。
                List<int> existingScores = GetExistingScores(guid, course);
                int? previousBest = existingScores.Count > 0 ? existingScores.Max() : (int?)null;

                if (previousBest.HasValue && score <= previousBest.Value)
                {
                    if (!silent)
                    {
                        LogMessage($"自己ベストを更新していないため登録をスキップしました: 曲={songName ?? guid}, コース={course}, スコア={score}（自己ベスト {previousBest.Value}）");
                    }
                    return false;
                }

                InsertScoreToSupabase(guid, songName, genre, course, score, maxCombo, great, good, miss, renda, gauge, pcName);

                int rank = existingScores.Count(s => s > score) + 1;
                int total = existingScores.Count + 1;

                string displayName = songName ?? guid;

                if (!silent)
                {
                    LogMessage($"登録完了: 曲={displayName}, コース={course}, スコア={score}");

                    string courseName = (course >= 0 && course < CourseNames.Length) ? CourseNames[course] : $"コース{course}";
                    bool isNewRecord = !previousBest.HasValue || score > previousBest.Value;

                    // タイトルは新記録の時だけ「全一更新」を表示し、それ以外はタイトルなしで楽曲名から始める
                    string title = "";
                    if (isNewRecord)
                    {
                        string diffText = previousBest.HasValue ? $"（+{score - previousBest.Value}点）" : "";
                        title = $"【全一更新！{diffText}】";
                    }

                    string description =
                        $"### 楽曲名 : {displayName}\n" +
                        $"ジャンル : {(string.IsNullOrEmpty(genre) ? "不明" : genre)}\n" +
                        $"コース : {courseName}\n" +
                        $"PC : {pcName}\n\n" +
                        $"スコア : {score:N0}\n" +
                        $"良 : {great}\n" +
                        $"可 : {good}\n" +
                        $"不可 : {miss}\n" +
                        $"連打数 : {renda}\n" +
                        $"最大コンボ : {maxCombo}\n\n" +
                        $"ランキング : {rank}位（{total}件中）";

                    int color = isNewRecord ? 0xF1C40F : 0x3498DB;
                    SendDiscordMessage(title, color, description);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    LogMessage("エラーが発生しました: " + ex.Message);
                    SendDiscordMessage("⚠️ スコア登録に失敗しました", 0xE74C3C,
                        $"ファイル : {Path.GetFileName(fullPath)}\nエラー : {ex.Message}");
                }
                return false;
            }
        }

        // Supabase の接続情報（URL・匿名キー）が無ければ例外を投げる
        private (string url, string apiKey) GetSupabaseCredentials()
        {
            string url = ConfigurationManager.AppSettings["SupabaseUrl"];
            string apiKey = ConfigurationManager.AppSettings["SupabaseAnonKey"];
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Supabaseの接続設定（SupabaseUrl / SupabaseAnonKey）がApp.configにありません。");
            }
            return (url, apiKey);
        }

        // 指定した曲・コースの、Supabase上に既にある全スコアを取得する
        private List<int> GetExistingScores(string hash, int course)
        {
            var (url, apiKey) = GetSupabaseCredentials();

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{url}/rest/v1/scores?title=eq.{Uri.EscapeDataString(hash)}&course=eq.{course}&select=score");
            request.Headers.Add("apikey", apiKey);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = httpClient.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var serializer = new JavaScriptSerializer();
            var rows = serializer.Deserialize<List<Dictionary<string, object>>>(json);
            return rows.Select(r => Convert.ToInt32(r["score"])).ToList();
        }

        // 新しいスコアを Supabase の scores テーブルへ登録する（player_name は空のまま）
        private void InsertScoreToSupabase(string hash, string songName, string genre, int course,
            int score, int maxCombo, int great, int good, int miss, int renda, int gauge, string pcName)
        {
            var (url, apiKey) = GetSupabaseCredentials();

            var payload = new Dictionary<string, object>
            {
                ["title"] = hash,
                ["song_name"] = songName,
                ["genre"] = genre,
                ["course"] = course,
                ["score"] = score,
                ["max_combo"] = maxCombo,
                ["great"] = great,
                ["good"] = good,
                ["miss"] = miss,
                ["renda"] = renda,
                ["gauge"] = gauge,
                ["source_pc"] = pcName,
            };

            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{url}/rest/v1/scores");
            request.Headers.Add("apikey", apiKey);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = httpClient.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        // Discord Webhook へ Embed 形式の通知を送信する（失敗してもアプリ本体には影響させない）
        private void SendDiscordMessage(string title, int color, string description)
        {
            string webhookUrl = ConfigurationManager.AppSettings["DiscordWebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl))
            {
                return;
            }

            try
            {
                var payload = new Dictionary<string, object>
                {
                    ["embeds"] = new object[]
                    {
                        new Dictionary<string, object>
                        {
                            ["title"] = title,
                            ["description"] = description,
                            ["color"] = color,
                            ["timestamp"] = DateTime.UtcNow.ToString("o"),
                        }
                    }
                };

                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(payload);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                httpClient.PostAsync(webhookUrl, httpContent).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogMessage("Discord通知に失敗しました: " + ex.Message);
            }
        }

        // ListBoxにログを表示する処理
        private void LogMessage(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            listBoxLog.Items.Add($"[{time}] {message}");
            // 自動で一番下までスクロールさせる
            listBoxLog.TopIndex = listBoxLog.Items.Count - 1;
        }
    }
}