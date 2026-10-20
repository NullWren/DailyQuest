# WrenFeed

A small, offline Windows quest log for daily and weekly games. Launch **WrenFeed** from the desktop shortcut. No login, internet connection, server, browser, package manager, or subscription is involved.

## Using it

- **+ Game** adds a quest, a short description, and any number of extra lines. Enable **Checkbox** on a line to make it an individual task; leave it off for a plain note. Blank lines are ignored when saving.
- Set the optional **Game group** while editing to show related quests in one panel. For example, set both Tears of Guthix and Kingdom check-in to **OSRS**. The group heading has no checkbox; each quest keeps its own completion, reset rule, and history. Existing entries are ungrouped until you edit them. Use **+ Quest** in a group to add another quest there.
- Check a quest to complete all its tasks. Completing every checkable task automatically completes the quest. Unchecking the quest clears its tasks; unchecking one task reopens the quest.
- The **···** menu edits, reorders, or archives a quest. Archiving keeps its history. Restore archived quests in Settings.
- **History** shows a read-only snapshot for a date. Past game names, descriptions, task text, and results stay intact when you change today's list. Today is updated as you edit it.
- **Weekly** has its own game list and recent completion history. Each weekly game can reset on a fixed UTC weekday and time, or at **00:00 UTC on the seventh day after its last completion**. The second option suits Tears of Guthix: a completion on Sunday UTC becomes available the following Sunday at 00:00 UTC. The tab shows the next reset in your local time after completion.
- Daily quests can reset at **local midnight** (the default), at a chosen **UTC time**, or a chosen **number of hours after completion** (24 hours by default). This allows game tasks with server-wide resets or rolling timers to share a group. For Forma, choose 23 hours and complete the quest when you start the next build. The window detects resets while open. If the app is closed, the next launch creates the current cycle. Unopened cycles have no recorded result and are never assumed complete.
- Settings includes **always on top**, completion animations, and backup export/restore. Backups include daily and weekly games and their history. Restoring a backup replaces the journal, after confirmation, and preserves a separate copy of the journal being replaced.
- Resize the window to suit your list. Closing the window exits the app; it does not keep running in the tray. Window size is remembered.
- Keyboard: **Ctrl+N** adds a game, **Ctrl+H** toggles History, **Ctrl+,** opens Settings, **Esc** closes a dialog.

## Local files

Installed executable: `%LOCALAPPDATA%\Programs\WrenFeed\WrenFeed.exe`

Data: `%LOCALAPPDATA%\DailyQuest\journal.json` (kept at the original path so existing progress carries into WrenFeed)

The app writes after each change using an atomic replacement and retains the previous save as `journal.backup.json`. A file lock prevents two instances from writing the same journal. A syntactically unreadable journal can be recovered from the backup, preserving the unreadable original. Unknown schema versions and structurally invalid data are refused without silently resetting your journal. Use **Export backup** for additional copies; the automatic backup is only one save behind, not a long-term backup archive.

These local app/data paths are outside this source folder, which is under OneDrive. The app itself makes no network calls. Exported files can be placed wherever you choose, including a synced folder if desired.

## Development

- `src/Model.cs`: plain data types, checklist rules, dated snapshots, per-quest schedules, and storage. Stable quest/line IDs and UTC completion timestamps support future streak calculations. Local daily day keys remain local calendar dates; UTC and rolling daily cycles, as well as weekly cycles, have independent records. Journals from versions 1–3 migrate to version 4 on load.
- `src/App.cs`: WPF windows and interactions. No third-party runtime packages.
- `src/Theme.xaml`: colors, fonts, and reusable control styles. One quest-log preset ships initially. Additional skins can use separate resource dictionaries without changing stored completion history.
- `src/app-icon.png` is the transparent cyber-wren and bird-feeder artwork for the shortcut icon. `src/app.ico` contains Windows icon sizes and is embedded in the executable.
- `tests/ModelTests.cs`: isolated model/storage tests; generates a separate UI fixture with example games.

Build with `powershell -ExecutionPolicy Bypass -File .\build.ps1` from this directory. It uses the .NET Framework compiler already included with this Windows installation. Run model/storage tests with `powershell -ExecutionPolicy Bypass -File .\test.ps1` and in-process WPF editor tests with `powershell -ExecutionPolicy Bypass -File .\test-editor.ps1`. No downloads are required.

Run `install.ps1` to copy the executable into the per-user Programs folder and create the desktop shortcut. No administrator privileges are required. Close WrenFeed before updating it. The source and build output remain in this project folder.

The current shareable Windows package is in `release/WrenFeed.zip`. It contains only the app, a double-click launcher, and a friend-friendly README. Rebuild it with `package.ps1` after source changes.

GitHub Actions runs the Windows build and both test suites on pushes and pull requests, then saves a fresh ZIP as a downloadable workflow artifact. It does not sign or publish a release automatically.

For isolated testing: `WrenFeed.exe --data-dir "C:\path\to\test-folder"`. The desktop shortcut uses the normal local data folder and starts with no sample games.

## Future additions

Streak displays, additional preset skins, notifications, and syncing are not implemented. History is recorded at game and optional subtask level, with stable IDs and a versioned JSON schema, so these features have room to grow. Editing historical completions is unavailable.
