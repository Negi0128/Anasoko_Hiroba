using System;
using System.IO;

namespace AnasPack
{
    // 段位結果ファイル（dani\Local\*.bin）の読み取りと、解禁条件の判定。
    // フォーマットの詳細は docs/unlock-pack-spec.md を参照。
    public static class DanBinReader
    {
        // Batch（合否バッジ）の値:
        // -1=不合格 / 1=合格 / 2=金合格 / 3=フルコンボ / 4=フルコンボ金 / 5=全良 / 6=全良金
        // ゲーム側は過去最高のバッジのみを保存する（値が下がることはない）

        public const string ConditionPass = "pass";
        public const string ConditionGold = "gold";
        public const string ConditionFullCombo = "fullcombo";
        public const string ConditionAllPerfect = "allperfect";

        // 段位bin の先頭16バイトから Batch を読み取る。読めない場合は null を返す。
        // ※ ファイル後半は段位定義に依存する可変長のため読まない
        public static int? ReadBatch(string path)
        {
            try
            {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    reader.ReadInt32(); // Score
                    reader.ReadInt32(); // MainGauge
                    reader.ReadInt32(); // MaxCount
                    return reader.ReadInt32(); // Batch
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool IsKnownCondition(string condition)
        {
            return condition == ConditionPass || condition == ConditionGold
                || condition == ConditionFullCombo || condition == ConditionAllPerfect;
        }

        // Batch値が解禁条件を満たしているかを判定する。
        // ※ Batchの序列は線形ではない（3=フルコンボ赤 は金合格ではない）ため、
        //   単純な大小比較で済ませず条件ごとに判定式を分ける
        public static bool IsConditionMet(int batch, string condition)
        {
            switch (condition)
            {
                case ConditionPass: return batch >= 1;
                case ConditionGold: return batch == 2 || batch == 4 || batch == 6;
                case ConditionFullCombo: return batch >= 3;
                case ConditionAllPerfect: return batch >= 5;
                default: return false;
            }
        }

        public static string ConditionDisplayName(string condition)
        {
            switch (condition)
            {
                case ConditionPass: return "合格";
                case ConditionGold: return "金合格";
                case ConditionFullCombo: return "フルコンボ";
                case ConditionAllPerfect: return "全良";
                default: return condition;
            }
        }
    }
}
