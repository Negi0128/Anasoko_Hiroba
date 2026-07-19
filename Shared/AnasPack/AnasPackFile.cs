using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace AnasPack
{
    // .anskpack ファイル（実体はzip）の読み取りと展開（Monitor用）。エントリ（v3・設計書9.4）:
    //   manifest.json          … 平文 UTF-8 のパック情報
    //   dan.zip                … 平文。段位セット一式（dani.def＋<N,名前>/dani.json＋<N,名前>/fumen/*）
    //   reward_<rule_id>.enc   … ルールごとのごほうび内部zip（曲フォルダがトップ）を PackCrypto で暗号化
    public static class AnasPackFile
    {
        public const string Extension = ".anskpack";
        private const string ManifestEntryName = "manifest.json";
        private const string DanZipEntryName = "dan.zip";

        // ルールごとのごほうびエントリ名を組み立てる（reward_<rule_id>.enc）
        public static string RewardEntryName(string ruleId)
        {
            return "reward_" + ruleId + ".enc";
        }

        // ---- 読み取り ----

        public static PackManifest ReadManifest(string packPath)
        {
            using (var archive = ZipFile.OpenRead(packPath))
            {
                var entry = archive.GetEntry(ManifestEntryName);
                if (entry == null)
                {
                    throw new InvalidDataException("manifest.json が見つかりません。パックファイルが壊れています。");
                }
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                {
                    return PackManifest.FromJson(reader.ReadToEnd());
                }
            }
        }

        // ---- 展開（Monitor用）----

        // dan.zip（段位セット一式）を Dani\<NNN>-<set_folder>\ へ展開する。
        // <NNN> は Dani 直下の既存 "NNN-" と衝突しない最小番号（3桁ゼロ埋め、例 001-, 002-）。
        // 段位フォルダ名「N,名前」はそのまま保持される。
        // Zip Slip（"..\" などで導入先の外へ書き込ませる攻撃）はエントリごとに検証して拒否する。
        // 戻り値は導入先の絶対パス（Dani\<NNN>-<set_folder>）。
        public static string InstallSet(string packPath, string daniRootFolder)
        {
            var manifest = ReadManifest(packPath);
            if (string.IsNullOrWhiteSpace(manifest.set_folder))
            {
                throw new InvalidDataException("manifest に set_folder がありません。");
            }
            if (string.IsNullOrWhiteSpace(daniRootFolder) || !Directory.Exists(daniRootFolder))
            {
                throw new DirectoryNotFoundException("Daniフォルダが見つかりません: " + daniRootFolder);
            }

            string installDirName = NextAvailableSetNumber(daniRootFolder).ToString("D3") + "-" + manifest.set_folder;
            string installDir = Path.Combine(daniRootFolder, installDirName);

            using (var archive = ZipFile.OpenRead(packPath))
            {
                var danEntry = archive.GetEntry(DanZipEntryName);
                if (danEntry == null)
                {
                    throw new InvalidDataException("dan.zip が見つかりません。パックファイルが壊れています。");
                }
                using (var danStream = danEntry.Open())
                using (var ms = new MemoryStream())
                {
                    danStream.CopyTo(ms);
                    ms.Position = 0;
                    ExtractZipSafely(ms, installDir);
                }
            }

            return Path.GetFullPath(installDir);
        }

        // Dani 直下の既存フォルダ名 "NNN-..." の番号と衝突しない最小の正整数を返す。
        // Dani Generator の nextAvailableSetNumber と同方式（先頭の連続数字を番号として扱う）。
        private static int NextAvailableSetNumber(string daniRootFolder)
        {
            var used = new HashSet<int>();
            var leadingNumber = new Regex(@"^(\d+)-");
            foreach (var dir in Directory.GetDirectories(daniRootFolder))
            {
                string name = Path.GetFileName(dir);
                var m = leadingNumber.Match(name);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int n))
                {
                    used.Add(n);
                }
            }
            int candidate = 1;
            while (used.Contains(candidate)) candidate++;
            return candidate;
        }

        // reward_<ruleId>.enc を復号（鍵材料=ruleId）して destFolder 直下へ曲フォルダ単位で展開し、
        // 展開した destFolder 相対パス一覧を返す（Monitor用）。
        // Zip Slip 対策は ExtractZipSafely で行う。
        public static List<string> ExtractRewardForRule(string packPath, string ruleId, string destFolder)
        {
            if (string.IsNullOrWhiteSpace(ruleId)) throw new ArgumentException("rule_id が指定されていません。");

            string entryName = RewardEntryName(ruleId);
            byte[] encrypted;
            using (var archive = ZipFile.OpenRead(packPath))
            {
                var entry = archive.GetEntry(entryName);
                if (entry == null)
                {
                    throw new InvalidDataException(entryName + " が見つかりません。パックファイルが壊れています。");
                }
                using (var stream = entry.Open())
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    encrypted = ms.ToArray();
                }
            }

            // ごほうび鍵は rule_id を鍵材料に導出する（PackCrypto は変更不要）
            byte[] innerZip = PackCrypto.Decrypt(encrypted, ruleId);
            using (var ms = new MemoryStream(innerZip))
            {
                return ExtractZipSafely(ms, destFolder);
            }
        }

        // zip ストリームを destRoot へ安全に展開する共通処理。
        // Zip Slip 対策: 各エントリの最終書き込み先を GetFullPath で正規化し、destRoot 配下でなければ例外。
        // 戻り値は展開したファイルの destRoot 相対パス一覧（'/' 区切り）。
        private static List<string> ExtractZipSafely(Stream zipStream, string destRoot)
        {
            var extracted = new List<string>();

            string root = Path.GetFullPath(destRoot);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                root += Path.DirectorySeparatorChar;
            }

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.Length == 0) continue; // フォルダエントリはスキップ（ファイル展開時に親を作る）

                    string dest = Path.GetFullPath(Path.Combine(destRoot, entry.FullName));
                    if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("展開先の外へ書き込もうとする不正なパスを検出しました: " + entry.FullName);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    entry.ExtractToFile(dest, overwrite: true);

                    string rel = dest.Substring(root.Length).Replace('\\', '/');
                    extracted.Add(rel);
                }
            }

            return extracted;
        }
    }
}
