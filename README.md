# Daily Quest

A small, offline Windows quest log for daily and weekly games. Launch **Daily Quest** from the desktop shortcut. No login, internet connection, server, browser, package manager, or subscription is involved.

## Using it

- **+ Game** adds a game, a short description, and any number of extra lines. Enable **Checkbox** on a line to make it an individual task; leave it off for a plain note. Blank lines are ignored when saving.
- Check the game to complete all its tasks. Completing every checkable task automatically completes the game. Unchecking the game clears its tasks; unchecking one task reopens the game.
- The **···** menu edits, reorders, or archives a game. Archiving keeps its history. Restore archived games in Settings.
- **History** shows a read-only snapshot for a date. Past game names, descriptions, task text, and results stay intact when you change today's list. Today is updated as you edit it.
- **Weekly** has its own game list and recent completion history. Each weekly game can reset on a fixed UTC weekday and time, or at **00:00 UTC on the seventh day after its last completion**. The second option suits Tears of Guthix: a completion on Sunday UTC becomes available the following Sunday at 00:00 UTC. The tab shows the next reset in your local time after completion.
- The checklist resets at **local midnight**, including while the window stays open. If the app is closed, the next launch creates the current day's list. Unopened days have no recorded result and are never assumed complete. The app uses your Windows local date/time.
- Settings includes **always on top**, completion animations, and backup export/restore. Backups include daily and weekly games and their history. Restoring a backup replaces the journal, after confirmation, and preserves a separate copy of the journal being replaced.
- Resize the window to suit your list. Closing the window exits the app; it does not keep running in the tray. Window size is remembered.
- Keyboard: **Ctrl+N** adds a game, **Ctrl+H** toggles History, **Ctrl+,** opens Settings, **Esc** closes a dialog.

## Local files

Installed executable: `%LOCALAPPDATA%\Programs\DailyQuest\DailyQuest.exe`

Data: `%LOCALAPPDATA%\DailyQuest\journal.json`

The app writes after each change using an atomic replacement and retains the previous save as `journal.backup.json`. A file lock prevents two instances from writing the same journal. A syntactically unreadable journal can be recovered from the backup, preserving the unreadable original. Unknown schema versions and structurally invalid data are refused without silently resetting your journal. Use **Export backup** for additional copies; the automatic backup is only one save behind, not a long-term backup archive.

These local app/data paths are outside this source folder, which is under OneDrive. The app itself makes no network calls. Exported files can be placed wherever you choose, including a synced folder if desired.

## Development

- `src/Model.cs`: plain data types, checklist rules, dated snapshots, weekly schedules, and storage. Stable game/line IDs and UTC completion timestamps support future streak calculations. Daily day keys remain local calendar dates. Journals from version 1 migrate to version 2 on load.
- `src/App.cs`: WPF windows and interactions. No third-party runtime packages.
- `src/Theme.xaml`: colors, fonts, and reusable control styles. One quest-log preset ships initially. Additional skins can use separate resource dictionaries without changing stored completion history.
- `tests/ModelTests.cs`: isolated model/storage tests; generates a separate UI fixture with example games.

Build with `powershell -ExecutionPolicy Bypass -File .\build.ps1` from this directory. It uses the .NET Framework compiler already included with this Windows installation. Run model/storage tests with `powershell -ExecutionPolicy Bypass -File .\test.ps1` and in-process WPF editor tests with `powershell -ExecutionPolicy Bypass -File .\test-editor.ps1`. No downloads are required.

Run `install.ps1` to copy the executable into the per-user Programs folder and create the desktop shortcut. No administrator privileges are required. Close Daily Quest before updating it. The source and build output remain in this project folder.

The current shareable Windows package is in `release/DailyQuest.zip`. It contains only the app, a double-click launcher, and a friend-friendly README. Rebuild it with `package.ps1` after source changes.

For isolated testing: `DailyQuest.exe --data-dir "C:\path\to\test-folder"`. The desktop shortcut uses the normal local data folder and starts with no sample games.

## Future additions

Streak displays, additional preset skins, notifications, and syncing are not implemented. History is recorded at game and optional subtask level, with stable IDs and a versioned JSON schema, so these features have room to grow. Editing historical completions is unavailable.
