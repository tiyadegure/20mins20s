# 20min20s

Windows-only eye-break reminder app based on the 20-20-20 rule.

[简体中文](./README.zh-CN.md)

`20min20s` accumulates only effective usage time. If there is no real keyboard or mouse input for a while, the timer pauses. When a break becomes due during fullscreen apps such as games or videos, the reminder is deferred and shown after fullscreen exits.

## Features

- Counts only active use, not just uptime
- Pauses timing after configurable inactivity
- Defers reminders during fullscreen or configured foreground processes
- Shows a 20-second break reminder after 20 minutes of effective use
- Uses a softer centered break overlay instead of a harsh full-white screen
- Includes tray status, settings, statistics, and logging

## Download

Download the latest packaged Windows build from:

- [GitHub Releases](https://github.com/tiyadegure/20mins20s/releases/latest)

Use the full zip package. Do not download only the standalone `20min20s.exe`, because the app depends on adjacent runtime files and DLLs.

## How It Works

- The timer counts only real usage time.
- If there is no keyboard or mouse input for a while, timing pauses automatically.
- If a reminder becomes due while you are in fullscreen, it is deferred and shown after fullscreen exits.
- The break screen uses a lower-glare centered card layout to stay visible without flashing a full white screen.
- The tray icon reflects the current state so you can see whether the app is active, paused, suspended, or waiting to remind you.

## Run

1. Download the latest release zip.
2. Extract it to any folder.
3. Run `20min20s.exe`.
4. The app stays in the system tray after startup.

## Notes

- This project is Windows-only.
- The current repository and release channel are `20min20s`.
