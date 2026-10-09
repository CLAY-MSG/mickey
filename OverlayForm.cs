using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Mickey;

/// <summary>
/// 点击穿透的置顶悬浮指示点（WS_EX_LAYERED + UpdateLayeredWindow）。
/// 四角位置：紧贴屏幕角，显示四分之一圆；顶部居中：贴顶边显示半圆。
/// 仅在麦克风状态变化时重绘一次，空闲时零 CPU/GPU 渲染开销，对游戏帧率影响可忽略。
/// </summary>
public sealed class OverlayForm : Form
{
    public static readonly string[] PresetKeys = { "TopLeft", "TopCenter", "TopRight", "BottomLeft", "BottomRight" };
    public static readonly string[] PresetNames = { "左上", "顶部居中", "右上", "左下", "右下" };

    private bool _muted;
    private string _presetKey = "TopCenter";
    private float _radius = 9f;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Size = new Size(20, 20);
        BackColor = Color.FromArgb(16, 16, 20);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED
                | NativeMethods.WS_EX_TRANSPARENT
                | NativeMethods.WS_EX_TOOLWINDOW
                | NativeMethods.WS_EX_NOACTIVATE
                | NativeMethods.WS_EX_TOPMOST;
            return cp;
        }
    }

    public void ApplyPosition(string presetKey)
    {
        _presetKey = presetKey;
        bool corner = presetKey != "TopCenter";
        _radius = corner ? 9f * 1.5f : 9f; // 四角放大 1.5 倍
        Size = corner ? new Size(30, 30) : new Size(20, 20);

        var wa = Screen.PrimaryScreen!.WorkingArea;
        Location = presetKey switch
        {
            "TopLeft" => new Point(wa.Left, wa.Top),
            "TopRight" => new Point(wa.Right - Width, wa.Top),
            "BottomLeft" => new Point(wa.Left, wa.Bottom - Height),
            "BottomRight" => new Point(wa.Right - Width, wa.Bottom - Height),
            _ => new Point(wa.Left + (wa.Width - Width) / 2, wa.Top), // TopCenter
        };
    }

    /// <summary>更新状态并重绘一次（仅状态变化时调用）。</summary>
    public void UpdateState(bool muted)
    {
        _muted = muted;
        Render();
    }

    private void Render()
    {
        int w = Width, h = Height;
        using var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var accent = _muted ? Color.FromArgb(235, 87, 87) : Color.FromArgb(76, 217, 100);
            float r = _radius;

            // 圆心位于贴合屏幕边缘/角落的一侧，位图边界外的部分自然裁掉：
            // 四角 = 四分之一圆，顶部居中 = 半圆
            var (cx, cy) = _presetKey switch
            {
                "TopLeft" => (0f, 0f),
                "TopRight" => ((float)w, 0f),
                "BottomLeft" => (0f, (float)h),
                "BottomRight" => ((float)w, (float)h),
                _ => (w / 2f, 0f), // TopCenter
            };
            var rect = new RectangleF(cx - r, cy - r, r * 2, r * 2);

            using (var dot = new SolidBrush(accent))
                g.FillEllipse(dot, rect);

            // 深色描边，保证在任意游戏背景下可见（贴边一侧被位图边界裁掉，不会画出直线）
            using var ring = new Pen(Color.FromArgb(200, 16, 16, 20), 2f);
            g.DrawEllipse(ring, rect);
        }

        SetBitmap(bitmap);
    }

    private void SetBitmap(Bitmap bitmap)
    {
        IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;
        try
        {
            hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
            oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

            var size = new Size(bitmap.Width, bitmap.Height);
            var source = new Point(0, 0);
            var topPos = new Point(Left, Top);
            var blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA,
            };

            NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
            {
                NativeMethods.SelectObject(memDc, oldBitmap);
                NativeMethods.DeleteObject(hBitmap);
            }
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            NativeMethods.DeleteDC(memDc);
        }
    }
}
