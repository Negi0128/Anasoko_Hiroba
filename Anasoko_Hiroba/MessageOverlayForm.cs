using System;
using System.Drawing;
using System.Windows.Forms;

namespace Anasoko_Hiroba
{
    // モニター中インジケーターの横に、解禁メッセージなどを数秒だけ表示するオーバーレイ。
    // クリックはすべて下のウィンドウへ透過し、表示時にフォーカスも奪わない（プレイの邪魔をしない）。
    public class MessageOverlayForm : Form
    {
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private readonly Label label;
        private readonly System.Windows.Forms.Timer hideTimer;

        public MessageOverlayForm()
        {
            // デザイナーを使わない素のフォームなので、フォント基準の自動スケーリングは明示的に切る
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(20, 20, 20);
            Opacity = 0.9; // 若干透過させる

            label = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Yu Gothic UI", 14F, FontStyle.Bold),
                Location = new Point(14, 10),
                MaximumSize = new Size(600, 0), // 長文は折り返す
            };
            Controls.Add(label);

            // 10秒で自動的に消す
            hideTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            hideTimer.Tick += (s, e) => { hideTimer.Stop(); Hide(); };
        }

        // 表示時にウィンドウをアクティブ化しない（ゲーム側のフォーカスを奪わない）
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        // メッセージを表示する。anchorRight は「この点の左側に、垂直中央がこの高さに来る」ように配置する基準点。
        public void ShowMessage(string text, Point anchorRight)
        {
            label.Text = text;

            // ラベルの好みサイズ（折り返し込み）に余白を足してフォームサイズを決める
            Size preferred = label.PreferredSize;
            Size = new Size(preferred.Width + 28, preferred.Height + 20);

            int x = anchorRight.X - Width;
            int y = anchorRight.Y - Height / 2;

            // 画面の外に出ないよう軽くクランプする
            Rectangle wa = Screen.FromPoint(anchorRight).WorkingArea;
            if (x < wa.Left) x = wa.Left;
            if (y < wa.Top) y = wa.Top;
            if (y + Height > wa.Bottom) y = wa.Bottom - Height;
            Location = new Point(x, y);

            hideTimer.Stop();
            if (!Visible) Show();
            else Invalidate();
            hideTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // 金色の枠で「解禁」らしさを出す
            using (var pen = new Pen(Color.FromArgb(241, 196, 15), 2))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}
