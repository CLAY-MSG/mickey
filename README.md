# Mickey

<div align="center">

轻量的 Windows 麦克风开关托盘工具

一键静音/开启默认麦克风，托盘图标实时反映状态，支持全局快捷键、开机自启与屏幕悬浮指示，对无边框全屏游戏的帧率影响可忽略。

[![.NET](https://img.shields.io/badge/.NET-7.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows&logoColor=white)](#)
[![C#](https://img.shields.io/badge/C%23-9.0-239120?logo=csharp&logoColor=white)](#)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](#license)

</div>

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [Build from Source](#build-from-source)
- [Project Structure](#project-structure)
- [Under the Hood](#under-the-hood)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [License](#license)

## Features

- **麦克风开关** — 通过 Windows Core Audio API（`IAudioEndpointVolume`）静音/开启系统默认麦克风设备
- **托盘状态图标** — 绿色边框 = 开启，红色边框带斜杠 = 已静音；每 2 秒轮询同步外部静音变化
- **全局快捷键** — 默认 `Ctrl + Alt + M`，设置窗口内自定义录制（支持 Ctrl / Alt / Win / Shift 或 F1~F12）
- **开机自启** — 写入 HKCU 注册表 Run 键，无需管理员权限
- **屏幕悬浮指示** — 点击穿透的置顶圆点，绿色 = 开启，红色 = 已静音
  - 四角：紧贴屏幕角，四分之一圆（30×30，放大 1.5 倍）
  - 顶部居中：贴顶边半圆（20×20）
  - 仅在状态变化时重绘一次，空闲时零 CPU/GPU 开销
- **单实例运行** — 互斥锁防止重复启动

## Screenshots

> 待补充：托盘图标、悬浮圆点、设置窗口截图

## Getting Started

### Prerequisites

- Windows 10 / 11
- [.NET 7 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/7.0)（仅依赖框架模式需要）

### Run

从 [Releases](https://github.com/your-name/mickey/releases) 下载 `Mickey.exe`，双击运行即可。

## Usage

1. 左键点击托盘图标 → 切换麦克风开关
2. 右键托盘图标 → 打开菜单：
   - 开启/关闭麦克风
   - 开机自启
   - 屏幕悬浮显示
   - 设置（快捷键 / 自启 / 悬浮位置）
   - 退出

## Build from Source

```bash
git clone https://github.com/your-name/mickey.git
cd mickey
```

```powershell
# 构建
dotnet build Mickey/Mickey.csproj -c Release

# 发布单文件 exe（依赖 .NET 7 Runtime，约 180 KB）
dotnet publish Mickey/Mickey.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist

# 发布自包含 exe（无需 Runtime，约 150 MB）
dotnet publish Mickey/Mickey.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
```

## Project Structure

| File | Description |
|------|-------------|
| `Program.cs` | 入口，单实例互斥，启动 `TrayAppContext` |
| `TrayAppContext.cs` | 托盘图标、右键菜单、轮询同步、快捷键调度、悬浮窗管理 |
| `MicController.cs` | Core Audio COM 互操作，获取/设置/切换麦克风静音 |
| `OverlayForm.cs` | 屏幕悬浮指示窗（`WS_EX_LAYERED` + `UpdateLayeredWindow`） |
| `SettingsForm.cs` | 设置窗口（快捷键录制、自启、悬浮开关与位置） |
| `TrayIcons.cs` | 运行时绘制托盘状态图标 |
| `HotkeyWindow.cs` | 接收 `WM_HOTKEY` 的隐藏窗口 |
| `AutoStartManager.cs` | HKCU 注册表 Run 键管理 |
| `AppSettings.cs` | 设置持久化（`%APPDATA%\Mickey\settings.json`） |
| `NativeMethods.cs` | P/Invoke 声明（全局快捷键、分层窗口、GDI） |

## Under the Hood

### 麦克风控制

使用 Windows Core Audio API 的 `IAudioEndpointVolume` 接口操作默认捕获设备（`eCapture` / `eConsole`）的静音属性，无需第三方音频库。

### 悬浮窗性能

采用 `WS_EX_LAYERED` + `UpdateLayeredWindow` 方案，对无边框全屏游戏帧率影响最小：

- 状态不变时无重绘，CPU/GPU 占用为 0
- DWM 仅合成 20×20 或 30×30 的小表面
- `WS_EX_TRANSPARENT` — 鼠标点击完全穿透
- `WS_EX_NOACTIVATE` + `WS_EX_TOOLWINDOW` — 永不抢焦点，不进 Alt-Tab / 任务栏

### 快捷键

`RegisterHotKey` 注册系统级全局热键，`MOD_NOREPEAT` 避免长按重复触发。

## Configuration

设置文件：`%APPDATA%\Mickey\settings.json`

```json
{
  "HotkeyModifiers": 3,
  "HotkeyVirtualKey": 77,
  "HotkeyText": "Ctrl + Alt + M",
  "AutoStart": false,
  "OverlayEnabled": true,
  "OverlayPosition": "TopCenter"
}
```

| Field | Description |
|-------|-------------|
| `OverlayPosition` | `TopLeft` / `TopCenter` / `TopRight` / `BottomLeft` / `BottomRight` |

## Contributing

Issues 和 Pull Requests 欢迎。

## License

This project is licensed under the MIT License.

