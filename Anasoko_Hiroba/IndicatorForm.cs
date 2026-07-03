using System;
using System.Drawing;
using System.Windows.Forms;

namespace Anasoko_Hiroba
{
    // モニター中であることを示す、画面右下隅に固定表示される赤丸のオーバーレイ
    public class IndicatorForm : Form
    {
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        public IndicatorForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(28, 28);

            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;

            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - 60, area.Bottom - 60);
        }

        // マウスクリックを下のウィンドウへ透過させる（操作の邪魔をしない）
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
            using (var brush = new SolidBrush(Color.Red))
            {
                e.Graphics.FillEllipse(brush, 4, 4, Width - 8, Height - 8);
            }
        }
    }
}
