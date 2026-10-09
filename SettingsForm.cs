using System.ComponentModel;

namespace Mickey;

public sealed class SettingsForm : Form
{
    private readonly TextBox _hotkeyBox;
    private readonly Label _hint;
    private readonly CheckBox _autoStartBox;
    private readonly CheckBox _overlayBox;
    private readonly ComboBox _overlayPosCombo;

    private uint _mods;
    private uint _vk;
    private string _comboText = "";

    public AppSettings NewSettings { get; private set; } = new();

    public SettingsForm(AppSettings current)
    {
        Text = "Mickey 设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(432, 206);

        var title = new Label { Text = "全局快捷键", Location = new Point(16, 16), AutoSize = true };

        _hotkeyBox = new TextBox
        {
            Location = new Point(16, 38),
            Size = new Size(244, 25),
            ReadOnly = true,
            Text = current.HotkeyVirtualKey == 0 ? "(未设置)" : current.HotkeyText,
        };
        _mods = current.HotkeyModifiers & 0xF;
        _vk = current.HotkeyVirtualKey;
        _comboText = current.HotkeyText;
        _hotkeyBox.KeyDown += OnHotkeyKeyDown;

        _hint = new Label
        {
            Text = "点击输入框后按下组合键，如 Ctrl + Alt + M。\n需包含 Ctrl / Alt / Win，或直接使用 F1~F12。",
            Location = new Point(16, 70),
            Size = new Size(400, 36),
            ForeColor = Color.Gray,
        };

        var clearBtn = new Button { Text = "清除", Location = new Point(268, 37), Size = new Size(72, 25) };
        clearBtn.Click += (_, _) =>
        {
            _vk = 0;
            _mods = 0;
            _comboText = "";
            _hotkeyBox.Text = "(未设置)";
            _hint.Text = "";
        };

        var defaultBtn = new Button { Text = "恢复默认", Location = new Point(346, 37), Size = new Size(70, 25) };
        defaultBtn.Click += (_, _) =>
        {
            _mods = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;
            _vk = (uint)Keys.M;
            _comboText = "Ctrl + Alt + M";
            _hotkeyBox.Text = _comboText;
            _hint.Text = "";
        };

        _autoStartBox = new CheckBox
        {
            Text = "开机自动启动",
            Location = new Point(16, 114),
            AutoSize = true,
            Checked = current.AutoStart,
        };

        _overlayBox = new CheckBox
        {
            Text = "屏幕悬浮显示",
            Location = new Point(16, 144),
            AutoSize = true,
            Checked = current.OverlayEnabled,
        };

        var posLabel = new Label { Text = "显示位置", Location = new Point(140, 146), AutoSize = true };
        _overlayPosCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(196, 142),
            Size = new Size(100, 25),
        };
        _overlayPosCombo.Items.AddRange(OverlayForm.PresetNames);
        var posIndex = Array.IndexOf(OverlayForm.PresetKeys, current.OverlayPosition);
        _overlayPosCombo.SelectedIndex = posIndex < 0 ? 1 : posIndex;
        _overlayPosCombo.Enabled = _overlayBox.Checked;
        _overlayBox.CheckedChanged += (_, _) => _overlayPosCombo.Enabled = _overlayBox.Checked;

        var saveBtn = new Button { Text = "保存", Location = new Point(250, 176), Size = new Size(80, 30) };
        saveBtn.Click += OnSave;

        var cancelBtn = new Button { Text = "取消", Location = new Point(336, 176), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };

        Controls.AddRange(new Control[] { title, _hotkeyBox, clearBtn, defaultBtn, _hint, _autoStartBox, _overlayBox, posLabel, _overlayPosCombo, saveBtn, cancelBtn });
        AcceptButton = saveBtn;
        CancelButton = cancelBtn;
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        var key = e.KeyCode;
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.None)
            return;

        if ((uint)key > 0xFF)
        {
            _hint.Text = "不支持该按键。";
            return;
        }

        uint mods = 0;
        if (e.Control) mods |= NativeMethods.MOD_CONTROL;
        if (e.Shift) mods |= NativeMethods.MOD_SHIFT;
        if (e.Alt) mods |= NativeMethods.MOD_ALT;
        if ((NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
            (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0)
            mods |= NativeMethods.MOD_WIN;

        bool isFnKey = key is >= Keys.F1 and <= Keys.F24;
        if (mods == 0 && !isFnKey)
        {
            _hint.Text = "组合键需包含 Ctrl / Alt / Win，或使用 F1~F12。";
            return;
        }

        _mods = mods;
        _vk = (uint)key;
        _comboText = Describe(mods, (int)key);
        _hotkeyBox.Text = _comboText;
        _hint.Text = "";
    }

    private static string Describe(uint mods, int vk)
    {
        var parts = new List<string>();
        if ((mods & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((mods & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((mods & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
        if ((mods & NativeMethods.MOD_WIN) != 0) parts.Add("Win");

        var key = (Keys)vk;
        var converter = new KeysConverter();
        parts.Add(converter.ConvertToString(key) ?? key.ToString());
        return string.Join(" + ", parts);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        var posIndex = Math.Clamp(_overlayPosCombo.SelectedIndex, 0, OverlayForm.PresetKeys.Length - 1);
        NewSettings = new AppSettings
        {
            HotkeyModifiers = _vk == 0 ? 0 : _mods,
            HotkeyVirtualKey = _vk,
            HotkeyText = _vk == 0 ? "" : _comboText,
            AutoStart = _autoStartBox.Checked,
            OverlayEnabled = _overlayBox.Checked,
            OverlayPosition = OverlayForm.PresetKeys[posIndex],
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}
