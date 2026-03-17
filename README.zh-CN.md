# 20min20s

基于 20-20-20 规则的 Windows 护眼提醒工具。

`20min20s` 只统计有效使用时长，不是单纯按开机或前台运行时间累加。如果一段时间内没有真实键盘或鼠标输入，计时会暂停；如果提醒触发时你正在全屏应用里，比如游戏、视频或演示，提醒会延后到退出全屏后再显示。

[English](./README.md)

## 当前功能

- 只统计有效使用时长，不把挂机时间算进去
- 支持按无输入时长自动暂停计时
- 支持全屏应用和指定前台进程下延后提醒
- 默认在有效使用 20 分钟后提醒远眺 20 秒
- 包含托盘状态、设置界面、统计视图和日志

## 项目结构

- `windows/20min20s`: 主 WPF 应用
- `windows/Project1.UI`: 共享 UI 组件库

`referrence/` 和 `docs/` 不会作为正式源码的一部分对外维护。`referrence/` 是迁移过程中参考用的目录，`docs/` 主要保留本地说明和发布文档。

## 构建

环境要求：

- Windows
- .NET SDK 8.x
- .NET Framework 4.8 targeting pack，或者 `Microsoft.NETFramework.ReferenceAssemblies.net48`

推荐直接运行：

```powershell
pwsh -File .\windows\build-20min20s.ps1
```

这个脚本现在默认执行 `Release` 构建，并刷新 `dist/` 下的分发产物。

如果你只想手动编译：

```powershell
dotnet msbuild .\windows\20min20s.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU"
```

Release：

```powershell
dotnet msbuild .\windows\20min20s.sln /t:Build /p:Configuration=Release /p:Platform="Any CPU"
```

主要输出位置：

- `windows/20min20s/bin/Debug/20min20s.exe`
- `windows/20min20s/bin/Release/20min20s.exe`
- `dist/20min20s.exe`
- `dist/20min20s-windows-1.4.3.zip`

## 更新与分发

应用内更新会读取：

- `https://github.com/zhangjoe120246-bot/20mins20s/releases/latest`

推荐上传到 GitHub Release 的资源文件名格式：

- `20min20s-windows-<version>.zip`

不要只单独分发一个 `20min20s.exe`。程序运行依赖同目录下的 DLL 和其他运行时文件，完整压缩包才是正确的发布形式。

## Release 发布

执行：

```powershell
pwsh -File .\windows\build-20min20s.ps1
```

之后会刷新：

- `windows/20min20s/bin/Release/`
- `dist/20min20s.exe`
- `dist/20min20s-windows-1.4.3.zip`

GitHub Release 的具体发布步骤见：

- [RELEASE.md](./RELEASE.md)

这个仓库来源于更早期的 `ProjectEye` 迁移和整理工作，但现在维护、构建和发布的正式项目是 `20min20s`。
