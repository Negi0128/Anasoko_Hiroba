using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AnasPack;

namespace AnasPackMaker
{
    // 楽曲解禁パック（.anskpack）を作成する配布者向けツール（v2: 段位道場も同梱するDLCパック方式）。
    // 仕様は docs/unlock-pack-spec.md を参照（特に8章がPack Makerの動作、3章がハッシュ計算）。
    // 共有コード（Shared/AnasPack）で実際の暗号化・zip組み立て・manifest検証・dan_hash計算を行い、
    // このクラスは入力収集とUI制御に専念する。
    //
    // 画面は「上から設定する順番」に並べている:
    //   ① 段位道場フォルダを選ぶ（合格を判定する段位）
    //   ② 解禁条件を選ぶ
    //   ③ ごほうび曲を追加する
    //   ④ パック情報（名前・作者・メッセージ）を入れる
    //   ⑤ パックを出力する
    // ※ 段位道場の「導入先フォルダ名」は Monitor 読み込み時に Dani フォルダを指定するため、
    //   作成側では設定不要。パック名から自動生成する。
    public class MainForm : Form
    {
        // 解禁条件のコンボボックス表示名と、DanBinReaderの定数値の対応
        private static readonly string[] ConditionLabels = { "合格", "金合格", "フルコンボ", "全良" };
        private static readonly string[] ConditionValues =
        {
            DanBinReader.ConditionPass,
            DanBinReader.ConditionGold,
            DanBinReader.ConditionFullCombo,
            DanBinReader.ConditionAllPerfect,
        };

        // ごほうび曲フォルダの一覧（Key=コピー元フォルダの絶対パス, Value=Songs相対の展開先）
        private readonly List<KeyValuePair<string, string>> folderEntries = new List<KeyValuePair<string, string>>();

        // 選択された段位道場フォルダと、そこから計算した段位名・dan_hash（未選択時は null）
        private string danFolder;
        private DanDefinition currentDan;

        private Label labelDanTitleValue;
        private Label labelDanHashValue;
        private ComboBox comboBoxCondition;
        private ListView listViewFolders;
        private TextBox textBoxName;
        private TextBox textBoxAuthor;
        private TextBox textBoxMessage;

        public MainForm()
        {
            Text = "Anasoko Pack Maker";
            ClientSize = new Size(820, 700);
            MinimumSize = new Size(780, 620);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Meiryo UI", 9F);

            BuildDanFolderGroup();   // ① 段位道場
            BuildConditionGroup();   // ② 解禁条件
            BuildRewardGroup();      // ③ ごほうび曲
            BuildPackInfoGroup();    // ④ パック情報
            BuildOutputButton();     // ⑤ 出力
        }

        // ① 段位道場フォルダ選択（dani.json を含むフォルダ）→ DanDefinition で段位名・dan_hash を自動表示
        private void BuildDanFolderGroup()
        {
            var group = new GroupBox
            {
                Text = "① 段位道場（合格を判定する段位）",
                Location = new Point(12, 12),
                Size = new Size(796, 108),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            Controls.Add(group);

            var buttonSelectDanFolder = new Button
            {
                Text = "段位フォルダを選択...",
                Location = new Point(12, 26),
                Size = new Size(180, 28),
            };
            buttonSelectDanFolder.Click += ButtonSelectDanFolder_Click;
            group.Controls.Add(buttonSelectDanFolder);

            group.Controls.Add(new Label
            {
                Text = "この段位に合格するとごほうび曲が解禁されます。dani.json を含むフォルダを選んでください。",
                Location = new Point(204, 32), AutoSize = true, ForeColor = Color.DimGray,
            });

            group.Controls.Add(new Label { Text = "段位名：", Location = new Point(12, 72), AutoSize = true });
            labelDanTitleValue = new Label
            {
                Text = "（未選択）", Location = new Point(80, 72), AutoSize = true,
                ForeColor = Color.DimGray, Font = new Font(Font, FontStyle.Bold),
            };
            group.Controls.Add(labelDanTitleValue);

            group.Controls.Add(new Label { Text = "判定ID：", Location = new Point(360, 72), AutoSize = true });
            labelDanHashValue = new Label { Text = "", Location = new Point(428, 72), AutoSize = true, ForeColor = Color.DimGray };
            group.Controls.Add(labelDanHashValue);
        }

        // ② 解禁条件（合格/金合格/フルコンボ/全良）
        private void BuildConditionGroup()
        {
            var group = new GroupBox
            {
                Text = "② 解禁条件",
                Location = new Point(12, 128),
                Size = new Size(796, 58),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            Controls.Add(group);

            group.Controls.Add(new Label { Text = "どのランクで解禁するか：", Location = new Point(12, 26), AutoSize = true });
            comboBoxCondition = new ComboBox
            {
                Location = new Point(190, 22),
                Size = new Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            comboBoxCondition.Items.AddRange(ConditionLabels);
            comboBoxCondition.SelectedIndex = 0; // 既定は「合格」
            group.Controls.Add(comboBoxCondition);
        }

        // ③ ごほうび曲フォルダ（コピー元フォルダとSongs内の展開先の一覧）
        private void BuildRewardGroup()
        {
            var group = new GroupBox
            {
                Text = "③ ごほうび曲（合格すると Songs フォルダに追加されます）",
                Location = new Point(12, 194),
                Size = new Size(796, 268),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            Controls.Add(group);

            // 追加した曲フォルダの一覧（フォルダ名のまま Songs へ入れるので、展開先の指定はさせない）
            listViewFolders = new ListView
            {
                Location = new Point(12, 26),
                Size = new Size(772, 188),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            listViewFolders.Columns.Add("追加する曲フォルダ", 760);
            group.Controls.Add(listViewFolders);

            var buttonAddFolder = new Button
            {
                Text = "フォルダ追加...",
                Location = new Point(12, 226),
                Size = new Size(150, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            buttonAddFolder.Click += ButtonAddFolder_Click;
            group.Controls.Add(buttonAddFolder);

            var buttonRemoveFolder = new Button
            {
                Text = "削除",
                Location = new Point(172, 226),
                Size = new Size(90, 28),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            buttonRemoveFolder.Click += ButtonRemoveFolder_Click;
            group.Controls.Add(buttonRemoveFolder);
        }

        // ④ パック情報（パック名・作者・解禁時メッセージ）
        private void BuildPackInfoGroup()
        {
            var group = new GroupBox
            {
                Text = "④ パック情報",
                Location = new Point(12, 470),
                Size = new Size(796, 150),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            Controls.Add(group);

            group.Controls.Add(new Label { Text = "パック名", Location = new Point(12, 30), AutoSize = true });
            textBoxName = new TextBox { Location = new Point(120, 26), Size = new Size(650, 23), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            group.Controls.Add(textBoxName);

            group.Controls.Add(new Label { Text = "作者", Location = new Point(12, 60), AutoSize = true });
            textBoxAuthor = new TextBox { Location = new Point(120, 56), Size = new Size(300, 23) };
            group.Controls.Add(textBoxAuthor);

            group.Controls.Add(new Label { Text = "解禁時メッセージ", Location = new Point(12, 92), AutoSize = true });
            textBoxMessage = new TextBox
            {
                Location = new Point(120, 90),
                Size = new Size(650, 48),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            group.Controls.Add(textBoxMessage);
        }

        // ⑤ パック出力ボタン
        private void BuildOutputButton()
        {
            var buttonBuildPack = new Button
            {
                Text = "⑤ パック出力...",
                Location = new Point(636, 632),
                Size = new Size(172, 42),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Font = new Font(Font, FontStyle.Bold),
            };
            buttonBuildPack.Click += ButtonBuildPack_Click;
            Controls.Add(buttonBuildPack);
        }

        // 段位道場フォルダ（dani.json を含むフォルダ）を選び、DanDefinition で段位名・dan_hash を計算して表示する
        private void ButtonSelectDanFolder_Click(object sender, EventArgs e)
        {
            string selected = ModernFolderDialog.Show(this, "段位道場フォルダを選択してください（dani.json を含むフォルダ）", danFolder);
            if (selected == null) return;

            try
            {
                var dan = new DanDefinition(selected);
                currentDan = dan;
                danFolder = selected;

                labelDanTitleValue.Text = dan.DanTitle;
                labelDanTitleValue.ForeColor = Color.Black;
                labelDanHashValue.Text = dan.DanHash;
                labelDanHashValue.ForeColor = Color.DimGray;
            }
            catch (Exception ex)
            {
                currentDan = null;
                danFolder = null;

                labelDanTitleValue.Text = "(エラー: 読み込めませんでした)";
                labelDanTitleValue.ForeColor = Color.Red;
                labelDanHashValue.Text = "";

                MessageBox.Show(this, "段位道場フォルダの読み込みに失敗しました。\n" + ex.Message, "Anasoko Pack Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ごほうび曲フォルダを追加する。Songsへの展開先はフォルダ名そのものにする（作成者に指定させない）。
        // 同名フォルダを別の場所から重複追加してしまうと展開時に上書き衝突するため、フォルダ名の重複は弾く。
        private void ButtonAddFolder_Click(object sender, EventArgs e)
        {
            string sourceDir = ModernFolderDialog.Show(this, "収録するごほうび曲フォルダを選択してください", null);
            if (sourceDir == null) return;

            string target = Path.GetFileName(sourceDir.TrimEnd('\\', '/'));
            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show(this, "このフォルダは追加できません（フォルダ名を取得できませんでした）。", "Anasoko Pack Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (folderEntries.Any(f => string.Equals(f.Value, target, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, "「" + target + "」という名前の曲フォルダはすでに追加されています。", "Anasoko Pack Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            folderEntries.Add(new KeyValuePair<string, string>(sourceDir, target));
            listViewFolders.Items.Add(new ListViewItem(sourceDir));
        }

        // 選択行を削除する
        private void ButtonRemoveFolder_Click(object sender, EventArgs e)
        {
            // インデックスがずれないよう後ろから削除する
            var indices = listViewFolders.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList();
            foreach (int index in indices)
            {
                folderEntries.RemoveAt(index);
                listViewFolders.Items.RemoveAt(index);
            }
        }

        // パックを出力し、直後に自動セルフテストを行う
        private void ButtonBuildPack_Click(object sender, EventArgs e)
        {
            if (currentDan == null || string.IsNullOrEmpty(danFolder))
            {
                MessageBox.Show(this, "段位道場フォルダが選択されていません。「段位フォルダを選択...」から選んでください。", "Anasoko Pack Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (folderEntries.Count == 0)
            {
                MessageBox.Show(this, "ごほうび曲フォルダが1件もありません。フォルダを追加してください。", "Anasoko Pack Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 段位フォルダの名前には Anasoko 側の命名規則（例: "14,十段" のようにインデックスを含む）が
            // あり、名前を変えると読み込めなくなる。そのため導入先フォルダ名はパック名ではなく、
            // 選択した段位フォルダの名前をそのまま使う（Monitor はこの名前で Dani へ展開する）。
            string installFolder = Path.GetFileName(danFolder.TrimEnd(Path.DirectorySeparatorChar, '/'));

            var manifest = new PackManifest
            {
                format_version = PackManifest.CurrentFormatVersion,
                name = textBoxName.Text.Trim(),
                author = textBoxAuthor.Text.Trim(),
                dan_title = currentDan.DanTitle,
                dan_hash = currentDan.DanHash,
                condition = ConditionValues[comboBoxCondition.SelectedIndex],
                message = textBoxMessage.Text,
                dan_install_folder = installFolder,
                reward_paths = folderEntries.Select(f => f.Value).ToList(),
            };

            string validationError = manifest.Validate();
            if (validationError != null)
            {
                MessageBox.Show(this, validationError, "Anasoko Pack Maker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string defaultFileName = SanitizeFileName(manifest.name);
            string savePath;
            using (var dialog = new SaveFileDialog
            {
                Title = "パックの出力先を選択",
                Filter = "楽曲解禁パック (*.anskpack)|*.anskpack",
                FileName = defaultFileName + AnasPackFile.Extension,
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                savePath = dialog.FileName;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                byte[] danZipBytes = AnasPackFile.BuildDanZip(danFolder);
                byte[] rewardZipBytes = AnasPackFile.BuildRewardZip(folderEntries);
                AnasPackFile.Create(savePath, manifest, danZipBytes, rewardZipBytes);

                int originalRewardFileCount = folderEntries.Sum(f => Directory.GetFiles(f.Key, "*", SearchOption.AllDirectories).Length);

                string selfTestError = RunSelfTest(savePath, originalRewardFileCount, out int extractedRewardCount);
                if (selfTestError != null)
                {
                    MessageBox.Show(this,
                        "パックは出力されましたが、セルフテストに失敗しました。\n" + selfTestError,
                        "Anasoko Pack Maker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(this,
                    "パックを出力しました（セルフテストOK・段位ハッシュ一致・ごほうび" + extractedRewardCount + "件）\n" + savePath,
                    "Anasoko Pack Maker", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "パックの出力に失敗しました。\n" + ex.Message, "Anasoko Pack Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // 出力直後のセルフテスト:
        //   1. manifestを読み直す
        //   2. 一時Daniフォルダへ段位道場を導入し、導入後のdani.jsonから dan_hash を再計算して manifest と一致するか確認
        //   3. 一時Songsフォルダへごほうび曲を復号展開し、ファイル数が元と一致するか確認
        private string RunSelfTest(string packPath, int originalRewardFileCount, out int extractedRewardCount)
        {
            extractedRewardCount = 0;
            string tempRoot = Path.Combine(Path.GetTempPath(), "AnasPackMaker_SelfTest_" + Guid.NewGuid().ToString("N"));
            string tempDaniRoot = Path.Combine(tempRoot, "Dani");
            string tempSongsRoot = Path.Combine(tempRoot, "Songs");

            try
            {
                PackManifest reloaded = AnasPackFile.ReadManifest(packPath);
                if (reloaded == null) return "manifest.json の再読込に失敗しました。";

                Directory.CreateDirectory(tempDaniRoot);
                string installedDanDir = AnasPackFile.InstallDan(packPath, tempDaniRoot);

                DanDefinition recomputed;
                try
                {
                    recomputed = new DanDefinition(installedDanDir);
                }
                catch (Exception ex)
                {
                    return "導入した段位道場からの dan_hash 再計算に失敗しました。\n" + ex.Message;
                }

                if (recomputed.DanHash != reloaded.dan_hash)
                {
                    return string.Format("段位ハッシュが一致しません（manifest:{0} / 再計算:{1}）。",
                        reloaded.dan_hash, recomputed.DanHash);
                }

                Directory.CreateDirectory(tempSongsRoot);
                List<string> extracted = AnasPackFile.ExtractReward(packPath, tempSongsRoot);
                extractedRewardCount = extracted.Count;

                if (extractedRewardCount != originalRewardFileCount)
                {
                    return string.Format("ごほうび曲の展開後ファイル数が一致しません（元:{0}件 / 展開後:{1}件）。",
                        originalRewardFileCount, extractedRewardCount);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
                }
                catch
                {
                    // 一時フォルダの削除失敗はセルフテスト結果には影響させない
                }
            }
        }

        // ファイル名に使えない文字（\ / : * ? " < > | と制御文字）を除去する。空になったら "pack"
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "pack";

            var sb = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (c == '\\' || c == '/' || c == ':' || c == '*' || c == '?' || c == '"' || c == '<' || c == '>' || c == '|')
                {
                    continue;
                }
                if (char.IsControl(c))
                {
                    continue;
                }
                sb.Append(c);
            }

            string result = sb.ToString().Trim();
            return result.Length == 0 ? "pack" : result;
        }
    }
}
