using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using AnasPack;

namespace Anasoko_Hiroba
{
    // 楽曲解禁パック（.anskpack、v3）の導入・照合・解禁を行う、UIに依存しないロジッククラス。
    // v3 では段位「セット」丸ごとをパックに同梱し、読み込み時に即 Dani フォルダへ導入する。
    // 1パックに複数の解禁ルール（rules）を持て、ルール単位で合格照合・解禁する（設計書9章）。
    // Form1 から分離しているのは、FileSystemWatcher のスレッドとUIスレッドの両方から
    // 呼ばれるためと、単体テストしやすくするため。
    public class PackUnlockService
    {
        private readonly string dbPath;
        private readonly string packsFolder;

        // 解禁処理（照合〜展開〜DB記録）や導入処理は FileSystemWatcher のスレッドと
        // UIスレッド（パック追加時・起動時チェック）の両方から呼ばれる可能性があるため、
        // 二重処理やzip展開の競合が起きないよう丸ごと直列化する。
        private readonly object unlockLock = new object();

        public PackUnlockService(string dbPath)
        {
            this.dbPath = dbPath;

            // exe横の Packs フォルダ。Application.StartupPath はUIに依存する（Formなしのテストで使えない）ため
            // 同じ場所を指す AppDomain.CurrentDomain.BaseDirectory を使う。
            this.packsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Packs");
            Directory.CreateDirectory(packsFolder);

            EnsureDatabase();
        }

        public string PacksFolder => packsFolder;

        // .anskpack 1件分の状態（一覧表示用）
        public class PackStatus
        {
            public string FileName;
            public PackManifest Manifest;
            public bool SetInstalled;
            public int RuleCount;
            public int UnlockedRuleCount;
        }

        // 解禁できたルール1件分の結果（ログ・Discord通知用）
        public class UnlockResult
        {
            public string FileName;
            public string PackName;
            public string RuleId;
            public string TargetDisplay;
            public string Condition;
            public string Message;
            public string RewardDest;
        }

        // installed_packs テーブル1行分（内部処理用）
        private class InstalledPackRow
        {
            public string PackFile;
            public string Name;
            public string SetInstallDir;
            public string RewardDestDir;
            public string ImportedAt;
        }

        // installed_pack_rules テーブル1行分（内部処理用。所属パック情報も併せて持つ）
        private class InstalledRuleRow
        {
            public string PackFile;
            public string PackName;
            public string RewardDestDir;
            public string RuleId;
            public string TargetDanHash;
            public string TargetDisplay;
            public string Condition;
            public bool RewardUnlocked;
            public List<string> RewardPaths;
            public string UnlockedAt;
        }

        private void EnsureDatabase()
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // 旧バージョン(v2以前)の installed_packs テーブルが残っていると、
                // CREATE TABLE IF NOT EXISTS は既存テーブルに何もしないため新カラム(set_install_dir 等)が
                // 無いまま INSERT に失敗する。スキーマが古ければ関連テーブルごと作り直す
                // （旧データはテスト用パックのみのため破棄してよい）。
                bool tableExists = false;
                bool hasSetInstallDir = false;
                using (var check = new SQLiteCommand("PRAGMA table_info(installed_packs);", conn))
                using (var reader = check.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tableExists = true;
                        if (string.Equals(reader["name"] as string, "set_install_dir", StringComparison.OrdinalIgnoreCase))
                        {
                            hasSetInstallDir = true;
                        }
                    }
                }
                if (tableExists && !hasSetInstallDir)
                {
                    foreach (var dropSql in new[]
                    {
                        "DROP TABLE IF EXISTS installed_packs;",
                        "DROP TABLE IF EXISTS installed_pack_rules;",
                        "DROP TABLE IF EXISTS unlocked_packs;" // さらに古い v1 の残骸も掃除
                    })
                    {
                        using (var drop = new SQLiteCommand(dropSql, conn)) drop.ExecuteNonQuery();
                    }
                }

                string packsSql = @"CREATE TABLE IF NOT EXISTS installed_packs (
                    pack_file        TEXT PRIMARY KEY,
                    name             TEXT NOT NULL,
                    set_install_dir  TEXT,
                    reward_dest_dir  TEXT,
                    imported_at      TEXT NOT NULL
                );";
                using (var cmd = new SQLiteCommand(packsSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string rulesSql = @"CREATE TABLE IF NOT EXISTS installed_pack_rules (
                    pack_file        TEXT NOT NULL,
                    rule_id          TEXT NOT NULL,
                    target_dan_hash  TEXT NOT NULL,
                    target_display   TEXT,
                    condition        TEXT NOT NULL,
                    reward_unlocked  INTEGER NOT NULL DEFAULT 0,
                    reward_paths     TEXT,
                    unlocked_at      TEXT,
                    PRIMARY KEY (pack_file, rule_id)
                );";
                using (var cmd = new SQLiteCommand(rulesSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // .anskpack を読み込み・検証したうえで Packs フォルダへコピーし、段位セットを即座に
        // Dani フォルダへ導入する。各ルールの対象段位ハッシュを導入した dani.json から計算して記録する。
        // rewardDestFolder は、合格時にごほうび曲を展開する配置先（プレイヤーが指定したフォルダの絶対パス）。
        // 失敗時は導入フォルダとコピーしたパックを削除してロールバックし、例外を投げる。
        public PackManifest ImportPack(string sourcePath, string daniRootFolder, string rewardDestFolder)
        {
            var manifest = AnasPackFile.ReadManifest(sourcePath);
            string error = manifest.Validate();
            if (error != null)
            {
                throw new InvalidDataException(error);
            }

            if (string.IsNullOrWhiteSpace(daniRootFolder) || !Directory.Exists(daniRootFolder))
            {
                throw new DirectoryNotFoundException("Daniフォルダが設定されていません。段位セットを導入できません。");
            }

            lock (unlockLock)
            {
                string destPath = Path.Combine(packsFolder, Path.GetFileName(sourcePath));
                string packFileNameForCleanup = Path.GetFileName(destPath);

                // 再導入（同名パックの再読み込み）に備えて、前回導入した段位セットフォルダと
                // 解禁済みルールの展開済みごほうび曲ファイルを、新規導入の前に削除しておく。
                // これをしないと再読込のたびに Dani に 001-, 002-... と同じセットが増殖してしまう。
                var previousRow = GetInstalledPackRow(packFileNameForCleanup);
                if (previousRow != null)
                {
                    if (!string.IsNullOrEmpty(previousRow.SetInstallDir))
                    {
                        string prevInstalledDir = ResolveSetInstallDir(previousRow.SetInstallDir, daniRootFolder);
                        if (!string.IsNullOrEmpty(prevInstalledDir) && Directory.Exists(prevInstalledDir))
                        {
                            try { Directory.Delete(prevInstalledDir, recursive: true); } catch { /* ベストエフォート */ }
                        }
                    }

                    string prevRewardDest = previousRow.RewardDestDir;
                    if (!string.IsNullOrEmpty(prevRewardDest) && Directory.Exists(prevRewardDest))
                    {
                        foreach (var rule in GetRuleRows(packFileNameForCleanup))
                        {
                            if (rule.RewardUnlocked && rule.RewardPaths != null)
                            {
                                RemoveRewardFiles(prevRewardDest, rule.RewardPaths);
                            }
                        }
                    }
                }

                File.Copy(sourcePath, destPath, overwrite: true);

                string installedDir = null;
                try
                {
                    // 段位セットを即Daniへ導入する（Dani\<NNN>-<set_folder>）
                    installedDir = AnasPackFile.InstallSet(destPath, daniRootFolder);
                    // set_install_dir には今後絶対パスを保存する（読む側は後方互換のため相対名も扱う）
                    string setInstallDirName = installedDir;

                    // ルールごとに、導入した対象段位フォルダの dani.json から対象段位ハッシュを計算する
                    var ruleHashes = new List<KeyValuePair<PackManifest.Rule, string>>();
                    foreach (var rule in manifest.rules)
                    {
                        string targetDir = Path.Combine(installedDir, rule.target_rank_folder);
                        string danHash = new DanDefinition(targetDir).DanHash;
                        ruleHashes.Add(new KeyValuePair<PackManifest.Rule, string>(rule, danHash));
                    }

                    string packFileName = Path.GetFileName(destPath);
                    using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            // 再導入に備えて、同名パックの既存行はいったん消してから入れ直す
                            using (var del = new SQLiteCommand("DELETE FROM installed_pack_rules WHERE pack_file = @packFile", conn))
                            {
                                del.Parameters.AddWithValue("@packFile", packFileName);
                                del.ExecuteNonQuery();
                            }

                            string packSql = @"INSERT OR REPLACE INTO installed_packs
                                (pack_file, name, set_install_dir, reward_dest_dir, imported_at)
                                VALUES (@packFile, @name, @setInstallDir, @rewardDestDir, @importedAt)";
                            using (var cmd = new SQLiteCommand(packSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@packFile", packFileName);
                                cmd.Parameters.AddWithValue("@name", manifest.name);
                                cmd.Parameters.AddWithValue("@setInstallDir", setInstallDirName);
                                cmd.Parameters.AddWithValue("@rewardDestDir", (object)rewardDestFolder ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@importedAt", DateTime.Now.ToString("s"));
                                cmd.ExecuteNonQuery();
                            }

                            string ruleSql = @"INSERT INTO installed_pack_rules
                                (pack_file, rule_id, target_dan_hash, target_display, condition, reward_unlocked, reward_paths, unlocked_at)
                                VALUES (@packFile, @ruleId, @danHash, @display, @condition, 0, NULL, NULL)";
                            foreach (var kv in ruleHashes)
                            {
                                using (var cmd = new SQLiteCommand(ruleSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@packFile", packFileName);
                                    cmd.Parameters.AddWithValue("@ruleId", kv.Key.rule_id);
                                    cmd.Parameters.AddWithValue("@danHash", kv.Value);
                                    cmd.Parameters.AddWithValue("@display", (object)kv.Key.target_dan_display ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@condition", kv.Key.condition);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                    }
                }
                catch
                {
                    // ロールバック: 導入した段位セットフォルダとコピーしたパックファイルを取り除く
                    if (installedDir != null && Directory.Exists(installedDir))
                    {
                        try { Directory.Delete(installedDir, recursive: true); } catch { /* ベストエフォート */ }
                    }
                    try { if (File.Exists(destPath)) File.Delete(destPath); } catch { /* ベストエフォート */ }

                    // DBに旧行が残ったまま参照先パックファイルだけ消える不整合を避けるため、
                    // 該当 pack_file の installed_packs / installed_pack_rules 行も削除する
                    try
                    {
                        using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                        {
                            conn.Open();
                            using (var transaction = conn.BeginTransaction())
                            {
                                using (var cmd = new SQLiteCommand("DELETE FROM installed_pack_rules WHERE pack_file = @packFile", conn))
                                {
                                    cmd.Parameters.AddWithValue("@packFile", packFileNameForCleanup);
                                    cmd.ExecuteNonQuery();
                                }
                                using (var cmd = new SQLiteCommand("DELETE FROM installed_packs WHERE pack_file = @packFile", conn))
                                {
                                    cmd.Parameters.AddWithValue("@packFile", packFileNameForCleanup);
                                    cmd.ExecuteNonQuery();
                                }
                                transaction.Commit();
                            }
                        }
                    }
                    catch { /* ベストエフォート */ }

                    throw;
                }
            }

            return manifest;
        }

        // Packs フォルダ内の全パックについて、manifestと導入・解禁状況を返す（一覧表示用）
        // daniRootFolder は set_install_dir が相対パス（旧バージョン形式）だった場合の解決に使う。
        // 省略時は set_install_dir が空でないかだけで SetInstalled を判定する（後方互換）。
        public List<PackStatus> GetPackStatuses(string daniRootFolder = null)
        {
            var result = new List<PackStatus>();
            if (!Directory.Exists(packsFolder)) return result;

            var packs = GetInstalledPackRows();
            var rulesByPack = GetRuleRowsByPack();

            foreach (var file in Directory.GetFiles(packsFolder, "*" + AnasPackFile.Extension))
            {
                string fileName = Path.GetFileName(file);
                if (!packs.TryGetValue(fileName, out InstalledPackRow packRow)) continue; // DB未記録（導入失敗の残骸等）は除外

                try
                {
                    var manifest = AnasPackFile.ReadManifest(file);
                    rulesByPack.TryGetValue(fileName, out List<InstalledRuleRow> rules);
                    int ruleCount = rules?.Count ?? 0;
                    int unlockedCount = rules?.Count(r => r.RewardUnlocked) ?? 0;

                    bool setInstalled;
                    if (daniRootFolder == null)
                    {
                        setInstalled = !string.IsNullOrEmpty(packRow.SetInstallDir);
                    }
                    else
                    {
                        string resolvedDir = ResolveSetInstallDir(packRow.SetInstallDir, daniRootFolder);
                        setInstalled = !string.IsNullOrEmpty(resolvedDir) && Directory.Exists(resolvedDir);
                    }

                    result.Add(new PackStatus
                    {
                        FileName = fileName,
                        Manifest = manifest,
                        SetInstalled = setInstalled,
                        RuleCount = ruleCount,
                        UnlockedRuleCount = unlockedCount,
                    });
                }
                catch
                {
                    // 壊れている・読めないパックは一覧から除外する
                }
            }

            return result;
        }

        // Scores フォルダ内の全プロファイルGUIDフォルダを走査し、未解禁ルールのうち
        // 条件を満たすものをまとめて解禁する（パック導入直後・起動時チェック用）。
        // ごほうび配置先は各パックの reward_dest_dir を使う。
        public List<UnlockResult> CheckAndUnlockAll(string scoresFolder)
        {
            var unlocked = new List<UnlockResult>();
            if (string.IsNullOrEmpty(scoresFolder) || !Directory.Exists(scoresFolder)) return unlocked;

            lock (unlockLock)
            {
                var pending = GetPendingRules();
                if (pending.Count == 0) return unlocked;

                var doneKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var profileDir in Directory.GetDirectories(scoresFolder))
                {
                    string localDir = Path.Combine(profileDir, "dani", "Local");
                    if (!Directory.Exists(localDir)) continue;

                    foreach (var rule in pending)
                    {
                        string key = RuleKey(rule);
                        if (doneKeys.Contains(key)) continue; // 既にこのチェックで解禁済み

                        string danBinPath = Path.Combine(localDir, rule.TargetDanHash + ".bin");
                        if (!File.Exists(danBinPath)) continue;

                        int? batch = DanBinReader.ReadBatch(danBinPath);
                        if (!batch.HasValue || !DanBinReader.IsConditionMet(batch.Value, rule.Condition)) continue;

                        var result = UnlockRuleInternal(rule);
                        if (result != null)
                        {
                            unlocked.Add(result);
                            doneKeys.Add(key);
                        }
                    }
                }
            }

            return unlocked;
        }

        // FileSystemWatcher が検知した段位bin 1件について、そのファイル名(=dan_hash)と一致する
        // 未解禁ルールがあれば条件判定し、満たしていれば解禁する。
        public List<UnlockResult> TryUnlockForDanBin(string danBinFullPath)
        {
            var unlocked = new List<UnlockResult>();
            string danHash = Path.GetFileNameWithoutExtension(danBinFullPath);

            lock (unlockLock)
            {
                var pending = GetPendingRules();

                foreach (var rule in pending)
                {
                    if (!string.Equals(rule.TargetDanHash, danHash, StringComparison.OrdinalIgnoreCase)) continue;

                    int? batch = DanBinReader.ReadBatch(danBinFullPath);
                    if (!batch.HasValue || !DanBinReader.IsConditionMet(batch.Value, rule.Condition)) continue;

                    var result = UnlockRuleInternal(rule);
                    if (result != null) unlocked.Add(result);
                }
            }

            return unlocked;
        }

        // パックを削除する: パックファイル・導入した段位セットフォルダ・
        // 全ルールの展開済みごほうび曲をまとめて除去し、DB記録（両テーブル）も削除する。
        // ごほうび配置先直下に元々あった曲は消さない（このパックが展開したファイルのみ対象）。
        public void DeletePack(string fileName, string daniRootFolder)
        {
            lock (unlockLock)
            {
                InstalledPackRow packRow = GetInstalledPackRow(fileName);

                // 導入した段位セットフォルダを削除する
                if (packRow != null && !string.IsNullOrEmpty(packRow.SetInstallDir))
                {
                    string installedDir = ResolveSetInstallDir(packRow.SetInstallDir, daniRootFolder);
                    if (!string.IsNullOrEmpty(installedDir) && Directory.Exists(installedDir))
                    {
                        try { Directory.Delete(installedDir, recursive: true); } catch { /* ベストエフォート */ }
                    }
                }

                // 各ルールの展開済みごほうび曲ファイルを、パックの reward_dest_dir 基準で削除する
                string rewardDest = packRow?.RewardDestDir;
                if (!string.IsNullOrEmpty(rewardDest) && Directory.Exists(rewardDest))
                {
                    foreach (var rule in GetRuleRows(fileName))
                    {
                        if (rule.RewardUnlocked && rule.RewardPaths != null)
                        {
                            RemoveRewardFiles(rewardDest, rule.RewardPaths);
                        }
                    }
                }

                // パックファイル本体を削除する
                string packPath = Path.Combine(packsFolder, fileName);
                if (File.Exists(packPath))
                {
                    try { File.Delete(packPath); } catch { /* ベストエフォート */ }
                }

                // DB記録（両テーブル）を削除する
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM installed_pack_rules WHERE pack_file = @packFile", conn))
                        {
                            cmd.Parameters.AddWithValue("@packFile", fileName);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmd = new SQLiteCommand("DELETE FROM installed_packs WHERE pack_file = @packFile", conn))
                        {
                            cmd.Parameters.AddWithValue("@packFile", fileName);
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }
            }
        }

        // 実際の解禁処理: reward_<ruleId>.enc を復号してごほうび配置先へ展開し、該当ルール行を解禁済みに更新する。
        // 配置先が未設定・存在しない場合は例外にせず null を返し、呼び出し元にスキップさせる
        //（配置先設定後の起動時チェックで回収されるため、解禁は保留のままにする）。
        // 呼び出し側で unlockLock を取得済みであること。
        private UnlockResult UnlockRuleInternal(InstalledRuleRow rule)
        {
            string rewardDest = rule.RewardDestDir;
            if (string.IsNullOrEmpty(rewardDest) || !Directory.Exists(rewardDest))
            {
                return null;
            }

            string packPath = Path.Combine(packsFolder, rule.PackFile);
            if (!File.Exists(packPath))
            {
                return null;
            }

            List<string> rewardPaths = AnasPackFile.ExtractRewardForRule(packPath, rule.RuleId, rewardDest);

            // メッセージは manifest の該当ルールから取得する（DBには保持していない）
            string message = null;
            try
            {
                var manifest = AnasPackFile.ReadManifest(packPath);
                var mrule = manifest.rules?.FirstOrDefault(r =>
                    string.Equals(r.rule_id, rule.RuleId, StringComparison.OrdinalIgnoreCase));
                message = mrule?.message;
            }
            catch { /* メッセージ取得失敗は解禁自体を止めない */ }

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = @"UPDATE installed_pack_rules SET
                    reward_unlocked = 1,
                    reward_paths = @rewardPaths,
                    unlocked_at = @unlockedAt
                    WHERE pack_file = @packFile AND rule_id = @ruleId";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@packFile", rule.PackFile);
                    cmd.Parameters.AddWithValue("@ruleId", rule.RuleId);
                    cmd.Parameters.AddWithValue("@rewardPaths", new JavaScriptSerializer().Serialize(rewardPaths));
                    cmd.Parameters.AddWithValue("@unlockedAt", DateTime.Now.ToString("s"));
                    cmd.ExecuteNonQuery();
                }
            }

            return new UnlockResult
            {
                FileName = rule.PackFile,
                PackName = rule.PackName,
                RuleId = rule.RuleId,
                TargetDisplay = rule.TargetDisplay,
                Condition = rule.Condition,
                Message = message,
                RewardDest = rewardDest,
            };
        }

        // ごほうび曲ファイルを配置先から削除し、削除の結果空になった中間フォルダも掃除する。
        // 配置先フォルダ（destRoot自身）は対象外。既存の他の曲が入っているフォルダは残す。
        private static void RemoveRewardFiles(string destRoot, List<string> rewardPaths)
        {
            string root = Path.GetFullPath(destRoot);
            // "C:\Reward" が "C:\RewardEvil" のような別フォルダを誤って通してしまわないよう、
            // 末尾セパレータを付けたうえで前方一致を見る（AnasPackFile.ExtractZipSafely と同方式）。
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                root += Path.DirectorySeparatorChar;
            }

            foreach (var relPath in rewardPaths)
            {
                if (string.IsNullOrWhiteSpace(relPath)) continue;

                string fullPath = Path.GetFullPath(Path.Combine(root, relPath.Replace('/', Path.DirectorySeparatorChar)));
                // 配置先フォルダの外を指すパスは無視する（安全のため）
                if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                }
                catch { /* ベストエフォート */ }

                // 空になった親フォルダを配置先直下に達するまで掃除する
                string dir = Path.GetDirectoryName(fullPath);
                while (!string.IsNullOrEmpty(dir)
                       && !string.Equals(Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar),
                                          root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (!Directory.Exists(dir) || Directory.EnumerateFileSystemEntries(dir).Any()) break;
                        Directory.Delete(dir);
                    }
                    catch
                    {
                        break; // ベストエフォート。消せなければそこで諦める
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }
        }

        // set_install_dir の値から実際の導入先フォルダの絶対パスを求める。
        // 今後は絶対パスを保存するが、旧バージョンで保存された「Daniフォルダ直下のフォルダ名」のみの
        // 値との後方互換のため、絶対パスならそのまま使い、そうでなければ daniRootFolder と結合する。
        private static string ResolveSetInstallDir(string setInstallDir, string daniRootFolder)
        {
            if (string.IsNullOrEmpty(setInstallDir)) return null;

            if (Path.IsPathRooted(setInstallDir))
            {
                return setInstallDir;
            }

            if (string.IsNullOrEmpty(daniRootFolder)) return null;
            return Path.Combine(daniRootFolder, setInstallDir);
        }

        private static string RuleKey(InstalledRuleRow rule)
        {
            return rule.PackFile + "|" + rule.RuleId;
        }

        // 全ルール行のうち「ごほうび未解禁」のもの（所属パックの reward_dest_dir 付き）を返す。
        // 呼び出し側で unlockLock を取得済みであること。
        private List<InstalledRuleRow> GetPendingRules()
        {
            return GetAllRuleRows().Where(r => !r.RewardUnlocked).ToList();
        }

        private InstalledPackRow GetInstalledPackRow(string packFile)
        {
            GetInstalledPackRows().TryGetValue(packFile, out InstalledPackRow row);
            return row;
        }

        private Dictionary<string, InstalledPackRow> GetInstalledPackRows()
        {
            var result = new Dictionary<string, InstalledPackRow>(StringComparer.OrdinalIgnoreCase);
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = @"SELECT pack_file, name, set_install_dir, reward_dest_dir, imported_at
                               FROM installed_packs";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new InstalledPackRow
                        {
                            PackFile = reader["pack_file"] as string,
                            Name = reader["name"] as string,
                            SetInstallDir = reader["set_install_dir"] as string,
                            RewardDestDir = reader["reward_dest_dir"] as string,
                            ImportedAt = reader["imported_at"] as string,
                        };
                        result[row.PackFile] = row;
                    }
                }
            }
            return result;
        }

        // 全ルール行を、所属パックの名前と reward_dest_dir を付けて返す。
        private List<InstalledRuleRow> GetAllRuleRows()
        {
            var result = new List<InstalledRuleRow>();
            var serializer = new JavaScriptSerializer();

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                string sql = @"SELECT r.pack_file, r.rule_id, r.target_dan_hash, r.target_display, r.condition,
                                      r.reward_unlocked, r.reward_paths, r.unlocked_at,
                                      p.name AS pack_name, p.reward_dest_dir AS reward_dest_dir
                               FROM installed_pack_rules r
                               JOIN installed_packs p ON p.pack_file = r.pack_file";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string rewardPathsJson = reader["reward_paths"] as string;
                        result.Add(new InstalledRuleRow
                        {
                            PackFile = reader["pack_file"] as string,
                            PackName = reader["pack_name"] as string,
                            RewardDestDir = reader["reward_dest_dir"] as string,
                            RuleId = reader["rule_id"] as string,
                            TargetDanHash = reader["target_dan_hash"] as string,
                            TargetDisplay = reader["target_display"] as string,
                            Condition = reader["condition"] as string,
                            RewardUnlocked = Convert.ToInt32(reader["reward_unlocked"]) != 0,
                            RewardPaths = string.IsNullOrEmpty(rewardPathsJson)
                                ? null
                                : serializer.Deserialize<List<string>>(rewardPathsJson),
                            UnlockedAt = reader["unlocked_at"] as string,
                        });
                    }
                }
            }
            return result;
        }

        private List<InstalledRuleRow> GetRuleRows(string packFile)
        {
            return GetAllRuleRows()
                .Where(r => string.Equals(r.PackFile, packFile, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private Dictionary<string, List<InstalledRuleRow>> GetRuleRowsByPack()
        {
            var result = new Dictionary<string, List<InstalledRuleRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in GetAllRuleRows())
            {
                if (!result.TryGetValue(row.PackFile, out List<InstalledRuleRow> list))
                {
                    list = new List<InstalledRuleRow>();
                    result[row.PackFile] = list;
                }
                list.Add(row);
            }
            return result;
        }
    }
}
