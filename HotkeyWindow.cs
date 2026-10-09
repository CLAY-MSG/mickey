namespace Mickey;

/// <summary>接收 WM_HOTKEY 消息的隐藏窗口句柄。</summary>
public sealed class HotkeyWindow : NativeWindow, IDisposable
{
    public event Action? HotkeyPressed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY)
        {
            HotkeyPressed?.Invoke();
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose() => DestroyHandle();
}
