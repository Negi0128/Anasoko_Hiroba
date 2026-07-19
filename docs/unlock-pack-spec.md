# 楽曲解禁パック（.anskpack）仕様書 v2

段位道場そのものと、その合格ごほうび曲をまとめて配布する「DLCパック」の仕組み。

- **Anasoko_Monitor**（本リポジトリ）… パックの読み込み・段位道場の導入・合格検知・ごほうび曲の解禁・パック管理を行う
- **Anasoko Pack Maker**（PackMaker フォルダ、アセンブリ名 AnasPackMaker）… パックを作成する配布者向けツール

> v1（.anaspack）からの変更: 段位道場ファイルをパックに同梱し、読み込み時に Dani フォルダへ導入するようにした。
> 拡張子を **.anskpack** に変更。v1 形式のサポートは廃止（旧パックは実在しないため）。

## 1. 用語

| 用語 | 意味 |
|---|---|
| 段位道場ファイル | 段位を定義する一式。`*.def` と、その配下の `dani.json` ＋ `fumen`（.tja/.ogg）。ゲームは Dani フォルダ内の `*.def` を再帰検索して段位を認識する |
| 段位bin | `Data\Scores\<プロファイルGUID>\dani\Local\<dan_hash>.bin`。段位の自己ベスト記録 |
| dan_hash | 段位bin のファイル名（拡張子なし）。段位定義から計算で再現できる（3章） |
| ごほうび曲 | 合格時に Songs フォルダへ展開される楽曲 |

## 2. 段位bin のフォーマット（先頭16バイトのみ使用）

Anasoko本体の `DanRecord.cs` が書き込むリトルエンディアンのバイナリ。

| オフセット | 型 | 内容 |
|---|---|---|
| 0 | int32 | Score |
| 4 | int32 | MainGauge（魂ゲージ） |
| 8 | int32 | MaxCount |
| 12 | int32 | **Batch（合否バッジ）** |

以降は段位定義に依存する可変長のため読まない。
Batch: `-1=不合格 / 1=合格 / 2=金合格 / 3=フルコンボ / 4=フルコンボ金 / 5=全良 / 6=全良金`
（過去最高のみ保存・序列は非線形。判定は下表に従う）

## 3. 段位ハッシュ（dan_hash）の計算 ★検証済み

段位定義（dani.json ＋ 譜面TJA）から、ゲームが作る段位bin のファイル名を計算で再現できる。
`AnasCore.dll` の **`AnasCore.Record.GenerateID(string)`（static, 戻り値string）** を使う。

手順（`DanRecord.cs` の実装に一致することを実データで確認済み）:

1. dani.json の `tja_Path` の順に、各TJAファイルのヘッダから `TITLE:` / `SUBTITLE:` を読む
   - エンコーディングは **SJIS（コードページ932）**
   - 値は Trim。`COURSE` 行に達したら読み取り終了
   - **プレフィックス（`--` / `++`）は取り除かず、生の値を使う**
2. `board` 文字列を組み立てる（順序厳守）:
   ```
   board = ""
   foreach theme_Borders[i]:
       foreach values[j]:
           board += ((double)red).ToString() + ((double)gold).ToString()
   board += ((double)theme_Gauge.red).ToString() + ((double)theme_Gauge.gold).ToString()
   foreach theme_Genre[k]:
       board += theme_Genre[k]
   ```
   ※ 数値は double 化して ToString()（整数値なら "75" のように小数点なしになる）
3. `passphrase = subTitle[0] + title[0] + subTitle[1] + title[1] + subTitle[2] + title[2] + board`
4. `dan_hash = AnasCore.Record.GenerateID(passphrase)`

検証例（十段）: board=`75507416317824126500100100GoodMissRoll` → dan_hash=`944b4997-f995f658-93e615cd-9103ec22`（実在binと一致）

## 4. 解禁条件（condition）

| condition | 表示名 | 判定式 |
|---|---|---|
| `pass` | 合格 | Batch >= 1 |
| `gold` | 金合格 | Batch が 2, 4, 6 のいずれか |
| `fullcombo` | フルコンボ | Batch >= 3 |
| `allperfect` | 全良 | Batch >= 5 |

## 5. .anskpack ファイル構造

実体は ZIP。エントリは3つ。

```
○○.anskpack (zip)
├─ manifest.json   … 平文 UTF-8。パック情報
├─ dan.zip         … 平文。段位道場ファイル（Dani導入先フォルダを基準とした相対パス）
└─ reward.enc      … 「内部zip」を PackCrypto で暗号化（Songsフォルダ相対）。合格までネタバレ防止
```

- **段位道場は平文**（読み込み時に即Daniへ導入するため隠す意味がない）。
- **ごほうび曲だけ暗号化**（合格するまで曲名を見られないように）。鍵は dan_hash を含めて導出。

### manifest.json

```json
{
  "format_version": 2,
  "name": "十段チャレンジ解禁パック",
  "author": "Negi",
  "dan_title": "十段",
  "dan_hash": "944b4997-f995f658-93e615cd-9103ec22",
  "condition": "pass",
  "message": "十段合格おめでとう！新曲が解禁されました！",
  "dan_install_folder": "十段チャレンジ",
  "reward_paths": ["解禁曲/新曲A"]
}
```

- `dan_install_folder`: Dani フォルダ直下に作るサブフォルダ名（段位道場の導入先）。パック名から自動生成し、ファイル名に使えない文字は除去する
- `dan_hash`: Pack Maker が3章の手順で計算して格納する。Monitor は導入時に、実際にDaniへ入れた dani.json から**再計算して検証**する（改ざん・計算差異の検出）
- `reward_paths`: ごほうび曲の内部zip最上位フォルダ一覧（表示用）
- 1パック = 1段位・1条件

### 暗号化（reward.enc）
- AES-256-CBC + PKCS7。形式 `[IV 16バイト][暗号文]`。鍵導出は v1 の `PackCrypto` を流用（`EmbeddedSecret|dan_hash` から Rfc2898）

## 6. 共有コード（Shared/AnasPack/）

両プロジェクトからソースリンクで参照する。**このフォルダは仕様の一部。**
両プロジェクトとも `AnasCore.dll`（リポジトリ直下 `..\AnasCore.dll`）を参照する必要がある。

| ファイル | 内容 |
|---|---|
| `PackManifest.cs` | manifest.json（v2）の読み書きと妥当性チェック |
| `PackCrypto.cs` | reward.enc の暗号化/復号（v1から流用） |
| `DanBinReader.cs` | 段位bin の Batch 読み取りと条件判定（v1から流用） |
| `DanDefinition.cs` | ★新規。dani.json＋TJAから段位名と dan_hash を求める（3章） |
| `AnasPackFile.cs` | .anskpack の作成/読取/段位道場の導入/ごほうび曲の展開（Zip Slip対策込み） |

## 7. Monitor 側の動作

### 設定（永続化。Properties.Settings に追加）
- `DaniFolderPath`: 段位道場の導入先（Anasoko の Dani フォルダ）
- Songs フォルダは既存設定を流用（ごほうび曲はゲームの Songs へ直接展開する）

### パック読み込み（「anskpack読み込み」ボタン）
1. .anskpack を選択 → manifest 検証
2. Dani フォルダ・Songs フォルダが未設定なら、その場でフォルダ選択させて設定へ保存（設定済みなら再利用。変更も可能にする）
3. Packs\ フォルダ（exe横）へコピー
4. **段位道場を即導入**: dan.zip を `Dani\<dan_install_folder>\` へ展開（Zip Slip対策）。導入した dani.json から dan_hash を再計算し manifest と一致するか検証
5. installed_packs テーブルへ記録（段位導入済み・ごほうび未解禁）
6. 導入直後に解禁チェック（既に合格済みなら即ごほうび解禁）
7. パック一覧を更新。「Anasokoを再起動すると段位道場に追加されます」をログ＆通知

### 段位合格の検知（既存 FileSystemWatcher を利用）
- パスに `\dani\Local\` を含む .bin の更新を検知 → ファイル名(=dan_hash)が未解禁パックと一致 → Batch 読取 → 条件判定 → ごほうび曲を Songs へ展開 → installed_packs を解禁済みに更新 → ログ＆Discord通知（「🎉 楽曲解禁！」＋再起動案内）

### パック一覧（常時表示）と削除
- 一覧列: パック名 / 対象段位（dan_title） / 条件 / 状態（段位導入済み・解禁済み）
- **削除**: 確認ダイアログの上で、パックファイル・導入した段位道場フォルダ・（解禁済みなら）展開したごほうび曲ファイルをまとめて除去し、DB記録も削除

### installed_packs テーブル（score_data.db）
```sql
CREATE TABLE IF NOT EXISTS installed_packs (
    pack_file       TEXT PRIMARY KEY,
    name            TEXT NOT NULL,
    dan_title       TEXT,
    dan_hash        TEXT NOT NULL,
    condition       TEXT NOT NULL,
    dan_install_dir TEXT,            -- Dani以下に導入したフォルダ名
    imported_at     TEXT NOT NULL,
    reward_unlocked INTEGER NOT NULL DEFAULT 0,
    reward_paths    TEXT,            -- 展開したSongs相対パスのJSON配列
    unlocked_at     TEXT
);
```

## 8. Pack Maker 側の動作（Anasoko Pack Maker）

1画面の WinForms。
- パック名・作者・メッセージ入力
- **段位道場フォルダを選択**（dani.json を含むフォルダ）→ `DanDefinition` で段位名（dani.jsonのtitle）と dan_hash を自動表示。導入先フォルダ名の既定はパック名
- 解禁条件（4種ドロップダウン）
- ごほうび曲フォルダのリスト（追加／展開先編集／削除。Songs相対、`..`や絶対パス拒否）
- 「パック出力」→ 検証 → dan.zip（段位道場フォルダを丸ごと）＋ reward.enc（ごほうび曲）＋ manifest を .anskpack に → セルフテスト（再読込・段位ハッシュ再計算一致・ごほうび復号展開のファイル数一致）

## 9. ビルド
- Monitor: `MSBuild Anasoko_Hiroba\Anasoko_Hiroba.csproj -t:Restore,Build -p:Configuration=Release -p:RestorePackagesConfig=true`
  （MSBuild: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`）
- Pack Maker: `dotnet build PackMaker -c Release`
- AnasCore.dll はネットDL由来でブロックされていることがある。テストで `Assembly.LoadFrom` が 0x80131515 で失敗する場合は `Assembly.Load(File.ReadAllBytes(path))` を使う
