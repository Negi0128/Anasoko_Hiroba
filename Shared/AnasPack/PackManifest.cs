using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace AnasPack
{
    // .anskpack 内の manifest.json（v3）の内容（平文 UTF-8。パック情報）。
    // v3 では段位「セット」丸ごとを同梱し、1パックに複数の解禁ルール（rules）を持てる。
    // 設計は docs/pack-integration-plan.md 9.4 を参照。
    public class PackManifest
    {
        public const int CurrentFormatVersion = 3;

        public int format_version { get; set; }
        public string name { get; set; }
        public string author { get; set; }
        // ごほうび暗号鍵の導出には各 rule の rule_id を使うが、パック識別子としても持つ
        public string pack_id { get; set; }
        // Dani へ導入する段位セットのフォルダ名（Monitorが採番で衝突回避）
        public string set_folder { get; set; }
        // 複数のごほうび条件
        public List<Rule> rules { get; set; }

        // 1つの解禁ルール = {対象段位, 条件, ごほうび曲, メッセージ}
        public class Rule
        {
            // ごほうび暗号鍵の導出に使う（ランダム。reward_<rule_id>.enc の鍵材料）
            public string rule_id { get; set; }
            // トリガーとなる段位フォルダ名（セット内で一意。例 "18,達人"）
            public string target_rank_folder { get; set; }
            // 表示名（例 "達人"。空 title ならフォルダ名の "," 以降）
            public string target_dan_display { get; set; }
            // pass/gold/fullcombo/allperfect
            public string condition { get; set; }
            public string message { get; set; }
            // ごほうび曲フォルダ名（表示用。配置先は読み込み時にプレイヤーが指定）
            public List<string> reward_songs { get; set; }
        }

        public static PackManifest FromJson(string json)
        {
            return new JavaScriptSerializer().Deserialize<PackManifest>(json);
        }

        public string ToJson()
        {
            return new JavaScriptSerializer().Serialize(this);
        }

        // 内容の妥当性チェック。問題があればその説明を、正常なら null を返す。
        public string Validate()
        {
            // v3 のみ受け付ける（v2 以前のサポートは廃止済み）
            if (format_version != CurrentFormatVersion)
            {
                return "対応していないパック形式のバージョンです。Monitorを最新版に更新してください。";
            }
            if (string.IsNullOrWhiteSpace(name)) return "パック名が設定されていません。";
            if (string.IsNullOrWhiteSpace(pack_id)) return "パックID（pack_id）が設定されていません。";
            if (string.IsNullOrWhiteSpace(set_folder)) return "段位セットの導入先フォルダ名（set_folder）が設定されていません。";
            if (rules == null || rules.Count == 0) return "解禁ルール（rules）が1つも設定されていません。";

            foreach (var rule in rules)
            {
                if (rule == null) return "解禁ルール（rules）の内容が不正です。";
                if (string.IsNullOrWhiteSpace(rule.rule_id)) return "解禁ルールのID（rule_id）が設定されていません。";
                if (string.IsNullOrWhiteSpace(rule.target_rank_folder)) return "解禁ルールの対象段位フォルダ（target_rank_folder）が設定されていません。";
                if (string.IsNullOrWhiteSpace(rule.condition)) return "解禁ルールの条件（condition）が設定されていません。";
                if (!DanBinReader.IsKnownCondition(rule.condition)) return "解禁ルールの条件（condition）が不正です。";
            }
            return null;
        }
    }
}
