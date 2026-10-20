# Files and development

[← Back to the quick start](../README.md)

## Local files and backups

The portable executable is `WrenFeed.exe` in the folder extracted from the ZIP. The optional developer `install.ps1` script copies it to `%LOCALAPPDATA%\Programs\WrenFeed\WrenFeed.exe` and creates a desktop shortcut without administrator privileges.

The journal is stored at `%LOCALAPPDATA%\DailyQuest\journal.json`. This original Daily Quest path is retained so existing progress carries into WrenFeed. It is separate from both the portable ZIP folder and the source repository.

The app writes after each change using an atomic replacement and retains the previous save as `journal.backup.json`. A file lock prevents two instances from writing the same journal. A syntactically unreadable journal can be recovered from the backup, preserving the unreadable original. Unknown schema versions and structurally invalid data are refused without silently resetting the journal. Use **Export backup** for additional copies; the automatic backup is only one save behind, not a long-term archive.

Normal checklist use makes no network calls. Only an explicit leaderboard upload fetches the service address and sends the reviewed public tally and private invite code. The invite code is encrypted separately for the current Windows account and is not included in journal backups.

## Source map

- `src/Model.cs`: plain data types, checklist rules, dated snapshots, per-quest schedules, and storage. Stable quest/line IDs and UTC completion timestamps support future streak calculations. Local daily day keys remain local calendar dates; UTC and rolling daily cycles, as well as weekly cycles, have independent records. Journals from versions 1–3 migrate to version 4 on load.
- `src/App.cs`: WPF windows and interactions. No third-party runtime packages.
- `src/QuestImport.cs`: strict, versioned quest-file parsing, validation, and duplicate planning. `import/` has the template and format guide.
- `src/Theme.xaml`: the original default appearance and reusable control styles. `src/Themes.cs` validates and applies optional declarative theme files; `themes/` has eight presets, previews, a template, and its own README. The small theme mascots are built-in vector drawings, not external downloads.
- `src/Leaderboard.cs`: all-time completion tally, encrypted local invite storage, and opt-in upload. `leaderboard/` contains the optional Cloudflare receiver; `docs/` contains the GitHub Pages site.
- `src/app-icon.png` and `src/app.ico`: cyber-wren and bird-feeder artwork for the embedded Windows shortcut icon.
- `tests/ModelTests.cs`: isolated model/storage tests; it generates a separate UI fixture with example games.

## Build and verify

Run `powershell -ExecutionPolicy Bypass -File .\build.ps1` from the repository root. It uses the .NET Framework compiler included with this Windows installation. Run `test.ps1`, `test-import.ps1`, `test-editor.ps1`, `test-themes.ps1`, and `test-leaderboard.ps1` for model, import, WPF editor, theme/settings, and tally checks. Run `node --test leaderboard/worker/test.mjs` for the separate receiver. Building and using the desktop app need no downloads.

Run `install.ps1` for the per-user install and shortcut. Close WrenFeed before updating it. The source and build output remain in this project folder.

The current shareable Windows package is `release/WrenFeed.zip`. It contains the app, a double-click launcher, a friend-friendly README, the quest-import template and guide, and optional theme files with their own guide. Rebuild it with `package.ps1` after source changes.

GitHub Actions runs the Windows build and tests on pushes and pull requests, then saves a fresh ZIP as a downloadable workflow artifact. It does not sign or publish a release automatically.

For isolated testing: `WrenFeed.exe --data-dir "C:\path\to\test-folder"`. The desktop shortcut uses the normal local data folder and starts with no sample games.

## Room to grow

Streak displays, notifications, automated leaderboard scoring, and syncing are not implemented. History is recorded at game and optional subtask level, with stable IDs and a versioned JSON schema. Editing historical completions is unavailable.
