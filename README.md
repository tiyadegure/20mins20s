# 20min20s

Windows-only eye-break reminder app based on the 20-20-20 rule.

[简体中文](./README.zh-CN.md)

`20min20s` accumulates only effective usage time. If there is no real keyboard or mouse input for a while, the timer pauses. When a break becomes due during fullscreen apps such as games or videos, the reminder is deferred and shown after fullscreen exits.

## Current behavior

- Counts only active use, not just uptime
- Pauses timing after configurable inactivity
- Defers reminders during fullscreen or configured foreground processes
- Shows a 20-second break reminder after 20 minutes of effective use
- Includes tray status, settings UI, statistics, and logging

## Project layout

- `windows/20min20s`: main WPF app
- `windows/Project1.UI`: shared UI library

The `referrence/` and `docs/` directories are intentionally excluded from this repository. `referrence/` was used as an external reference during migration, and `docs/` is kept only for local notes.

## Build

Requirements:

- Windows
- .NET SDK 8.x for `dotnet msbuild`
- .NET Framework 4.8 targeting pack or `Microsoft.NETFramework.ReferenceAssemblies.net48`

Build with:

```powershell
pwsh -File .\windows\build-20min20s.ps1
```

The packaging script now defaults to `Release` and refreshes the distributable files in `dist/`.

Or:

```powershell
dotnet msbuild .\windows\20min20s.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU"
```

Release build:

```powershell
dotnet msbuild .\windows\20min20s.sln /t:Build /p:Configuration=Release /p:Platform="Any CPU"
```

Output:

- `windows/20min20s/bin/Debug/20min20s.exe`
- `windows/20min20s/bin/Release/20min20s.exe`
- `dist/20min20s.exe`
- `dist/20min20s-windows-1.4.3.zip`

## Updates

The in-app update check now reads releases from:

- `https://github.com/zhangjoe120246-bot/20mins20s/releases/latest`

Recommended release asset format:

- `20min20s-windows-<version>.zip`

Do not use a standalone `20min20s.exe` downloaded by itself. The app depends on adjacent runtime files and DLLs from the full release package.

This app is derived from an earlier exploratory migration based on `ProjectEye`, but the current repository and release channel are `20min20s`.

## Release

Release builds can be prepared with:

```powershell
pwsh -File .\windows\build-20min20s.ps1
```

This updates:

- `windows/20min20s/bin/Release/`
- `dist/20min20s.exe`
- `dist/20min20s-windows-1.4.3.zip`

GitHub Release publishing notes are documented in [RELEASE.md](./RELEASE.md).
