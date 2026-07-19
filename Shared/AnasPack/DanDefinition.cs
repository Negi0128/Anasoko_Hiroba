using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using AnasCore;

namespace AnasPack
{
    // 段位定義（dani.json ＋ 参照TJA）から、ゲームが作る段位bin のファイル名（dan_hash）を
    // 計算で再現するクラス。手順は docs/unlock-pack-spec.md 3章に厳密に従う。
    // PackMaker が「フォルダを選ぶと段位名と dan_hash を自動表示」する用途と、
    // Monitor が「導入した dani.json から再計算して manifest と突き合わせ検証」する用途で共用する。
    public sealed class DanDefinition
    {
        // dani.json（=段位定義本体）のパース用DTO。JavaScriptSerializer は public プロパティに束縛する。
        private sealed class DaniJson
        {
            public string title { get; set; }
            public List<string> tja_Path { get; set; }
            public List<Border> theme_Borders { get; set; }
            public Gauge theme_Gauge { get; set; }
            public List<string> theme_Genre { get; set; }
        }

        private sealed class Border
        {
            public List<BorderValue> values { get; set; }
        }

        // 数値は double で受ける。dani.json には "100"（整数）と "100.0"（小数）の両表記があり、
        // ゲーム本体（DaniData の ValueData）も Red/Gold を double で保持しているため、
        // int で読むと "100.0" のような小数表記の段位が解析できなくなる。
        private sealed class BorderValue
        {
            public double red { get; set; }
            public double gold { get; set; }
        }

        private sealed class Gauge
        {
            public double red { get; set; }
            public double gold { get; set; }
        }

        // dani.json の title。段位名として表示に使う
        public string DanTitle { get; private set; }

        // 3章の手順で再現した段位bin のファイル名（拡張子なし）
        public string DanHash { get; private set; }

        // 実際に使用した dani.json の絶対パスと、その所在フォルダ（TJA 解決の基準）
        public string DaniJsonPath { get; private set; }
        public string DaniFolder { get; private set; }

        // dani.json が参照している譜面TJAの絶対パス一覧（PackMaker が同梱有無の確認に使う）
        public List<string> ReferencedTjaFiles { get; private set; }

        // 段位定義フォルダ（dani.json を含む）を指定して構築する。
        // folder 直下に無くても再帰的に dani.json を1つだけ探す（0個/複数は例外）。
        public DanDefinition(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentException("段位定義フォルダのパスが指定されていません。");
            }
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException("段位定義フォルダが見つかりません: " + folder);
            }

            DaniJsonPath = FindSingleDaniJson(folder);
            DaniFolder = Path.GetDirectoryName(DaniJsonPath);

            var dani = ParseDaniJson(DaniJsonPath);
            ValidateDani(dani);

            // TJA からタイトル/サブタイトルを読む（TJA 内の相対パスは dani.json の所在フォルダ基準）
            var titles = new List<string>();
            var subTitles = new List<string>();
            var tjaFiles = new List<string>();
            foreach (var rel in dani.tja_Path)
            {
                string tjaPath = Path.GetFullPath(Path.Combine(DaniFolder, rel.Replace('\\', Path.DirectorySeparatorChar)));
                if (!File.Exists(tjaPath))
                {
                    throw new FileNotFoundException("dani.json が参照するTJAが見つかりません: " + rel);
                }
                tjaFiles.Add(tjaPath);

                string title, subTitle;
                ReadTjaHeader(tjaPath, out title, out subTitle);
                titles.Add(title);
                subTitles.Add(subTitle);
            }

            ReferencedTjaFiles = tjaFiles;
            DanTitle = dani.title;

            string board = BuildBoard(dani);
            string passphrase = BuildPassphrase(titles, subTitles, board);

            // dan_hash は AnasCore が段位bin のファイル名に使う ID。static メソッドで生成する
            DanHash = Record.GenerateID(passphrase);
        }

        // 3章2の board 文字列を組み立てる。数値は (double) 化してから ToString()（整数値なら小数点なし）。
        // ゲーム本体 DanRecord.cs と同じく既定カルチャの ToString() を用いて、そのマシンが作る bin 名を再現する。
        private static string BuildBoard(DaniJson dani)
        {
            var sb = new StringBuilder();
            foreach (var border in dani.theme_Borders)
            {
                foreach (var v in border.values)
                {
                    sb.Append(((double)v.red).ToString());
                    sb.Append(((double)v.gold).ToString());
                }
            }
            sb.Append(((double)dani.theme_Gauge.red).ToString());
            sb.Append(((double)dani.theme_Gauge.gold).ToString());
            foreach (var genre in dani.theme_Genre)
            {
                sb.Append(genre);
            }
            return sb.ToString();
        }

        // 3章3の passphrase を組み立てる。TJA ごとに subTitle→title の順で連結し、末尾に board。
        private static string BuildPassphrase(List<string> titles, List<string> subTitles, string board)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < titles.Count; i++)
            {
                sb.Append(subTitles[i]);
                sb.Append(titles[i]);
            }
            sb.Append(board);
            return sb.ToString();
        }

        // TJA ヘッダから TITLE:/SUBTITLE: を読む。SJIS(932)、値は Trim、COURSE 行で終了。
        // プレフィックス（-- / ++）は仕様どおり除去しない。見つからなければ空文字列。
        private static void ReadTjaHeader(string tjaPath, out string title, out string subTitle)
        {
            title = "";
            subTitle = "";
            var sjis = Encoding.GetEncoding(932);
            foreach (var raw in File.ReadAllLines(tjaPath, sjis))
            {
                // 先頭行に紛れ込む BOM だけは判定を壊すので取り除く（値そのものは加工しない）
                string line = raw.TrimStart('﻿');
                if (line.StartsWith("COURSE", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                if (line.StartsWith("TITLE:", StringComparison.Ordinal))
                {
                    title = line.Substring("TITLE:".Length).Trim();
                }
                else if (line.StartsWith("SUBTITLE:", StringComparison.Ordinal))
                {
                    subTitle = line.Substring("SUBTITLE:".Length).Trim();
                }
            }
        }

        // 段位定義フォルダ配下から dani.json を1つだけ特定する。0個/複数はどちらも扱いを確定できないため例外。
        private static string FindSingleDaniJson(string folder)
        {
            var hits = Directory.GetFiles(folder, "dani.json", SearchOption.AllDirectories);
            if (hits.Length == 0)
            {
                throw new FileNotFoundException("指定フォルダに dani.json が見つかりません: " + folder);
            }
            if (hits.Length > 1)
            {
                throw new InvalidDataException(
                    "指定フォルダに dani.json が複数見つかりました。段位定義フォルダを1つに絞ってください:" +
                    Environment.NewLine + string.Join(Environment.NewLine, hits));
            }
            return hits[0];
        }

        private static DaniJson ParseDaniJson(string path)
        {
            string json = File.ReadAllText(path, new UTF8Encoding(false));
            DaniJson dani;
            try
            {
                dani = new JavaScriptSerializer().Deserialize<DaniJson>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("dani.json の解析に失敗しました: " + path, ex);
            }
            if (dani == null)
            {
                throw new InvalidDataException("dani.json の内容が空です: " + path);
            }
            return dani;
        }

        // dan_hash 計算に必要な項目が揃っているかを確認する（欠落は日本語で通知）
        private static void ValidateDani(DaniJson dani)
        {
            if (dani.tja_Path == null || dani.tja_Path.Count == 0)
            {
                throw new InvalidDataException("dani.json に tja_Path がありません。");
            }
            if (dani.theme_Borders == null)
            {
                throw new InvalidDataException("dani.json に theme_Borders がありません。");
            }
            foreach (var border in dani.theme_Borders)
            {
                if (border == null || border.values == null)
                {
                    throw new InvalidDataException("dani.json の theme_Borders の形式が不正です（values がありません）。");
                }
            }
            if (dani.theme_Gauge == null)
            {
                throw new InvalidDataException("dani.json に theme_Gauge がありません。");
            }
            if (dani.theme_Genre == null)
            {
                throw new InvalidDataException("dani.json に theme_Genre がありません。");
            }
        }
    }
}
