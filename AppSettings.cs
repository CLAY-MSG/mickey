using System.Text.Json;

namespace Mickey;

public sealed class AppSettings
{
    public uint HotkeyModifiers { get; set; } = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;
    public uint HotkeyVirtualKey { get; set; } = 0x4D; // M
    public string HotkeyText { get; set; } = "Ctrl + Alt + M";
    public bool AutoStart { get; set; }
    public bool OverlayEnabled { get; set; } = true;
    public string OverlayPosition { get; set; } = "TopCenter";

    private static string DirPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mickey");

    public static string FilePath => Path.Combine(DirPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (settings != null) return settings;
            }
        }
        catch
        {
            // 设置文件损坏时使用默认值
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DirPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // 忽略保存失败
        }
    }
}
