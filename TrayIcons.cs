using System.Drawing.Drawing2D;

namespace Mickey;

/// <summary>运行时绘制托盘图标：绿色边框 = 开启，红色边框 + 斜杠 = 已静音。</summary>
public static class TrayIcons
{
    public static Icon Create(bool muted)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var accent = muted ? Color.FromArgb(235, 87, 87) : Color.FromArgb(76, 217, 100);
            var micColor = muted ? Color.FromArgb(185, 185, 192) : Color.White;

            using (var bg = new SolidBrush(Color.FromArgb(38, 38, 46)))
                g.FillEllipse(bg, 0, 0, 31, 31);
            using (var border = new Pen(accent, 2.5f))
                g.DrawEllipse(border, 1.5f, 1.5f, 28, 28);

            using var micBrush = new SolidBrush(micColor);
            using var micPen = new Pen(micColor, 2.2f);

            // 麦克风拾音头（胶囊形）
            using var capsule = new GraphicsPath();
            capsule.AddArc(12f, 5f, 8f, 10f, 180f, 180f);
            capsule.AddArc(12f, 10f, 8f, 10f, 0f, 180f);
            capsule.CloseFigure();
            g.FillPath(micBrush, capsule);

            // 支架、弧形托架与底座
            g.DrawLine(micPen, 16f, 15f, 16f, 21f);
            g.DrawArc(micPen, 10f, 11f, 12f, 12f, 15f, 150f);
            g.DrawLine(micPen, 11f, 24f, 21f, 24f);

            // 静音斜杠
            if (muted)
            {
                using var slash = new Pen(accent, 3f);
                g.DrawLine(slash, 7f, 25f, 25f, 7f);
            }
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }
}
