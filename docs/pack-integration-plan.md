# 解禁パック作成を Dani Generator へ統合する設計・計画

> **状態（2026-07）**: フェーズ1〜6 実装完了・クロスツール通し検証済み。
> Monitorはv3対応、Dani Generatorにパック生成コア＋UI（パスワード: `n_enu_taiko`）を追加、
> C# PackMaker は引退。Generator の実パッケージビルドは各自の dev 環境（better-sqlite3 再ビルド可）で要確認。

## 0. 目的

段位を作る **Dani Generator**（Electron + React + TypeScript）に「楽曲解禁パック(.anskpack)」の作成機能を組み込み、
**「段位を作る → ごほうび曲を足す → パックを出力」を1つのツールで完結**させる。
C# の standalone Pack Maker は役目を終えて引退。Monitor（C#）はパック形式の小改修のみ。

## 1. なぜ統合が妥当か

Dani Generator は既にパック作成に必要な部品を持っている：

| 必要な機能 | Dani Generator の既存資産 |
|---|---|
| 段位道場ファイル生成（dani.def＋段位フォルダ＋fumen） | `exportService.buildExportPlan()`（dani.def＋`<N,名前>/dani.json`＋`fumen/*`を生成） |
| TJA解析（cp932/BOM対応） | `tjaParser.ts` / `encoding.ts`（iconv-lite） |
| dani.json 読み書き | `daniJsonCodec.ts` |
| zip 生成 | `yazl`（`exportSetToZip` で実績あり） |
| 段位フォルダ命名規則「N,名前」 | `folderNameForRank()` |
| React UI・IPC・設定永続化(better-sqlite3) | 一式あり |

## 2. 唯一の技術的障害と解決策

**障害**: 段位ハッシュ `AnasCore.Record.GenerateID` は AnasCore.dll 独自アルゴリズム
（MD5/SHA1/SHA1・UTF8/SJIS/UTF16 いずれとも不一致を確認済み）。TypeScript へ移植不可。

**解決策 — パック作成時にハッシュを計算しない設計にする（v3）**:
- 照合用ハッシュは **Monitor が導入時に自分で計算**する（Monitor は .NET で AnasCore を持つ。
  現状も導入後に `DanDefinition` で再計算して検証している）。
- ごほうびの暗号鍵は **dan_hash に依存させない**（後述 pack_id 方式）。標準 SHA-256 + AES-256-CBC のみで、
  Node(crypto) と .NET が同一結果を出せる。

→ これで **Electron 側は AnasCore（.NET）に一切依存せず**、既存の JS 部品だけでパックを作れる。

## 3. パック形式 v3

v2 からの主な変更：
- パックは **段位「セット」丸ごと**（dani.def＋全段位フォルダ＋fumen）を同梱する
  （v2は単一段位フォルダのみ。dani.def が無く Anasoko が段位を発見できない問題があった）。
- 解禁トリガーは **セット内の1段位**を指定する。
- ごほうび暗号鍵は dan_hash ではなく **pack_id** から導出（移植可能なSHA-256）。
- manifest に dan_hash は持たない（Monitorが導入時に対象段位のdani.jsonから計算）。

### .anskpack 構造（zip）
```
○○.anskpack
├─ manifest.json   … 平文
├─ dan.zip         … 平文。段位セット一式（dani.def＋<N,名前>/dani.json＋<N,名前>/fumen/*）
└─ reward.enc      … ごほうび内部zip(Songs相対)を AES-256-CBC で暗号化
```

### manifest.json（v3）
```json
{
  "format_version": 3,
  "name": "十段チャレンジ解禁パック",
  "author": "Negi",
  "message": "十段合格おめでとう！",
  "pack_id": "5f3c...uuid",          // ごほうび暗号鍵の導出に使う（ランダム）
  "set_folder": "十段チャレンジ",     // Dani への導入先セットフォルダ名（Monitorが採番で衝突回避）
  "target_rank_folder": "14,十段",   // 解禁トリガーとなる段位フォルダ名（セット内で一意）
  "dan_title": "十段",               // 表示用（対象段位の title、空ならフォルダ名で代替）
  "condition": "pass",               // pass/gold/fullcombo/allperfect
  "reward_paths": ["解禁曲/新曲A"]
}
```

### ごほうび暗号鍵（移植可能・GenerateID非依存）
```
key = SHA-256( EMBEDDED_SECRET_BYTES ++ UTF8(pack_id) )   // 32バイト
形式 = [IV 16バイト][AES-256-CBC/PKCS7 暗号文]
```
- `EMBEDDED_SECRET` は両コードベース（Electron main / Monitor）で共有する定数文字列。
- Node は `crypto.createHash('sha256')` と `crypto.createCipheriv('aes-256-cbc', ...)`、
  .NET は `SHA256` と `Aes` で同一に計算できる（相互運用検証をタスクに含める）。

## 4. Dani Generator 側の実装

### メインプロセス（src/main/services/）
- 新規 `packService.ts`:
  - `buildDanZipBuffer(set)`: 既存 `buildExportPlan()` を再利用し、セット一式を yazl で zip バッファ化。
  - `buildRewardEncBuffer(rewardFolders, pack_id)`: ごほうび曲フォルダ群を内部zip化→AES暗号化。
  - `writePack(destPath, manifest, danZip, rewardEnc)`: 3エントリの .anskpack を yazl で書き出し。
  - `runSelfTest(...)`: 出力後に manifest再読込＋復号展開のファイル数一致を確認（PackMakerと同等）。
- 新規 IPC `pack.ipc.ts`: レンダラからの「パック出力」要求を受ける。ダイアログは既存 `dialogs.ipc.ts` 流用。

### レンダラ（src/renderer/）
- セット編集画面（`SetEditorScreen.tsx`）に「解禁パックを出力…」を追加、または専用モーダル：
  - 対象段位（セット内 rank のドロップダウン）
  - 解禁条件（4種）
  - ごほうび曲フォルダ（追加/削除。フォルダ名のまま Songs へ入る＝Pack Makerで確定した方針）
  - パック名／作者／メッセージ
  - 出力ボタン → IPC → セルフテスト結果を表示

### 依存
- 追加ライブラリ不要（yazl/yauzl/iconv-lite/crypto は既存）。

## 5. Monitor（C#）側の変更

- `PackManifest`: v3 フィールド（pack_id, set_folder, target_rank_folder）。dan_hash はオプション扱い。
- `PackCrypto`: pack_id からの鍵導出（v3）を追加。
- `AnasPackFile.InstallDan`: dan.zip（セット一式）を `Dani\<set_folder>` へ展開。
  セットフォルダ名は既存の他セットと衝突しないよう採番（`NNN-<set_folder>`、Dani Generatorの
  `nextAvailableSetNumber` と同方式）。段位フォルダ名「N,名前」は保持。
- 導入後、`DanDefinition( Dani\<導入セット>\<target_rank_folder> )` で **対象段位の dan_hash を計算**し
  installed_packs に保存。以後の合格照合はこの dan_hash で行う（監視ロジックは現状のまま）。
- `AnasPackFile.ExtractReward`: pack_id 由来の鍵で復号。
- v2(.anskpack) 対応は廃止（test/test2 はテスト用のため）。v1(.anaspack) は既に廃止済み。

## 6. Pack Maker（C#）の扱い

- 統合完了後に **引退**。`PackMaker/` プロジェクトと共有UI(`Shared/Ui/ModernFolderDialog.cs`は
  Monitorも使うので残す)を整理。`Shared/AnasPack` の DanDefinition/DanBinReader/AnasPackFile/PackCrypto は
  Monitor が引き続き使うため残す（PackMaker専用のBuild系メソッドは整理可）。

## 7. 作業フェーズ（見積り順）

1. **v3形式の確定＋相互運用の実証**（最重要・小）: Node と .NET で
   「同じ pack_id → 同じ鍵 → 相互に暗号化/復号できる」ことをヘッドレスで検証。
2. **Monitor を v3 対応**（中）: manifest/暗号/InstallDan/対象段位ハッシュ計算。既存テスト方式で検証。
3. **Dani Generator に packService＋IPC**（中）: buildExportPlan 再利用。ユニットテスト（vitest）追加。
4. **Dani Generator の UI**（中）: パック出力モーダル。
5. **通し検証**（小）: Dani Generatorで十段パック作成 → Monitorで読み込み→段位導入→合格→解禁までを実データで。
6. **Pack Maker 引退＋ドキュメント更新**（小）。

## 8. リスク・未決事項

- **相互運用**: Node/.NET の AES・SHA-256 の一致はフェーズ1で必ず先に実証してから他を進める。
- **EMBEDDED_SECRET の共有**: 2つのコードベースで同じ定数を持つ（どちらも難読化目的で、真の秘匿ではない前提）。
- **段位セットの発見性**: パックは dani.def を含むセットを導入するので Anasoko から発見可能になる（v2の課題を解消）。
- **空タイトル段位**: dan_title が空なら対象段位フォルダ名を表示に使う（`100 本家段位道場2025` の十段が該当）。
- **既存 v2 パックの移行**: 破棄（テスト用のみ）。本番配布前に統合するため実害なし。

---

## 9. 追加要件（ユーザー確認を反映）

### 9.1 既存機能を壊さない・作成はパスワードでガード
- Dani Generator の既存機能はそのまま。anskpack作成は**追加のオプション機能**。
- 作成画面へ入るときに**パスワード**を要求する（身内配布のみを想定した抑止。真のセキュリティではない）。
- 一度認証したらそのセッション中は再入力不要、程度でよい。

### 9.2 対象は「セット丸ごと」＋どの段位にごほうびを付けるか
- パックは段位セット一式（dani.def＋全段位）を同梱する（Generatorの出力単位に一致）。
- **セット内のどの段位に、どのごほうびを付けるかを設定**する。
  例: 「14,十段」〜「18,達人」を含むセットで「達人に合格したらごほうびAを解禁」。

### 9.3 複数ごほうび条件（rules）
- 1パックに**複数のルール**を持てる構造にする。各ルール = {対象段位, 条件, ごほうび曲, メッセージ}。
- 段位ごとに別々のごほうびを付けられる（例: 十段合格→A / 達人合格→B）。
- 各ルールのごほうびは**個別に暗号化**し、その段位に合格するまで他ルールの中身も見えないようにする。

### 9.4 manifest v3（rules配列版・確定）
```json
{
  "format_version": 3,
  "name": "○○パック",
  "author": "Negi",
  "pack_id": "uuid",
  "set_folder": "十段道場",              // Dani導入先セットフォルダ名（=セットのタイトル基準。Monitorが採番で衝突回避）
  "rules": [
    {
      "rule_id": "uuid",                 // ごほうび暗号鍵の導出に使う（ランダム）
      "target_rank_folder": "18,達人",   // トリガー段位フォルダ
      "target_dan_display": "達人",      // 表示名（"18,達人"→"達人"。空titleならフォルダ名の","以降）
      "condition": "pass",
      "message": "達人合格おめでとう！",
      "reward_songs": ["新曲B"]          // 表示用の曲フォルダ名（配置先は読み込み時にプレイヤーが指定）
    }
  ]
}
```
- pack内エントリ: `manifest.json`＋`dan.zip`（セット一式・平文）＋ルールごとに `reward_<rule_id>.enc`（暗号化）。
- 各 `reward_<rule_id>.enc` の内部zipは**曲フォルダをトップに置く**（ジャンルフォルダは付けない）。
- ごほうび鍵 = `SHA-256( EMBEDDED_SECRET ++ rule_id )`。

### 9.5 表示ルール
- 対象段位の表示名は、その段位の dani.json の title を使う。**空なら段位フォルダ名の「,」以降**
  （"14,十段"→"十段"）。Generator/Monitor 両方でこの規則。

### 9.6 ごほうび曲の配置（決定：プレイヤーが読み込み時に指定）
- 配置先フォルダ（ジャンル/カテゴリ相当）は**パックには含めず、Monitor 読み込み時にプレイヤーが指定**する。
- ごほうび曲は、その**指定フォルダの直下**に曲フォルダ単位で展開する（例: プレイヤーが `Songs\解禁曲` を
  指定 → `Songs\解禁曲\新曲B\...`）。指定先はパック単位で installed_packs に記録し、後から合格したときも同じ場所へ。

### 9.7 Monitor の読み込みフロー（案内文）
1. .anskpack を開く → **段位道場フォルダ（Dani）** と **ごほうび曲を入れるフォルダ（Songs内の任意のジャンルフォルダ）** を指定
2. セットを Dani へ導入（段位フォルダ名は保持）。導入先は installed_packs に記録
3. 導入完了メッセージ（ルールごと）:
   「段位道場「○○（＝target_dan_display）」を〈条件〉すると、新曲が「〇〇（＝プレイヤー指定フォルダ名）」に追加されます！」
4. 既に合格済みなら即解禁（現行どおり）。

## 9.8 確定した暗号スキーム（Node↔.NET 相互運用を実証済み）
両コードベースで**完全に同一**の結果になることを双方向で確認済み。
- 鍵導出: `PBKDF2( password = EMBEDDED_SECRET + "|" + rule_id, salt = SALT, iterations = 100000, hash = SHA-256, length = 32 bytes )`
  - `EMBEDDED_SECRET = "AnasokoHiroba.AnasPack.v1|c4a92f61e8d05b37"`（既存 PackCrypto と同値）
  - `SALT = [0x5A,0x0E,0x91,0x3C,0xB7,0x44,0xD2,0x68,0x1F,0xA3,0x7D,0x59,0xE6,0x02,0x8B,0xC5]`（既存 PackCrypto と同値）
- 暗号: `AES-256-CBC` + `PKCS7`。出力 = `[IV 16バイト][暗号文]`。
- .NET: `Rfc2898DeriveBytes(string,byte[],int,HashAlgorithmName.SHA256).GetBytes(32)` ＋ `Aes`。
  → **既存 `PackCrypto.Encrypt/Decrypt(bytes, keyMaterial)` の keyMaterial に rule_id を渡すだけでよい**（コード変更ほぼ不要）。
- Node: `crypto.pbkdf2Sync(pw, salt, 100000, 32, 'sha256')` ＋ `crypto.createCipheriv('aes-256-cbc', key, iv)`。

## 9.9 installed_packs スキーマ（v3・rules対応）
ルール単位で合格照合するため、パック行とルール行を分ける。
```sql
CREATE TABLE IF NOT EXISTS installed_packs (
    pack_file        TEXT PRIMARY KEY,
    name             TEXT NOT NULL,
    set_install_dir  TEXT,            -- Dani以下に導入したセットフォルダ名（採番後）
    reward_dest_dir  TEXT,            -- プレイヤーが指定したごほうび配置先（絶対パス）
    imported_at      TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS installed_pack_rules (
    pack_file        TEXT NOT NULL,
    rule_id          TEXT NOT NULL,
    target_dan_hash  TEXT NOT NULL,   -- 導入時に DanDefinition で計算
    target_display   TEXT,
    condition        TEXT NOT NULL,
    reward_unlocked  INTEGER NOT NULL DEFAULT 0,
    reward_paths     TEXT,            -- 展開した相対パスのJSON
    unlocked_at      TEXT,
    PRIMARY KEY (pack_file, rule_id)
);
```

## 10. 決定事項（P1〜P4 解決済み）
- **P1 複数ルール**: 最初から複数対応（rules 配列）。
- **P2 パスワード**: Dani Generator に**固定文字列を埋め込み**、作成画面に入る前に要求（身内向けの抑止）。
- **P3 ごほうび配置先**: パックには持たせず、**Monitor 読み込み時にプレイヤーが指定したフォルダの直下**に配置。
- **P4 セットフォルダ名**: セットのタイトル基準（既存 export と同方式、Monitor が採番で衝突回避）。
