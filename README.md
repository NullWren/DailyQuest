# WrenFeed

A small, offline Windows quest log for daily and weekly games. No login, internet connection, server, browser, package manager, or subscription is involved.

## Download and start

1. Download the ready-to-run [WrenFeed.zip](release/WrenFeed.zip). On GitHub, open that file and choose **Download raw file**. The repository's **Code → Download ZIP** contains source code; use the linked WrenFeed ZIP for the app.
2. Right-click the downloaded ZIP and choose **Extract All**. Open the extracted `WrenFeed` folder.
3. Double-click `WrenFeed.exe`. You can keep the extracted folder wherever you like. If Windows shows an unsigned-app warning, choose **More info → Run anyway** if you trust this download.
4. For a desktop shortcut, right-click `WrenFeed.exe` in that folder and choose **Show more options → Send to → Desktop (create shortcut)**. Keep the extracted folder in place so the shortcut continues to work.

The ZIP also contains an `import` template and six optional themes. The app stores your list and history separately in your local app-data folder, so replacing the extracted app folder with a new version will keep your progress. Close WrenFeed before replacing its files.

## Using it

- **+ Game** adds a quest, a short description, and any number of extra lines. Enable **Checkbox** on a line to make it an individual task; leave it off for a plain note. Blank lines are ignored when saving.
- Set the optional **Game group** while editing to show related quests in one panel. For example, set both Tears of Guthix and Kingdom check-in to **OSRS**. The group heading has no checkbox; each quest keeps its own completion, reset rule, and history. Existing entries are ungrouped until you edit them. Use **+ Quest** in a group to add another quest there.
- Check a quest to complete all its tasks. Completing every checkable task automatically completes the quest. Unchecking the quest clears its tasks; unchecking one task reopens the quest.
- The **···** menu edits, reorders, or archives a quest. Archiving keeps its history. Restore archived quests in Settings.
- **History** shows a read-only snapshot for a date. Past game names, descriptions, task text, and results stay intact when you change today's list. Today is updated as you edit it.
- **Weekly** has its own game list and recent completion history. Each weekly game can reset on a fixed UTC weekday and time, or at **00:00 UTC on the seventh day after its last completion**. The second option suits Tears of Guthix: a completion on Sunday UTC becomes available the following Sunday at 00:00 UTC. The tab shows the next reset in your local time after completion.
- Daily quests can reset at **local midnight** (the default), at a chosen **UTC time**, or a chosen **number of hours after completion** (24 hours by default). This allows game tasks with server-wide resets or rolling timers to share a group. For Forma, choose 23 hours and complete the quest when you start the next build. The window detects resets while open. If the app is closed, the next launch creates the current cycle. Unopened cycles have no recorded result and are never assumed complete.
- Settings includes **always on top**, a **20–100% visibility slider**, completion animations, optional themes, and backup export/restore. Choose **Fade everything** for whole-window opacity or **Fade backgrounds only** to keep quest text clear over your game. The second mode gives text a contrast halo over games and reduces the extra tint on quest cards. Small buttons stay solid so they remain easy to use. Settings and other modal dialogs cannot be minimized while they block the quest log. Backups include daily and weekly games and their history. Restoring a backup replaces the journal, after confirmation, and preserves a separate copy of the journal being replaced.
- **Settings → Import quests** adds daily and weekly quests from one JSON file. Start with [the template and its README](import/README.md), which cover every field in the quest editor. Review additions and skipped duplicates before saving. Importing quests leaves existing progress and history intact; restoring a backup remains the full-journal replacement option.
- **Settings → Appearance** keeps the original design as Default. Choose **Install theme file…** to add an optional local theme, then select it in the list. [Six themes, previews, and a custom-theme template](themes/README.md) are supplied in the repo and shareable ZIP. Theme files are stored separately from the journal and never execute code or use the network.
- Drag the WrenFeed title bar to move the window, drag its edges or corners to resize, or use the title-bar minimize and close buttons. Closing exits the app; it does not keep running in the tray. Window size is remembered.
- Keyboard: **Ctrl+N** adds a game, **Ctrl+H** toggles History, **Ctrl+,** opens Settings, **Esc** closes a dialog.

## Local files

Portable executable: `WrenFeed.exe` in the folder extracted from the ZIP.

If you run the developer `install.ps1` script, it installs to `%LOCALAPPDATA%\Programs\WrenFeed\WrenFeed.exe` and creates a desktop shortcut.

Data: `%LOCALAPPDATA%\DailyQuest\journal.json` (kept at the original path so existing progress carries into WrenFeed)

The app writes after each change using an atomic replacement and retains the previous save as `journal.backup.json`. A file lock prevents two instances from writing the same journal. A syntactically unreadable journal can be recovered from the backup, preserving the unreadable original. Unknown schema versions and structurally invalid data are refused without silently resetting your journal. Use **Export backup** for additional copies; the automatic backup is only one save behind, not a long-term backup archive.

The journal path is separate from both the portable ZIP folder and the source repository. The app itself makes no network calls. Exported backups can be placed wherever you choose, including a synced folder if desired.

## Development

- `src/Model.cs`: plain data types, checklist rules, dated snapshots, per-quest schedules, and storage. Stable quest/line IDs and UTC completion timestamps support future streak calculations. Local daily day keys remain local calendar dates; UTC and rolling daily cycles, as well as weekly cycles, have independent records. Journals from versions 1–3 migrate to version 4 on load.
- `src/App.cs`: WPF windows and interactions. No third-party runtime packages.
- `src/QuestImport.cs`: strict, versioned quest-file parsing, validation, and duplicate planning. `import/` has the template and its format guide.
- `src/Theme.xaml`: the original default appearance and reusable control styles. `src/Themes.cs` validates and applies optional declarative theme files; `themes/` has six presets, previews, a template, and its own README. The small theme mascots are built-in vector drawings, not external downloads.
- `src/app-icon.png` is the transparent cyber-wren and bird-feeder artwork for the shortcut icon. `src/app.ico` contains Windows icon sizes and is embedded in the executable.
- `tests/ModelTests.cs`: isolated model/storage tests; generates a separate UI fixture with example games.

Build with `powershell -ExecutionPolicy Bypass -File .\build.ps1` from this directory. It uses the .NET Framework compiler already included with this Windows installation. Run model/storage tests with `powershell -ExecutionPolicy Bypass -File .\test.ps1`, quest-import tests with `powershell -ExecutionPolicy Bypass -File .\test-import.ps1`, in-process WPF editor tests with `powershell -ExecutionPolicy Bypass -File .\test-editor.ps1`, and theme/settings tests with `powershell -ExecutionPolicy Bypass -File .\test-themes.ps1`. No downloads are required.

Run `install.ps1` to copy the executable into the per-user Programs folder and create the desktop shortcut. No administrator privileges are required. Close WrenFeed before updating it. The source and build output remain in this project folder.

The current shareable Windows package is in `release/WrenFeed.zip`. It contains the app, a double-click launcher, a friend-friendly README, the quest-import template and guide, and optional theme files with their own guide. Rebuild it with `package.ps1` after source changes.

GitHub Actions runs the Windows build and all four test suites on pushes and pull requests, then saves a fresh ZIP as a downloadable workflow artifact. It does not sign or publish a release automatically.

For isolated testing: `WrenFeed.exe --data-dir "C:\path\to\test-folder"`. The desktop shortcut uses the normal local data folder and starts with no sample games.

## Future additions

Streak displays, notifications, leaderboard uploads, and syncing are not implemented. History is recorded at game and optional subtask level, with stable IDs and a versioned JSON schema, so these features have room to grow. Editing historical completions is unavailable.
