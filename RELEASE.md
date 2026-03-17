# Releasing 20min20s

## Goal

Publish a complete Windows release package that works with the in-app updater and can be downloaded directly from GitHub Releases.

## Build The Release Package

From the repository root:

```powershell
pwsh -File .\windows\build-20min20s.ps1
```

This updates:

- `windows/20min20s/bin/Release/`
- `dist/20min20s.exe`
- `dist/20min20s-windows-1.4.3.zip`

## What To Upload

Upload the zip package, not just the standalone exe:

- `dist/20min20s-windows-1.4.3.zip`

Reason:

- `20min20s.exe` depends on adjacent DLLs and runtime files
- the updater expects a full package release, not a single executable

## Suggested GitHub Release Steps

1. Make sure `main` is pushed and clean.
2. Run the packaging script.
3. Verify the timestamp of `dist/20min20s-windows-1.4.3.zip`.
4. Create a Git tag for the release version.
5. Draft a GitHub Release from that tag.
6. Upload the zip from `dist/`.
7. Publish the release.

## Suggested Release Title

- `20min20s 1.4.3`

## Suggested Asset Name

- `20min20s-windows-1.4.3.zip`

## Verification Checklist

- The app starts from the extracted zip folder.
- Tray icon and tray menu reflect the latest UI.
- Fullscreen deferred reminder still works.
- `Release` build uses the real configured reminder interval, not the 30-second debug shortcut.
- In-app update points to the latest GitHub release.
