using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Anasoko_Hiroba
{
    // モニター中であることを示す、半透明の赤丸オーバーレイ（位置は手動指定可能）
    public class IndicatorForm : Form
    {
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // 位置指定モード中かどうか（true の間はドラッグでの移動を受け付ける）
        public bool PositioningMode { get; private set; }

        private bool dragging;
        private Point dragStart;

        private static readonly Size IndicatorSize = new Size(36, 36);

        public IndicatorForm()
        {
            // デザイナーを使わない素のフォームだと、フォント基準の自動スケーリングが縦横不均等にかかり
            // 正方形のはずのウィンドウが楕円に見えるほど歪むことがあるため、明示的に無効化する
            AutoScaleMode = AutoScaleMode.None;

            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = IndicatorSize;

            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            Opacity = 0.65; // 若干透過させる

            // Anasokoウィンドウが見つかるまでの仮位置（画面右下隅）
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - 60, area.Bottom - 60);

            MouseDown += IndicatorForm_MouseDown;
            MouseMove += IndicatorForm_MouseMove;
            MouseUp += IndicatorForm_MouseUp;
        }

        // 何らかの理由でサイズが歪んだ場合の保険として、正方形に戻す
        public void EnsureSquareSize()
        {
            if (Size != IndicatorSize) Size = IndicatorSize;
        }

        // 指定したウィンドウ範囲の右下隅付近に移動する（自動位置。手動指定時は使わない）
        public void PositionAt(Rectangle windowRect)
        {
            EnsureSquareSize();
            Location = new Point(windowRect.Right - 60, windowRect.Bottom - 60);
        }

        // 位置指定モードに入る：クリック透過を解除してドラッグ移動できるようにする
        public void EnablePositioning()
        {
            PositioningMode = true;
            SetClickThrough(false);
            Opacity = 0.9;
            Invalidate();
        }

        // 位置指定モードを終了し、通常のクリック透過表示に戻す
        public void DisablePositioning()
        {
            PositioningMode = false;
            SetClickThrough(true);
            Opacity = 0.65;
            Invalidate();
        }

        private void SetClickThrough(bool enabled)
        {
            if (!IsHandleCreated) return;
            int style = GetWindowLong(Handle, GWL_EXSTYLE);
            style = enabled ? (style | WS_EX_TRANSPARENT) : (style & ~WS_EX_TRANSPARENT);
            SetWindowLong(Handle, GWL_EXSTYLE, style);
        }

        private void IndicatorForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (!PositioningMode) return;
            dragging = true;
            dragStart = e.Location;
        }

        private void IndicatorForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y);
        }

        private void IndicatorForm_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        // マウスクリックを下のウィンドウへ透過させる（操作の邪魔をしない。位置指定モード中は解除される）
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // TransparencyKey（色抜き）方式のため AntiAlias は使わない（縁がマゼンタ色でにじんで見えるのを防ぐ）
            using (var brush = new SolidBrush(Color.Red))
            {
                e.Graphics.FillEllipse(brush, 3, 3, Width - 6, Height - 6);
            }
            using (var borderPen = new Pen(Color.Black, 2))
            {
                e.Graphics.DrawEllipse(borderPen, 3, 3, Width - 6, Height - 6);
            }

            if (PositioningMode)
            {
                using (var pen = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawEllipse(pen, 1, 1, Width - 3, Height - 3);
                }
            }
        }
    }
}
