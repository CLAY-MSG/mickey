namespace Mickey;

public sealed class TrayAppContext : ApplicationContext
{
    private const int HotkeyId = 1;

    private readonly NotifyIcon _tray;
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly Icon _iconOn;
    private readonly Icon _iconOff;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly ToolStripMenuItem _overlayItem;
    private AppSettings _settings;
    private OverlayForm? _overlay;
    private bool _muted;
    private string _deviceName = "麦克风";

    public TrayAppContext()
    {
        _settings = AppSettings.Load();

        _iconOn = TrayIcons.Create(muted: false);
        _iconOff = TrayIcons.Create(muted: true);

        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.HotkeyPressed += ToggleMic;

        try
        {
            _deviceName = MicController.GetDefaultDeviceName();
            _muted = MicController.GetMuted();
        }
        catch
        {
            // 没有麦克风设备时使用默认状态
        }

        _tray = new NotifyIcon
        {
            Icon = _muted ? _iconOff : _iconOn,
            Visible = true,
        };

        var menu = new ContextMenuStrip();

        _toggleItem = new ToolStripMenuItem(_muted ? "开启麦克风" : "关闭麦克风");
        _toggleItem.Font = new Font(_toggleItem.Font, FontStyle.Bold);
        _toggleItem.Click += (_, _) => ToggleMic();

        _autoStartItem = new ToolStripMenuItem("开机自启")
        {
            CheckOnClick = true,
            Checked = AutoStartManager.IsEnabled(),
        };
        _autoStartItem.Click += (_, _) =>
        {
            AutoStartManager.Set(_autoStartItem.Checked);
            _settings.AutoStart = _autoStartItem.Checked;
            _settings.Save();
        };

        _overlayItem = new ToolStripMenuItem("屏幕悬浮显示")
        {
            CheckOnClick = true,
            Checked = _settings.OverlayEnabled,
        };
        _overlayItem.Click += (_, _) =>
        {
            _settings.OverlayEnabled = _overlayItem.Checked;
            _settings.Save();
            SetOverlayVisible(_overlayItem.Checked);
        };

        var settingsItem = new ToolStripMenuItem("设置(&S)...");
        settingsItem.Click += (_, _) => ShowSettings();

        var exitItem = new ToolStripMenuItem("退出(&X)");
        exitItem.Click += (_, _) => ExitApp();

        menu.Items.AddRange(new ToolStripItem[]
        {
            _toggleItem,
            new ToolStripSeparator(),
            _autoStartItem,
            _overlayItem,
            settingsItem,
            new ToolStripSeparator(),
            exitItem,
        });
        menu.Opening += (_, _) => _autoStartItem.Checked = AutoStartManager.IsEnabled();
        _tray.ContextMenuStrip = menu;
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ToggleMic();
        };

        UpdateTray();

        _pollTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _pollTimer.Tick += (_, _) => PollMicState();
        _pollTimer.Start();

        ApplyHotkey(_settings);

        SetOverlayVisible(_settings.OverlayEnabled);
    }

    private void ToggleMic()
    {
        try
        {
            _muted = MicController.Toggle();
            UpdateTray();
            _overlay?.UpdateState(_muted);
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法操作麦克风设备：" + ex.Message,
                "Mickey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PollMicState()
    {
        try
        {
            bool muted = MicController.GetMuted();
            if (muted != _muted)
            {
                _muted = muted;
                UpdateTray();
                _overlay?.UpdateState(_muted);
            }
        }
        catch
        {
            // 设备暂时不可用时保持现状
        }
    }

    private void SetOverlayVisible(bool show)
    {
        if (show)
        {
            if (_overlay == null || _overlay.IsDisposed)
                _overlay = new OverlayForm();

            _overlay.ApplyPosition(_settings.OverlayPosition);
            _ = _overlay.Handle; // 先创建窗口句柄，ULW 在显示前写入内容，避免闪黑块
            _overlay.UpdateState(_muted);
            _overlay.Show();
        }
        else
        {
            _overlay?.Hide();
        }
    }

    private void UpdateTray()
    {
        _tray.Icon = _muted ? _iconOff : _iconOn;
        _toggleItem.Text = _muted ? "开启麦克风" : "关闭麦克风";

        var tip = $"麦克风：{(_muted ? "已静音" : "开启中")} - {_deviceName}";
        if (tip.Length > 63) tip = tip[..63];
        _tray.Text = tip;
    }

    private bool ApplyHotkey(AppSettings settings)
    {
        _ = NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HotkeyId);
        if (settings.HotkeyVirtualKey == 0) return true;
        return NativeMethods.RegisterHotKey(
            _hotkeyWindow.Handle, HotkeyId,
            settings.HotkeyModifiers | NativeMethods.MOD_NOREPEAT,
            settings.HotkeyVirtualKey);
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() != DialogResult.OK) return;

        var next = form.NewSettings;
        var old = _settings;
        if (ApplyHotkey(next))
        {
            _settings = next;
            _settings.Save();
            AutoStartManager.Set(next.AutoStart);
            _autoStartItem.Checked = next.AutoStart;
            _overlayItem.Checked = next.OverlayEnabled;
            SetOverlayVisible(next.OverlayEnabled);
        }
        else
        {
            ApplyHotkey(old);
            MessageBox.Show("快捷键注册失败：该组合键可能已被其他程序占用，请换一个。",
                "Mickey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExitApp()
    {
        _pollTimer.Stop();
        try { NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HotkeyId); } catch { }
        _overlay?.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _iconOn.Dispose();
        _iconOff.Dispose();
        Application.Exit();
    }
}
